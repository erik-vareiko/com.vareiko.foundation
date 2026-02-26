using Vareiko.Foundation.App;
using Vareiko.Foundation.Time;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Connectivity
{
    public sealed class ConnectivityService : IConnectivityService, IInitializable, ITickable, System.IDisposable
    {
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly ConnectivityConfig _config;
        private readonly SignalBus _signalBus;
        private readonly INetworkReachabilityProvider _reachabilityProvider;
        private readonly IApplicationLifecycleService _applicationLifecycleService;

        private NetworkReachability _reachability = NetworkReachability.NotReachable;
        private bool _isOnline;
        private float _nextPollAt;
        private float _nextFocusRefreshAt;

        [Inject]
        public ConnectivityService(
            IFoundationTimeProvider timeProvider,
            [InjectOptional] ConnectivityConfig config = null,
            [InjectOptional] SignalBus signalBus = null,
            [InjectOptional] INetworkReachabilityProvider reachabilityProvider = null,
            [InjectOptional] IApplicationLifecycleService applicationLifecycleService = null)
        {
            _timeProvider = timeProvider;
            _config = config;
            _signalBus = signalBus;
            _reachabilityProvider = reachabilityProvider ?? new UnityNetworkReachabilityProvider();
            _applicationLifecycleService = applicationLifecycleService;
        }

        public bool IsOnline => _isOnline;
        public NetworkReachability Reachability => _reachability;

        public void Initialize()
        {
            Refresh();
            _nextPollAt = _timeProvider.Time + GetPollInterval();

            if (ShouldRefreshOnFocusRegained() && _applicationLifecycleService != null)
            {
                _applicationLifecycleService.FocusChanged += OnFocusChanged;
            }
        }

        public void Dispose()
        {
            if (ShouldRefreshOnFocusRegained() && _applicationLifecycleService != null)
            {
                _applicationLifecycleService.FocusChanged -= OnFocusChanged;
            }
        }

        public void Tick()
        {
            if (_timeProvider.Time < _nextPollAt)
            {
                return;
            }

            Refresh();
            _nextPollAt = _timeProvider.Time + GetPollInterval();
        }

        public void Refresh()
        {
            NetworkReachability nextReachability = _reachabilityProvider.GetReachability();
            bool nextOnline = nextReachability != NetworkReachability.NotReachable;
            if (nextReachability == _reachability && nextOnline == _isOnline)
            {
                return;
            }

            _reachability = nextReachability;
            _isOnline = nextOnline;
            _signalBus?.Fire(new ConnectivityChangedSignal(_isOnline, _reachability));
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                return;
            }

            if (_timeProvider.Time < _nextFocusRefreshAt)
            {
                return;
            }

            Refresh();
            _nextPollAt = _timeProvider.Time + GetPollInterval();
            _nextFocusRefreshAt = _timeProvider.Time + GetFocusRefreshCooldown();
        }

        private float GetPollInterval()
        {
            if (_config == null || _config.PollIntervalSeconds <= 0f)
            {
                return 1f;
            }

            return _config.PollIntervalSeconds;
        }

        private bool ShouldRefreshOnFocusRegained()
        {
            return _config == null || _config.RefreshOnFocusRegained;
        }

        private float GetFocusRefreshCooldown()
        {
            return _config != null ? _config.FocusRefreshCooldownSeconds : 0.25f;
        }
    }
}
