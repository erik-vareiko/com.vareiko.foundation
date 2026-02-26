using UnityEngine;

namespace Vareiko.Foundation.Backend
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Remote Config Cache Config")]
    public sealed class RemoteConfigCacheConfig : ScriptableObject
    {
        [SerializeField] private bool _refreshOnInitialize = true;
        [SerializeField] private bool _autoRefresh = true;
        [SerializeField] private float _refreshIntervalSeconds = 60f;

        public bool RefreshOnInitialize => _refreshOnInitialize;
        public bool AutoRefresh => _autoRefresh;
        public float RefreshIntervalSeconds => _refreshIntervalSeconds <= 0f ? 60f : _refreshIntervalSeconds;
    }
}
