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

    /// <summary>
    /// Turtle pet - slow and steady wins the race!
    /// </summary>
    public class Turtle : BasePet
    {
        private static readonly string[] _turtleNames =
        [
            "Shelly", "Tank", "Speedy", "Turbo", "Donatello", "Leonardo",
            "Raphael", "Michelangelo", "Crush", "Squirt", "Shelldon", "Franklin",
            "Myrtle", "Tortuga", "Koopa", "Bowser", "Terrapin", "Snapper"
        ];

        private static readonly Random _nameRandom = new();

        public Turtle(PetColor color = PetColor.Green, string name = null)
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Turtle;

        public override string HelloMessage => "Slow and steady! 🐢";

        public override string Emoji => "🐢";

        /// <summary>
        /// Turtle sprite faces right by default.
        /// </summary>
        public override bool FacesLeftByDefault => false;

        /// <summary>
        /// Turtles are slow but determined.
        /// </summary>
        public override bool CanClimb => false;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Green,
                PetColor.Brown,
                PetColor.Gray
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _turtleNames[_nameRandom.Next(_turtleNames.Length)];
        }

        public static Turtle CreateRandom()
        {
            PetColor[] colors = [PetColor.Green, PetColor.Brown, PetColor.Gray];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Turtle(color);
        }
    }

    /// <summary>
    /// Bunny pet - hopping around the status bar!
    /// </summary>
    public class Bunny : BasePet
    {
        private static readonly string[] _bunnyNames =
        [
            "Thumper", "Flopsy", "Cotton", "Snowball", "Bun Bun", "Hoppy",
            "Cinnabun", "Pepper", "Clover", "Hazel", "Dusty", "Nibbles",
            "Butterscotch", "Cocoa", "Oreo", "Patches", "Peanut", "Binky"
        ];

        private static readonly Random _nameRandom = new();

        public Bunny(PetColor color = PetColor.White, string name = null)
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Bunny;

        public override string HelloMessage => "Hop hop! 🐰";

        public override string Emoji => "🐰";

        /// <summary>
        /// Bunny sprite faces right by default.
        /// </summary>
        public override bool FacesLeftByDefault => false;

        public override bool CanClimb => false;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.White,
                PetColor.Brown,
                PetColor.Gray,
                PetColor.Black,
                PetColor.LightBrown,
                PetColor.Pink
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _bunnyNames[_nameRandom.Next(_bunnyNames.Length)];
        }

        public static Bunny CreateRandom()
        {
            PetColor[] colors = [PetColor.White, PetColor.Brown, PetColor.Gray, PetColor.Black, PetColor.LightBrown];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Bunny(color);
        }
    }

    /// <summary>
    /// Raccoon pet - mischievous little trash panda!
    /// </summary>
    public class Raccoon : BasePet
    {
        private static readonly string[] _raccoonNames =
        [
            "Bandit", "Rocket", "Rascal", "Sly", "Sneaky", "Patches",
            "Trash Panda", "Ringtail", "Ringo", "Shadow", "Masked",
            "Rocky", "Meeko", "Gizmo", "Scooter", "Whiskers", "Chester"
        ];

        private static readonly Random _nameRandom = new();

        public Raccoon(PetColor color = PetColor.Gray, string name = null)
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Raccoon;

        public override string HelloMessage => "Got any snacks? 🦝";

        public override string Emoji => "🦝";

        public override bool CanClimb => true;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Gray,
                PetColor.DarkGray
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _raccoonNames[_nameRandom.Next(_raccoonNames.Length)];
        }

        public static Raccoon CreateRandom()
        {
            PetColor[] colors = [PetColor.Gray, PetColor.DarkGray];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Raccoon(color);
        }
    }

    /// <summary>
    /// Wolf pet - majestic and loyal!
    /// </summary>
    public class Wolf : BasePet
    {
        private static readonly string[] _wolfNames =
        [
            "Shadow", "Luna", "Ghost", "Fang", "Storm", "Winter",
            "Timber", "Frost", "Ash", "Blaze", "Hunter", "Dakota",
            "Sierra", "Kodiak", "Midnight", "Silver", "Nymeria", "Grey Wind"
        ];

        private static readonly Random _nameRandom = new();

        public Wolf(PetColor color = PetColor.Gray, string name = null)
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.Wolf;

        public override string HelloMessage => "Awoooo! 🐺";

        public override string Emoji => "🐺";

        public override bool CanClimb => false;

        /// <summary>
        /// Wolf sprite faces left by default.
        /// </summary>
        public override bool FacesLeftByDefault => true;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Gray,
                PetColor.White,
                PetColor.DarkGray
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _wolfNames[_nameRandom.Next(_wolfNames.Length)];
        }

        public static Wolf CreateRandom()
        {
            PetColor[] colors = [PetColor.Gray, PetColor.White, PetColor.DarkGray];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Wolf(color);
        }
    }

    /// <summary>
    /// T-Rex pet - tiny arms, big personality!
    /// </summary>
    public class TRex : BasePet
    {
        private static readonly string[] _trexNames =
        [
            "Rex", "Rexy", "Chomper", "Tiny", "Spike", "Dino",
            "T-Bone", "Raptor", "Tyrant", "Crusher", "Fang", "Scales",
            "Boulder", "Stomp", "Thunder", "Roary", "Godzilla", "Reptar"
        ];

        private static readonly Random _nameRandom = new();

        public TRex(PetColor color = PetColor.Green, string name = null)
            : base(color, name)
        {
        }

        public override PetType PetType => PetType.TRex;

        public override string HelloMessage => "RAWR! 🦖";

        public override string Emoji => "🦖";

        /// <summary>
        /// Those tiny arms can't climb much.
        /// </summary>
        public override bool CanClimb => false;

        /// <summary>
        /// T-Rex faces right by default in our sprite.
        /// </summary>
        public override bool FacesLeftByDefault => false;

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Green,
                PetColor.Brown,
                PetColor.Purple,
                PetColor.Red,
                PetColor.Orange
            ];
        }

        protected override string GenerateDefaultName()
        {
            return _trexNames[_nameRandom.Next(_trexNames.Length)];
        }

        public static TRex CreateRandom()
        {
            PetColor[] colors = [PetColor.Green, PetColor.Brown, PetColor.Purple, PetColor.Red];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new TRex(color);
        }
    }
}
