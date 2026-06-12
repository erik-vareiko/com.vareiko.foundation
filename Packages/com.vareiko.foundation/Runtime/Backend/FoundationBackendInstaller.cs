using System;
using UnityEngine;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Signals;
using Vareiko.Foundation.Time;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Backend
{
    public static class FoundationBackendInstaller
    {
        public static void Install(
            IContainerBuilder builder,
            BackendConfig config = null,
            BackendReliabilityConfig reliabilityConfig = null,
            BackendCommandConfig commandConfig = null,
            RemoteConfigCacheConfig remoteConfigCacheConfig = null)
        {
            BackendReliabilityConfig resolvedReliability = reliabilityConfig != null ? reliabilityConfig : ScriptableObject.CreateInstance<BackendReliabilityConfig>();
            BackendCommandConfig resolvedCommand = commandConfig != null ? commandConfig : ScriptableObject.CreateInstance<BackendCommandConfig>();
            RemoteConfigCacheConfig resolvedRemoteCache = remoteConfigCacheConfig != null ? remoteConfigCacheConfig : ScriptableObject.CreateInstance<RemoteConfigCacheConfig>();

            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<BackendConfig>());
            builder.RegisterInstance(resolvedReliability);
            builder.RegisterInstance(resolvedCommand);
            builder.RegisterInstance(resolvedRemoteCache);

            // Inner providers are registered as their concrete type only (never as the public
            // interface) so the decorator wrappers below can resolve them without a cycle.
            Func<IObjectResolver, IBackendService> resolveBackendInner;
            if (config != null && config.Provider == BackendProviderType.PlayFab)
            {
                builder.Register<PlayFabBackendService>(Lifetime.Singleton);
                resolveBackendInner = r => r.Resolve<PlayFabBackendService>();
            }
            else
            {
                builder.Register<NullBackendService>(Lifetime.Singleton);
                resolveBackendInner = r => r.Resolve<NullBackendService>();
            }

            Func<IObjectResolver, ICloudFunctionService> resolveCloudFunctionInner;
            if (config != null && config.Provider == BackendProviderType.PlayFab && config.EnableCloudFunctions)
            {
                builder.Register<PlayFabCloudFunctionService>(Lifetime.Singleton);
                resolveCloudFunctionInner = r => r.Resolve<PlayFabCloudFunctionService>();
            }
            else
            {
                builder.Register<NullCloudFunctionService>(Lifetime.Singleton);
                resolveCloudFunctionInner = r => r.Resolve<NullCloudFunctionService>();
            }

            Func<IObjectResolver, IRemoteConfigService> resolveRemoteConfigInner;
            if (config != null && config.Provider == BackendProviderType.PlayFab && config.EnableRemoteConfig)
            {
                builder.Register<PlayFabRemoteConfigService>(Lifetime.Singleton);
                resolveRemoteConfigInner = r => r.Resolve<PlayFabRemoteConfigService>();
            }
            else
            {
                builder.Register<NullRemoteConfigService>(Lifetime.Singleton);
                resolveRemoteConfigInner = r => r.Resolve<NullRemoteConfigService>();
            }

            builder.Register<PlayerPrefsCloudFunctionQueueStore>(Lifetime.Singleton).As<ICloudFunctionQueueStore>();
            builder.Register<PlayerPrefsCloudCommandQueueStore>(Lifetime.Singleton).As<ICloudCommandQueueStore>();
            builder.Register<CloudCommandRetryClassifier>(Lifetime.Singleton).As<ICloudCommandRetryClassifier>();

            builder.Register<IBackendService>(r => new RetryingBackendService(
                    resolveBackendInner(r),
                    resolvedReliability,
                    r.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton);

            // Decorator wrappers that drive lifecycle (IInitializable/ITickable) are entry points;
            // RegisterEntryPoint's AsImplementedInterfaces exposes their public service interfaces.
            builder.RegisterEntryPoint<CachedRemoteConfigService>(r => new CachedRemoteConfigService(
                    resolveRemoteConfigInner(r),
                    r.Resolve<IFoundationTimeProvider>(),
                    resolvedRemoteCache,
                    r.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .AsSelf();

            builder.RegisterEntryPoint<ReliableCloudFunctionService>(r => new ReliableCloudFunctionService(
                    resolveCloudFunctionInner(r),
                    r.Resolve<IConnectivityService>(),
                    resolvedReliability,
                    r.Resolve<IFoundationSignalBus>(),
                    r.Resolve<ICloudFunctionQueueStore>()),
                Lifetime.Singleton)
                .AsSelf();

            builder.RegisterEntryPoint<CloudCommandService>(r => new CloudCommandService(
                    resolveCloudFunctionInner(r),
                    resolvedReliability,
                    resolvedCommand,
                    r.Resolve<ICloudCommandRetryClassifier>(),
                    r.Resolve<ICloudCommandQueueStore>(),
                    r.Resolve<IConnectivityService>(),
                    r.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .AsSelf();
        }
    }
}
