using Zenject;

namespace Vareiko.Foundation.Backend
{
    public static class FoundationBackendInstaller
    {
        public static void Install(DiContainer container, BackendConfig config = null, BackendReliabilityConfig reliabilityConfig = null)
        {
            if (container.HasBinding<IBackendService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<BackendAuthStateChangedSignal>();
            container.DeclareSignal<BackendOperationRetriedSignal>();
            container.DeclareSignal<CloudFunctionQueuedSignal>();
            container.DeclareSignal<CloudFunctionQueueFlushedSignal>();

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            if (reliabilityConfig != null)
            {
                container.BindInstance(reliabilityConfig).IfNotBound();
            }

            if (config != null && config.Provider == BackendProviderType.PlayFab)
            {
                container.Bind<IBackendService>().WithId("BackendInner").To<PlayFabBackendService>().AsSingle();
            }
            else
            {
                container.Bind<IBackendService>().WithId("BackendInner").To<NullBackendService>().AsSingle();
            }

            if (config != null && config.Provider == BackendProviderType.PlayFab && config.EnableCloudFunctions)
            {
                container.Bind<ICloudFunctionService>().WithId("CloudFunctionInner").To<PlayFabCloudFunctionService>().AsSingle();
            }
            else
            {
                container.Bind<ICloudFunctionService>().WithId("CloudFunctionInner").To<NullCloudFunctionService>().AsSingle();
            }

            container.Bind<IBackendService>().To<RetryingBackendService>().AsSingle();
            container.Bind<IRemoteConfigService>().To<NullRemoteConfigService>().AsSingle();
            container.BindInterfacesAndSelfTo<ReliableCloudFunctionService>().AsSingle().NonLazy();
        }
    }
}
