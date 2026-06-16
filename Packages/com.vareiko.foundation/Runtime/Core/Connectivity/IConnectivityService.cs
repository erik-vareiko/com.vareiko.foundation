using UnityEngine;

namespace Vareiko.Foundation.Connectivity
{
    public interface IConnectivityService
    {
        bool IsOnline { get; }
        NetworkReachability Reachability { get; }
        void Refresh();
    }
}
