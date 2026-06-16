using System;

namespace Vareiko.Foundation.Common
{
    [Serializable]
    public readonly struct HealthCheckResult
    {
        public readonly bool IsHealthy;
        public readonly string Message;

        public HealthCheckResult(bool isHealthy, string message)
        {
            IsHealthy = isHealthy;
            Message = message ?? string.Empty;
        }

        public static HealthCheckResult Healthy(string message = "")
        {
            return new HealthCheckResult(true, message);
        }

        public static HealthCheckResult Unhealthy(string message)
        {
            return new HealthCheckResult(false, message);
        }
    }
}
