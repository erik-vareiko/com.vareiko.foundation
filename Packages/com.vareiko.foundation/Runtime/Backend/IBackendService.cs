using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Backend
{
    public interface IBackendService
    {
        BackendProviderType Provider { get; }
        bool IsConfigured { get; }
        bool IsAuthenticated { get; }
        UniTask<BackendAuthResult> LoginAnonymousAsync(string customId, CancellationToken cancellationToken = default);
        UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default);
        UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default);
    }
}
