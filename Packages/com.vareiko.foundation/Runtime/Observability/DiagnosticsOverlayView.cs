using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public sealed class DiagnosticsOverlayView : MonoBehaviour
    {
        [SerializeField] private bool _visibleByDefault = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F9;

        private IDiagnosticsService _diagnosticsService;
        private ObservabilityConfig _config;
        private bool _isVisible;
        private GUIStyle _style;

        [Inject]
        public void Construct(
            [InjectOptional] IDiagnosticsService diagnosticsService = null,
            [InjectOptional] ObservabilityConfig config = null)
        {
            _diagnosticsService = diagnosticsService;
            _config = config;
        }

        private void Awake()
        {
            bool overlayEnabled = _config != null && _config.EnableDiagnosticsOverlay;
            _isVisible = overlayEnabled && _visibleByDefault;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_toggleKey))
            {
                _isVisible = !_isVisible;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _diagnosticsService == null || _diagnosticsService.Snapshot == null)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    normal = { textColor = Color.white }
                };
            }

            DiagnosticsSnapshot s = _diagnosticsService.Snapshot;
            GUILayout.BeginArea(new Rect(10f, 10f, 460f, 220f), GUI.skin.box);
            GUILayout.Label("Foundation Diagnostics", _style);
            GUILayout.Label($"Boot Completed: {s.IsBootCompleted} | Boot Failed: {s.IsBootFailed}", _style);
            if (!string.IsNullOrWhiteSpace(s.LastBootError))
            {
                GUILayout.Label($"Boot Error: {s.LastBootError}", _style);
            }

            GUILayout.Label($"Online: {s.IsOnline}", _style);
            GUILayout.Label($"Loading: {s.IsLoading} ({s.LoadingProgress:0.00})", _style);
            GUILayout.Label($"Backend Configured: {s.IsBackendConfigured} | Auth: {s.IsBackendAuthenticated}", _style);
            GUILayout.Label($"Remote Config Values: {s.RemoteConfigValues}", _style);
            GUILayout.Label($"Assets Tracked: {s.TrackedAssets} | References: {s.AssetReferences}", _style);
            GUILayout.Label($"Updated At: {s.LastUpdatedAt:0.00}", _style);
            GUILayout.EndArea();
        }
    }
}
