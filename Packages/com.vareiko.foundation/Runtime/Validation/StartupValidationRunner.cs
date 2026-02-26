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
            for (int i = 0; i < _rules.Count; i++)
            {
                IStartupValidationRule rule = _rules[i];
                if (rule == null)
                {
                    continue;
                }

                StartupValidationResult result = rule.Validate();
                string ruleName = string.IsNullOrWhiteSpace(rule.Name) ? rule.GetType().Name : rule.Name;

                if (result.IsValid)
                {
                    _signalBus?.Fire(new StartupValidationPassedSignal(ruleName, result.Message));
                    continue;
                }

                _signalBus?.Fire(new StartupValidationFailedSignal(ruleName, result.Message));
                Debug.LogError($"[Foundation Validation] {ruleName}: {result.Message}");
            }
        }
    }
}
