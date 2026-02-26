using System.Collections.Generic;
using Zenject;

namespace Vareiko.Foundation.Save
{
    public sealed class SaveMigrationService : ISaveMigrationService
    {
        private readonly List<ISaveMigration> _migrations;

        [Inject]
        public SaveMigrationService([InjectOptional] List<ISaveMigration> migrations = null)
        {
            _migrations = migrations ?? new List<ISaveMigration>(0);
        }

        public SaveMigrationResult Migrate(string slot, string key, int fromVersion, int toVersion, string payload)
        {
            if (toVersion < 1)
            {
                return SaveMigrationResult.Fail(fromVersion, "Target schema version is invalid.");
            }

            if (fromVersion == toVersion)
            {
                return SaveMigrationResult.Succeed(toVersion, payload);
            }

            if (fromVersion > toVersion)
            {
                return SaveMigrationResult.Fail(fromVersion, "Downgrade migrations are not supported.");
            }

            int version = fromVersion;
            string currentPayload = payload;
            while (version < toVersion)
            {
                ISaveMigration migration = FindMigration(slot, key, version);
                if (migration == null)
                {
                    return SaveMigrationResult.Fail(version, $"Missing migration for {slot}/{key} from v{version}.");
                }

                currentPayload = migration.Migrate(currentPayload);
                version = migration.ToVersion;
            }

            return SaveMigrationResult.Succeed(version, currentPayload);
        }

        private ISaveMigration FindMigration(string slot, string key, int fromVersion)
        {
            for (int i = 0; i < _migrations.Count; i++)
            {
                ISaveMigration migration = _migrations[i];
                if (migration == null)
                {
                    continue;
                }

                if (migration.FromVersion != fromVersion)
                {
                    continue;
                }

                if (!migration.AppliesTo(slot, key))
                {
                    continue;
                }

                if (migration.ToVersion <= migration.FromVersion)
                {
                    continue;
                }

                return migration;
            }

            return null;
        }
    }
}
