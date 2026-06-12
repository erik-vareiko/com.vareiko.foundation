using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Save
{
    public static class FoundationSaveInstaller
    {
        public static void Install(
            IContainerBuilder builder,
            SaveSchemaConfig schemaConfig = null,
            SaveSecurityConfig securityConfig = null,
            AutosaveConfig autosaveConfig = null)
        {
            builder.RegisterInstance(schemaConfig != null ? schemaConfig : ScriptableObject.CreateInstance<SaveSchemaConfig>());
            builder.RegisterInstance(securityConfig != null ? securityConfig : ScriptableObject.CreateInstance<SaveSecurityConfig>());
            builder.RegisterInstance(autosaveConfig != null ? autosaveConfig : ScriptableObject.CreateInstance<AutosaveConfig>());

            string saveRootPath = Path.Combine(Application.persistentDataPath, "saves");

            // PlayerPrefsSaveStorage has two string ctor params (rootPath, keyPrefix). WithParameter<string>
            // matches by type and would feed saveRootPath to BOTH, corrupting the key prefix. Build via a
            // factory so keyPrefix keeps its baked default.
            builder.Register<ISaveStorage>(_ => new PlayerPrefsSaveStorage(saveRootPath), Lifetime.Singleton);
            builder.Register<JsonUnitySaveSerializer>(Lifetime.Singleton);
            builder.Register<SecureSaveSerializer>(Lifetime.Singleton).As<ISaveSerializer>();
            builder.Register<SaveMigrationService>(resolver => new SaveMigrationService(
                    new List<ISaveMigration>(resolver.Resolve<IEnumerable<ISaveMigration>>())),
                Lifetime.Singleton)
                .As<ISaveMigrationService>();
            builder.Register<PreferLocalSaveConflictResolver>(Lifetime.Singleton).As<ISaveConflictResolver>();
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>().WithParameter<string>(saveRootPath);
            builder.Register<CloudSaveSyncService>(Lifetime.Singleton).As<ICloudSaveSyncService>();
            builder.RegisterEntryPoint<AutosaveService>(Lifetime.Singleton).AsSelf();
        }
    }
}
