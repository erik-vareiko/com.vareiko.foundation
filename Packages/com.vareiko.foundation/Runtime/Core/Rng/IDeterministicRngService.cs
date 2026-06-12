namespace Vareiko.Foundation.Rng
{
    public interface IDeterministicRngService
    {
        void Initialize(int rootSeed);
        IDeterministicRngStream CreateStream(string scope);
        IDeterministicRngStream RestoreStream(string scope, RngStreamState state);
    }
}
