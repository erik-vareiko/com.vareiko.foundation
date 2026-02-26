using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Backend
{
    public interface IRemoteConfigService
    {
        bool IsReady { get; }
        UniTask RefreshAsync(CancellationToken cancellationToken = default);
        bool TryGetString(string key, out string value);
        bool TryGetInt(string key, out int value);
        bool TryGetFloat(string key, out float value);
        IReadOnlyDictionary<string, string> Snapshot();
    }
}
