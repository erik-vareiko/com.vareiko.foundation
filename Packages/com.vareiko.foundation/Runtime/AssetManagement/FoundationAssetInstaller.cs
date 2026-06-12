using System.Collections.Generic;
using UnityEngine;
using Vareiko.Foundation.Signals;
using VContainer;
using MessagePipe;

namespace Vareiko.Foundation.AssetManagement
{
    public static class FoundationAssetInstaller
    {
        public static void Install(IContainerBuilder builder, AssetServiceConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AssetServiceConfig>());
            builder.Register<ResourcesAssetProvider>(Lifetime.Singleton).As<IAssetProvider>();
            builder.Register<AddressablesAssetProvider>(Lifetime.Singleton).As<IAssetProvider>();
            builder.Register<AssetService>(resolver => new AssetService(
                    new List<IAssetProvider>(resolver.Resolve<IEnumerable<IAssetProvider>>()),
                    resolver.Resolve<AssetServiceConfig>(),
                    resolver.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .As<IAssetService>();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<AssetLoadedSignal>(signalOptions);
            builder.RegisterMessageBroker<AssetLoadFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<AssetReleasedSignal>(signalOptions);
            builder.RegisterMessageBroker<AssetReferenceChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<AssetWarmupCompletedSignal>(signalOptions);
            builder.RegisterMessageBroker<AssetLeakDetectedSignal>(signalOptions);
        }
    }
}
