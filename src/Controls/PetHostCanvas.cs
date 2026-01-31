using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;

namespace VSPets.Controls
{
    /// <summary>
    /// A Canvas that hosts all pet controls and manages their positioning
    /// relative to the Visual Studio status bar.
    /// </summary>
    public class PetHostCanvas : Canvas
    {
        private readonly DispatcherTimer _updateTimer;
        private readonly List<FrameworkElement> _pets = new List<FrameworkElement>();
        private readonly object _petLock = new object();

        /// <summary>
        /// The floor Y position (bottom of the status bar in local coordinates).
        /// Pets walk at Y = 0 (on the floor).
        /// </summary>
        public double FloorY => 0;

        /// <summary>
        /// The left boundary for pet movement.
        /// </summary>
        public double LeftBoundary => 0;

        /// <summary>
        /// The right boundary for pet movement.
        /// </summary>
        public double RightBoundary => ActualWidth;

        /// <summary>
        /// Event fired on each animation frame (for pet movement updates).
        /// </summary>
        public event EventHandler<AnimationFrameEventArgs> AnimationFrame;

        public PetHostCanvas()
        {
            // Make the canvas transparent and overlay-able
            Background = Brushes.Transparent;
            IsHitTestVisible = true;
            ClipToBounds = false;

            // Set up the animation timer (targeting ~30fps for smooth but efficient animation)
            _updateTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _updateTimer.Tick += OnAnimationTick;

            // Start/stop timer based on visibility
            IsVisibleChanged += OnVisibilityChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Adds a pet control to the canvas.
        /// </summary>
        /// <param name="petControl">The pet control to add.</param>
        /// <param name="initialX">Initial X position.</param>
        public void AddPet(FrameworkElement petControl, double initialX = 100)
        {
            if (petControl == null)
            {
                return;
            }

            lock (_petLock)
            {
                if (_pets.Contains(petControl))
                {
                    return;
                }

                _pets.Add(petControl);
            }

            // Position the pet at the floor level
            SetLeft(petControl, initialX);
            SetBottom(petControl, FloorY);

            Children.Add(petControl);

            // Ensure timer is running if we have pets
            EnsureTimerState();
        }

        /// <summary>
        /// Removes a pet control from the canvas.
        /// </summary>
        public void RemovePet(FrameworkElement petControl)
        {
            if (petControl == null)
            {
                return;
            }

            lock (_petLock)
            {
                _pets.Remove(petControl);
            }

            Children.Remove(petControl);

            // Stop timer if no pets
            EnsureTimerState();
        }

        /// <summary>
        /// Gets all pet controls currently on the canvas.
        /// </summary>
        public IReadOnlyList<FrameworkElement> GetPets()
        {
            lock (_petLock)
            {
                return _pets.ToArray();
            }
        }

        /// <summary>
        /// Positions a pet at the specified coordinates.
        /// </summary>
        /// <param name="petControl">The pet to position.</param>
        /// <param name="x">X coordinate (left edge).</param>
        /// <param name="y">Y coordinate (bottom edge - distance from floor).</param>
        public void PositionPet(FrameworkElement petControl, double x, double y)
        {
            if (petControl == null || !Children.Contains(petControl))
            {
                return;
            }

            // Clamp X to boundaries
            var clampedX = Math.Max(LeftBoundary, Math.Min(x, RightBoundary - petControl.ActualWidth));
            
            SetLeft(petControl, clampedX);
            SetBottom(petControl, y);
        }

        /// <summary>
        /// Gets the X position of a pet (left edge).
        /// </summary>
        public double GetPetX(FrameworkElement petControl)
        {
            if (petControl == null)
            {
                return 0;
            }

            var left = GetLeft(petControl);
            return double.IsNaN(left) ? 0 : left;
        }

        /// <summary>
        /// Gets the Y position of a pet (distance from floor).
        /// </summary>
        public double GetPetY(FrameworkElement petControl)
        {
            if (petControl == null)
            {
                return 0;
            }

            var bottom = GetBottom(petControl);
            return double.IsNaN(bottom) ? 0 : bottom;
        }

        /// <summary>
        /// Checks if a pet is at or near the left boundary.
        /// </summary>
        public bool IsAtLeftEdge(FrameworkElement petControl, double threshold = 5)
        {
            var x = GetPetX(petControl);
            return x <= LeftBoundary + threshold;
        }

        /// <summary>
        /// Checks if a pet is at or near the right boundary.
        /// </summary>
        public bool IsAtRightEdge(FrameworkElement petControl, double threshold = 5)
        {
            var x = GetPetX(petControl);
            var petWidth = petControl?.ActualWidth ?? 32;
            return x + petWidth >= RightBoundary - threshold;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureTimerState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EnsureTimerState();
        }

        private void EnsureTimerState()
        {
            bool shouldRun;
            lock (_petLock)
            {
                shouldRun = _pets.Count > 0 && IsVisible && IsLoaded;
            }

            if (shouldRun && !_updateTimer.IsEnabled)
            {
                _updateTimer.Start();
            }
            else if (!shouldRun && _updateTimer.IsEnabled)
            {
                _updateTimer.Stop();
            }
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            var args = new AnimationFrameEventArgs
            {
                DeltaTime = _updateTimer.Interval.TotalSeconds,
                CanvasWidth = ActualWidth,
                CanvasHeight = ActualHeight
            };

            AnimationFrame?.Invoke(this, args);
        }

        /// <summary>
        /// Converts a local canvas X coordinate to screen X coordinate.
        /// </summary>
        public double LocalToScreenX(double localX)
        {
            try
            {
                var screenPoint = PointToScreen(new Point(localX, 0));
                return screenPoint.X;
            }
            catch
            {
                return localX;
            }
        }

        /// <summary>
        /// Converts a screen X coordinate to local canvas X coordinate.
        /// </summary>
        public double ScreenToLocalX(double screenX)
        {
            try
            {
                var localPoint = PointFromScreen(new Point(screenX, 0));
                return localPoint.X;
            }
            catch
            {
                return screenX;
            }
        }
    }

    /// <summary>
    /// Event arguments for animation frame updates.
    /// </summary>
    public class AnimationFrameEventArgs : EventArgs
    {
        /// <summary>
        /// Time since last frame in seconds.
        /// </summary>
        public double DeltaTime { get; set; }

        /// <summary>
        /// Current canvas width.
        /// </summary>
        public double CanvasWidth { get; set; }

        /// <summary>
        /// Current canvas height.
        /// </summary>
        public double CanvasHeight { get; set; }
    }
}
