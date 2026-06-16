using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Settings
{
    public interface ISettingsService
    {
        bool IsLoaded { get; }
        GameSettings Current { get; }
        UniTask LoadAsync(CancellationToken cancellationToken = default);
        UniTask SaveAsync(CancellationToken cancellationToken = default);
        void Apply(GameSettings settings, bool saveImmediately = false);
    }
}
