using System.Collections.Generic;

namespace Vareiko.Foundation.Rng
{
    public interface IDeterministicRngStream
    {
        int NextInt(int minInclusive, int maxExclusive);
        float NextFloat01();
        bool NextChance(float probability01);
        int PickWeightedIndex(IReadOnlyList<float> weights);
        RngStreamState CaptureState();
    }
}
