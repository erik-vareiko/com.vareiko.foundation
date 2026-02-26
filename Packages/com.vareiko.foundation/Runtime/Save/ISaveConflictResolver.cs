namespace Vareiko.Foundation.Save
{
    public interface ISaveConflictResolver
    {
        SaveConflictResolution Resolve(string slot, string key, string localPayload, string cloudPayload);
    }
}
