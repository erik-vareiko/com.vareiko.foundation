using UnityEngine;

namespace Vareiko.Foundation.Connectivity
{
    public readonly struct ConnectivityChangedSignal
    {
        public readonly bool IsOnline;
        public readonly NetworkReachability Reachability;

        public ConnectivityChangedSignal(bool isOnline, NetworkReachability reachability)
        {
            IsOnline = isOnline;
            Reachability = reachability;
        }
    }
}
