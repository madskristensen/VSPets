using System;
using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Tiger pet implementation - a fierce but cuddly big cat with stripes!
    /// </summary>
    public class Tiger : BasePet
    {
        private static readonly string[] TigerNames =
        [
            "Tony", "Stripes", "Rajah", "Shere Khan", "Hobbes", "Tigger",
            "Khan", "Sabre", "Blaze", "Amber", "Ember", "Kovu",
            "Bengal", "Sultan", "Zara", "Tora", "Raja", "Fang"
        ];

        private static readonly Random NameRandom = new();

        public Tiger(PetColor color = PetColor.Orange, string name = null)
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Tiger;

        public override string HelloMessage => "Roar! 🐯";

        public override string Emoji => "🐯";

        /// <summary>
        /// Tigers are powerful, agile hunters.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Active;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Orange,
                PetColor.White,
                PetColor.Gold
            ];
        }

        protected override string GenerateDefaultName()
        {
            return TigerNames[NameRandom.Next(TigerNames.Length)];
        }

        /// <summary>
        /// Creates a random tiger with a random color.
        /// </summary>
        public static Tiger CreateRandom()
        {
            PetColor[] colors = [PetColor.Orange, PetColor.White, PetColor.Gold];
            PetColor color = colors[NameRandom.Next(colors.Length)];
            return new Tiger(color);
        }
    }
}
