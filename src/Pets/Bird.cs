using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Bird pet implementation - cheerful little songbird!
    /// </summary>
    public class Bird(PetColor color = PetColor.Blue, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _birdNames =
        [
            "Tweety", "Robin", "Sky", "Pip", "Chirpy", "Sunny",
            "Jay", "Wren", "Sparrow", "Peep", "Kiwi", "Phoenix",
            "Skye", "Bluebell", "Goldie", "Coco", "Mango", "Sora"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Bird;

        public override string HelloMessage => "Tweet tweet! 🐦";

        public override string Emoji => "🐦";

        /// <summary>
        /// Birds are quick and lively.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Active;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Blue,
                PetColor.Yellow,
                PetColor.Red,
                PetColor.White,
                PetColor.Green
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _birdNames[_nameRandom.Next(_birdNames.Length)];
        }

        public static Bird CreateRandom()
        {
            PetColor[] colors = [PetColor.Blue, PetColor.Yellow, PetColor.Red, PetColor.White, PetColor.Green];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Bird(color);
        }
    }
}
