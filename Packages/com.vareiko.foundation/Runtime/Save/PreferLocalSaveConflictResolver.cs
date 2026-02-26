namespace Vareiko.Foundation.Save
{
    public sealed class PreferLocalSaveConflictResolver : ISaveConflictResolver
    {
        public SaveConflictResolution Resolve(string slot, string key, string localPayload, string cloudPayload)
        {
            if (string.IsNullOrWhiteSpace(localPayload) && !string.IsNullOrWhiteSpace(cloudPayload))
            {
                return SaveConflictResolution.Cloud(cloudPayload);
            }

            return SaveConflictResolution.Local(localPayload);
        }
    }
}
