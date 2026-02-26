using UnityEngine;

namespace Vareiko.Foundation.Connectivity
{
    public interface INetworkReachabilityProvider
    {
        NetworkReachability GetReachability();
    }
}
