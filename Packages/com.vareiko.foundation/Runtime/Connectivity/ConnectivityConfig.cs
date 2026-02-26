using UnityEngine;

namespace Vareiko.Foundation.Connectivity
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Connectivity Config")]
    public sealed class ConnectivityConfig : ScriptableObject
    {
        [SerializeField] private float _pollIntervalSeconds = 1f;

        public float PollIntervalSeconds => _pollIntervalSeconds;
    }
}
