using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Save;

namespace Vareiko.Foundation.Tests.TestDoubles
{
    public sealed class InMemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>(System.StringComparer.Ordinal);

        public UniTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(_files.ContainsKey(path));
        }

        public UniTask<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string value;
            _files.TryGetValue(path, out value);
            return UniTask.FromResult(value);
        }

        public UniTask WriteTextAsync(string path, string value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _files[path] = value ?? string.Empty;
            return UniTask.CompletedTask;
        }

        public UniTask DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _files.Remove(path);
            return UniTask.CompletedTask;
        }
    }
}
