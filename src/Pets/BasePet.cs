using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Base implementation for all pet types.
    /// Contains common behavior and state machine logic.
    /// </summary>
    public abstract class BasePet : IPet
    {
        private readonly Random _random = new Random();
        private PetState _currentState;
        private PetDirection _direction;
        private double _x;
        private double _y;
        private double _stateTimer;
        private double _stateDuration;
        private PetState? _returnToState;
        private Image _spriteImage;
        private Border _container;

        // Idle animation state
        private double _idleAnimationTimer;
        private double _breathingPhase;
        private double _nextBehaviorTime;
        private bool _isDoingBehavior;
        private string _currentBehavior;

        // Frame animation state
        private int _currentFrame;
        private double _frameTimer;
        private int _frameCount;
        private double _frameDuration;

        // Edge exit/re-entry state
        private bool _hasExitedScreen;
        private double _reentryDelay;
        private bool _reenterFromOpposite;

        // Per-pet randomization factors (set once at creation for uniqueness)
        private readonly double _speedVariation;      // 0.8 to 1.2 multiplier
        private readonly double _stateTimeVariation;  // 0.7 to 1.3 multiplier
        private readonly double _exitChanceVariation; // Varies exit probability

        // Movement speeds (pixels per second)
        private const double WalkSpeed = 30;
        private const double RunSpeed = 80;
        private const double ClimbSpeed = 40;
        private const double FallSpeed = 120;

        // State durations (seconds)
        private const double MinIdleDuration = 2.0;
        private const double MaxIdleDuration = 8.0;
        private const double MinWalkDuration = 3.0;
        private const double MaxWalkDuration = 10.0;
        private const double MinRunDuration = 2.0;
        private const double MaxRunDuration = 5.0;
        private const double HappyDuration = 1.5;
        private const double LandingDuration = 0.5;

        // Idle animation settings
        private const double BreathingSpeed = 1.5; // cycles per second
        private const double BreathingAmplitude = 0.03; // scale variation
        private const double MinBehaviorInterval = 5.0;
        private const double MaxBehaviorInterval = 15.0;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public abstract PetType PetType { get; }
        public PetColor Color { get; }
        public PetSize Size { get; set; } = PetSize.Small;
        public PetSpeed SpeedSetting { get; set; } = PetSpeed.Normal;

        public PetState CurrentState => _currentState;
        public PetDirection Direction => _direction;
        public double X => _x;
        public double Y => _y;
        public FrameworkElement Control => _container;

        public virtual bool CanClimb => true;
        public abstract string HelloMessage { get; }
        public abstract string Emoji { get; }

        /// <summary>
        /// Current breathing animation scale (1.0 = normal).
        /// </summary>
        public double BreathingScale { get; private set; } = 1.0;

        /// <summary>
        /// Current behavior being performed (null if none).
        /// </summary>
        public string CurrentBehavior => _currentBehavior;

        /// <summary>
        /// Current animation frame index.
        /// </summary>
        public int CurrentFrame => _currentFrame;

        public event EventHandler<PetStateChangedEventArgs> StateChanged;
        public event EventHandler<PetPositionChangedEventArgs> PositionChanged;
        public event EventHandler<PetSpeechEventArgs> Speech;
        public event EventHandler<PetBehaviorEventArgs> BehaviorTriggered;

        /// <summary>
        /// Event fired when the animation frame changes.
        /// </summary>
        public event EventHandler<PetFrameChangedEventArgs> FrameChanged;

        protected BasePet(PetColor color, string name = null)
        {
            Color = color;
            Name = name ?? GenerateDefaultName();
            _direction = PetDirection.Right;
            _nextBehaviorTime = RandomRange(MinBehaviorInterval, MaxBehaviorInterval);

            // Initialize per-pet random variations for unique movement patterns
            _speedVariation = 0.8 + _random.NextDouble() * 0.4;       // 0.8 to 1.2
            _stateTimeVariation = 0.7 + _random.NextDouble() * 0.6;   // 0.7 to 1.3
            _exitChanceVariation = 0.15 + _random.NextDouble() * 0.3; // 0.15 to 0.45 exit probability

            CreateControl();
            SetState(PetState.Idle);
        }

        /// <summary>
        /// Gets possible colors for this pet type.
        /// </summary>
        public abstract PetColor[] GetPossibleColors();

        /// <summary>
        /// Gets possible random behaviors for this pet type.
        /// </summary>
        public virtual string[] GetPossibleBehaviors()
        {
            return new[] { "yawn", "stretch", "look_around", "ear_twitch", "tail_wag" };
        }

        /// <summary>
        /// Gets the sprite label for the current state.
        /// </summary>
        protected virtual string GetSpriteLabel(PetState state)
        {
            return state switch
            {
                PetState.Idle => "idle",
                PetState.Walking => "walk",
                PetState.Running => "walk_fast",
                PetState.Sleeping => "lie",
                PetState.Happy => "swipe",
                PetState.Exiting => "walk",
                PetState.Entering => "walk",
                _ => "idle"
            };
        }

        /// <summary>
        /// Generates a default name for this pet type.
        /// </summary>
        protected abstract string GenerateDefaultName();

        private void CreateControl()
        {
            _spriteImage = new Image
            {
                Width = (int)Size,
                Height = (int)Size,
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            _container = new Border
            {
                Background = Brushes.Transparent,
                Child = _spriteImage,
                IsHitTestVisible = true,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = $"{Name} ({Emoji})"
            };

            // Set up mouse hover for happiness
            _container.MouseEnter += (s, e) => TriggerHappy();
            _container.MouseLeave += (s, e) => { /* Optional: end happy early */ };

            UpdateSprite();
        }

        public void Update(double deltaTime, double canvasWidth)
        {
            _stateTimer += deltaTime;
            _idleAnimationTimer += deltaTime;

            // Update frame-based animation (leg movement)
            UpdateFrameAnimation(deltaTime);

            // Update breathing animation (subtle scale pulsing)
            UpdateBreathingAnimation(deltaTime);

            // Update random behaviors
            UpdateRandomBehaviors(deltaTime);

            // Update position based on current state
            UpdateMovement(deltaTime, canvasWidth);

            // Check for state transitions
            if (_stateTimer >= _stateDuration)
            {
                TransitionToNextState(canvasWidth);
            }
        }

        private void UpdateFrameAnimation(double deltaTime)
        {
            _frameTimer += deltaTime;

            if (_frameTimer >= _frameDuration && _frameCount > 0)
            {
                _frameTimer = 0;
                var oldFrame = _currentFrame;
                _currentFrame = (_currentFrame + 1) % _frameCount;

                if (oldFrame != _currentFrame)
                {
                    FrameChanged?.Invoke(this, new PetFrameChangedEventArgs
                    {
                        Frame = _currentFrame,
                        State = _currentState
                    });
                }
            }
        }

        private void UpdateBreathingAnimation(double deltaTime)
        {
            // Only breathe when idle or sleeping
            if (_currentState == PetState.Idle || _currentState == PetState.Sleeping)
            {
                _breathingPhase += deltaTime * BreathingSpeed * 2 * Math.PI;
                if (_breathingPhase > Math.PI * 2)
                {
                    _breathingPhase -= Math.PI * 2;
                }

                // Sinusoidal breathing effect
                BreathingScale = 1.0 + Math.Sin(_breathingPhase) * BreathingAmplitude;
            }
            else
            {
                BreathingScale = 1.0;
            }
        }

        private void UpdateRandomBehaviors(double deltaTime)
        {
            // Only trigger behaviors when idle
            if (_currentState != PetState.Idle)
            {
                _isDoingBehavior = false;
                _currentBehavior = null;
                return;
            }

            _nextBehaviorTime -= deltaTime;

            if (_nextBehaviorTime <= 0 && !_isDoingBehavior)
            {
                // Trigger a random behavior
                var behaviors = GetPossibleBehaviors();
                if (behaviors.Length > 0)
                {
                    _currentBehavior = behaviors[_random.Next(behaviors.Length)];
                    _isDoingBehavior = true;

                    // Emit the behavior for visual feedback
                    BehaviorTriggered?.Invoke(this, new PetBehaviorEventArgs
                    {
                        Behavior = _currentBehavior,
                        DurationMs = GetBehaviorDuration(_currentBehavior)
                    });

                    // Show speech bubble for some behaviors
                    var speech = GetBehaviorSpeech(_currentBehavior);
                    if (!string.IsNullOrEmpty(speech))
                    {
                        Speech?.Invoke(this, new PetSpeechEventArgs
                        {
                            Message = speech,
                            DurationMs = 1500
                        });
                    }
                }

                // Reset timer for next behavior
                _nextBehaviorTime = RandomRange(MinBehaviorInterval, MaxBehaviorInterval);

                // End behavior after a short time
                System.Threading.Tasks.Task.Delay(GetBehaviorDuration(_currentBehavior)).ContinueWith(_ =>
                {
                    _isDoingBehavior = false;
                    _currentBehavior = null;
                });
            }
        }

        private int GetBehaviorDuration(string behavior)
        {
            return behavior switch
            {
                "yawn" => 2000,
                "stretch" => 1500,
                "look_around" => 2500,
                "ear_twitch" => 500,
                "tail_wag" => 1000,
                _ => 1000
            };
        }

        private string GetBehaviorSpeech(string behavior)
        {
            return behavior switch
            {
                "yawn" => "🥱",
                "stretch" => "😌",
                "look_around" => "👀",
                "ear_twitch" => "🐾",
                "tail_wag" => "😊",
                _ => null
            };
        }

        private void UpdateMovement(double deltaTime, double canvasWidth)
        {
            var oldX = _x;
            var oldY = _y;
            double speedMultiplier = GetSpeedMultiplier();

            // Apply per-pet speed variation for unique movement
            var petSpeedFactor = speedMultiplier * _speedVariation;

            switch (_currentState)
            {
                case PetState.Walking:
                    MoveHorizontally(WalkSpeed * petSpeedFactor * deltaTime, canvasWidth);
                    break;

                case PetState.Running:
                    MoveHorizontally(RunSpeed * petSpeedFactor * deltaTime, canvasWidth);
                    break;

                case PetState.Exiting:
                    // Walk at normal speed off screen
                    MoveHorizontally(WalkSpeed * petSpeedFactor * deltaTime, canvasWidth);

                    // Check if fully off screen
                    if (_hasExitedScreen)
                    {
                        HandleScreenExit(canvasWidth);
                    }
                    break;

                case PetState.Entering:
                    // Walk onto the screen
                    MoveHorizontally(WalkSpeed * petSpeedFactor * deltaTime, canvasWidth);
                    break;
            }

            if (Math.Abs(oldX - _x) > 0.001 || Math.Abs(oldY - _y) > 0.001)
            {
                PositionChanged?.Invoke(this, new PetPositionChangedEventArgs
                {
                    OldX = oldX,
                    OldY = oldY,
                    NewX = _x,
                    NewY = _y
                });

                UpdateControlPosition();
            }
        }

        private void HandleScreenExit(double canvasWidth)
        {
            _hasExitedScreen = false;
            var petSize = (int)Size;

            // Randomly decide whether to re-enter from the same side or opposite side
            var reenterFromOpposite = _random.NextDouble() < 0.5;

            if (reenterFromOpposite)
            {
                // Re-enter from the opposite side
                if (_direction == PetDirection.Right)
                {
                    // Exited right, enter from left
                    _x = -petSize;
                    // Keep direction (walking right, entering from left)
                }
                else
                {
                    // Exited left, enter from right
                    _x = canvasWidth + petSize;
                    // Keep direction (walking left, entering from right)
                }
            }
            else
            {
                // Re-enter from the same side (turn around)
                if (_direction == PetDirection.Right)
                {
                    // Exited right, re-enter from right walking left
                    _x = canvasWidth + petSize;
                    _direction = PetDirection.Left;
                    UpdateSpriteDirection();
                }
                else
                {
                    // Exited left, re-enter from left walking right
                    _x = -petSize;
                    _direction = PetDirection.Right;
                    UpdateSpriteDirection();
                }
            }

            // Switch to entering state
            SetState(PetState.Entering);
        }

        private void MoveHorizontally(double distance, double canvasWidth)
        {
            var direction = _direction == PetDirection.Right ? 1 : -1;
            _x += distance * direction;

            var petSize = (int)Size;

            // Check if pet has completely exited the screen
            if (_currentState == PetState.Exiting)
            {
                // Walking off right edge
                if (_direction == PetDirection.Right && _x > canvasWidth + petSize)
                {
                    _hasExitedScreen = true;
                }
                // Walking off left edge
                else if (_direction == PetDirection.Left && _x < -petSize * 2)
                {
                    _hasExitedScreen = true;
                }
            }
            else if (_currentState == PetState.Entering)
            {
                // Stop entering state once fully on screen
                var maxX = canvasWidth - petSize;
                if (_x >= petSize && _x <= maxX - petSize)
                {
                    // Fully on screen, transition to normal behavior
                    SetState(PetState.Walking);
                }
            }
            else
            {
                // Normal boundary behavior - start exiting when hitting edge
                var maxX = canvasWidth - petSize;
                if (_x <= 0 || _x >= maxX)
                {
                    // Use per-pet exit chance variation for unique behavior
                    if (_random.NextDouble() < _exitChanceVariation)
                    {
                        SetState(PetState.Exiting);
                    }
                    else
                    {
                        // Normal behavior - turn around
                        _x = Math.Max(0, Math.Min(_x, maxX));
                        ToggleDirection();
                    }
                }
            }
        }

        private double GetSpeedMultiplier()
        {
            return SpeedSetting switch
            {
                PetSpeed.Lazy => 0.3,
                PetSpeed.Slow => 0.6,
                PetSpeed.Normal => 1.0,
                PetSpeed.Active => 1.4,
                PetSpeed.Hyper => 2.0,
                _ => 1.0
            };
        }

        private void TransitionToNextState(double canvasWidth)
        {
            // If returning from temporary state (like Happy)
            if (_returnToState.HasValue)
            {
                SetState(_returnToState.Value);
                _returnToState = null;
                return;
            }

            var nextState = ChooseNextState(canvasWidth);
            SetState(nextState);
        }

        private PetState ChooseNextState(double canvasWidth)
        {
            // Normal state transitions
            var roll = _random.NextDouble();

            return _currentState switch
            {
                PetState.Idle => roll < 0.4 ? PetState.Walking :
                                 roll < 0.6 ? PetState.Running :
                                 roll < 0.7 ? PetState.Sleeping :
                                 PetState.Idle,

                PetState.Walking => roll < 0.3 ? PetState.Idle :
                                    roll < 0.5 ? ToggleDirection() :
                                    roll < 0.7 ? PetState.Running :
                                    PetState.Walking,

                PetState.Running => roll < 0.4 ? PetState.Walking :
                                    roll < 0.6 ? PetState.Idle :
                                    roll < 0.8 ? ToggleDirection() :
                                    PetState.Running,

                PetState.Sleeping => roll < 0.7 ? PetState.Idle : PetState.Sleeping,

                _ => PetState.Idle
            };
        }

        private PetState ToggleDirection()
        {
            _direction = _direction == PetDirection.Left ? PetDirection.Right : PetDirection.Left;
            UpdateSpriteDirection();
            return _currentState; // Stay in same state
        }

        public void SetState(PetState state)
        {
            var oldState = _currentState;
            _currentState = state;
            _stateTimer = 0;
            _stateDuration = GetStateDuration(state);

            // Initialize frame animation for this state
            var renderer = Animation.ProceduralSpriteRenderer.Instance;
            _currentFrame = 0;
            _frameTimer = 0;
            _frameCount = renderer.GetFrameCount(state);
            _frameDuration = renderer.GetFrameDuration(state);

            UpdateSprite();

            if (oldState != state)
            {
                StateChanged?.Invoke(this, new PetStateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = state
                });

                // Show speech bubble for certain state transitions
                if (state == PetState.Sleeping)
                {
                    Speech?.Invoke(this, new PetSpeechEventArgs
                    {
                        Message = "💤",
                        DurationMs = 2000
                    });
                }
            }
        }

        private double GetStateDuration(PetState state)
        {
            // Apply per-pet variation to state durations for unique timing
            var baseDuration = state switch
            {
                PetState.Idle => RandomRange(MinIdleDuration, MaxIdleDuration),
                PetState.Walking => RandomRange(MinWalkDuration, MaxWalkDuration),
                PetState.Running => RandomRange(MinRunDuration, MaxRunDuration),
                PetState.Sleeping => RandomRange(5, 15),
                PetState.Happy => HappyDuration,
                PetState.Exiting => 30.0, // Long duration - will be cut short when off screen
                PetState.Entering => 30.0, // Long duration - will transition to Walking when on screen
                _ => 3.0
            };

            // Apply per-pet time variation (except for special states)
            if (state != PetState.Exiting && state != PetState.Entering && state != PetState.Happy)
            {
                baseDuration *= _stateTimeVariation;
            }

            return baseDuration;
        }

        private double RandomRange(double min, double max)
        {
            return min + _random.NextDouble() * (max - min);
        }

        public void TriggerHappy()
        {
            if (_currentState == PetState.Happy)
            {
                return;
            }

            _returnToState = _currentState;
            SetState(PetState.Happy);

            Speech?.Invoke(this, new PetSpeechEventArgs
            {
                Message = "👋",
                DurationMs = 1500
            });
        }

        /// <summary>
        /// Makes the pet react to a build completion.
        /// </summary>
        /// <param name="success">True if build succeeded, false if failed.</param>
        public void ReactToBuild(bool success)
        {
            if (success)
            {
                // Celebrate success!
                _returnToState = _currentState;
                SetState(PetState.Happy);

                var celebrations = new[] { "🎉", "✅", "🎊", "🥳", "👏" };
                var message = celebrations[_random.Next(celebrations.Length)];

                Speech?.Invoke(this, new PetSpeechEventArgs
                {
                    Message = message,
                    DurationMs = 2500
                });
            }
            else
            {
                // Sad about failure
                var sadMessages = new[] { "😢", "❌", "😿", "💔", "🙁" };
                var message = sadMessages[_random.Next(sadMessages.Length)];

                Speech?.Invoke(this, new PetSpeechEventArgs
                {
                    Message = message,
                    DurationMs = 2500
                });
            }
        }

        /// <summary>
        /// Shows a speech bubble with the specified message.
        /// </summary>
        public void ShowSpeech(string message, int durationMs = 2000)
        {
            Speech?.Invoke(this, new PetSpeechEventArgs
            {
                Message = message,
                DurationMs = durationMs
            });
        }

        public void SetPosition(double x, double y)
        {
            var oldX = _x;
            var oldY = _y;

            _x = x;
            _y = y;

            UpdateControlPosition();

            PositionChanged?.Invoke(this, new PetPositionChangedEventArgs
            {
                OldX = oldX,
                OldY = oldY,
                NewX = _x,
                NewY = _y
            });
        }

        public void SetDirection(PetDirection direction)
        {
            if (_direction != direction)
            {
                _direction = direction;
                UpdateSpriteDirection();
            }
        }

        private void UpdateSprite()
        {
            // Use the procedural sprite renderer for animated frames
            var renderer = Animation.ProceduralSpriteRenderer.Instance;
            var sprite = renderer.RenderFrame(PetType, Color, _currentState, _currentFrame, (int)Size);

            if (sprite != null)
            {
                _spriteImage.Source = sprite;
            }

            UpdateSpriteDirection();
        }

        /// <summary>
        /// Refreshes the sprite for the current frame (called by PetControl on FrameChanged).
        /// </summary>
        public void RefreshSprite()
        {
            UpdateSprite();
        }

        private void UpdateSpriteDirection()
        {
            // Flip horizontally for left-facing
            var scaleX = _direction == PetDirection.Left ? -1 : 1;
            _spriteImage.RenderTransform = new ScaleTransform(scaleX, 1);
        }

        private void UpdateControlPosition()
        {
            if (_container != null)
            {
                Canvas.SetLeft(_container, _x);
                Canvas.SetBottom(_container, _y);
            }
        }

        public virtual void Dispose()
        {
            _container?.MouseEnter -= (s, e) => TriggerHappy();
            _spriteImage = null;
            _container = null;
        }
    }
}
