using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vareiko.Foundation.Signals;

namespace Vareiko.Foundation.Common
{
    public sealed class HealthCheckRunner : IHealthCheckRunner
    {
        private readonly List<IHealthCheck> _checks;
        private readonly IFoundationSignalBus _signalBus;

        public HealthCheckRunner(List<IHealthCheck> checks = null, IFoundationSignalBus signalBus = null)
        {
            _checks = checks ?? new List<IHealthCheck>(0);
            _signalBus = signalBus;
        }

        public async UniTask<IReadOnlyList<HealthCheckResult>> RunAsync(CancellationToken cancellationToken = default)
        {
            List<HealthCheckResult> results = new List<HealthCheckResult>(_checks.Count);
            for (int i = 0; i < _checks.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IHealthCheck check = _checks[i];
                if (check == null)
                {
                    continue;
                }

                string checkName = string.IsNullOrWhiteSpace(check.Name) ? check.GetType().Name : check.Name;
                HealthCheckResult result;
                try
                {
                    result = await check.CheckAsync(cancellationToken);
                }
                catch (System.Exception exception)
                {
                    result = HealthCheckResult.Unhealthy(exception.Message);
                    Debug.LogException(exception);
                }

                results.Add(result);
                if (result.IsHealthy)
                {
                    _signalBus?.Publish(new HealthCheckPassedSignal(checkName, result.Message));
                }
                else
                {
                    _signalBus?.Publish(new HealthCheckFailedSignal(checkName, result.Message));
                }
            }

            return results;
        }
    }
}
