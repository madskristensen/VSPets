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
        private readonly List<IPet> _petsUpdateBuffer = [];
        private readonly Dictionary<Guid, PetControl> _petControls = [];
        private readonly Dictionary<Guid, PetRenderState> _petRenderStates = [];
        private readonly ReaderWriterLockSlim _petLock = new();
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

        private enum UpdateQualityLevel
        {
            Normal,
            Reduced,
            Minimal
        }

        private struct PetRenderState
        {
            public double X;
            public double Y;
            public PetDirection Direction;
        }

        private static readonly TimeSpan _normalTickInterval = TimeSpan.FromMilliseconds(33);
        private static readonly TimeSpan _reducedTickInterval = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan _minimalTickInterval = TimeSpan.FromMilliseconds(66);
        private const double _highStressThresholdSeconds = 0.055;
        private const double _criticalStressThresholdSeconds = 0.090;
        private const double _recoverToNormalThresholdSeconds = 0.040;
        private const double _recoverToReducedThresholdSeconds = 0.065;
        private const double _stressSmoothingFactor = 0.12;
        private const double _qualityChangeCooldownSeconds = 2.0;

        private UpdateQualityLevel _updateQualityLevel = UpdateQualityLevel.Normal;
        private double _smoothedFrameDeltaSeconds = _normalTickInterval.TotalSeconds;
        private DateTime _nextQualityChangeAllowedAt = DateTime.MinValue;
        private double _randomThrowCheckAccumulator;
        private double _interactionCheckAccumulator;
        private double _breathingUpdateAccumulator;

        private PetHostCanvas _hostCanvas;
        private DispatcherTimer _updateTimer;
        private DateTime _lastUpdateTime;
        private const double _maxDeltaTime = 0.1; // Cap at 100ms to prevent teleporting during CPU spikes
        private const double _renderPositionEpsilon = 0.01;
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

        public bool IsReducedPerformanceMode => _updateQualityLevel != UpdateQualityLevel.Normal;

        public bool IsMinimalPerformanceMode => _updateQualityLevel == UpdateQualityLevel.Minimal;

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
                Interval = _normalTickInterval // ~30 FPS
            };
            _updateTimer.Tick += OnUpdateTick;
            ResetAdaptivePerformanceState();
            _lastUpdateTime = DateTime.Now;
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
                _petRenderStates[pet.Id] = new PetRenderState
                {
                    X = double.NaN,
                    Y = double.NaN,
                    Direction = pet.Direction
                };
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
                _petRenderStates.Remove(petId);
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

            // Calculate actual elapsed time and clamp to prevent teleporting during CPU spikes
            var now = DateTime.Now;
            var actualDelta = (now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;

            UpdateAdaptivePerformanceMode(actualDelta, now);

            // Clamp deltaTime: use actual time but cap it to prevent large jumps
            // When CPU is busy, frames get delayed - without clamping, pets would "teleport"
            var deltaTime = Math.Min(actualDelta, _maxDeltaTime);

            var canvasWidth = _hostCanvas?.ActualWidth ?? VSPets.StatusBarInjector.StatusBarWidth;

            // Update ball physics
            UpdateBall(deltaTime, canvasWidth);

            IReadOnlyList<IPet> petsToUpdate;
            _petLock.EnterReadLock();
            try
            {
                _petsUpdateBuffer.Clear();
                _petsUpdateBuffer.AddRange(_pets);
                petsToUpdate = _petsUpdateBuffer;
            }
            finally
            {
                _petLock.ExitReadLock();
            }

            var shouldUpdateBreathing = ShouldRunBreathingUpdate(deltaTime);

            foreach (IPet pet in petsToUpdate)
            {
                try
                {
                    // Handle chasing behavior - only the assigned chaser should chase the ball
                    if (pet is BasePet basePet && basePet.State == PetState.Chasing && _activeBall != null && _activeBall.ChasingPetId == basePet.Id)
                    {
                        UpdateChasingPet(basePet, deltaTime);
                    }
                    else if (pet is BasePet bp && bp.State == PetState.Chasing && (_activeBall == null || _activeBall.ChasingPetId != bp.Id))
                    {
                        // Pet was chasing but is no longer the assigned chaser (new ball thrown or ball gone)
                        bp.ForceState(PetState.Idle);
                    }
                    else
                    {
                        pet.Update(deltaTime, canvasWidth);
                    }

                    // Update control position and breathing animation
                    PetControl control;
                    PetRenderState renderState;
                    bool hasControl;

                    _petLock.EnterReadLock();
                    try
                    {
                        hasControl = _petControls.TryGetValue(pet.Id, out control);
                        if (hasControl)
                        {
                            _petRenderStates.TryGetValue(pet.Id, out renderState);
                        }
                        else
                        {
                            renderState = default;
                        }
                    }
                    finally
                    {
                        _petLock.ExitReadLock();
                    }

                    if (hasControl)
                    {
                        if (double.IsNaN(renderState.X) || Math.Abs(renderState.X - pet.X) > _renderPositionEpsilon)
                        {
                            Canvas.SetLeft(control, pet.X);
                            renderState.X = pet.X;
                        }

                        if (double.IsNaN(renderState.Y) || Math.Abs(renderState.Y - pet.Y) > _renderPositionEpsilon)
                        {
                            Canvas.SetBottom(control, pet.Y);
                            renderState.Y = pet.Y;
                        }

                        if (renderState.Direction != pet.Direction)
                        {
                            control.SetDirection(pet.Direction);
                            renderState.Direction = pet.Direction;
                        }

                        _petLock.EnterWriteLock();
                        try
                        {
                            _petRenderStates[pet.Id] = renderState;
                        }
                        finally
                        {
                            _petLock.ExitWriteLock();
                        }

                        if (shouldUpdateBreathing)
                        {
                            control.UpdateBreathing(); // Drive breathing animation from central timer
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }

            if (ShouldRunInteractionCheck(deltaTime, out var interactionElapsed))
            {
                // Check for pet interactions (when pets are close to each other)
                CheckPetInteractions(petsToUpdate, interactionElapsed);
            }

            if (ShouldRunRandomThrowCheck(deltaTime, out var randomThrowElapsed))
            {
                // Random ball throw (rare)
                CheckRandomBallThrow(petsToUpdate, randomThrowElapsed);
            }
        }

        // Random ball throw timing
        private double _timeSinceLastBallThrow;
        private const double _minTimeBetweenRandomThrows = 30.0;  // At least 30 seconds between random throws
        private const double _randomThrowChancePerSecond = 0.005; // 0.5% chance per second (very rare)

        private void CheckRandomBallThrow(IReadOnlyList<IPet> pets, double deltaTime)
        {
            // Don't throw if ball is already active
            if (_activeBall != null && _activeBall.IsActive)
            {
                return;
            }

            // Don't throw if no pets
            if (pets.Count == 0)
            {
                return;
            }

            // Don't throw if any pet is already chasing
            for (var i = 0; i < pets.Count; i++)
            {
                if (pets[i] is BasePet chasingPet && chasingPet.State == PetState.Chasing)
                {
                    return;
                }
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
                // Pick a random eligible pet without allocating intermediate lists
                BasePet thrower = null;
                var eligibleCount = 0;

                for (var i = 0; i < pets.Count; i++)
                {
                    if (pets[i] is not BasePet candidate ||
                        candidate.State == PetState.Sleeping ||
                        candidate.State == PetState.Dragging)
                    {
                        continue;
                    }

                    eligibleCount++;
                    if (_random.Next(eligibleCount) == 0)
                    {
                        thrower = candidate;
                    }
                }

                if (thrower != null)
                {
                    var throwX = thrower.X + (int)thrower.Size / 2;

                    // Fire and forget the ball throw (with error logging)
                    _ = ThrowBallAsync(throwX, thrower.Id).ContinueWith(
                        t => t.Exception.Log(),
                        TaskContinuationOptions.OnlyOnFaulted);
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
                BasePet chasingPet = null;
                _petLock.EnterReadLock();
                try
                {
                    for (var i = 0; i < _pets.Count; i++)
                    {
                        if (_pets[i].Id == _activeBall.ChasingPetId.Value)
                        {
                            chasingPet = _pets[i] as BasePet;
                            break;
                        }
                    }
                }
                finally
                {
                    _petLock.ExitReadLock();
                }

                if (chasingPet != null)
                {
                    var distance = Math.Abs(chasingPet.X + (int)chasingPet.Size / 2 - _activeBall.X);
                    if (distance < _catchDistance)
                    {
                        OnBallCaught(chasingPet);
                    }
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

            var newX = pet.X + direction * _chaseSpeed * deltaTime;
            pet.SetPosition(newX, pet.Y);
            pet.SetDirection(direction > 0 ? PetDirection.Right : PetDirection.Left);

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
                BasePet nearestEligiblePet = null;
                BasePet nearestNonThrowerPet = null;
                var nearestEligibleDistance = double.MaxValue;
                var nearestNonThrowerDistance = double.MaxValue;

                for (var i = 0; i < _pets.Count; i++)
                {
                    if (_pets[i] is not BasePet basePet)
                    {
                        continue;
                    }

                    if (basePet.State == PetState.Sleeping || basePet.State == PetState.Dragging)
                    {
                        continue;
                    }

                    var distance = Math.Abs(basePet.X - _activeBall.X);

                    if (distance < nearestEligibleDistance)
                    {
                        nearestEligibleDistance = distance;
                        nearestEligiblePet = basePet;
                    }

                    if (basePet.Id != throwerId && distance < nearestNonThrowerDistance)
                    {
                        nearestNonThrowerDistance = distance;
                        nearestNonThrowerPet = basePet;
                    }
                }

                BasePet chasingPet = nearestNonThrowerPet ?? nearestEligiblePet;
                if (chasingPet == null)
                {
                    return;
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

        private void CheckPetInteractions(IReadOnlyList<IPet> pets, double elapsedTime)
        {
            _lastInteractionTime += elapsedTime;

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

            ResetAdaptivePerformanceState();

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
            ResetAdaptivePerformanceState();

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
                for (var i = 0; i < _hiddenPetsData.Count; i++)
                {
                    PetData petData = _hiddenPetsData[i];
                    await AddPetAsync(petData.PetType, petData.Color, petData.Name);

                    // Small delay between spawns to avoid overlap
                    if (i < _hiddenPetsData.Count - 1)
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
            ResetAdaptivePerformanceState();

            _petLock.EnterWriteLock();
            try
            {
                // Dispose all controls first
                foreach (PetControl control in _petControls.Values)
                {
                    control.Dispose();
                }
                _petControls.Clear();
                _petRenderStates.Clear();

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

        private void UpdateAdaptivePerformanceMode(double actualDelta, DateTime now)
        {
            if (actualDelta <= 0)
            {
                return;
            }

            _smoothedFrameDeltaSeconds = (_smoothedFrameDeltaSeconds * (1.0 - _stressSmoothingFactor)) +
                                         (actualDelta * _stressSmoothingFactor);

            if (now < _nextQualityChangeAllowedAt)
            {
                return;
            }

            var previousLevel = _updateQualityLevel;

            switch (_updateQualityLevel)
            {
                case UpdateQualityLevel.Normal:
                    if (_smoothedFrameDeltaSeconds >= _criticalStressThresholdSeconds)
                    {
                        _updateQualityLevel = UpdateQualityLevel.Minimal;
                    }
                    else if (_smoothedFrameDeltaSeconds >= _highStressThresholdSeconds)
                    {
                        _updateQualityLevel = UpdateQualityLevel.Reduced;
                    }
                    break;

                case UpdateQualityLevel.Reduced:
                    if (_smoothedFrameDeltaSeconds >= _criticalStressThresholdSeconds)
                    {
                        _updateQualityLevel = UpdateQualityLevel.Minimal;
                    }
                    else if (_smoothedFrameDeltaSeconds <= _recoverToNormalThresholdSeconds)
                    {
                        _updateQualityLevel = UpdateQualityLevel.Normal;
                    }
                    break;

                case UpdateQualityLevel.Minimal:
                    if (_smoothedFrameDeltaSeconds <= _recoverToReducedThresholdSeconds)
                    {
                        _updateQualityLevel = UpdateQualityLevel.Reduced;
                    }
                    break;
            }

            if (previousLevel != _updateQualityLevel)
            {
                ApplyUpdateQualityInterval();
                _nextQualityChangeAllowedAt = now.AddSeconds(_qualityChangeCooldownSeconds);
            }
        }

        private void ApplyUpdateQualityInterval()
        {
            if (_updateTimer == null)
            {
                return;
            }

            _updateTimer.Interval = _updateQualityLevel switch
            {
                UpdateQualityLevel.Normal => _normalTickInterval,
                UpdateQualityLevel.Reduced => _reducedTickInterval,
                UpdateQualityLevel.Minimal => _minimalTickInterval,
                _ => _normalTickInterval
            };
        }

        private bool ShouldRunRandomThrowCheck(double deltaTime, out double elapsedTime)
        {
            var intervalSeconds = _updateQualityLevel switch
            {
                UpdateQualityLevel.Normal => 0,
                UpdateQualityLevel.Reduced => 0.10,
                UpdateQualityLevel.Minimal => 0.20,
                _ => 0
            };

            return ShouldRunThrottledWork(ref _randomThrowCheckAccumulator, deltaTime, intervalSeconds, out elapsedTime);
        }

        private bool ShouldRunInteractionCheck(double deltaTime, out double elapsedTime)
        {
            var intervalSeconds = _updateQualityLevel switch
            {
                UpdateQualityLevel.Normal => 0,
                UpdateQualityLevel.Reduced => 0.10,
                UpdateQualityLevel.Minimal => 0.20,
                _ => 0
            };

            return ShouldRunThrottledWork(ref _interactionCheckAccumulator, deltaTime, intervalSeconds, out elapsedTime);
        }

        private bool ShouldRunBreathingUpdate(double deltaTime)
        {
            var intervalSeconds = _updateQualityLevel switch
            {
                UpdateQualityLevel.Normal => 0,
                UpdateQualityLevel.Reduced => 0.10,
                UpdateQualityLevel.Minimal => 0.20,
                _ => 0
            };

            return ShouldRunThrottledWork(ref _breathingUpdateAccumulator, deltaTime, intervalSeconds, out _);
        }

        private static bool ShouldRunThrottledWork(ref double accumulator, double deltaTime, double intervalSeconds, out double elapsedTime)
        {
            if (intervalSeconds <= 0)
            {
                elapsedTime = deltaTime;
                return true;
            }

            accumulator += deltaTime;
            if (accumulator < intervalSeconds)
            {
                elapsedTime = 0;
                return false;
            }

            elapsedTime = accumulator;
            accumulator = 0;
            return true;
        }

        private void ResetAdaptivePerformanceState()
        {
            _updateQualityLevel = UpdateQualityLevel.Normal;
            _smoothedFrameDeltaSeconds = _normalTickInterval.TotalSeconds;
            _nextQualityChangeAllowedAt = DateTime.MinValue;
            _randomThrowCheckAccumulator = 0;
            _interactionCheckAccumulator = 0;
            _breathingUpdateAccumulator = 0;

            ApplyUpdateQualityInterval();
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
