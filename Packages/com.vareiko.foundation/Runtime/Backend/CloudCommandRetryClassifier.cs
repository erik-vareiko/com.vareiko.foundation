using System;
using System.Collections.Generic;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public enum CloudCommandFailureKind
    {
        Unknown = 0,
        Retryable = 1,
        NonRetryable = 2,
        SuccessLike = 3
    }

    public readonly struct CloudCommandFailureClassification
    {
        public readonly CloudCommandFailureKind Kind;
        public readonly string ErrorCode;

        public CloudCommandFailureClassification(CloudCommandFailureKind kind, string errorCode)
        {
            Kind = kind;
            ErrorCode = errorCode ?? string.Empty;
        }
    }

    public interface ICloudCommandRetryClassifier
    {
        CloudCommandFailureClassification Classify(string errorCode, string errorMessage);
    }

    public sealed class CloudCommandRetryClassifier : ICloudCommandRetryClassifier
    {
        private static readonly string[] RetryableMessageMarkers =
        {
            "timeout",
            "timed out",
            "unavailable",
            "network",
            "temporar"
        };

        private static readonly string[] NonRetryableMessageMarkers =
        {
            "auth",
            "unauthor",
            "forbidden",
            "validation",
            "config",
            "dependency"
        };

        private static readonly string[] SuccessLikeMessageMarkers =
        {
            "duplicate",
            "already processed",
            "already_applied",
            "idempotenc"
        };

        private readonly Dictionary<string, BackendCommandConfig.ErrorRetryabilityRule> _rules;

        [Inject]
        public CloudCommandRetryClassifier([InjectOptional] BackendCommandConfig config = null)
        {
            _rules = new Dictionary<string, BackendCommandConfig.ErrorRetryabilityRule>(StringComparer.OrdinalIgnoreCase);
            if (config == null || config.ErrorRules == null)
            {
                return;
            }

            for (int i = 0; i < config.ErrorRules.Count; i++)
            {
                BackendCommandConfig.ErrorRetryabilityRule rule = config.ErrorRules[i];
                if (rule == null)
                {
                    continue;
                }

                string code = Normalize(rule.ErrorCode);
                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                _rules[code] = rule;
            }
        }

        public CloudCommandFailureClassification Classify(string errorCode, string errorMessage)
        {
            string normalizedCode = Normalize(errorCode);
            if (!string.IsNullOrEmpty(normalizedCode) && _rules.TryGetValue(normalizedCode, out BackendCommandConfig.ErrorRetryabilityRule configured))
            {
                if (configured.TreatAsSuccess)
                {
                    return new CloudCommandFailureClassification(CloudCommandFailureKind.SuccessLike, normalizedCode);
                }

                return configured.IsRetryable
                    ? new CloudCommandFailureClassification(CloudCommandFailureKind.Retryable, normalizedCode)
                    : new CloudCommandFailureClassification(CloudCommandFailureKind.NonRetryable, normalizedCode);
            }

            if (ContainsAny(normalizedCode, "duplicate", "idempotency") || ContainsAny(errorMessage, SuccessLikeMessageMarkers))
            {
                return new CloudCommandFailureClassification(CloudCommandFailureKind.SuccessLike, string.IsNullOrEmpty(normalizedCode) ? "Duplicate" : normalizedCode);
            }

            if (ContainsAny(normalizedCode, "timeout", "unavailable", "network") || ContainsAny(errorMessage, RetryableMessageMarkers))
            {
                return new CloudCommandFailureClassification(CloudCommandFailureKind.Retryable, string.IsNullOrEmpty(normalizedCode) ? "Transport.Unavailable" : normalizedCode);
            }

            if (ContainsAny(normalizedCode, "auth", "validation", "config", "dependency") || ContainsAny(errorMessage, NonRetryableMessageMarkers))
            {
                return new CloudCommandFailureClassification(CloudCommandFailureKind.NonRetryable, string.IsNullOrEmpty(normalizedCode) ? "Validation.Failed" : normalizedCode);
            }

            return new CloudCommandFailureClassification(CloudCommandFailureKind.NonRetryable, string.IsNullOrEmpty(normalizedCode) ? "Unknown" : normalizedCode);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool ContainsAny(string source, params string[] patterns)
        {
            if (string.IsNullOrWhiteSpace(source) || patterns == null || patterns.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < patterns.Length; i++)
            {
                string pattern = patterns[i];
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                if (source.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
