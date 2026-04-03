using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Backend
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Backend Command Config")]
    public sealed class BackendCommandConfig : ScriptableObject
    {
        [Serializable]
        public sealed class ErrorRetryabilityRule
        {
            [SerializeField] private string _errorCode = string.Empty;
            [SerializeField] private bool _isRetryable;
            [SerializeField] private bool _treatAsSuccess;

            public string ErrorCode => _errorCode ?? string.Empty;
            public bool IsRetryable => _isRetryable;
            public bool TreatAsSuccess => _treatAsSuccess;
        }

        [SerializeField] private string _gatewayFunctionName = "CommandGateway";
        [SerializeField] private string _defaultRequestVersion = "1";
        [SerializeField] private int _maxPayloadBytes = 65536;
        [SerializeField] private int _queueTtlHours = 24;
        [SerializeField] private List<ErrorRetryabilityRule> _errorRules = new List<ErrorRetryabilityRule>();

        public string GatewayFunctionName => string.IsNullOrWhiteSpace(_gatewayFunctionName) ? "CommandGateway" : _gatewayFunctionName.Trim();
        public string DefaultRequestVersion => string.IsNullOrWhiteSpace(_defaultRequestVersion) ? "1" : _defaultRequestVersion.Trim();
        public int MaxPayloadBytes => _maxPayloadBytes < 128 ? 128 : _maxPayloadBytes;
        public int QueueTtlHours => _queueTtlHours < 1 ? 1 : _queueTtlHours;
        public IReadOnlyList<ErrorRetryabilityRule> ErrorRules => _errorRules;
    }
}
