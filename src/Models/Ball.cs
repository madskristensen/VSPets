namespace VSPets.Models
{
    /// <summary>
    /// Represents a ball that pets can chase.
    /// </summary>
    public class Ball
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Gets or sets the X position of the ball.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the horizontal velocity (pixels per second).
        /// </summary>
        public double Velocity { get; set; }

        /// <summary>
        /// Gets or sets whether the ball is active (visible and moving).
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the current state of the ball.
        /// </summary>
        public BallState State { get; set; }

        /// <summary>
        /// Gets or sets the pet currently chasing this ball, if any.
        /// </summary>
        public Guid? ChasingPetId { get; set; }

        /// <summary>
        /// Gets the size of the ball sprite.
        /// </summary>
        public int Size => 16;

        /// <summary>
        /// Friction applied to velocity each frame (0-1, where 1 = no friction).
        /// </summary>
        private const double _friction = 0.985;

        /// <summary>
        /// Minimum velocity before ball stops.
        /// </summary>
        private const double _minVelocity = 5.0;

        /// <summary>
        /// Creates a new ball at the specified position.
        /// </summary>
        /// <param name="x">Starting X position.</param>
        /// <param name="throwRight">If true, ball goes right; if false, goes left; if null, random direction.</param>
        public Ball(double x, bool? throwRight = null)
        {
            X = x;
            IsActive = true;
            State = BallState.Rolling;

            // Determine direction
            var goRight = throwRight ?? _random.Next(2) == 0;

            // Initial velocity (150-250 pixels per second)
            Velocity = (150 + _random.Next(100)) * (goRight ? 1 : -1);
        }

        /// <summary>
        /// Updates the ball physics.
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds.</param>
        /// <param name="canvasWidth">Width of the canvas for boundary checks.</param>
        public void Update(double deltaTime, double canvasWidth)
        {
            if (!IsActive || State == BallState.Caught)
            {
                return;
            }

            // Apply velocity
            X += Velocity * deltaTime;

            // Apply friction
            Velocity *= _friction;

            // Bounce off edges
            if (X < 0)
            {
                X = 0;
                Velocity = Math.Abs(Velocity) * 0.7; // Bounce with energy loss
            }
            else if (X > canvasWidth - Size)
            {
                X = canvasWidth - Size;
                Velocity = -Math.Abs(Velocity) * 0.7; // Bounce with energy loss
            }

            // Stop if velocity is very low
            if (Math.Abs(Velocity) < _minVelocity)
            {
                Velocity = 0;
                State = BallState.Stopped;
            }
        }

        /// <summary>
        /// Called when a pet catches the ball.
        /// </summary>
        public void Catch()
        {
            State = BallState.Caught;
            Velocity = 0;
            IsActive = false;
        }
    }

    /// <summary>
    /// States for the ball.
    /// </summary>
    public enum BallState
    {
        /// <summary>
        /// Ball is rolling/bouncing.
        /// </summary>
        Rolling,

        /// <summary>
        /// Ball has stopped moving.
        /// </summary>
        Stopped,

        /// <summary>
        /// Ball was caught by a pet.
        /// </summary>
        Caught
    }
}
