namespace Vareiko.Foundation.Rng
{
    internal struct Pcg32
    {
        private const ulong Multiplier = 6364136223846793005ul;

        private ulong _state;
        private ulong _increment;
        private ulong _step;

        public Pcg32(uint seed, ulong sequence)
        {
            _state = 0ul;
            _increment = (sequence << 1) | 1ul;
            _step = 0ul;

            NextUInt();
            _state += seed;
            NextUInt();
        }

        public Pcg32(RngStreamState state)
        {
            _state = state.State;
            _increment = state.Increment | 1ul;
            _step = state.Step;
        }

        public RngStreamState CaptureState()
        {
            return new RngStreamState(_state, _increment, _step);
        }

        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = oldState * Multiplier + _increment;
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            uint value = (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
            _step++;
            return value;
        }
    }
}
