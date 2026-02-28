using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class PlayFabRemoteConfigService : IRemoteConfigService
    {
        private readonly Dictionary<string, string> _snapshot = new Dictionary<string, string>(0, System.StringComparer.Ordinal);
        private readonly BackendConfig _config;
        private bool _isReady;

        [Inject]
        public PlayFabRemoteConfigService([InjectOptional] BackendConfig config = null)
        {
            _config = config;
        }

        public bool IsReady => _isReady;

        public async UniTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_config == null || !_config.EnableRemoteConfig)
            {
                _snapshot.Clear();
                _isReady = true;
                return;
            }

            if (_config.Provider != BackendProviderType.PlayFab || string.IsNullOrWhiteSpace(_config.TitleId))
            {
                _snapshot.Clear();
                _isReady = true;
                Debug.LogWarning("PlayFabRemoteConfigService: PlayFab is not configured.");
                return;
            }

#if PLAYFAB_SDK
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            _snapshot.Clear();
            _isReady = true;
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            _snapshot.Clear();
            _isReady = true;
            Debug.LogWarning("PlayFabRemoteConfigService: PLAYFAB_SDK is not defined.");
#endif
        }

        public bool TryGetString(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            return _snapshot.TryGetValue(key, out value);
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
            return _snapshot;
        }
    }
}
