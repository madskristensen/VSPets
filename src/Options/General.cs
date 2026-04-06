using System.ComponentModel;
using System.Runtime.InteropServices;
using VSPets.Models;

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
    public class General : BaseOptionModel<General>, IRatingConfig
    {
        [Category("Pets")]
        [DisplayName("Auto-spawn Pet on Startup")]
        [Description("Automatically add a random pet when Visual Studio starts")]
        [DefaultValue(true)]
        public bool AutoSpawnOnStartup { get; set; } = true;

        [Category("Behavior")]
        [DisplayName("Pet Speed")]
        [Description("How fast the pets move relative to each animal's natural speed (Lazy, Slow, Normal, Active, Hyper)")]
        [DefaultValue(PetSpeed.Normal)]
        [TypeConverter(typeof(EnumConverter))]
        public PetSpeed PetSpeed { get; set; } = PetSpeed.Normal;

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

        [Browsable(false)]
        [DefaultValue(0)]
        public int RatingRequests { get; set; }
    }
}
