namespace Vareiko.Foundation.AssetManagement
{
    public readonly struct AssetLoadedSignal
    {
        public readonly string Key;
        public readonly string TypeName;

        public AssetLoadedSignal(string key, string typeName)
        {
            Key = key;
            TypeName = typeName;
        }
    }

    public readonly struct AssetLoadFailedSignal
    {
        public readonly string Key;
        public readonly string Error;

        public AssetLoadFailedSignal(string key, string error)
        {
            Key = key;
            Error = error;
        }
    }

    public readonly struct AssetWarmupCompletedSignal
    {
        public readonly int Count;

        public AssetWarmupCompletedSignal(int count)
        {
            Count = count;
        }
    }
}
