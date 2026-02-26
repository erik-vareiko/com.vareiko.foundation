using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Common
{
    public interface IHealthCheck
    {
        string Name { get; }
        UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
    }
}
