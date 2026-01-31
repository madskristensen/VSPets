using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VSPets
{
    /// <summary>
    /// Injects a pet overlay canvas that sits ON TOP of the Visual Studio status bar.
    /// The canvas is positioned at the bottom of the main window, visually above the status bar.
    /// </summary>
    internal static class StatusBarInjector
    {
        private static Panel _statusBarPanel;
        private static Grid _rootGrid;
        private static Canvas _overlayCanvas;
        private static bool _isInitialized;

        /// <summary>
        /// Gets the status bar panel width for boundary calculations.
        /// </summary>
        public static double StatusBarWidth => _statusBarPanel?.ActualWidth ?? 0;

        /// <summary>
        /// Gets the status bar panel height.
        /// </summary>
        public static double StatusBarHeight => _statusBarPanel?.ActualHeight ?? 22;

        /// <summary>
        /// Injects a Canvas overlay that sits on top of the status bar.
        /// The canvas is positioned at the bottom of the main window.
        /// </summary>
        public static async Task<bool> InjectControlAsync(FrameworkElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!_isInitialized)
            {
                _isInitialized = await EnsureUIAsync();
            }

            if (_rootGrid == null || _statusBarPanel == null)
            {
                return false;
            }

            if (element is not Canvas canvas)
            {
                return false;
            }

            // Store reference to the overlay canvas
            _overlayCanvas = canvas;

            // Configure the canvas to overlay at the bottom of the window, above the status bar
            // Background = null allows clicks to pass through empty canvas areas
            // IsHitTestVisible = true allows child controls (pets) to receive mouse events
            canvas.Background = null;
            canvas.IsHitTestVisible = true;
            canvas.HorizontalAlignment = HorizontalAlignment.Stretch;
            canvas.VerticalAlignment = VerticalAlignment.Bottom;

            // Position the canvas just above the status bar
            // Subtract 6 pixels so pets overlap with the status bar (appear grounded)
            canvas.Margin = new Thickness(0, 0, 0, StatusBarHeight - 6);

            // Add the canvas to the root grid as an overlay (it will be on top of everything)
            if (!_rootGrid.Children.Contains(canvas))
            {
                // Set high z-index to ensure it's on top
                Panel.SetZIndex(canvas, 10000);
                Grid.SetRowSpan(canvas, 100); // Span all rows
                Grid.SetColumnSpan(canvas, 100); // Span all columns
                _rootGrid.Children.Add(canvas);
            }

            // Update canvas position when status bar size changes
            _statusBarPanel.SizeChanged += OnStatusBarSizeChanged;

            return true;
        }

        private static void OnStatusBarSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_overlayCanvas != null)
            {
                _overlayCanvas.Margin = new Thickness(0, 0, 0, StatusBarHeight - 6);
            }
        }

        /// <summary>
        /// Removes the overlay canvas from the window.
        /// </summary>
        public static async Task<bool> RemoveControlAsync(FrameworkElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_rootGrid != null && _rootGrid.Children.Contains(element))
            {
                _rootGrid.Children.Remove(element);

                if (_statusBarPanel != null)
                {
                    _statusBarPanel.SizeChanged -= OnStatusBarSizeChanged;
                }

                if (element == _overlayCanvas)
                {
                    _overlayCanvas = null;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the screen coordinates of the status bar for positioning calculations.
        /// </summary>
        public static async Task<Rect> GetStatusBarBoundsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_statusBarPanel == null)
            {
                return Rect.Empty;
            }

            try
            {
                Point topLeft = _statusBarPanel.PointToScreen(new Point(0, 0));
                return new Rect(topLeft.X, topLeft.Y, _statusBarPanel.ActualWidth, _statusBarPanel.ActualHeight);
            }
            catch
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// Ensures the required UI elements are available, with retry logic.
        /// </summary>
        private static async Task<bool> EnsureUIAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Try to find the required elements multiple times with a delay
            for (var i = 0; i < 10; i++)
            {
                // Find the status bar panel to track its size/position
                _statusBarPanel = FindChild<DockPanel>(Application.Current.MainWindow, "StatusBarPanel");

                // Find the root grid of the main window where we can add our overlay
                _rootGrid = FindRootGrid(Application.Current.MainWindow);

                if (_statusBarPanel != null && _rootGrid != null)
                {
                    return true;
                }

                await Task.Delay(500);
            }

            return false;
        }

        /// <summary>
        /// Finds the root Grid in the main window where we can add overlays.
        /// </summary>
        private static Grid FindRootGrid(DependencyObject parent)
        {
            if (parent == null)
            {
                return null;
            }

            // Try to find the first Grid that's a direct child or is the content
            if (parent is Window window && window.Content is Grid grid)
            {
                return grid;
            }

            // Otherwise search for the first Grid in the visual tree
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is Grid foundGrid)
                {
                    return foundGrid;
                }

                Grid result = FindRootGrid(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively searches for a child element of a specific type and name.
        /// </summary>
        private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            T foundChild = null;
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is not T typedChild)
                {
                    foundChild = FindChild<T>(child, childName);

                    if (foundChild != null)
                    {
                        break;
                    }
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        foundChild = typedChild;
                        break;
                    }
                    else
                    {
                        foundChild = FindChild<T>(child, childName);

                        if (foundChild != null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    foundChild = typedChild;
                    break;
                }
            }

            return foundChild;
        }

        /// <summary>
        /// Finds all children of a specific type in the visual tree.
        /// </summary>
        public static void FindChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            if (parent == null || results == null)
            {
                return;
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    results.Add(typedChild);
                }

                FindChildren(child, results);
            }
        }
    }
}
