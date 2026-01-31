using System.Windows;
using VSPets.Models;

namespace VSPets.Pets
{

    /// <summary>
    /// Interface for all pet implementations.
    /// </summary>
    public interface IPet : IDisposable
    {
        /// <summary>
        /// Unique identifier for this pet instance.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// User-assigned name for the pet.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Type of pet (Cat, Dog, etc.).
        /// </summary>
        PetType PetType { get; }

        /// <summary>
        /// Color/variant of the pet.
        /// </summary>
        PetColor Color { get; }

        /// <summary>
        /// Size of the pet sprite.
        /// </summary>
        PetSize Size { get; set; }

        /// <summary>
        /// Speed behavior preset.
        /// </summary>
        PetSpeed SpeedSetting { get; set; }

        /// <summary>
        /// Current behavioral state.
        /// </summary>
        PetState CurrentState { get; }

        /// <summary>
        /// Direction the pet is facing.
        /// </summary>
        PetDirection Direction { get; }

        /// <summary>
        /// Current X position (left edge).
        /// </summary>
        double X { get; }

        /// <summary>
        /// Current Y position (distance from floor, 0 = on ground).
        /// </summary>
        double Y { get; }

        /// <summary>
        /// The WPF control representing this pet.
        /// </summary>
        FrameworkElement Control { get; }

        /// <summary>
        /// Whether the pet can climb tool windows.
        /// </summary>
        bool CanClimb { get; }

        /// <summary>
        /// Greeting/hello message for this pet type.
        /// </summary>
        string HelloMessage { get; }

        /// <summary>
        /// Emoji representing this pet type.
        /// </summary>
        string Emoji { get; }

        /// <summary>
        /// Updates the pet's state and position for one animation frame.
        /// </summary>
        /// <param name="deltaTime">Time since last frame in seconds.</param>
        /// <param name="canvasWidth">Width of the canvas/boundary.</param>
        void Update(double deltaTime, double canvasWidth);

        /// <summary>
        /// Moves the pet to a specific position.
        /// </summary>
        void SetPosition(double x, double y);

        /// <summary>
        /// Triggers the happy/smile animation (e.g., on mouse hover).
        /// </summary>
        void TriggerHappy();

        /// <summary>
        /// Forces the pet into a specific state.
        /// </summary>
        void SetState(PetState state);

        /// <summary>
        /// Changes the pet's facing direction.
        /// </summary>
        void SetDirection(PetDirection direction);

        /// <summary>
        /// Whether the pet is currently being dragged.
        /// </summary>
        bool IsDragging { get; }

        /// <summary>
        /// Starts dragging the pet.
        /// </summary>
        void StartDrag();

        /// <summary>
        /// Ends dragging and resumes normal behavior.
        /// </summary>
        void EndDrag();

        /// <summary>
        /// Event fired when the pet's state changes.
        /// </summary>
        event EventHandler<PetStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Event fired when the pet's position changes.
        /// </summary>
        event EventHandler<PetPositionChangedEventArgs> PositionChanged;

        /// <summary>
        /// Event fired when the pet wants to display a speech bubble.
        /// </summary>
        event EventHandler<PetSpeechEventArgs> Speech;
    }

    /// <summary>
    /// Event arguments for pet state changes.
    /// </summary>
    public class PetStateChangedEventArgs : EventArgs
    {
        public PetState OldState { get; set; }
        public PetState NewState { get; set; }
    }

    /// <summary>
    /// Event arguments for pet position changes.
    /// </summary>
    public class PetPositionChangedEventArgs : EventArgs
    {
        public double OldX { get; set; }
        public double OldY { get; set; }
        public double NewX { get; set; }
        public double NewY { get; set; }
    }

    /// <summary>
    /// Event arguments for pet speech bubbles.
    /// </summary>
    public class PetSpeechEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int DurationMs { get; set; } = 3000;
    }

    /// <summary>
    /// Event arguments for pet random behaviors (yawn, stretch, etc.).
    /// </summary>
    public class PetBehaviorEventArgs : EventArgs
    {
        /// <summary>
        /// The behavior being performed (e.g., "yawn", "stretch", "look_around").
        /// </summary>
        public string Behavior { get; set; }

        /// <summary>
        /// How long the behavior lasts in milliseconds.
        /// </summary>
        public int DurationMs { get; set; } = 1000;
    }

    /// <summary>
    /// Event arguments for animation frame changes.
    /// </summary>
    public class PetFrameChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The new frame index.
        /// </summary>
        public int Frame { get; set; }

        /// <summary>
        /// Current animation state.
        /// </summary>
        public PetState State { get; set; }
    }
}
