using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
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

        public PetSelectionDialog()
        {
            InitializeComponent();

            SourceInitialized += OnSourceInitialized;

            // Populate ComboBox with PetType enum values (alphabetically sorted)
            var petTypes = Enum.GetValues(typeof(PetType))
                .Cast<PetType>()
                .OrderBy(p => p.ToString())
                .ToList();

            foreach (PetType type in petTypes)
            {
                PetTypeComboBox.Items.Add(type);
            }

            PetTypeComboBox.SelectedIndex = 0;

            // Initial update handled by SelectionChanged event which fires after setting index
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

        private void PetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PetTypeComboBox.SelectedItem is PetType type)
            {
                SelectedPetType = type;
                UpdateColorOptions(type);
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
                    PetColorComboBox.Visibility = Visibility.Visible;
                    ColorLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    PetColorComboBox.Visibility = Visibility.Collapsed;
                    ColorLabel.Visibility = Visibility.Collapsed;
                    SelectedColor = null;
                }
            }
            else
            {
                // Handle non-BasePet implementations if any (currently all are BasePet)
                PetColorComboBox.Visibility = Visibility.Collapsed;
                 ColorLabel.Visibility = Visibility.Collapsed;
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

                PreviewPetControl.Pet = pet;
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
