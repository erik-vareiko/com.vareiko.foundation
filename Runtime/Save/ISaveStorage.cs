using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Save
{
    public interface ISaveStorage
    {
        UniTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
        UniTask<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);
        UniTask WriteTextAsync(string path, string value, CancellationToken cancellationToken = default);
        UniTask DeleteAsync(string path, CancellationToken cancellationToken = default);
    }
}
