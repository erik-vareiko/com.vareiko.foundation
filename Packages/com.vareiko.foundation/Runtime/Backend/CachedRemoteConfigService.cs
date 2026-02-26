using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Time;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class CachedRemoteConfigService : IRemoteConfigService, IInitializable, ITickable
    {
        private readonly IRemoteConfigService _inner;
        private readonly IFoundationTimeProvider _timeProvider;
        private readonly SignalBus _signalBus;
        private readonly RemoteConfigCacheConfig _config;

        private Dictionary<string, string> _cache = new Dictionary<string, string>(0, System.StringComparer.Ordinal);
        private float _nextRefreshAt;
        private bool _isRefreshing;

        [Inject]
        public CachedRemoteConfigService(
            [Inject(Id = "RemoteConfigInner")] IRemoteConfigService inner,
            IFoundationTimeProvider timeProvider,
            [InjectOptional] RemoteConfigCacheConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _inner = inner;
            _timeProvider = timeProvider;
            _config = config;
            _signalBus = signalBus;
        }

        public bool IsReady => _inner != null && _inner.IsReady;

        public void Initialize()
        {
            if (_config == null || _config.RefreshOnInitialize)
            {
                RefreshSafeAsync().Forget();
            }

            _nextRefreshAt = _timeProvider.Time + GetRefreshIntervalSeconds();
        }

        public void Tick()
        {
            if (_isRefreshing)
            {
                return;
            }

            if (_config != null && !_config.AutoRefresh)
            {
                return;
            }

            if (_timeProvider.Time < _nextRefreshAt)
            {
                return;
            }

            RefreshSafeAsync().Forget();
        }

        public async UniTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (_inner == null)
            {
                _cache.Clear();
                return;
            }

            await _inner.RefreshAsync(cancellationToken);
            CopySnapshot(_inner.Snapshot());
            _nextRefreshAt = _timeProvider.Time + GetRefreshIntervalSeconds();
            _signalBus?.Fire(new RemoteConfigRefreshedSignal(_cache.Count, string.Empty));
        }

        public bool TryGetString(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            return _cache.TryGetValue(key, out value);
        }

        public bool TryGetInt(string key, out int value)
        {
            value = default;
            string raw;
            if (!TryGetString(key, out raw))
            {
                return false;
            }

            return int.TryParse(raw, out value);
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = default;
            string raw;
            if (!TryGetString(key, out raw))
            {
                return false;
            }

            return float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        public IReadOnlyDictionary<string, string> Snapshot()
        {
            return _cache;
        }

        private async UniTaskVoid RefreshSafeAsync()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                await RefreshAsync();
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (System.Exception exception)
            {
                _signalBus?.Fire(new RemoteConfigRefreshFailedSignal(exception.Message));
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void CopySnapshot(IReadOnlyDictionary<string, string> source)
        {
            _cache.Clear();
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in source)
            {
                _cache[pair.Key] = pair.Value;
            }
        }

        private float GetRefreshIntervalSeconds()
        {
            if (_config == null)
            {
                return 60f;
            }

            return _config.RefreshIntervalSeconds;
        }
    }
}
