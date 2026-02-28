using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class PlayFabBackendService : IBackendService
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyData = new Dictionary<string, string>(0, StringComparer.Ordinal);
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
        public bool IsConfigured =>
            _config != null &&
            _config.Provider == BackendProviderType.PlayFab &&
            !string.IsNullOrWhiteSpace(_config.TitleId);
        public bool IsAuthenticated => _isAuthenticated;

        public async UniTask<BackendAuthResult> LoginAnonymousAsync(string customId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsConfigured)
            {
                SetAuthState(false, string.Empty);
                return BackendAuthResult.Fail("PlayFab is not configured.", BackendErrorCode.ConfigurationInvalid);
            }

            string normalizedCustomId = NormalizeCustomId(customId);
            if (string.IsNullOrEmpty(normalizedCustomId))
            {
                SetAuthState(false, string.Empty);
                return BackendAuthResult.Fail("CustomId is null or empty.", BackendErrorCode.ValidationFailed);
            }

            if (_isAuthenticated && string.Equals(_playerId, normalizedCustomId, StringComparison.Ordinal))
            {
                return BackendAuthResult.Succeed(_playerId);
            }

#if PLAYFAB_SDK
            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                SetAuthState(true, normalizedCustomId);
                return BackendAuthResult.Succeed(_playerId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetAuthState(false, string.Empty);
                return BackendAuthResult.Fail("PlayFab login failed.", BackendErrorCode.ProviderUnavailable, true);
            }
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            Debug.LogWarning("PlayFabBackendService: PLAYFAB_SDK is not defined. Falling back to unavailable mode.");
            SetAuthState(false, string.Empty);
            return BackendAuthResult.Fail("PLAYFAB_SDK is not installed.", BackendErrorCode.DependencyMissing);
#endif
        }

        public UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsConfigured)
            {
                return UniTask.FromResult(BackendPlayerDataResult.Fail("PlayFab is not configured.", BackendErrorCode.ConfigurationInvalid));
            }

            if (!_isAuthenticated)
            {
                return UniTask.FromResult(BackendPlayerDataResult.Fail("Not authenticated.", BackendErrorCode.AuthenticationRequired));
            }

#if PLAYFAB_SDK
            return UniTask.FromResult(BackendPlayerDataResult.Succeed(EmptyData));
#else
            return UniTask.FromResult(BackendPlayerDataResult.Fail("PLAYFAB_SDK is not installed.", BackendErrorCode.DependencyMissing));
#endif
        }

        public UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsConfigured || !_isAuthenticated || data == null)
            {
                return UniTask.FromResult(false);
            }

            foreach (KeyValuePair<string, string> pair in data)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    return UniTask.FromResult(false);
                }
            }

            return UniTask.FromResult(true);
        }

        private static string NormalizeCustomId(string customId)
        {
            if (string.IsNullOrWhiteSpace(customId))
            {
                return string.Empty;
            }

            return customId.Trim();
        }

        private void SetAuthState(bool isAuthenticated, string playerId)
        {
            string normalizedPlayerId = string.IsNullOrWhiteSpace(playerId) ? string.Empty : playerId.Trim();
            bool changed = _isAuthenticated != isAuthenticated || !string.Equals(_playerId, normalizedPlayerId, StringComparison.Ordinal);
            _isAuthenticated = isAuthenticated;
            _playerId = isAuthenticated ? normalizedPlayerId : string.Empty;

            if (changed)
            {
                _signalBus?.Fire(new BackendAuthStateChangedSignal(_isAuthenticated, _playerId));
            }
        }
    }
}
