using System.Windows.Media.Imaging;
using VSPets.Models;

namespace VSPets.Controls
{
    /// <summary>
    /// Lightweight view-model used by the pet selection dialog's ComboBox to
    /// display a rendered sprite icon next to each pet type name.
    /// </summary>
    public sealed class PetTypeItem
    {
        public PetTypeItem(PetType petType, string displayName, BitmapSource icon)
        {
            PetType = petType;
            DisplayName = displayName;
            Icon = icon;
        }

        public PetType PetType { get; }

        public string DisplayName { get; }

        public BitmapSource Icon { get; }

        public override string ToString() => DisplayName;
    }
}
