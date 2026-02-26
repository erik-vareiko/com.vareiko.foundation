using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Bootstrap
{
    public interface IBootstrapTask
    {
        int Order { get; }
        string Name { get; }
        UniTask ExecuteAsync(CancellationToken cancellationToken);
    }
}
