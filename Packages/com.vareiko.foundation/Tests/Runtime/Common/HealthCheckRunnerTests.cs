using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vareiko.Foundation.Common;
using Zenject;

namespace Vareiko.Foundation.Tests.Common
{
    public sealed class HealthCheckRunnerTests
    {
        [Test]
        public async Task RunAsync_ProcessesChecks_AndEmitsSignals()
        {
            SignalBus signalBus = CreateSignalBus();
            int passedSignals = 0;
            List<string> failedCheckNames = new List<string>(2);

            signalBus.Subscribe<HealthCheckPassedSignal>(_ => passedSignals++);
            signalBus.Subscribe<HealthCheckFailedSignal>(signal => failedCheckNames.Add(signal.CheckName));

            List<IHealthCheck> checks = new List<IHealthCheck>
            {
                new FixedHealthCheck("network", HealthCheckResult.Healthy("ok")),
                new FixedHealthCheck(string.Empty, HealthCheckResult.Unhealthy("offline")),
                new ThrowingHealthCheck("backend", "boom"),
                null
            };

            HealthCheckRunner runner = new HealthCheckRunner(checks, signalBus);
            IReadOnlyList<HealthCheckResult> results = await runner.RunAsync();

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].IsHealthy, Is.True);
            Assert.That(results[1].IsHealthy, Is.False);
            Assert.That(results[2].IsHealthy, Is.False);
            Assert.That(results[2].Message, Is.EqualTo("boom"));
            Assert.That(passedSignals, Is.EqualTo(1));
            Assert.That(failedCheckNames.Count, Is.EqualTo(2));
            Assert.That(failedCheckNames[0], Is.EqualTo("FixedHealthCheck"));
            Assert.That(failedCheckNames[1], Is.EqualTo("backend"));
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<HealthCheckPassedSignal>();
            container.DeclareSignal<HealthCheckFailedSignal>();
            return container.Resolve<SignalBus>();
        }

        private sealed class FixedHealthCheck : IHealthCheck
        {
            private readonly HealthCheckResult _result;
            public string Name { get; }

            public FixedHealthCheck(string name, HealthCheckResult result)
            {
                Name = name;
                _result = result;
            }

            public UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(_result);
            }
        }

        private sealed class ThrowingHealthCheck : IHealthCheck
        {
            private readonly string _message;
            public string Name { get; }

            public ThrowingHealthCheck(string name, string message)
            {
                Name = name;
                _message = message;
            }

            public UniTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException(_message);
            }
        }
    }
}
