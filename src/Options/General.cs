using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VSPets.Options
{
    /// <summary>
    /// Options page for VS Pets settings.
    /// </summary>
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    /// <summary>
    /// General settings for VS Pets.
    /// </summary>
    public class General : BaseOptionModel<General>
    {
        [Category("Pets")]
        [DisplayName("Maximum Pets")]
        [Description("Maximum number of pets allowed at once (1-10)")]
        [DefaultValue(5)]
        public int MaxPets { get; set; } = 5;

        [Category("Pets")]
        [DisplayName("Auto-spawn Pet on Startup")]
        [Description("Automatically add a random pet when Visual Studio starts")]
        [DefaultValue(true)]
        public bool AutoSpawnOnStartup { get; set; } = true;

        [Category("Behavior")]
        [DisplayName("Pet Speed")]
        [Description("How fast the pets move (Lazy, Normal, Energetic)")]
        [DefaultValue(SpeedOption.Normal)]
        [TypeConverter(typeof(EnumConverter))]
        public SpeedOption PetSpeed { get; set; } = SpeedOption.Normal;

        [Category("Behavior")]
        [DisplayName("Enable Idle Animations")]
        [Description("Show subtle breathing and movement animations when pets are idle")]
        [DefaultValue(true)]
        public bool EnableIdleAnimations { get; set; } = true;

        [Category("Behavior")]
        [DisplayName("Enable Random Behaviors")]
        [Description("Pets occasionally yawn, stretch, or look around")]
        [DefaultValue(true)]
        public bool EnableRandomBehaviors { get; set; } = true;

        [Category("Persistence")]
        [DisplayName("Remember Pets")]
        [Description("Save and restore your pets when Visual Studio restarts")]
        [DefaultValue(true)]
        public bool RememberPets { get; set; } = true;
    }

    /// <summary>
    /// Speed options for pets.
    /// </summary>
    public enum SpeedOption
    {
        Lazy,
        Normal,
        Energetic
    }
}
