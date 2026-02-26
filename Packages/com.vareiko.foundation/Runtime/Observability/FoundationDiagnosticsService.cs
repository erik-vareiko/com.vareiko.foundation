using System.Collections.Generic;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Loading;
using Vareiko.Foundation.Time;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public sealed class FoundationDiagnosticsService : IDiagnosticsService, IInitializable, ITickable, System.IDisposable
    {
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly ObservabilityConfig _config;
        private readonly IConnectivityService _connectivityService;
        private readonly ILoadingService _loadingService;
        private readonly IBackendService _backendService;
        private readonly IRemoteConfigService _remoteConfigService;
        private readonly SignalBus _signalBus;
        private readonly DiagnosticsSnapshot _snapshot = new DiagnosticsSnapshot();

        private float _nextUpdateAt;

        [Inject]
        public FoundationDiagnosticsService(
            IFoundationTimeProvider timeProvider,
            [InjectOptional] ObservabilityConfig config = null,
            [InjectOptional] IConnectivityService connectivityService = null,
            [InjectOptional] ILoadingService loadingService = null,
            [InjectOptional] IBackendService backendService = null,
            [InjectOptional] IRemoteConfigService remoteConfigService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _timeProvider = timeProvider;
            _config = config;
            _connectivityService = connectivityService;
            _loadingService = loadingService;
            _backendService = backendService;
            _remoteConfigService = remoteConfigService;
            _signalBus = signalBus;
        }

        public DiagnosticsSnapshot Snapshot => _snapshot;

        public void Initialize()
        {
            if (_signalBus != null)
            {
                _signalBus.Subscribe<ApplicationBootCompletedSignal>(OnBootCompleted);
                _signalBus.Subscribe<ApplicationBootFailedSignal>(OnBootFailed);
                _signalBus.Subscribe<AppStateChangedSignal>(OnAppStateChanged);
            }

            Tick();
            _nextUpdateAt = _timeProvider.Time + GetRefreshInterval();
        }

        public void Dispose()
        {
            if (_signalBus != null)
            {
                _signalBus.Unsubscribe<ApplicationBootCompletedSignal>(OnBootCompleted);
                _signalBus.Unsubscribe<ApplicationBootFailedSignal>(OnBootFailed);
                _signalBus.Unsubscribe<AppStateChangedSignal>(OnAppStateChanged);
            }
        }

        public void Tick()
        {
            if (_timeProvider.Time < _nextUpdateAt)
            {
                return;
            }

            _snapshot.IsOnline = _connectivityService != null && _connectivityService.IsOnline;
            _snapshot.IsLoading = _loadingService != null && _loadingService.IsLoading;
            _snapshot.LoadingProgress = _loadingService != null ? _loadingService.Progress : 0f;
            _snapshot.IsBackendConfigured = _backendService != null && _backendService.IsConfigured;
            _snapshot.IsBackendAuthenticated = _backendService != null && _backendService.IsAuthenticated;
            IReadOnlyDictionary<string, string> remoteValues = _remoteConfigService != null ? _remoteConfigService.Snapshot() : null;
            _snapshot.RemoteConfigValues = remoteValues != null ? remoteValues.Count : 0;
            _snapshot.LastUpdatedAt = _timeProvider.Time;

            _nextUpdateAt = _timeProvider.Time + GetRefreshInterval();
            _signalBus?.Fire(new DiagnosticsSnapshotUpdatedSignal(_snapshot));
        }

        private void OnBootCompleted(ApplicationBootCompletedSignal signal)
        {
            _snapshot.IsBootCompleted = true;
            _snapshot.IsBootFailed = false;
            _snapshot.LastBootError = string.Empty;
        }

        private void OnBootFailed(ApplicationBootFailedSignal signal)
        {
            _snapshot.IsBootCompleted = false;
            _snapshot.IsBootFailed = true;
            _snapshot.LastBootError = signal.Error ?? string.Empty;
        }

        private void OnAppStateChanged(AppStateChangedSignal signal)
        {
            if (signal.Previous == AppState.Boot && signal.Current != AppState.Boot)
            {
                _snapshot.IsBootCompleted = true;
                _snapshot.IsBootFailed = false;
                _snapshot.LastBootError = string.Empty;
            }
        }

        private float GetRefreshInterval()
        {
            if (_config == null)
            {
                return 0.25f;
            }

            return _config.DiagnosticsRefreshIntervalSeconds;
        }
    }
}
