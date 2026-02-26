using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Common;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class RetryingBackendService : IBackendService
    {
        private readonly IBackendService _inner;
        private readonly SignalBus _signalBus;
        private readonly RetryPolicy _retryPolicy;

        [Inject]
        public RetryingBackendService(
            [Inject(Id = "BackendInner")] IBackendService inner,
            [InjectOptional] BackendReliabilityConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _inner = inner;
            _signalBus = signalBus;

            bool enabled = config == null || config.EnableRetry;
            int attempts = config != null ? config.MaxAttempts : 3;
            int delayMs = config != null ? config.InitialDelayMs : 250;
            _retryPolicy = new RetryPolicy(enabled, attempts, delayMs);
        }

        public BackendProviderType Provider => _inner.Provider;
        public bool IsConfigured => _inner.IsConfigured;
        public bool IsAuthenticated => _inner.IsAuthenticated;

        public UniTask<BackendAuthResult> LoginAnonymousAsync(string customId, CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(
                token => _inner.LoginAnonymousAsync(customId, token),
                result => result.Success,
                (attempt, maxAttempts, _) => _signalBus?.Fire(new BackendOperationRetriedSignal("LoginAnonymous", attempt, maxAttempts, string.Empty)),
                cancellationToken);
        }

        public UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(
                _inner.GetPlayerDataAsync,
                result => result.Success,
                (attempt, maxAttempts, _) => _signalBus?.Fire(new BackendOperationRetriedSignal("GetPlayerData", attempt, maxAttempts, string.Empty)),
                cancellationToken);
        }

        public UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(
                token => _inner.SetPlayerDataAsync(data, token),
                result => result,
                (attempt, maxAttempts, _) => _signalBus?.Fire(new BackendOperationRetriedSignal("SetPlayerData", attempt, maxAttempts, string.Empty)),
                cancellationToken);
        }
    }
}
