using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        public PetType SelectedPetType { get; private set; }
        public PetColor? SelectedColor { get; private set; }

        public PetSelectionDialog()
        {
            InitializeComponent();

            // Populate ComboBox with PetType enum values
            foreach (PetType type in Enum.GetValues(typeof(PetType)))
            {
                PetTypeComboBox.Items.Add(type);
            }

            PetTypeComboBox.SelectedIndex = 0;

            // Initial update handled by SelectionChanged event which fires after setting index
        }

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
            var dummyPet = PetManager.Instance.CreatePet(type, null);
            if (dummyPet is BasePet basePet)
            {
                var colors = basePet.GetPossibleColors();
                if (colors != null && colors.Length > 0)
                {
                    PetColorComboBox.Items.Add("Random"); // Default option
                    foreach (var color in colors)
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
             var pet = PetManager.Instance.CreatePet(SelectedPetType, SelectedColor);

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
