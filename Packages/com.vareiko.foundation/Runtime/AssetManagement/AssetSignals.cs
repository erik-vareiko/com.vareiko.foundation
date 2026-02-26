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

    public readonly struct AssetReferenceChangedSignal
    {
        public readonly string Key;
        public readonly int ReferenceCount;
        public readonly int TotalTrackedAssets;

        public AssetReferenceChangedSignal(string key, int referenceCount, int totalTrackedAssets)
        {
            Key = key;
            ReferenceCount = referenceCount;
            TotalTrackedAssets = totalTrackedAssets;
        }
    }

    public readonly struct AssetReleasedSignal
    {
        public readonly string Key;
        public readonly int ReferenceCount;
        public readonly int TotalTrackedAssets;

        public AssetReleasedSignal(string key, int referenceCount, int totalTrackedAssets)
        {
            Key = key;
            ReferenceCount = referenceCount;
            TotalTrackedAssets = totalTrackedAssets;
        }
    }

    public readonly struct AssetLeakDetectedSignal
    {
        public readonly string Key;
        public readonly int ReferenceCount;

        public AssetLeakDetectedSignal(string key, int referenceCount)
        {
            Key = key;
            ReferenceCount = referenceCount;
        }
    }
}
