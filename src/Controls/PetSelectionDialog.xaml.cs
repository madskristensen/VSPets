using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.PlatformUI;
using VSPets.Animation;
using VSPets.Models;
using VSPets.Services;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using VSPets.Pets;

namespace VSPets.Controls
{
    /// <summary>
    /// Interaction logic for PetSelectionDialog.xaml
    /// </summary>
    public partial class PetSelectionDialog : DialogWindow
    {
        private const int _dwmwaUseImmersiveDarkMode = 20;
        private const int _dwmwaCaptionColor = 35;
        private const int _dwmwaTextColor = 36;

        public PetType SelectedPetType { get; private set; }
        public PetColor? SelectedColor { get; private set; }

        private readonly DispatcherTimer _animationTimer;
        private DateTime _lastAnimationTick;

        public PetSelectionDialog()
        {
            InitializeComponent();

            SourceInitialized += OnSourceInitialized;
            Closed += OnClosed;

            _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 fps is plenty for sprite animation
            };
            _animationTimer.Tick += OnAnimationTick;

            // Populate ComboBox with a rendered sprite icon next to each pet name (alphabetically sorted)
            var petItems = Enum.GetValues(typeof(PetType))
                .Cast<PetType>()
                .Select(CreatePetTypeItem)
                .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (PetTypeItem item in petItems)
            {
                PetTypeComboBox.Items.Add(item);
            }

            PetTypeComboBox.SelectedIndex = 0;

            // Initial update handled by SelectionChanged event which fires after setting index
            _lastAnimationTick = DateTime.UtcNow;
            _animationTimer.Start();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _animationTimer.Stop();
            _animationTimer.Tick -= OnAnimationTick;
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            var deltaTime = (now - _lastAnimationTick).TotalSeconds;
            _lastAnimationTick = now;

            // Advance the sprite's animation frame without moving it - keeps the
            // walk cycle playing in place inside the preview card.
            if (PreviewPetControl?.Pet is BasePet basePet)
            {
                basePet.UpdateAnimation(deltaTime);
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            try
            {
                ApplyTitleBarTheme();
            }
            catch (Exception ex)
            {
                _ = ex.LogAsync();
            }
        }

        private void ApplyTitleBarTheme()
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            if (TryGetResourceColor(EnvironmentColors.ToolWindowBackgroundBrushKey, out Color captionColor))
            {
                int captionColorRef = ToColorRef(captionColor);
                _ = DwmSetWindowAttribute(handle, _dwmwaCaptionColor, ref captionColorRef, sizeof(int));
            }

            if (TryGetResourceColor(EnvironmentColors.ToolWindowTextBrushKey, out Color textColor))
            {
                int textColorRef = ToColorRef(textColor);
                _ = DwmSetWindowAttribute(handle, _dwmwaTextColor, ref textColorRef, sizeof(int));
            }

            var darkMode = 1;
            _ = DwmSetWindowAttribute(handle, _dwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));
        }

        private bool TryGetResourceColor(object key, out Color color)
        {
            if (TryFindResource(key) is SolidColorBrush brush)
            {
                color = brush.Color;
                return true;
            }

            color = default;
            return false;
        }

        private static int ToColorRef(Color color)
            => color.R | (color.G << 8) | (color.B << 16);

        private static PetTypeItem CreatePetTypeItem(PetType type)
        {
            IPet template = PetManager.Instance.CreatePet(type, null);
            PetColor color = template?.Color ?? default;

            BitmapSource icon = ProceduralSpriteRenderer.Instance.RenderFrame(
                type,
                color,
                PetState.Idle,
                0,
                (int)PetSize.Medium);

            return new PetTypeItem(type, FormatPetName(type), icon);
        }

        private static string FormatPetName(PetType type)
        {
            // Insert spaces before interior capital letters (RubberDuck -> Rubber Duck, TRex -> T Rex).
            var name = type.ToString();
            var sb = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    sb.Append(' ');
                }
                sb.Append(name[i]);
            }
            return sb.ToString();
        }

        private void PetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PetTypeComboBox.SelectedItem is PetTypeItem item)
            {
                SelectedPetType = item.PetType;
                UpdateColorOptions(item.PetType);
                UpdatePreview();
            }
        }

        private void PetColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PetColorComboBox.SelectedItem is PetColor color)
            {
                SelectedColor = color;
            }
            // Also handle "Random" selection if we add it, for now assuming nullable color means random/default
            else if (PetColorComboBox.SelectedItem is string)
            {
                 SelectedColor = null;
            }

            UpdatePreview();
        }

        private void UpdateColorOptions(PetType type)
        {
            PetColorComboBox.Items.Clear();

            // Temporary pet to get available colors
            // Note: We create a dummy pet just to access GetPossibleColors()
            // Optimization: Could make GetPossibleColors static or use a helper service, but this works given current architecture
            IPet dummyPet = PetManager.Instance.CreatePet(type, null);
            if (dummyPet is BasePet basePet)
            {
                PetColor[] colors = basePet.GetPossibleColors();
                if (colors != null && colors.Length > 0)
                {
                    PetColorComboBox.Items.Add("Random"); // Default option
                    foreach (PetColor color in colors)
                    {
                        PetColorComboBox.Items.Add(color);
                    }
                    PetColorComboBox.SelectedIndex = 0; // Select Random by default
                    ColorPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ColorPanel.Visibility = Visibility.Collapsed;
                    SelectedColor = null;
                }
            }
            else
            {
                // Handle non-BasePet implementations if any (currently all are BasePet)
                ColorPanel.Visibility = Visibility.Collapsed;
                SelectedColor = null;
            }
        }

        private void UpdatePreview()
        {
            // Create a pet with selected type and color
            // If color is null (Random selected), CreatePet will handle randomization or default logic,
            // BUT for preview "Random" might be confusing if it keeps changing. 
            // Actually, for preview, if "Random" is selected, we might want to show *a* valid color, 
            // or just let the manager pick one (which it does).
            // However, to make the preview stable when "Random" is selected, we might want to pick the first available color 
            // or cache the random choice. For simplicity, let's rely on Manager's creation logic.
            // Wait, if "Random" is selected in dropdown (SelectedColor == null), CreatePet creates a pet with random color (e.g. Cat.CreateRandom uses random color).
            // That is acceptable for "Random".

            // If a specific color is selected, pass it.
            IPet pet = PetManager.Instance.CreatePet(SelectedPetType, SelectedColor);

             if (pet != null)
             {
                // We need to set directions etc so it renders correctly
                pet.SetDirection(PetDirection.Right);
                pet.SetState(PetState.Walking); // Walking usually shows side profile nicely

                // Render the source sprite at the largest available size so the Viewbox
                // scales up a crisper bitmap in the preview card.
                pet.Size = PetSize.Large;

                PreviewPetControl.Pet = pet;
                PreviewPetControl.SetSize(PetSize.Large);
             }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
