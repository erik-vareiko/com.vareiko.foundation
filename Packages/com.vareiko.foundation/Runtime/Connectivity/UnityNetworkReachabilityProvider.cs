using UnityEngine;

namespace Vareiko.Foundation.Connectivity
{
    public sealed class UnityNetworkReachabilityProvider : INetworkReachabilityProvider
    {
        public NetworkReachability GetReachability()
        {
            return Application.internetReachability;
        }
    }
}
