using UnityEngine;
using VContainer;
using VContainer.Unity;

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
    }
}
