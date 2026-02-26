using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class PlayFabRemoteConfigService : IRemoteConfigService
    {
        private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>(0);
        private readonly BackendConfig _config;

        [Inject]
        public PlayFabRemoteConfigService([InjectOptional] BackendConfig config = null)
        {
            _config = config;
        }

        public bool IsReady => true;

        public async UniTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if PLAYFAB_SDK
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            if (_config != null && _config.EnableRemoteConfig)
            {
                Debug.LogWarning("PlayFabRemoteConfigService: PLAYFAB_SDK is not defined.");
            }
#endif
        }

        public bool TryGetString(string key, out string value)
        {
            value = string.Empty;
            return false;
        }

        public bool TryGetInt(string key, out int value)
        {
            value = default;
            return false;
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = default;
            return false;
        }

        public IReadOnlyDictionary<string, string> Snapshot()
        {
            return Empty;
        }
    }
}
