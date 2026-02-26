using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Common
{
    public sealed class HealthCheckRunner : IHealthCheckRunner
    {
        private readonly List<IHealthCheck> _checks;
        private readonly SignalBus _signalBus;

        [Inject]
        public HealthCheckRunner([InjectOptional] List<IHealthCheck> checks = null, [InjectOptional] SignalBus signalBus = null)
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
                    _signalBus?.Fire(new HealthCheckPassedSignal(checkName, result.Message));
                }
                else
                {
                    _signalBus?.Fire(new HealthCheckFailedSignal(checkName, result.Message));
                }
            }

            return results;
        }
    }
}
