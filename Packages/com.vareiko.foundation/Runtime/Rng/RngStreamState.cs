using System;

namespace Vareiko.Foundation.Rng
{
    [Serializable]
    public readonly struct RngStreamState
    {
        public readonly ulong State;
        public readonly ulong Increment;
        public readonly ulong Step;

        public RngStreamState(ulong state, ulong increment, ulong step)
        {
            State = state;
            Increment = increment;
            Step = step;
        }
    }
}
