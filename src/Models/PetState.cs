namespace VSPets.Models
{
    /// <summary>
    /// States in the pet behavior state machine.
    /// </summary>
    public enum PetState
    {
        /// <summary>
        /// Pet is sitting still, idle animation playing.
        /// </summary>
        Idle,

        /// <summary>
        /// Pet is walking slowly.
        /// </summary>
        Walking,

        /// <summary>
        /// Pet is running/moving quickly.
        /// </summary>
        Running,

        /// <summary>
        /// Pet is lying down, resting.
        /// </summary>
        Sleeping,

        /// <summary>
        /// Pet is showing happiness (triggered by hover).
        /// </summary>
        Happy,

        /// <summary>
        /// Pet is walking off the edge of the screen.
        /// </summary>
        Exiting,

        /// <summary>
        /// Pet is walking back onto the screen from an edge.
        /// </summary>
        Entering,

        /// <summary>
        /// Pet is being dragged by the user.
        /// </summary>
        Dragging,

        /// <summary>
        /// Pet is chasing a ball.
        /// </summary>
        Chasing
    }

    /// <summary>
    /// Direction the pet is facing/moving.
    /// </summary>
    public enum PetDirection
    {
        /// <summary>
        /// Facing/moving left.
        /// </summary>
        Left,

        /// <summary>
        /// Facing/moving right.
        /// </summary>
        Right
    }
}
