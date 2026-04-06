using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VSPets.Controls
{
    /// <summary>
    /// A Canvas that hosts all pet controls and manages their positioning
    /// relative to the Visual Studio status bar.
    /// </summary>
    public class PetHostCanvas : Canvas
    {
        private readonly List<FrameworkElement> _pets = [];
        private readonly object _petLock = new();

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

        public PetHostCanvas()
        {
            // Make the canvas transparent and overlay-able
            Background = Brushes.Transparent;
            IsHitTestVisible = true;
            ClipToBounds = false;
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

        /// <summary>
        /// Converts a local canvas X coordinate to screen X coordinate.
        /// </summary>
        public double LocalToScreenX(double localX)
        {
            try
            {
                Point screenPoint = PointToScreen(new Point(localX, 0));
                return screenPoint.X;
            }
            catch (Exception ex)
            {
                ex.Log();
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
                Point localPoint = PointFromScreen(new Point(screenX, 0));
                return localPoint.X;
            }
            catch (Exception ex)
            {
                ex.Log();
                return screenX;
            }
        }
    }
}
