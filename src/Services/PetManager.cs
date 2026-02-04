using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using VSPets.Animation;
using VSPets.Controls;
using VSPets.Models;
using VSPets.Pets;
using Image = System.Windows.Controls.Image;

namespace VSPets.Services
{
    /// <summary>
    /// Central service for managing all pets in Visual Studio.
    /// </summary>
    public class PetManager : IDisposable
    {
        private static PetManager _instance;
        private static readonly object _instanceLock = new();

        private readonly List<IPet> _pets = [];
        private readonly Dictionary<Guid, PetControl> _petControls = [];
        private readonly ReaderWriterLockSlim _petLock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly Random _random = new(); // Shared random for better variance

        // Track last spawn side to avoid spawning multiple pets from same side
        private bool _lastSpawnedFromLeft;
        private bool _isFirstSpawn = true; // Track if this is the first spawn
        private DateTime _lastSpawnTime = DateTime.MinValue;
        private const double _minSpawnDelaySeconds = 2.0; // Minimum time between spawns from same side

        // Ball management
        private Ball _activeBall;
        private Image _ballImage;
        private int _ballFrame;
        private double _ballFrameTime;
        private const double _ballFrameDuration = 0.1; // 10 FPS for ball animation
        private const double _chaseSpeed = 180; // Pixels per second when chasing
        private const double _catchDistance = 20; // Distance at which pet catches ball

        private PetHostCanvas _hostCanvas;
        private DispatcherTimer _updateTimer;
        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isHidden;
        private List<PetData> _hiddenPetsData;

        /// <summary>
        /// Gets the singleton instance of the pet manager.
        /// </summary>
        public static PetManager Instance
        {
            get
            {
                lock (_instanceLock)
                {
                    _instance ??= new PetManager();
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Event fired when a pet is added.
        /// </summary>
        public event EventHandler<PetEventArgs> PetAdded;

        /// <summary>
        /// Event fired when a pet is removed.
        /// </summary>
        public event EventHandler<PetEventArgs> PetRemoved;

        /// <summary>
        /// Gets the current pet count.
        /// </summary>
        public int PetCount
        {
            get
            {
                _petLock.EnterReadLock();
                try
                {
                    return _pets.Count;
                }
                finally
                {
                    _petLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets whether the manager is initialized and running.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets whether the pets are currently hidden.
        /// </summary>
        public bool IsHidden => _isHidden;

        private PetManager()
        {
        }

        /// <summary>
        /// Initializes the pet manager and injects into Visual Studio.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Initialize the status bar injection
            var injected = await VSPets.StatusBarInjector.InjectControlAsync(CreateHostCanvas());

            if (!injected)
            {
                return;
            }

            // Set up the update timer for pet animations
            _updateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _updateTimer.Tick += OnUpdateTick;
            _updateTimer.Start();

            _isInitialized = true;
        }

        /// <summary>
        /// Creates the host canvas for pets.
        /// </summary>
        private Canvas CreateHostCanvas()
        {
            _hostCanvas = new PetHostCanvas
            {
                Height = 40, // Slightly taller than status bar for jumping pets
                MinWidth = 200,
                ClipToBounds = false
            };

            return _hostCanvas;
        }

        /// <summary>
        /// Adds a new pet to the status bar.
        /// </summary>
        public async Task<IPet> AddPetAsync(PetType petType, PetColor? color = null, string name = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            // Create the pet
            IPet pet = CreatePet(petType, color);
            if (pet == null)
            {
                return null;
            }

            // Pre-warm sprite cache in background to avoid rendering stutters
            // This renders common animation frames off the UI thread
            ProceduralSpriteRenderer.Instance.PreWarmCacheAsync(pet.PetType, pet.Color, (int)pet.Size);

            // Set custom name if provided
            if (!string.IsNullOrWhiteSpace(name))
            {
                pet.Name = name;
            }

            // Create the control
            var control = PetControl.Create(pet);
            control.SetSize(pet.Size);

            // Determine spawn side - alternate sides and respect minimum delay
            var canvasWidth = StatusBarInjector.StatusBarWidth;
            var petSize = (int)pet.Size;
            var timeSinceLastSpawn = (DateTime.Now - _lastSpawnTime).TotalSeconds;

            bool enterFromLeft;
            if (_isFirstSpawn)
            {
                // First spawn ever - truly random 50/50
                enterFromLeft = _random.NextDouble() < 0.5;
                _isFirstSpawn = false;
            }
            else if (timeSinceLastSpawn < _minSpawnDelaySeconds)
            {
                // Recent spawn - force opposite side to avoid overlap
                enterFromLeft = !_lastSpawnedFromLeft;
            }
            else
            {
                // Enough time passed - randomly choose but slightly favor opposite of last
                enterFromLeft = _random.NextDouble() < (_lastSpawnedFromLeft ? 0.7 : 0.3);
            }

            _lastSpawnedFromLeft = enterFromLeft;
            _lastSpawnTime = DateTime.Now;

            double initialX;
            PetDirection initialDirection;

            if (enterFromLeft)
            {
                // Start off-screen to the left, walk right
                initialX = -petSize;
                initialDirection = PetDirection.Right;
            }
            else
            {
                // Start off-screen to the right, walk left
                initialX = canvasWidth + petSize;
                initialDirection = PetDirection.Left;
            }

            // Set position and direction on pet model FIRST
            pet.SetPosition(initialX, 0);
            pet.SetDirection(initialDirection);
            pet.SetState(PetState.Entering); // Use Entering state for walking onto screen

            // Update the control to reflect the correct direction BEFORE adding to canvas
            control.SetDirection(initialDirection);

            // Add to tracking
            _petLock.EnterWriteLock();
            try
            {
                _pets.Add(pet);
                _petControls[pet.Id] = control;
            }
            finally
            {
                _petLock.ExitWriteLock();
            }

            // Add to canvas
            _hostCanvas.AddPet(control, initialX);

            PetAdded?.Invoke(this, new PetEventArgs { Pet = pet });

            return pet;
        }

        /// <summary>
        /// Adds a random pet.
        /// </summary>
        public async Task<IPet> AddRandomPetAsync()
        {
            PetType[] petTypes = [PetType.Cat, PetType.Dog, PetType.Fox, PetType.Bear, PetType.Axolotl];
            PetType petType = petTypes[_random.Next(petTypes.Length)];

            return await AddPetAsync(petType);
        }

        /// <summary>
        /// Removes a pet by ID.
        /// </summary>
        public async Task<bool> RemovePetAsync(Guid petId)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IPet pet;
            PetControl control;

            _petLock.EnterWriteLock();
            try
            {
                pet = _pets.FirstOrDefault(p => p.Id == petId);
                if (pet == null)
                {
                    return false;
                }

                _pets.Remove(pet);
                _petControls.TryGetValue(petId, out control);
                _petControls.Remove(petId);
            }
            finally
            {
                _petLock.ExitWriteLock();
            }

            if (control != null)
            {
                _hostCanvas.RemovePet(control);
                control.Dispose(); // Clean up event subscriptions and timers
            }

            pet.Dispose();

            PetRemoved?.Invoke(this, new PetEventArgs { Pet = pet });

            return true;
        }

        /// <summary>
        /// Removes all pets.
        /// </summary>
        public async Task RemoveAllPetsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            List<Guid> petIds;
            _petLock.EnterReadLock();
            try
            {
                petIds = [.. _pets.Select(p => p.Id)];
            }
            finally
            {
                _petLock.ExitReadLock();
            }

            foreach (Guid id in petIds)
            {
                await RemovePetAsync(id);
            }
        }

        /// <summary>
        /// Gets all current pets.
        /// </summary>
        public IReadOnlyList<IPet> GetPets()
        {
            _petLock.EnterReadLock();
            try
            {
                return [.. _pets];
            }
            finally
            {
                _petLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets a pet by ID.
        /// </summary>
        public IPet GetPet(Guid petId)
        {
            _petLock.EnterReadLock();
            try
            {
                return _pets.FirstOrDefault(p => p.Id == petId);
            }
            finally
            {
                _petLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Notifies all pets about a build completion.
        /// </summary>
        /// <param name="success">True if build succeeded, false if failed.</param>
        public void NotifyBuildComplete(bool success)
        {
            List<IPet> pets;
            _petLock.EnterReadLock();
            try
            {
                pets = [.. _pets];
            }
            finally
            {
                _petLock.ExitReadLock();
            }

            foreach (IPet pet in pets)
            {
                if (pet is BasePet basePet)
                {
                    basePet.ReactToBuild(success);
                }
            }
        }

        public IPet CreatePet(PetType petType, PetColor? color)
        {
            return petType switch
            {
                PetType.Cat => color.HasValue ? new Cat(color.Value) : Cat.CreateRandom(),
                PetType.Dog => color.HasValue ? new Dog(color.Value) : Dog.CreateRandom(),
                PetType.Fox => color.HasValue ? new Fox(color.Value) : Fox.CreateRandom(),
                PetType.Bear => color.HasValue ? new Bear(color.Value) : Bear.CreateRandom(),
                PetType.Axolotl => color.HasValue ? new Axolotl(color.Value) : Axolotl.CreateRandom(),
                PetType.Clippy => new Clippy(),
                PetType.RubberDuck => color.HasValue ? new RubberDuck(color.Value) : RubberDuck.CreateRandom(),
                PetType.Turtle => color.HasValue ? new Turtle(color.Value) : Turtle.CreateRandom(),
                PetType.Bunny => color.HasValue ? new Bunny(color.Value) : Bunny.CreateRandom(),
                PetType.Raccoon => color.HasValue ? new Raccoon(color.Value) : Raccoon.CreateRandom(),
                PetType.TRex => color.HasValue ? new TRex(color.Value) : TRex.CreateRandom(),
                PetType.Wolf => color.HasValue ? new Wolf(color.Value) : Wolf.CreateRandom(),
                _ => Cat.CreateRandom()
            };
        }

        private void OnUpdateTick(object sender, EventArgs e)
        {
            if (!_isInitialized || _isDisposed)
            {
                return;
            }

            var deltaTime = _updateTimer.Interval.TotalSeconds;
            var canvasWidth = _hostCanvas?.ActualWidth ?? VSPets.StatusBarInjector.StatusBarWidth;

            // Update ball physics
            UpdateBall(deltaTime, canvasWidth);

            List<IPet> petsToUpdate;
            _petLock.EnterReadLock();
            try
            {
                petsToUpdate = [.. _pets];
            }
            finally
            {
                _petLock.ExitReadLock();
            }

            foreach (IPet pet in petsToUpdate)
            {
                try
                {
                    // Handle chasing behavior
                    if (pet is BasePet basePet && basePet.State == PetState.Chasing && _activeBall != null)
                    {
                        UpdateChasingPet(basePet, deltaTime);
                    }
                    else
                    {
                        pet.Update(deltaTime, canvasWidth);
                    }

                    // Update control position and breathing animation
                    if (_petControls.TryGetValue(pet.Id, out PetControl control))
                    {
                        Canvas.SetLeft(control, pet.X);
                        Canvas.SetBottom(control, pet.Y);
                        control.SetDirection(pet.Direction);
                        control.UpdateBreathing(); // Drive breathing animation from central timer
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }

            // Check for pet interactions (when pets are close to each other)
            CheckPetInteractions(petsToUpdate);

            // Random ball throw (rare)
            CheckRandomBallThrow(petsToUpdate, deltaTime);
        }

        // Random ball throw timing
        private double _timeSinceLastBallThrow;
        private const double _minTimeBetweenRandomThrows = 30.0;  // At least 30 seconds between random throws
        private const double _randomThrowChancePerSecond = 0.005; // 0.5% chance per second (very rare)

        private void CheckRandomBallThrow(List<IPet> pets, double deltaTime)
        {
            // Don't throw if ball is already active
            if (_activeBall != null && _activeBall.IsActive)
            {
                return;
            }

            // Don't throw if no pets or any pet is already chasing
            if (pets.Count == 0 || pets.Any(p => p is BasePet bp && bp.State == PetState.Chasing))
            {
                return;
            }

            _timeSinceLastBallThrow += deltaTime;

            // Enforce minimum time between throws
            if (_timeSinceLastBallThrow < _minTimeBetweenRandomThrows)
            {
                return;
            }

            // Random chance to throw
            if (_random.NextDouble() < _randomThrowChancePerSecond * deltaTime)
            {
                // Pick a random pet to "throw" the ball
                var eligiblePets = pets
                    .OfType<BasePet>()
                    .Where(p => p.State != PetState.Sleeping && p.State != PetState.Dragging)
                    .ToList();

                if (eligiblePets.Count > 0)
                {
                    BasePet thrower = eligiblePets[_random.Next(eligiblePets.Count)];
                    var throwX = thrower.X + (int)thrower.Size / 2;

                    // Fire and forget the ball throw
                    _ = ThrowBallAsync(throwX, thrower.Id);
                    _timeSinceLastBallThrow = 0;

                    // Show the thrower being playful
                    thrower.ShowSpeech("⚾", 1000);
                }
            }
        }

        private void UpdateBall(double deltaTime, double canvasWidth)
        {
            if (_activeBall == null || !_activeBall.IsActive)
            {
                return;
            }

            // Update ball physics
            _activeBall.Update(deltaTime, canvasWidth);

            // Update ball animation frame
            _ballFrameTime += deltaTime;
            if (_ballFrameTime >= _ballFrameDuration)
            {
                _ballFrameTime = 0;
                _ballFrame = (_ballFrame + 1) % 4;
            }

            // Update ball image
            if (_ballImage != null)
            {
                Canvas.SetLeft(_ballImage, _activeBall.X);
                _ballImage.Source = ProceduralSpriteRenderer.Instance.RenderBall(_activeBall.Size, _ballFrame);
            }

            // Check if chasing pet caught the ball
            if (_activeBall.ChasingPetId.HasValue)
            {
                _petLock.EnterReadLock();
                try
                {
                    if (_pets.FirstOrDefault(p => p.Id == _activeBall.ChasingPetId.Value) is BasePet chasingPet)
                    {
                        var distance = Math.Abs(chasingPet.X + (int)chasingPet.Size / 2 - _activeBall.X);
                        if (distance < _catchDistance)
                        {
                            OnBallCaught(chasingPet);
                        }
                    }
                }
                finally
                {
                    _petLock.ExitReadLock();
                }
            }
        }

        private void UpdateChasingPet(BasePet pet, double deltaTime)
        {
            if (_activeBall == null)
            {
                // Ball gone, return to normal behavior
                pet.ForceState(PetState.Idle);
                return;
            }

            // Move toward ball
            var petCenterX = pet.X + (int)pet.Size / 2;
            var ballCenterX = _activeBall.X + _activeBall.Size / 2;
            var direction = ballCenterX > petCenterX ? 1 : -1;

            pet.X += direction * _chaseSpeed * deltaTime;
            pet.Direction = direction > 0 ? PetDirection.Right : PetDirection.Left;

            // Update animation
            pet.UpdateAnimation(deltaTime);
        }

        private void OnBallCaught(BasePet catcher)
        {
            // Pet caught the ball!
            _activeBall.Catch();
            catcher.ForceState(PetState.Happy);
            catcher.ShowSpeech("🎉", 2000);

            // Remove ball image
            if (_ballImage != null && _hostCanvas != null)
            {
                _hostCanvas.Children.Remove(_ballImage);
                _ballImage = null;
            }

            _activeBall = null;
        }

        /// <summary>
        /// Throws a ball from the specified position.
        /// </summary>
        /// <param name="fromX">X position to throw from.</param>
        /// <param name="throwerId">ID of the pet that threw the ball (for exclusion delay).</param>
        public async Task ThrowBallAsync(double fromX, Guid throwerId)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Remove existing ball if any
            if (_ballImage != null && _hostCanvas != null)
            {
                _hostCanvas.Children.Remove(_ballImage);
                _ballImage = null;
            }

            // Create new ball
            _activeBall = new Ball(fromX);
            _ballFrame = 0;
            _ballFrameTime = 0;

            // Create ball image
            _ballImage = new Image
            {
                Width = _activeBall.Size,
                Height = _activeBall.Size,
                Source = ProceduralSpriteRenderer.Instance.RenderBall(_activeBall.Size, 0)
            };

            // Position ball (at bottom of status bar)
            Canvas.SetLeft(_ballImage, _activeBall.X);
            Canvas.SetBottom(_ballImage, 4); // Slightly above the very bottom
            Panel.SetZIndex(_ballImage, 50); // Below pets but visible

            // Add to canvas
            _hostCanvas?.Children.Add(_ballImage);

            // Small delay before pet starts chasing (looks like a "throw")
            await Task.Delay(250);

            // Find nearest pet to chase (can be the thrower if it's the only pet)
            AssignChasingPet(throwerId);
        }

        private void AssignChasingPet(Guid throwerId)
        {
            if (_activeBall == null)
            {
                return;
            }

            _petLock.EnterReadLock();
            try
            {
                // Collect eligible pets (not sleeping, not dragging)
                var eligiblePets = new List<(BasePet pet, double distance)>();

                foreach (IPet pet in _pets)
                {
                    if (pet is not BasePet basePet)
                    {
                        continue;
                    }

                    // Skip pets that are busy (sleeping, dragging, etc.)
                    if (basePet.State == PetState.Sleeping || basePet.State == PetState.Dragging)
                    {
                        continue;
                    }

                    var distance = Math.Abs(basePet.X - _activeBall.X);
                    eligiblePets.Add((basePet, distance));
                }

                if (eligiblePets.Count == 0)
                {
                    return;
                }

                BasePet chasingPet;

                if (eligiblePets.Count == 1)
                {
                    // Only one pet - it chases (solo fetch)
                    chasingPet = eligiblePets[0].pet;
                }
                else
                {
                    // Multiple pets - prefer a pet OTHER than the thrower
                    // Find nearest non-thrower
                    var nonThrowers = eligiblePets
                        .Where(p => p.pet.Id != throwerId)
                        .OrderBy(p => p.distance)
                        .ToList();

                    if (nonThrowers.Count > 0)
                    {
                        // Another pet chases
                        chasingPet = nonThrowers[0].pet;
                    }
                    else
                    {
                        // All eligible pets are the thrower (shouldn't happen, but fallback)
                        chasingPet = eligiblePets.OrderBy(p => p.distance).First().pet;
                    }
                }

                _activeBall.ChasingPetId = chasingPet.Id;
                chasingPet.ForceState(PetState.Chasing);
            }
            finally
            {
                _petLock.ExitReadLock();
            }
        }

        private double _lastInteractionTime;
        private const double _interactionCooldown = 8.0; // Seconds between interactions

        private void CheckPetInteractions(List<IPet> pets)
        {
            _lastInteractionTime += _updateTimer.Interval.TotalSeconds;

            // Don't check too frequently
            if (_lastInteractionTime < _interactionCooldown || pets.Count < 2)
            {
                return;
            }

            // Check distance between each pair of pets
            for (var i = 0; i < pets.Count; i++)
            {
                for (var j = i + 1; j < pets.Count; j++)
                {
                    IPet pet1 = pets[i];
                    IPet pet2 = pets[j];

                    var distance = Math.Abs(pet1.X - pet2.X);
                    var interactionDistance = (int)pet1.Size + (int)pet2.Size;

                    if (distance < interactionDistance)
                    {
                        // Pets are meeting!
                        TriggerPetMeeting(pet1, pet2);
                        _lastInteractionTime = 0;
                        return; // Only one interaction per check
                    }
                }
            }
        }

        private void TriggerPetMeeting(IPet pet1, IPet pet2)
        {
            string[] greetings = ["👋", "❤️", "🤝", "✨", "💕"];
            var greeting = greetings[_random.Next(greetings.Length)];

            // Make both pets happy and show a greeting
            if (pet1 is BasePet basePet1)
            {
                basePet1.TriggerHappy();
                basePet1.ShowSpeech(greeting, 2000);
            }
            if (pet2 is BasePet basePet2)
            {
                basePet2.TriggerHappy();
                basePet2.ShowSpeech(greeting, 2000);
            }
        }

        /// <summary>
        /// Shuts down the pet manager.
        /// </summary>
        public async Task ShutdownAsync()
        {
            if (!_isInitialized)
            {
                return;
            }

            _updateTimer?.Stop();

            await RemoveAllPetsAsync();

            if (_hostCanvas != null)
            {
                await VSPets.StatusBarInjector.RemoveControlAsync(_hostCanvas);
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Hides all pets by removing them from WPF completely.
        /// Pet data is stored so they can be restored when shown again.
        /// </summary>
        public async Task HidePetsAsync()
        {
            if (_isHidden || !_isInitialized)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Store current pet data for restoration
            _petLock.EnterReadLock();
            try
            {
                _hiddenPetsData = _pets.Select(p => new PetData
                {
                    PetType = p.PetType,
                    Color = p.Color,
                    Name = p.Name
                }).ToList();
            }
            finally
            {
                _petLock.ExitReadLock();
            }

            // Stop the update timer
            _updateTimer?.Stop();

            // Remove all pets
            List<Guid> petIds;
            _petLock.EnterReadLock();
            try
            {
                petIds = [.. _pets.Select(p => p.Id)];
            }
            finally
            {
                _petLock.ExitReadLock();
            }

            foreach (Guid id in petIds)
            {
                await RemovePetAsync(id);
            }

            // Remove the canvas from WPF
            if (_hostCanvas != null)
            {
                await VSPets.StatusBarInjector.RemoveControlAsync(_hostCanvas);
                _hostCanvas = null;
            }

            _isHidden = true;
            _isInitialized = false;
        }

        /// <summary>
        /// Shows pets by re-injecting the canvas and restoring previously hidden pets.
        /// </summary>
        public async Task ShowPetsAsync()
        {
            if (!_isHidden)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Re-initialize (creates new canvas and injects it)
            await InitializeAsync();

            // Restore saved pets
            if (_hiddenPetsData != null && _hiddenPetsData.Any())
            {
                foreach (PetData petData in _hiddenPetsData)
                {
                    await AddPetAsync(petData.PetType, petData.Color, petData.Name);

                    // Small delay between spawns to avoid overlap
                    if (petData != _hiddenPetsData.Last())
                    {
                        await Task.Delay(_random.Next(2000, 5000));
                    }
                }

                _hiddenPetsData = null;
            }

            _isHidden = false;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _updateTimer?.Stop();

            _petLock.EnterWriteLock();
            try
            {
                // Dispose all controls first
                foreach (PetControl control in _petControls.Values)
                {
                    control.Dispose();
                }
                _petControls.Clear();

                // Then dispose all pets
                foreach (IPet pet in _pets)
                {
                    pet.Dispose();
                }
                _pets.Clear();
            }
            finally
            {
                _petLock.ExitWriteLock();
            }

            _petLock.Dispose();
            _hostCanvas = null;
        }
    }

    /// <summary>
    /// Event arguments for pet events.
    /// </summary>
    public class PetEventArgs : EventArgs
    {
        public IPet Pet { get; set; }
    }
}
