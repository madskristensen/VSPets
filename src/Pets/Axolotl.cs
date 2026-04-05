using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Axolotl pet implementation.
    /// </summary>
    public class Axolotl(PetColor color = PetColor.Pink, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _axolotlNames =
        [
            "Axel", "Bubble", "Fin", "Splash", "Noodle", "Lotl",
            "Pinky", "Blue", "Goldie", "Sprint", "Gilly", "Dr. Shrunk"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Axolotl;

        // Axolotls are underwater, but in VS they float/swim in air? or crawl?
        // They are amphibians, they can walk but usually swim.
        // Let's say "Bloop! 🦎" (closest emoji, maybe fish 🐟 or water 💧)
        public override string HelloMessage => "Bloop! 🫧";

        public override string Emoji => "🦎";

        /// <summary>
        /// Axolotls are gentle, slow-moving aquatic creatures.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Slow;

        public override bool CanClimb => true; // They can stick to glass!

        public override bool FacesLeftByDefault => false; // Axolotl art faces right

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Pink,
                PetColor.Blue,
                PetColor.Gold,
                PetColor.White,
                PetColor.Black
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _axolotlNames[_nameRandom.Next(_axolotlNames.Length)];
        }

        public static Axolotl CreateRandom()
        {
            PetColor[] colors = [PetColor.Pink, PetColor.Blue, PetColor.Gold];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Axolotl(color);
        }
    }
}
