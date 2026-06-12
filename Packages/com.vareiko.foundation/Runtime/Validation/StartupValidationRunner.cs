using System.Collections.Generic;
using UnityEngine;
using Vareiko.Foundation.Signals;
using Zenject;

namespace Vareiko.Foundation.Validation
{
    public sealed class StartupValidationRunner : VContainer.Unity.IInitializable
    {
        private readonly List<IStartupValidationRule> _rules;
        private readonly IFoundationSignalBus _signalBus;

        [Inject]
        public StartupValidationRunner([InjectOptional] List<IStartupValidationRule> rules = null, [InjectOptional] IFoundationSignalBus signalBus = null)
        {
            _rules = rules ?? new List<IStartupValidationRule>(0);
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            int total = 0;
            int passed = 0;
            int warnings = 0;
            int errors = 0;

            for (int i = 0; i < _rules.Count; i++)
            {
                IStartupValidationRule rule = _rules[i];
                if (rule == null)
                {
                    continue;
                }

                total++;
                StartupValidationResult result = rule.Validate();
                string ruleName = string.IsNullOrWhiteSpace(rule.Name) ? rule.GetType().Name : rule.Name;

                if (result.Severity == StartupValidationSeverity.Error)
                {
                    errors++;
                    _signalBus?.Publish(new StartupValidationFailedSignal(ruleName, result.Message));
                    Debug.LogError($"[Foundation Validation] {ruleName}: {result.Message}");
                    continue;
                }

                if (result.Severity == StartupValidationSeverity.Warning)
                {
                    warnings++;
                    _signalBus?.Publish(new StartupValidationWarningSignal(ruleName, result.Message));
                    Debug.LogWarning($"[Foundation Validation] {ruleName}: {result.Message}");
                    continue;
                }

                passed++;
                _signalBus?.Publish(new StartupValidationPassedSignal(ruleName, result.Message));
            }

            _signalBus?.Publish(new StartupValidationCompletedSignal(total, passed, warnings, errors));
        }
    }
}
