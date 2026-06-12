using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Common
{
    public interface IHealthCheckRunner
    {
        UniTask<IReadOnlyList<HealthCheckResult>> RunAsync(CancellationToken cancellationToken = default);
    }
}
