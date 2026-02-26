namespace Vareiko.Foundation.Save
{
    public interface ISaveMigrationService
    {
        SaveMigrationResult Migrate(string slot, string key, int fromVersion, int toVersion, string payload);
    }
}
