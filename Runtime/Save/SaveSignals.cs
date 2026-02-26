namespace Vareiko.Foundation.Save
{
    public readonly struct SaveWrittenSignal
    {
        public readonly string Slot;
        public readonly string Key;

        public SaveWrittenSignal(string slot, string key)
        {
            Slot = slot;
            Key = key;
        }
    }

    public readonly struct SaveDeletedSignal
    {
        public readonly string Slot;
        public readonly string Key;

        public SaveDeletedSignal(string slot, string key)
        {
            Slot = slot;
            Key = key;
        }
    }

    public readonly struct SaveMigratedSignal
    {
        public readonly string Slot;
        public readonly string Key;
        public readonly int FromVersion;
        public readonly int ToVersion;

        public SaveMigratedSignal(string slot, string key, int fromVersion, int toVersion)
        {
            Slot = slot;
            Key = key;
            FromVersion = fromVersion;
            ToVersion = toVersion;
        }
    }

    public readonly struct SaveLoadFailedSignal
    {
        public readonly string Slot;
        public readonly string Key;
        public readonly string Error;

        public SaveLoadFailedSignal(string slot, string key, string error)
        {
            Slot = slot;
            Key = key;
            Error = error;
        }
    }
}
