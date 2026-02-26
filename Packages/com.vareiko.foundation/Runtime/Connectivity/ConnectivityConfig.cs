using UnityEngine;

namespace Vareiko.Foundation.Connectivity
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Connectivity Config")]
    public sealed class ConnectivityConfig : ScriptableObject
    {
        [SerializeField] private float _pollIntervalSeconds = 1f;
        [SerializeField] private bool _refreshOnFocusRegained = true;
        [SerializeField] private float _focusRefreshCooldownSeconds = 0.25f;

        public float PollIntervalSeconds => _pollIntervalSeconds;
        public bool RefreshOnFocusRegained => _refreshOnFocusRegained;
        public float FocusRefreshCooldownSeconds => _focusRefreshCooldownSeconds < 0f ? 0f : _focusRefreshCooldownSeconds;
    }
}
