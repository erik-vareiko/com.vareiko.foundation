using System.Collections.Generic;
using NUnit.Framework;
using Vareiko.Foundation.Validation;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Validation
{
    public sealed class StartupValidationRunnerTests
    {
        [Test]
        public void Initialize_EmitsCompletedSummary_WithWarningAndErrorCounts()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            int passed = 0;
            int warnings = 0;
            int errors = 0;
            StartupValidationCompletedSignal summary = default;
            bool hasSummary = false;

            signalBus.Subscribe<StartupValidationPassedSignal>(_ => passed++);
            signalBus.Subscribe<StartupValidationWarningSignal>(_ => warnings++);
            signalBus.Subscribe<StartupValidationFailedSignal>(_ => errors++);
            signalBus.Subscribe<StartupValidationCompletedSignal>(signal =>
            {
                summary = signal;
                hasSummary = true;
            });

            List<IStartupValidationRule> rules = new List<IStartupValidationRule>
            {
                new FixedRule("pass", StartupValidationResult.Success("ok")),
                new FixedRule("warn", StartupValidationResult.Warning("warn")),
                new FixedRule("fail", StartupValidationResult.Fail("fail"))
            };

            StartupValidationRunner runner = new StartupValidationRunner(rules, signalBus);
            runner.Initialize();

            Assert.That(passed, Is.EqualTo(1));
            Assert.That(warnings, Is.EqualTo(1));
            Assert.That(errors, Is.EqualTo(1));
            Assert.That(hasSummary, Is.True);
            Assert.That(summary.TotalRules, Is.EqualTo(3));
            Assert.That(summary.PassedCount, Is.EqualTo(1));
            Assert.That(summary.WarningCount, Is.EqualTo(1));
            Assert.That(summary.ErrorCount, Is.EqualTo(1));
            Assert.That(summary.HasBlockingFailures, Is.True);
        }

        [Test]
        public void Initialize_WhenNoRules_EmitsEmptySummary()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            StartupValidationCompletedSignal summary = default;
            bool hasSummary = false;

            signalBus.Subscribe<StartupValidationCompletedSignal>(signal =>
            {
                summary = signal;
                hasSummary = true;
            });

            StartupValidationRunner runner = new StartupValidationRunner(new List<IStartupValidationRule>(), signalBus);
            runner.Initialize();

            Assert.That(hasSummary, Is.True);
            Assert.That(summary.TotalRules, Is.EqualTo(0));
            Assert.That(summary.PassedCount, Is.EqualTo(0));
            Assert.That(summary.WarningCount, Is.EqualTo(0));
            Assert.That(summary.ErrorCount, Is.EqualTo(0));
            Assert.That(summary.HasBlockingFailures, Is.False);
        }

        private sealed class FixedRule : IStartupValidationRule
        {
            private readonly StartupValidationResult _result;
            public string Name { get; }

            public FixedRule(string name, StartupValidationResult result)
            {
                Name = name;
                _result = result;
            }

            public StartupValidationResult Validate()
            {
                return _result;
            }
        }
    }
}
