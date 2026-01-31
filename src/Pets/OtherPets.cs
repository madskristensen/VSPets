using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Fox pet implementation.
    /// </summary>
    public class Fox(PetColor color = PetColor.Red, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _foxNames =
        [
            "Firefox", "Foxy", "Rusty", "Copper", "Amber", "Ginger",
            "Scout", "Blaze", "Autumn", "Maple", "Hazel", "Crimson",
            "Robin", "Swift", "Dash", "Finn", "Redd", "Scarlet"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Fox;

        public override string HelloMessage => "Yip! 🦊";

        public override string Emoji => "🦊";

        public override bool CanClimb => true;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Red,   // Red fox
                PetColor.White  // Arctic fox
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _foxNames[_nameRandom.Next(_foxNames.Length)];
        }

        public static Fox CreateRandom()
        {
            PetColor[] colors = [PetColor.Red, PetColor.White];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Fox(color);
        }
    }

    /// <summary>
    /// Clippy pet - the classic Office assistant!
    /// </summary>
    public class Clippy(string name = null) : BasePet(PetColor.Original, name ?? "Clippy")
    {
        public override PetType PetType => PetType.Clippy;

        public override string HelloMessage => "It looks like you're writing code. Would you like help? 📎";

        public override string Emoji => "📎";

        public override bool CanClimb => true;

        public override PetColor[] GetPossibleColors()
        {
            return [PetColor.Original];
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
        private static readonly Random _colorRandom = new();

        public RubberDuck(string name = null)
            : base(PetColor.Yellow, name ?? "Ducky")
        {
        }

        public RubberDuck(PetColor color, string name = null)
            : base(color, name ?? "Ducky")
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
            return
            [
                PetColor.Yellow,
                PetColor.White,
                PetColor.Black,
                PetColor.Blue,
                PetColor.Pink,
                PetColor.Gold,
                PetColor.Orange
            ];
        }

        protected override string GenerateDefaultName()
        {
            return "Ducky";
        }

        public static RubberDuck CreateRandom()
        {
            PetColor[] colors =
            [
                PetColor.Yellow,
                PetColor.White,
                PetColor.Black,
                PetColor.Blue,
                PetColor.Pink,
                PetColor.Gold,
                PetColor.Orange
            ];

            PetColor color = colors[_colorRandom.Next(colors.Length)];
            return new RubberDuck(color);
        }
    }
}
