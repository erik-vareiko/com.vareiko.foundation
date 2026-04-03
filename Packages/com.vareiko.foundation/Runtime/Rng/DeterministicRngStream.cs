using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.Rng
{
    internal sealed class DeterministicRngStream : IDeterministicRngStream
    {
        private Pcg32 _generator;

        public DeterministicRngStream(Pcg32 generator)
        {
            _generator = generator;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
            }

            uint range = (uint)(maxExclusive - minInclusive);
            uint threshold = (uint)(-range % range);
            uint value;
            do
            {
                value = _generator.NextUInt();
            }
            while (value < threshold);

            return (int)(value % range) + minInclusive;
        }

        public float NextFloat01()
        {
            uint value = _generator.NextUInt();
            return (value >> 8) * (1f / 16777216f);
        }

        public bool NextChance(float probability01)
        {
            if (probability01 <= 0f)
            {
                return false;
            }

            if (probability01 >= 1f)
            {
                return true;
            }

            return NextFloat01() < probability01;
        }

        public int PickWeightedIndex(IReadOnlyList<float> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                throw new ArgumentException("weights must not be empty.", nameof(weights));
            }

            double total = 0d;
            for (int i = 0; i < weights.Count; i++)
            {
                float weight = weights[i];
                if (weight > 0f)
                {
                    total += weight;
                }
            }

            if (total <= 0d)
            {
                throw new ArgumentException("weights must contain at least one positive value.", nameof(weights));
            }

            double roll = NextFloat01() * total;
            double cumulative = 0d;
            int fallback = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                float weight = weights[i];
                if (weight <= 0f)
                {
                    continue;
                }

                fallback = i;
                cumulative += weight;
                if (roll <= cumulative)
                {
                    return i;
                }
            }

            return fallback < 0 ? 0 : fallback;
        }

        public RngStreamState CaptureState()
        {
            return _generator.CaptureState();
        }
    }
}
