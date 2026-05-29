using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Fish pet implementation - swimming through your status bar!
    /// </summary>
    public class Fish(PetColor color = PetColor.Orange, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _fishNames =
        [
            "Bubbles", "Nemo", "Dory", "Goldie", "Finn", "Splash",
            "Marlin", "Coral", "Gill", "Flounder", "Sushi", "Wanda",
            "Jaws", "Guppy", "Minnow", "Pearl", "Aqua", "Neptune"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Fish;

        public override string HelloMessage => "Blub blub! 🐟";

        public override string Emoji => "🐟";

        /// <summary>
        /// Fish glide gracefully through the water.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Slow;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Orange,
                PetColor.Gold,
                PetColor.Blue,
                PetColor.Red,
                PetColor.White,
                PetColor.Purple
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _fishNames[_nameRandom.Next(_fishNames.Length)];
        }

        public static Fish CreateRandom()
        {
            PetColor[] colors = [PetColor.Orange, PetColor.Gold, PetColor.Blue, PetColor.Red, PetColor.Purple];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Fish(color);
        }
    }
}
