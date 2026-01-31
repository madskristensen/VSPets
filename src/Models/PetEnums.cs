namespace VSPets.Models
{
    /// <summary>
    /// Types of pets available in VS Pets.
    /// </summary>
    public enum PetType
    {
        Cat,
        Dog,
        Fox,
        Bear,
        Axolotl,
        Clippy,
        RubberDuck
    }

    /// <summary>
    /// Color variations for pets.
    /// </summary>
    public enum PetColor
    {
        // Universal colors
        Black,
        White,
        Brown,
        Gray,

        // Cat-specific
        Orange,
        LightBrown,

        // Dog-specific
        Red,      // Shiba Inu / Akita
        Akita,

        // Fox-specific
        // Uses Red and White

        // Axolotl-specific
        Pink,
        Blue,
        Gold,

        // Special
        Yellow,   // Rubber duck
        Original  // For Clippy
    }

    /// <summary>
    /// Size options for pet sprites.
    /// </summary>
    public enum PetSize
    {
        /// <summary>
        /// 20x20 pixels - very small, subtle
        /// </summary>
        Tiny = 20,

        /// <summary>
        /// 26x26 pixels - small, default
        /// </summary>
        Small = 26,

        /// <summary>
        /// 36x36 pixels - medium, more visible
        /// </summary>
        Medium = 36,

        /// <summary>
        /// 48x48 pixels - large, prominent
        /// </summary>
        Large = 48
    }

    /// <summary>
    /// Speed presets for pet movement.
    /// </summary>
    public enum PetSpeed
    {
        /// <summary>
        /// Very slow movement - mostly stationary
        /// </summary>
        Lazy = 0,

        /// <summary>
        /// Slow, relaxed movement
        /// </summary>
        Slow = 1,

        /// <summary>
        /// Normal movement speed
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Active, frequent movement
        /// </summary>
        Active = 3,

        /// <summary>
        /// Very energetic movement
        /// </summary>
        Hyper = 4
    }
}
