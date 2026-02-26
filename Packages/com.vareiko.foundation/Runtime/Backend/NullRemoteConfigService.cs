using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Backend
{
    public sealed class NullRemoteConfigService : IRemoteConfigService
    {
        private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

        public bool IsReady => true;

        public UniTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
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
