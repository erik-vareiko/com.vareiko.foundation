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
            return UniTask.FromResult(new BackendAuthResult(false, string.Empty, "Backend provider is not configured."));
        }

        public UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(new BackendPlayerDataResult(false, null, "Backend provider is not configured."));
        }

        public UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(false);
        }
    }
}
