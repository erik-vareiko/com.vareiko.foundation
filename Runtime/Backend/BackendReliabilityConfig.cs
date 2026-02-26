using UnityEngine;

namespace Vareiko.Foundation.Backend
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Backend Reliability Config")]
    public sealed class BackendReliabilityConfig : ScriptableObject
    {
        [SerializeField] private bool _enableRetry = true;
        [SerializeField] private int _maxAttempts = 3;
        [SerializeField] private int _initialDelayMs = 250;
        [SerializeField] private bool _enableCloudFunctionQueue = true;
        [SerializeField] private bool _queueFailedCloudFunctions = true;
        [SerializeField] private bool _autoFlushQueueOnReconnect = true;
        [SerializeField] private int _maxQueuedCloudFunctions = 32;

        public bool EnableRetry => _enableRetry;
        public int MaxAttempts => _maxAttempts < 1 ? 1 : _maxAttempts;
        public int InitialDelayMs => _initialDelayMs < 0 ? 0 : _initialDelayMs;
        public bool EnableCloudFunctionQueue => _enableCloudFunctionQueue;
        public bool QueueFailedCloudFunctions => _queueFailedCloudFunctions;
        public bool AutoFlushQueueOnReconnect => _autoFlushQueueOnReconnect;
        public int MaxQueuedCloudFunctions => _maxQueuedCloudFunctions < 1 ? 1 : _maxQueuedCloudFunctions;
    }
}
