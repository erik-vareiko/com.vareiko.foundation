using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Connectivity
{
    public static class FoundationConnectivityInstaller
    {
        public static void Install(IContainerBuilder builder, ConnectivityConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<ConnectivityConfig>());
            builder.Register<UnityNetworkReachabilityProvider>(Lifetime.Singleton).As<INetworkReachabilityProvider>();
            builder.RegisterEntryPoint<ConnectivityService>(Lifetime.Singleton).As<IConnectivityService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<ConnectivityChangedSignal>(signalOptions);
        }
    }
}
