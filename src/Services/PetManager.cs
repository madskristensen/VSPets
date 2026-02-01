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

        private PetHostCanvas _hostCanvas;
        private DispatcherTimer _updateTimer;
        private bool _isInitialized;
        private bool _isDisposed;

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
        /// Maximum number of pets allowed.
        /// </summary>
        public int MaxPets { get; set; } = 5;

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

            if (PetCount >= MaxPets)
            {
                return null;
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

            await new InvalidOperationException($"VSPets: Removed {pet.Name}").LogAsync();

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

            new InvalidOperationException($"VSPets: Notified {pets.Count} pets of build {(success ? "success" : "failure")}").Log();
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
                    pet.Update(deltaTime, canvasWidth);

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

            new InvalidOperationException($"VSPets: {pet1.Name} met {pet2.Name}!").Log();
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
