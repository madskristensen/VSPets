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
        {
            "Buddy", "Max", "Charlie", "Cooper", "Rocky", "Bear", "Duke",
            "Tucker", "Jack", "Bailey", "Buster", "Cody", "Jake", "Murphy",
            "Lucky", "Scout", "Rusty", "Oscar", "Winston", "Zeus", "Finn",
            "Rex", "Bruno", "Archie", "Toby", "Dexter", "Gus", "Louie"
        };

        private static readonly Random NameRandom = new Random();

        public Dog(PetColor color = PetColor.Brown, string name = null) 
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Dog;

        public override string HelloMessage => "Woof! 🐕";

        public override string Emoji => "🐕";

        /// <summary>
        /// Dogs can't climb as well as cats.
        /// </summary>
        public override bool CanClimb => false;

        public override PetColor[] GetPossibleColors()
        {
            return new[]
            {
                PetColor.Black,
                PetColor.White,
                PetColor.Brown,
                PetColor.Red,     // Shiba
                PetColor.Akita
            };
        }

        protected override string GenerateDefaultName()
        {
            return DogNames[NameRandom.Next(DogNames.Length)];
        }

        /// <summary>
        /// Dog-specific sprite mapping - dogs show happiness differently.
        /// </summary>
        protected override string GetSpriteLabel(PetState state)
        {
            // Dogs use 'swipe' animation for happy (tail wag effect)
            return base.GetSpriteLabel(state);
        }

        /// <summary>
        /// Creates a random dog with a random color.
        /// </summary>
        public static Dog CreateRandom()
        {
            var colors = new[] { PetColor.Black, PetColor.White, PetColor.Brown, PetColor.Red };
            var color = colors[NameRandom.Next(colors.Length)];
            return new Dog(color);
        }
    }
}
