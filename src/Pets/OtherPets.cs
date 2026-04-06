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

        /// <summary>
        /// Foxes are quick and agile.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Active;

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
        private static readonly Random _tipRandom = new();

        private static readonly string[] _clippyBehaviors =
        [
            "tip", "wave", "bounce", "blink", "lean", "wiggle_eyebrows"
        ];

        private static readonly string[] _codingTips =
        [
            "Ctrl+. opens Quick Actions! 💡",
            "Try Ctrl+Shift+P for commands 📎",
            "Don't forget to commit! 💾",
            "Have you written a test yet? 🧪",
            "Psst… Ctrl+K, Ctrl+D formats code ✨",
            "F12 goes to definition! 🔍",
            "Remember to take a break! ☕",
            "Ctrl+Shift+F finds in all files 📂",
            "Alt+Enter is your friend! 🙌",
            "It looks like you're writing a loop. Need help? 🔁",
        ];

        public override PetType PetType => PetType.Clippy;

        public override string HelloMessage => "It looks like you're writing code. Would you like help? 📎";

        public override string Emoji => "📎";

        public override PetColor[] GetPossibleColors()
        {
            return [PetColor.Original];
        }

        protected override string GenerateDefaultName()
        {
            return "Clippy";
        }

        /// <summary>
        /// Clippy-specific idle behaviors instead of animal ones.
        /// </summary>
        public override string[] GetPossibleBehaviors()
        {
            return _clippyBehaviors;
        }

        protected override int GetBehaviorDuration(string behavior)
        {
            return behavior switch
            {
                "tip" => 3000,
                "wave" => 1500,
                "bounce" => 1000,
                "blink" => 600,
                "lean" => 2000,
                "wiggle_eyebrows" => 1200,
                _ => base.GetBehaviorDuration(behavior)
            };
        }

        protected override string GetBehaviorSpeech(string behavior)
        {
            return behavior switch
            {
                "tip" => _codingTips[_tipRandom.Next(_codingTips.Length)],
                "wave" => "👋",
                "bounce" => "😄",
                "blink" => null,
                "lean" => "👀",
                "wiggle_eyebrows" => "🧐",
                _ => base.GetBehaviorSpeech(behavior)
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
        /// Rubber ducks waddle at a leisurely pace.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Slow;

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
    public class Turtle(PetColor color = PetColor.Green, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _turtleNames =
        [
            "Shelly", "Tank", "Speedy", "Turbo", "Donatello", "Leonardo",
            "Raphael", "Michelangelo", "Crush", "Squirt", "Shelldon", "Franklin",
            "Myrtle", "Tortuga", "Koopa", "Bowser", "Terrapin", "Snapper"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Turtle;

        public override string HelloMessage => "Slow and steady! 🐢";

        public override string Emoji => "🐢";

        /// <summary>
        /// Turtles are very slow - slow and steady wins the race!
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Lazy;

        /// <summary>
        /// Turtle sprite faces right by default.
        /// </summary>
        public override bool FacesLeftByDefault => false;

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
    public class Bunny(PetColor color = PetColor.White, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _bunnyNames =
        [
            "Thumper", "Flopsy", "Cotton", "Snowball", "Bun Bun", "Hoppy",
            "Cinnabun", "Pepper", "Clover", "Hazel", "Dusty", "Nibbles",
            "Butterscotch", "Cocoa", "Oreo", "Patches", "Peanut", "Binky"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Bunny;

        public override string HelloMessage => "Hop hop! 🐰";

        public override string Emoji => "🐰";

        /// <summary>
        /// Bunnies are quick little hoppers.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Active;

        /// <summary>
        /// Bunny sprite faces right by default.
        /// </summary>
        public override bool FacesLeftByDefault => false;

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
    public class Raccoon(PetColor color = PetColor.Gray, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _raccoonNames =
        [
            "Bandit", "Rocket", "Rascal", "Sly", "Sneaky", "Patches",
            "Trash Panda", "Ringtail", "Ringo", "Shadow", "Masked",
            "Rocky", "Meeko", "Gizmo", "Scooter", "Whiskers", "Chester"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Raccoon;

        public override string HelloMessage => "Got any snacks? 🦝";

        public override string Emoji => "🦝";

        /// <summary>
        /// Raccoons move at a moderate, opportunistic pace.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Normal;

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
    public class Wolf(PetColor color = PetColor.Gray, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _wolfNames =
        [
            "Shadow", "Luna", "Ghost", "Fang", "Storm", "Winter",
            "Timber", "Frost", "Ash", "Blaze", "Hunter", "Dakota",
            "Sierra", "Kodiak", "Midnight", "Silver", "Nymeria", "Grey Wind"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.Wolf;

        public override string HelloMessage => "Awoooo! 🐺";

        public override string Emoji => "🐺";

        /// <summary>
        /// Wolves are fast pack hunters.
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Active;

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
    public class TRex(PetColor color = PetColor.Green, string name = null) : BasePet(color, name)
    {
        private static readonly string[] _trexNames =
        [
            "Rex", "Rexy", "Chomper", "Tiny", "Spike", "Dino",
            "T-Bone", "Raptor", "Tyrant", "Crusher", "Fang", "Scales",
            "Boulder", "Stomp", "Thunder", "Roary", "Godzilla", "Reptar"
        ];

        private static readonly Random _nameRandom = new();

        public override PetType PetType => PetType.TRex;

        public override string HelloMessage => "RAWR! 🦖";

        public override string Emoji => "🦖";

        /// <summary>
        /// T-Rex is a terrifyingly fast apex predator!
        /// </summary>
        public override PetSpeed NaturalSpeed => PetSpeed.Hyper;

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
