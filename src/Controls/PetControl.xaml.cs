using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VSPets.Animation;
using VSPets.Models;
using VSPets.Pets;

namespace VSPets.Controls
{
    /// <summary>
    /// WPF control that renders and animates a pet.
    /// </summary>
    public partial class PetControl : UserControl
    {
        private IPet _pet;
        private BasePet _basePet; // For accessing breathing/behavior
        private DispatcherTimer _speechTimer;
        private DispatcherTimer _breathingTimer;
        private bool _isHovered;
        private ScaleTransform _breathingTransform;
        private ScaleTransform _flipTransform;

        /// <summary>
        /// Gets or sets the pet this control displays.
        /// </summary>
        public IPet Pet
        {
            get => _pet;
            set
            {
                if (_pet != null)
                {
                    _pet.StateChanged -= OnPetStateChanged;
                    _pet.PositionChanged -= OnPetPositionChanged;
                    _pet.Speech -= OnPetSpeech;

                    if (_basePet != null)
                    {
                        _basePet.BehaviorTriggered -= OnPetBehavior;
                        _basePet.FrameChanged -= OnPetFrameChanged;
                    }
                }

                _pet = value;
                _basePet = value as BasePet;

                if (_pet != null)
                {
                    _pet.StateChanged += OnPetStateChanged;
                    _pet.PositionChanged += OnPetPositionChanged;
                    _pet.Speech += OnPetSpeech;

                    if (_basePet != null)
                    {
                        _basePet.BehaviorTriggered += OnPetBehavior;
                        _basePet.FrameChanged += OnPetFrameChanged;
                    }

                    UpdateDisplay();
                    UpdateNameLabel();
                }
            }
        }

        public PetControl()
        {
            InitializeComponent();

            // Set up transforms - flip for direction, breathing for idle animation
            _flipTransform = new ScaleTransform(1.0, 1.0);
            _breathingTransform = new ScaleTransform(1.0, 1.0);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(_flipTransform);
            transformGroup.Children.Add(_breathingTransform);

            PetSprite.RenderTransform = transformGroup;
            PetSprite.RenderTransformOrigin = new Point(0.5, 0.5); // Scale from center for proper flipping

            // Set up event handlers
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseRightButtonUp += OnRightClick;

            // Initialize speech timer
            _speechTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(3000)
            };
            _speechTimer.Tick += (s, e) =>
            {
                HideSpeechBubble();
                _speechTimer.Stop();
            };

            // Initialize breathing animation timer (runs at ~30fps)
            _breathingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _breathingTimer.Tick += OnBreathingTick;
            _breathingTimer.Start();
        }

        private void OnBreathingTick(object sender, EventArgs e)
        {
            if (_basePet == null) return;

            // Apply breathing scale from the pet
            var scale = _basePet.BreathingScale;
            _breathingTransform.ScaleX = scale;
            _breathingTransform.ScaleY = scale;
        }

        private void OnPetFrameChanged(object sender, PetFrameChangedEventArgs e)
        {
            // Refresh the sprite when the animation frame changes
            _basePet?.RefreshSprite();

            // Also update our display to sync with the new frame
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateDisplay();
            }));
        }

        private void OnPetBehavior(object sender, PetBehaviorEventArgs e)
        {
            // Show a visual indicator for certain behaviors
            switch (e.Behavior)
            {
                case "yawn":
                    // Could animate mouth opening or show emoji
                    break;
                case "stretch":
                    // Could animate a stretch pose
                    AnimateStretch(e.DurationMs);
                    break;
                case "look_around":
                    // Could animate eyes moving
                    AnimateLookAround(e.DurationMs);
                    break;
                case "ear_twitch":
                    // Subtle twitch animation
                    AnimateTwitch(e.DurationMs);
                    break;
                case "tail_wag":
                    // Could animate tail wagging
                    AnimateTailWag(e.DurationMs);
                    break;
            }
        }

        private void AnimateStretch(int durationMs)
        {
            // Create a stretch animation (scale wider, then back)
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.15,
                Duration = TimeSpan.FromMilliseconds(durationMs / 2),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            _breathingTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        }

        private void AnimateLookAround(int durationMs)
        {
            // Create a slight horizontal shift animation
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 3,
                Duration = TimeSpan.FromMilliseconds(durationMs / 4),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            var translateTransform = new TranslateTransform();
            PetSprite.RenderTransform = new TransformGroup
            {
                Children = { _breathingTransform, translateTransform }
            };
            translateTransform.BeginAnimation(TranslateTransform.XProperty, animation);

            // Reset transform after animation
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            timer.Tick += (s, e) =>
            {
                PetSprite.RenderTransform = _breathingTransform;
                timer.Stop();
            };
            timer.Start();
        }

        private void AnimateTwitch(int durationMs)
        {
            // Quick scale twitch
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromMilliseconds(durationMs / 2),
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            _breathingTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void AnimateTailWag(int durationMs)
        {
            // Create a rotation wiggle animation
            var animation = new DoubleAnimation
            {
                From = -3,
                To = 3,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            var rotateTransform = new RotateTransform();
            PetSprite.RenderTransform = new TransformGroup
            {
                Children = { _breathingTransform, rotateTransform }
            };
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);

            // Reset transform after animation
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            timer.Tick += (s, e) =>
            {
                PetSprite.RenderTransform = _breathingTransform;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// Creates a PetControl for the specified pet.
        /// </summary>
        public static PetControl Create(IPet pet)
        {
            var control = new PetControl { Pet = pet };
            return control;
        }

        /// <summary>
        /// Updates the sprite size.
        /// </summary>
        public void SetSize(PetSize size)
        {
            var pixels = (int)size;
            PetSprite.Width = pixels;
            PetSprite.Height = pixels;
        }

        /// <summary>
        /// Sets the facing direction of the pet sprite.
        /// </summary>
        public void SetDirection(PetDirection direction)
        {
            // Sprite is drawn facing left by default, so flip when moving right
            _flipTransform.ScaleX = direction == PetDirection.Right ? -1 : 1;
        }

        /// <summary>
        /// Shows the speech bubble with a message.
        /// </summary>
        public void ShowSpeechBubble(string message, int durationMs = 3000)
        {
            SpeechText.Text = message;
            SpeechBubble.Visibility = Visibility.Visible;

            // Fade in animation
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            SpeechBubble.BeginAnimation(OpacityProperty, fadeIn);

            // Set up timer to hide
            _speechTimer.Stop();
            _speechTimer.Interval = TimeSpan.FromMilliseconds(durationMs);
            _speechTimer.Start();
        }

        /// <summary>
        /// Hides the speech bubble.
        /// </summary>
        public void HideSpeechBubble()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => SpeechBubble.Visibility = Visibility.Collapsed;
            SpeechBubble.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void UpdateDisplay()
        {
            if (_pet == null) return;

            // Update sprite based on current state and frame
            UpdateSprite();

            // Update direction
            SetDirection(_pet.Direction);

            // Update size
            SetSize(_pet.Size);
        }

        private void UpdateSprite()
        {
            if (_pet == null) return;

            // Use the procedural sprite renderer with the current animation frame
            var frame = _basePet?.CurrentFrame ?? 0;
            var sprite = ProceduralSpriteRenderer.Instance.RenderFrame(
                _pet.PetType, 
                _pet.Color, 
                _pet.CurrentState, 
                frame, 
                (int)_pet.Size);

            if (sprite != null)
            {
                PetSprite.Source = sprite;
            }
        }

        private void UpdateNameLabel()
        {
            if (_pet != null)
            {
                NameText.Text = $"{_pet.Name} {_pet.Emoji}";
                ToolTip = $"{_pet.Name} the {_pet.PetType}";
            }
        }

        private void OnPetStateChanged(object sender, PetStateChangedEventArgs e)
        {
            Dispatcher.Invoke(() => UpdateSprite());
        }

        private void OnPetPositionChanged(object sender, PetPositionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Canvas.SetLeft(this, e.NewX);
                Canvas.SetBottom(this, e.NewY);

                // Update direction in case it changed (pet should face direction of travel)
                if (_pet != null)
                {
                    SetDirection(_pet.Direction);
                }
            });
        }

        private void OnPetSpeech(object sender, PetSpeechEventArgs e)
        {
            Dispatcher.Invoke(() => ShowSpeechBubble(e.Message, e.DurationMs));
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _isHovered = true;

            // Show name label
            NameLabel.Visibility = Visibility.Visible;

            // Update sprite immediately to show happy expression
            UpdateSprite();

            // Trigger happy animation on the pet model
            _pet?.TriggerHappy();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isHovered = false;

            // Hide name label
            NameLabel.Visibility = Visibility.Collapsed;

            // Update sprite to return to normal expression
            UpdateSprite();
        }

        private void OnRightClick(object sender, MouseButtonEventArgs e)
        {
            // Context menu will show automatically
            // Show a greeting while menu is open
            ShowSpeechBubble(_pet?.HelloMessage ?? "Hello!", 2000);
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
        {
            if (_pet == null) return;

            // Create a simple input dialog
            var dialog = new Window
            {
                Title = "Rename Pet",
                Width = 300,
                Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var label = new TextBlock 
            { 
                Text = $"Enter a new name for {_pet.Name}:",
                Margin = new Thickness(0, 0, 0, 8)
            };
            var textBox = new TextBox 
            { 
                Text = _pet.Name,
                Margin = new Thickness(0, 0, 0, 10)
            };
            textBox.SelectAll();

            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var okButton = new Button 
            { 
                Content = "OK", 
                Width = 75, 
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            var cancelButton = new Button 
            { 
                Content = "Cancel", 
                Width = 75,
                IsCancel = true
            };

            okButton.Click += (s, args) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    _pet.Name = textBox.Text.Trim();
                    UpdateNameLabel();
                    ShowSpeechBubble($"Call me {_pet.Name}! 😊", 2000);
                }
                dialog.DialogResult = true;
                dialog.Close();
            };

            cancelButton.Click += (s, args) => dialog.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);
            dialog.Content = stackPanel;

            textBox.Focus();
            dialog.ShowDialog();
        }

        private async void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (_pet == null) return;

            // Show goodbye message
            ShowSpeechBubble("Bye bye! 👋", 1000);

            // Wait a moment for the message to show
            await System.Threading.Tasks.Task.Delay(500);

            // Remove the pet via PetManager
            await Services.PetManager.Instance.RemovePetAsync(_pet.Id);
        }

        private async void OnAddPetClick(object sender, RoutedEventArgs e)
        {
            // Add a random pet
            await Services.PetManager.Instance.AddRandomPetAsync();
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            // Open the VS Pets options page in Tools > Options
            Community.VisualStudio.Toolkit.VS.Settings.OpenAsync<Options.OptionsProvider.GeneralOptions>()
                .FileAndForget(nameof(OnSettingsClick));
        }
    }
}
