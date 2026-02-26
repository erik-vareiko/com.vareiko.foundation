using System.IO;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Save
{
    public static class FoundationSaveInstaller
    {
        public static void Install(
            DiContainer container,
            SaveSchemaConfig schemaConfig = null,
            SaveSecurityConfig securityConfig = null)
        {
            if (container.HasBinding<ISaveService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<SaveWrittenSignal>();
            container.DeclareSignal<SaveDeletedSignal>();
            container.DeclareSignal<SaveMigratedSignal>();
            container.DeclareSignal<SaveLoadFailedSignal>();

            if (schemaConfig != null)
            {
                container.BindInstance(schemaConfig).IfNotBound();
            }

            if (securityConfig != null)
            {
                container.BindInstance(securityConfig).IfNotBound();
            }

            container.Bind<string>()
                .WithId("SaveRootPath")
                .FromInstance(Path.Combine(Application.persistentDataPath, "saves"))
                .AsSingle();

            container.Bind<ISaveStorage>().To<FileSaveStorage>().AsSingle();
            container.Bind<JsonUnitySaveSerializer>().AsSingle();
            container.Bind<ISaveSerializer>().To<SecureSaveSerializer>().AsSingle();
            container.Bind<ISaveMigrationService>().To<SaveMigrationService>().AsSingle();
            container.Bind<ISaveConflictResolver>().To<PreferLocalSaveConflictResolver>().AsSingle();
            container.Bind<ISaveService>().To<SaveService>().AsSingle();
        }
    }
}
