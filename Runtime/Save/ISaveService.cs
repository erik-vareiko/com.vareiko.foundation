using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Save
{
    public interface ISaveService
    {
        UniTask SaveAsync<T>(string slot, string key, T model, CancellationToken cancellationToken = default);
        UniTask<T> LoadAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default);
        UniTask<bool> ExistsAsync(string slot, string key, CancellationToken cancellationToken = default);
        UniTask DeleteAsync(string slot, string key, CancellationToken cancellationToken = default);
    }
}
