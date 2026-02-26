using Vareiko.Foundation.Time;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Connectivity
{
    public sealed class ConnectivityService : IConnectivityService, IInitializable, ITickable
    {
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly ConnectivityConfig _config;
        private readonly SignalBus _signalBus;

        private NetworkReachability _reachability = NetworkReachability.NotReachable;
        private bool _isOnline;
        private float _nextPollAt;

        [Inject]
        public ConnectivityService(
            IFoundationTimeProvider timeProvider,
            [InjectOptional] ConnectivityConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _timeProvider = timeProvider;
            _config = config;
            _signalBus = signalBus;
        }

        public bool IsOnline => _isOnline;
        public NetworkReachability Reachability => _reachability;

        public void Initialize()
        {
            Refresh();
            _nextPollAt = _timeProvider.Time + GetPollInterval();
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
            NetworkReachability nextReachability = Application.internetReachability;
            bool nextOnline = nextReachability != NetworkReachability.NotReachable;
            if (nextReachability == _reachability && nextOnline == _isOnline)
            {
                return;
            }

            _reachability = nextReachability;
            _isOnline = nextOnline;
            _signalBus?.Fire(new ConnectivityChangedSignal(_isOnline, _reachability));
        }

        private float GetPollInterval()
        {
            if (_config == null || _config.PollIntervalSeconds <= 0f)
            {
                return 1f;
            }

            return _config.PollIntervalSeconds;
        }
    }
}
