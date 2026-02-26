using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class PlayFabBackendService : IBackendService
    {
        private readonly BackendConfig _config;
        private readonly SignalBus _signalBus;
        private bool _isAuthenticated;
        private string _playerId;

        [Inject]
        public PlayFabBackendService([InjectOptional] BackendConfig config = null, [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _signalBus = signalBus;
        }

        public BackendProviderType Provider => BackendProviderType.PlayFab;
        public bool IsConfigured => _config != null && !string.IsNullOrWhiteSpace(_config.TitleId);
        public bool IsAuthenticated => _isAuthenticated;

        public async UniTask<BackendAuthResult> LoginAnonymousAsync(string customId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsConfigured)
            {
                return new BackendAuthResult(false, string.Empty, "PlayFab is not configured.");
            }

#if PLAYFAB_SDK
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            _isAuthenticated = true;
            _playerId = customId;
            _signalBus?.Fire(new BackendAuthStateChangedSignal(true, _playerId));
            return new BackendAuthResult(true, _playerId, string.Empty);
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            Debug.LogWarning("PlayFabBackendService: PLAYFAB_SDK is not defined. Falling back to unavailable mode.");
            return new BackendAuthResult(false, string.Empty, "PLAYFAB_SDK is not installed.");
#endif
        }

        public UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_isAuthenticated)
            {
                return UniTask.FromResult(new BackendPlayerDataResult(false, null, "Not authenticated."));
            }

            return UniTask.FromResult(new BackendPlayerDataResult(true, new Dictionary<string, string>(), string.Empty));
        }

        public UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_isAuthenticated)
            {
                return UniTask.FromResult(false);
            }

            return UniTask.FromResult(true);
        }
    }
}
