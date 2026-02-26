using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Validation
{
    public sealed class StartupValidationRunner : IInitializable
    {
        private readonly List<IStartupValidationRule> _rules;
        private readonly SignalBus _signalBus;

        [Inject]
        public StartupValidationRunner([InjectOptional] List<IStartupValidationRule> rules = null, [InjectOptional] SignalBus signalBus = null)
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
                    _signalBus?.Fire(new StartupValidationFailedSignal(ruleName, result.Message));
                    Debug.LogError($"[Foundation Validation] {ruleName}: {result.Message}");
                    continue;
                }

                if (result.Severity == StartupValidationSeverity.Warning)
                {
                    warnings++;
                    _signalBus?.Fire(new StartupValidationWarningSignal(ruleName, result.Message));
                    Debug.LogWarning($"[Foundation Validation] {ruleName}: {result.Message}");
                    continue;
                }

                passed++;
                _signalBus?.Fire(new StartupValidationPassedSignal(ruleName, result.Message));
            }

            _signalBus?.Fire(new StartupValidationCompletedSignal(total, passed, warnings, errors));
        }
    }
}
