using Zenject;

namespace Vareiko.Foundation.Backend
{
    public static class FoundationBackendInstaller
    {
        public static void Install(
            DiContainer container,
            BackendConfig config = null,
            BackendReliabilityConfig reliabilityConfig = null,
            RemoteConfigCacheConfig remoteConfigCacheConfig = null)
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
            container.DeclareSignal<RemoteConfigRefreshedSignal>();
            container.DeclareSignal<RemoteConfigRefreshFailedSignal>();

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            if (reliabilityConfig != null)
            {
                container.BindInstance(reliabilityConfig).IfNotBound();
            }

            if (remoteConfigCacheConfig != null)
            {
                container.BindInstance(remoteConfigCacheConfig).IfNotBound();
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

            if (config != null && config.Provider == BackendProviderType.PlayFab && config.EnableRemoteConfig)
            {
                container.Bind<IRemoteConfigService>().WithId("RemoteConfigInner").To<PlayFabRemoteConfigService>().AsSingle();
            }
            else
            {
                container.Bind<IRemoteConfigService>().WithId("RemoteConfigInner").To<NullRemoteConfigService>().AsSingle();
            }

            container.Bind<IBackendService>().To<RetryingBackendService>().AsSingle();
            container.BindInterfacesAndSelfTo<CachedRemoteConfigService>().AsSingle().NonLazy();
            container.BindInterfacesAndSelfTo<ReliableCloudFunctionService>().AsSingle().NonLazy();
        }
    }
}
