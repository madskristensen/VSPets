using VSPets.Models;

namespace VSPets.Pets
{
    /// <summary>
    /// Bear pet implementation.
    /// </summary>
    public class Bear : BasePet
    {
        private static readonly string[] _bearNames =
        [
            "Teddy", "Grizz", "Paddington", "Winnie", "Baloo", "Koda",
            "Smokey", "Yogi", "Boo Boo", "Fozzie", "Corduroy", "Barnaby",
            "Ursa", "Bjorn", "Kenai"
        ];

        private static readonly Random _nameRandom = new();

        public Bear(PetColor color = PetColor.Brown, string name = null)
            : base(color, name)
        {
            // Bears are bigger!
            Size = PetSize.Medium; // Or maybe introduce Large? BasePet has Size property.
            // Let's stick to user default for now but maybe default larger?
            // Actually Size is initialized in BasePet to Small. 
            // We can override default in constructor.
            Size = PetSize.Medium; // Make bears medium by default
        }

        public override PetType PetType => PetType.Bear;

        public override string HelloMessage => "Roar! 🐻";

        public override string Emoji => "🐻";

        public override bool CanClimb => false; // Bears don't climb walls in IDE usually? Or do they? 
        // Real bears climb trees. Let's say false to differ from cats/dogs/others or true?
        // Let's keep false for variety if others are true.

        public override PetColor[] GetPossibleColors()
        {
            return
            [
                PetColor.Brown, // Grizzly
                PetColor.Black, // Black bear
                PetColor.White  // Polar bear
            ];
        }

        public override bool FacesLeftByDefault => false; // Bear art faces right

        protected override string GenerateDefaultName()
        {
            return _bearNames[_nameRandom.Next(_bearNames.Length)];
        }

        public static Bear CreateRandom()
        {
            PetColor[] colors = [PetColor.Brown, PetColor.Black, PetColor.White];
            PetColor color = colors[_nameRandom.Next(colors.Length)];
            return new Bear(color);
        }
    }
}
