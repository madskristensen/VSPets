using System;
using System.Collections.Generic;
using VSPets.Models;

namespace VSPets.Animation
{
    /// <summary>
    /// Defines the behavior state machine for pets walking on the status bar.
    /// Contains transition rules and probabilities.
    /// </summary>
    public class PetStateMachine
    {
        private readonly Random _random = new Random();
        private readonly Dictionary<PetState, StateTransition[]> _transitions;

        /// <summary>
        /// Current state of the pet.
        /// </summary>
        public PetState CurrentState { get; private set; }

        /// <summary>
        /// Time remaining in the current state (seconds).
        /// </summary>
        public double StateTimeRemaining { get; private set; }

        /// <summary>
        /// Event fired when the state changes.
        /// </summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;

        public PetStateMachine(PetState initialState = PetState.Idle)
        {
            _transitions = BuildTransitionTable();
            CurrentState = initialState;
            StateTimeRemaining = GetStateDuration(initialState);
        }

        /// <summary>
        /// Updates the state machine. Call this every frame.
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds.</param>
        /// <param name="context">Context for making state decisions.</param>
        /// <returns>True if state changed.</returns>
        public bool Update(double deltaTime, StateContext context)
        {
            StateTimeRemaining -= deltaTime;

            if (StateTimeRemaining <= 0)
            {
                var newState = ChooseNextState(context);
                if (newState != CurrentState)
                {
                    var oldState = CurrentState;
                    CurrentState = newState;
                    StateTimeRemaining = GetStateDuration(newState);

                    StateChanged?.Invoke(this, new StateChangedEventArgs
                    {
                        OldState = oldState,
                        NewState = newState
                    });

                    return true;
                }

                StateTimeRemaining = GetStateDuration(CurrentState);
            }

            return false;
        }

        /// <summary>
        /// Forces a specific state.
        /// </summary>
        public void ForceState(PetState state)
        {
            var oldState = CurrentState;
            CurrentState = state;
            StateTimeRemaining = GetStateDuration(state);

            if (oldState != state)
            {
                StateChanged?.Invoke(this, new StateChangedEventArgs
                {
                    OldState = oldState,
                    NewState = state
                });
            }
        }

        /// <summary>
        /// Temporarily enters a state and returns to the previous state when done.
        /// </summary>
        public void TriggerTemporaryState(PetState temporaryState, PetState returnState)
        {
            var oldState = CurrentState;
            CurrentState = temporaryState;
            StateTimeRemaining = GetStateDuration(temporaryState);

            StateChanged?.Invoke(this, new StateChangedEventArgs
            {
                OldState = oldState,
                NewState = temporaryState,
                ReturnState = returnState
            });
        }

        private PetState ChooseNextState(StateContext context)
        {
            if (!_transitions.TryGetValue(CurrentState, out var possibleTransitions))
            {
                return PetState.Idle;
            }

            // Roll for random transition
            var roll = _random.NextDouble();
            double cumulativeProbability = 0;

            foreach (var transition in possibleTransitions)
            {
                cumulativeProbability += transition.Probability;
                if (roll <= cumulativeProbability)
                {
                    return transition.TargetState;
                }
            }

            // Fallback
            return possibleTransitions.Length > 0 
                ? possibleTransitions[possibleTransitions.Length - 1].TargetState 
                : PetState.Idle;
        }

        private double GetStateDuration(PetState state)
        {
            return state switch
            {
                PetState.Idle => RandomRange(2.0, 8.0),
                PetState.Walking => RandomRange(3.0, 12.0),
                PetState.Running => RandomRange(2.0, 6.0),
                PetState.Sleeping => RandomRange(5.0, 20.0),
                PetState.Happy => 1.5,
                _ => 3.0
            };
        }

        private double RandomRange(double min, double max)
        {
            return min + _random.NextDouble() * (max - min);
        }

        private Dictionary<PetState, StateTransition[]> BuildTransitionTable()
        {
            return new Dictionary<PetState, StateTransition[]>
            {
                [PetState.Idle] = new[]
                {
                    new StateTransition(PetState.Idle, 0.35),
                    new StateTransition(PetState.Walking, 0.35),
                    new StateTransition(PetState.Running, 0.15),
                    new StateTransition(PetState.Sleeping, 0.15)
                },

                [PetState.Walking] = new[]
                {
                    new StateTransition(PetState.Walking, 0.40),
                    new StateTransition(PetState.Idle, 0.30),
                    new StateTransition(PetState.Running, 0.20),
                    new StateTransition(PetState.Sleeping, 0.10)
                },

                [PetState.Running] = new[]
                {
                    new StateTransition(PetState.Walking, 0.40),
                    new StateTransition(PetState.Idle, 0.35),
                    new StateTransition(PetState.Running, 0.25)
                },

                [PetState.Sleeping] = new[]
                {
                    new StateTransition(PetState.Sleeping, 0.60),
                    new StateTransition(PetState.Idle, 0.40)
                },

                [PetState.Happy] = new[]
                {
                    new StateTransition(PetState.Idle, 1.0)
                }
            };
        }
    }

    /// <summary>
    /// Represents a possible state transition.
    /// </summary>
    public struct StateTransition
    {
        public PetState TargetState { get; }
        public double Probability { get; }

        public StateTransition(PetState targetState, double probability)
        {
            TargetState = targetState;
            Probability = probability;
        }
    }

    /// <summary>
    /// Context information for state machine decisions.
    /// </summary>
    public class StateContext
    {
        public bool IsAtLeftEdge { get; set; }
        public bool IsAtRightEdge { get; set; }
        public bool IsHovered { get; set; }
    }

    /// <summary>
    /// Event arguments for state changes.
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        public PetState OldState { get; set; }
        public PetState NewState { get; set; }
        public PetState? ReturnState { get; set; }
    }
}
