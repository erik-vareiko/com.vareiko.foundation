using UnityEngine;

namespace Vareiko.Foundation.Observability
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Observability Config")]
    public sealed class ObservabilityConfig : ScriptableObject
    {
        [SerializeField] private FoundationLogLevel _minimumLogLevel = FoundationLogLevel.Info;
        [SerializeField] private bool _logToUnityConsole = true;
        [SerializeField] private bool _enableDiagnosticsOverlay;
        [SerializeField] private float _diagnosticsRefreshIntervalSeconds = 0.25f;

        public FoundationLogLevel MinimumLogLevel => _minimumLogLevel;
        public bool LogToUnityConsole => _logToUnityConsole;
        public bool EnableDiagnosticsOverlay => _enableDiagnosticsOverlay;
        public float DiagnosticsRefreshIntervalSeconds => _diagnosticsRefreshIntervalSeconds <= 0f ? 0.25f : _diagnosticsRefreshIntervalSeconds;
    }
}
