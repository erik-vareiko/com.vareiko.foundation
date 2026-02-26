using UnityEngine;
using Vareiko.Foundation.Connectivity;

namespace Vareiko.Foundation.Tests.TestDoubles
{
    public sealed class FakeConnectivityService : IConnectivityService
    {
        public bool IsOnline { get; set; } = true;
        public NetworkReachability Reachability { get; set; } = NetworkReachability.ReachableViaLocalAreaNetwork;

        public void Refresh()
        {
        }
    }
}
