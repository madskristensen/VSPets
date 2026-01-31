using System;
using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Fox pet implementation.
    /// </summary>
    public class Fox : BasePet
    {
        private static readonly string[] FoxNames =
        {
            "Firefox", "Foxy", "Rusty", "Copper", "Amber", "Ginger",
            "Scout", "Blaze", "Autumn", "Maple", "Hazel", "Crimson",
            "Robin", "Swift", "Dash", "Finn", "Redd", "Scarlet"
        };

        private static readonly Random NameRandom = new Random();

        public Fox(PetColor color = PetColor.Red, string name = null) 
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Fox;

        public override string HelloMessage => "Yip! 🦊";

        public override string Emoji => "🦊";

        public override bool CanClimb => true;

        public override PetColor[] GetPossibleColors()
        {
            return new[]
            {
                PetColor.Red,   // Red fox
                PetColor.White  // Arctic fox
            };
        }

        protected override string GenerateDefaultName()
        {
            return FoxNames[NameRandom.Next(FoxNames.Length)];
        }

        public static Fox CreateRandom()
        {
            var colors = new[] { PetColor.Red, PetColor.White };
            var color = colors[NameRandom.Next(colors.Length)];
            return new Fox(color);
        }
    }

    /// <summary>
    /// Clippy pet - the classic Office assistant!
    /// </summary>
    public class Clippy : BasePet
    {
        public Clippy(string name = null) 
            : base(PetColor.Original, name ?? "Clippy")
        {
        }

        public override PetType PetType => PetType.Clippy;

        public override string HelloMessage => "It looks like you're writing code. Would you like help? 📎";

        public override string Emoji => "📎";

        public override bool CanClimb => true;

        public override PetColor[] GetPossibleColors()
        {
            return new[] { PetColor.Original };
        }

        protected override string GenerateDefaultName()
        {
            return "Clippy";
        }

        protected override string GetSpriteLabel(PetState state)
        {
            // Clippy has different animation names
            return state switch
            {
                PetState.Idle => "idle",
                PetState.Walking => "walk",
                PetState.Running => "run",
                PetState.Happy => "wave",
                _ => base.GetSpriteLabel(state)
            };
        }
    }

    /// <summary>
    /// Rubber Duck pet - for debugging companionship!
    /// </summary>
    public class RubberDuck : BasePet
    {
        public RubberDuck(string name = null) 
            : base(PetColor.Yellow, name ?? "Ducky")
        {
        }

        public override PetType PetType => PetType.RubberDuck;

        public override string HelloMessage => "Quack! Tell me about your bug... 🦆";

        public override string Emoji => "🦆";

        /// <summary>
        /// Rubber ducks don't climb - they float!
        /// </summary>
        public override bool CanClimb => false;

        public override PetColor[] GetPossibleColors()
        {
            return new[] { PetColor.Yellow };
        }

        protected override string GenerateDefaultName()
        {
            return "Ducky";
        }
    }
}
