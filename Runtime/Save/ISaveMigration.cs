namespace Vareiko.Foundation.Save
{
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        bool AppliesTo(string slot, string key);
        string Migrate(string payload);
    }
}
