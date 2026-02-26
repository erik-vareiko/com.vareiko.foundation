using Zenject;

namespace Vareiko.Foundation.AssetManagement
{
    public static class FoundationAssetInstaller
    {
        public static void Install(DiContainer container, AssetServiceConfig config = null)
        {
            if (container.HasBinding<IAssetService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            container.DeclareSignal<AssetLoadedSignal>();
            container.DeclareSignal<AssetLoadFailedSignal>();
            container.DeclareSignal<AssetWarmupCompletedSignal>();
            container.DeclareSignal<AssetReferenceChangedSignal>();
            container.DeclareSignal<AssetReleasedSignal>();
            container.DeclareSignal<AssetLeakDetectedSignal>();

            container.Bind<IAssetProvider>().To<ResourcesAssetProvider>().AsSingle();
            container.Bind<IAssetProvider>().To<AddressablesAssetProvider>().AsSingle();
            container.Bind<IAssetService>().To<AssetService>().AsSingle();
        }
    }
}
