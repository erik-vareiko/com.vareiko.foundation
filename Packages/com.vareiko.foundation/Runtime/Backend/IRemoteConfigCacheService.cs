using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Backend
{
    public interface IRemoteConfigCacheService
    {
        int CachedValueCount { get; }
        UniTask ForceRefreshAsync(CancellationToken cancellationToken = default);
        void InvalidateCache(string reason = "manual");
    }
}
