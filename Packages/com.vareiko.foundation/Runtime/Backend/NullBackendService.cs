using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Backend
{
    public sealed class NullBackendService : IBackendService
    {
        public BackendProviderType Provider => BackendProviderType.None;
        public bool IsConfigured => false;
        public bool IsAuthenticated => false;

        public UniTask<BackendAuthResult> LoginAnonymousAsync(string customId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(BackendAuthResult.Fail("Backend provider is not configured.", BackendErrorCode.ConfigurationInvalid));
        }

        public UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(BackendPlayerDataResult.Fail("Backend provider is not configured.", BackendErrorCode.ConfigurationInvalid));
        }

        public UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(false);
        }
    }
}
