namespace Vareiko.Foundation.Features
{
    public readonly struct FeatureFlagsRefreshedSignal
    {
        public readonly int RemoteValueCount;

        public FeatureFlagsRefreshedSignal(int remoteValueCount)
        {
            RemoteValueCount = remoteValueCount;
        }
    }

    public readonly struct FeatureFlagOverriddenSignal
    {
        public readonly string Key;
        public readonly bool Value;

        public FeatureFlagOverriddenSignal(string key, bool value)
        {
            Key = key;
            Value = value;
        }
    }
}
