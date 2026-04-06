using System;
using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Dog pet implementation with dog-specific behaviors and attributes.
    /// </summary>
    public class Dog : BasePet
    {
        private static readonly string[] DogNames =
        [
            "Buddy", "Max", "Charlie", "Cooper", "Rocky", "Bear", "Duke",
            "Tucker", "Jack", "Bailey", "Buster", "Cody", "Jake", "Murphy",
            "Lucky", "Scout", "Rusty", "Oscar", "Winston", "Zeus", "Finn",
            "Rex", "Bruno", "Archie", "Toby", "Dexter", "Gus", "Louie"
        ];

        private static readonly Random NameRandom = new();

        public Dog(PetColor color = PetColor.Brown, string name = null) 
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Dog;

        public override string HelloMessage => "Woof! 🐕";

        public override string Emoji => "🐕";

        /// <summary>
        /// Dogs are energetic and love to run around.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Active;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Black,
                PetColor.White,
                PetColor.Brown,
                PetColor.Red,     // Shiba
                PetColor.Akita
            ];
        }

        protected override string GenerateDefaultName()
        {
            return DogNames[NameRandom.Next(DogNames.Length)];
        }

        /// <summary>
        /// Creates a random dog with a random color.
        /// </summary>
        public static Dog CreateRandom()
        {
            PetColor[] colors = [PetColor.Black, PetColor.White, PetColor.Brown, PetColor.Red];
            PetColor color = colors[NameRandom.Next(colors.Length)];
            return new Dog(color);
        }
    }
}
