using System;
using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Cat pet implementation with cat-specific behaviors and attributes.
    /// </summary>
    public class Cat : BasePet
    {
        private static readonly string[] CatNames =
        {
            "Whiskers", "Luna", "Milo", "Bella", "Oliver", "Leo", "Cleo",
            "Simba", "Shadow", "Nala", "Felix", "Ginger", "Mittens", "Smokey",
            "Tiger", "Misty", "Salem", "Oreo", "Patches", "Socks", "Pumpkin",
            "Binx", "Ghost", "Midnight", "Snowball", "Garfield", "Tom", "Sylvester"
        };

        private static readonly Random NameRandom = new();

        public Cat(PetColor color = PetColor.Orange, string name = null) 
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Cat;

        public override string HelloMessage => "Meow! 🐱";

        public override string Emoji => "🐱";

        /// <summary>
        /// Cats can climb and are good at it!
        /// </summary>
        public override bool CanClimb => true;

        public override PetColor[] GetPossibleColors()
        {
            return new[]
            {
                PetColor.Black,
                PetColor.White,
                PetColor.Brown,
                PetColor.Gray,
                PetColor.Orange,
                PetColor.LightBrown
            };
        }

        protected override string GenerateDefaultName()
        {
            return CatNames[NameRandom.Next(CatNames.Length)];
        }

        /// <summary>
        /// Creates a random cat with a random color.
        /// </summary>
        public static Cat CreateRandom()
        {
            PetColor[] colors = new[] { PetColor.Black, PetColor.White, PetColor.Orange, PetColor.Gray, PetColor.Brown };
            PetColor color = colors[NameRandom.Next(colors.Length)];
            return new Cat(color);
        }
    }
}
