using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

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
            // Newtonsoft is the default payload serializer since 3.0 (handles dictionaries,
            // nullables, polymorphism); it writes the same {"Value": ...} envelope as the
            // JsonUtility serializer, so pre-3.0 saves keep loading. JsonUnitySaveSerializer
            // stays registered for hosts that want the dependency-free fallback.
            builder.Register<JsonUnitySaveSerializer>(Lifetime.Singleton);
            builder.Register<NewtonsoftJsonSaveSerializer>(Lifetime.Singleton);
            builder.Register<ISaveSerializer>(resolver => new SecureSaveSerializer(
                    resolver.Resolve<NewtonsoftJsonSaveSerializer>(),
                    resolver.Resolve<SaveSecurityConfig>()),
                Lifetime.Singleton);
            builder.Register<SaveMigrationService>(resolver => new SaveMigrationService(
                    new List<ISaveMigration>(resolver.Resolve<IEnumerable<ISaveMigration>>())),
                Lifetime.Singleton)
                .As<ISaveMigrationService>();
            builder.Register<PreferLocalSaveConflictResolver>(Lifetime.Singleton).As<ISaveConflictResolver>();
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>().WithParameter<string>(saveRootPath);
            builder.RegisterEntryPoint<AutosaveService>(Lifetime.Singleton).AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<SaveWrittenSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveDeletedSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveLoadFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveBackupWrittenSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveRestoredFromBackupSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveMigratedSignal>(signalOptions);
            builder.RegisterMessageBroker<AutosaveTriggeredSignal>(signalOptions);
            builder.RegisterMessageBroker<AutosaveCompletedSignal>(signalOptions);
            builder.RegisterMessageBroker<AutosaveFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveCloudPushedSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveCloudPulledSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveCloudConflictResolvedSignal>(signalOptions);
            builder.RegisterMessageBroker<SaveCloudSyncFailedSignal>(signalOptions);
        }
    }
}
