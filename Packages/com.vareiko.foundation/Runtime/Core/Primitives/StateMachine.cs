using System;
using System.Collections.Generic;

namespace Vareiko.Foundation
{
    /// <summary>
    /// Minimal generic finite state machine: a current state, an optional transition guard, and
    /// a change event. <c>AppStateMachine</c> builds the application lifecycle on top of this;
    /// gameplay code can use it for any state set (enums, ids, references).
    /// </summary>
    public sealed class StateMachine<TState>
    {
        private readonly Func<TState, TState, bool> _transitionGuard;
        private readonly IEqualityComparer<TState> _comparer;

        public StateMachine(TState initial, Func<TState, TState, bool> transitionGuard = null, IEqualityComparer<TState> comparer = null)
        {
            Current = initial;
            _transitionGuard = transitionGuard;
            _comparer = comparer ?? EqualityComparer<TState>.Default;
        }

        public TState Current { get; private set; }

        /// <summary>Raised after every entered transition with (previous, current).</summary>
        public event Action<TState, TState> StateChanged;

        public bool IsIn(TState state)
        {
            return _comparer.Equals(Current, state);
        }

        /// <summary>
        /// Enters <paramref name="next"/> unless it equals the current state or the guard
        /// rejects the transition.
        /// </summary>
        public bool TryEnter(TState next)
        {
            if (_comparer.Equals(Current, next))
            {
                return false;
            }

            if (_transitionGuard != null && !_transitionGuard(Current, next))
            {
                return false;
            }

            ForceEnter(next);
            return true;
        }

        /// <summary>Enters <paramref name="next"/> bypassing the guard (still raises the event).</summary>
        public void ForceEnter(TState next)
        {
            TState previous = Current;
            Current = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
