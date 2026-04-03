using System;

namespace Vareiko.Foundation.Backend
{
    [Serializable]
    public readonly struct CloudCommandQueueItem
    {
        public readonly int Version;
        public readonly string FunctionName;
        public readonly string PayloadJson;
        public readonly string RequestJson;
        public readonly string IdempotencyKey;
        public readonly int AttemptCount;
        public readonly long FirstQueuedUnixMs;
        public readonly long LastAttemptUnixMs;

        public CloudCommandQueueItem(
            int version,
            string functionName,
            string payloadJson,
            string requestJson,
            string idempotencyKey,
            int attemptCount,
            long firstQueuedUnixMs,
            long lastAttemptUnixMs)
        {
            Version = version < 2 ? 2 : version;
            FunctionName = functionName ?? string.Empty;
            PayloadJson = payloadJson ?? string.Empty;
            RequestJson = requestJson ?? string.Empty;
            IdempotencyKey = idempotencyKey ?? string.Empty;
            AttemptCount = attemptCount < 0 ? 0 : attemptCount;
            FirstQueuedUnixMs = firstQueuedUnixMs;
            LastAttemptUnixMs = lastAttemptUnixMs;
        }

        public static CloudCommandQueueItem Create(
            string functionName,
            string payloadJson,
            string requestJson,
            string idempotencyKey,
            long nowUnixMs)
        {
            return new CloudCommandQueueItem(
                2,
                functionName,
                payloadJson,
                requestJson,
                idempotencyKey,
                0,
                nowUnixMs,
                0);
        }

        public CloudCommandQueueItem RegisterAttempt(long nowUnixMs)
        {
            return new CloudCommandQueueItem(
                Version,
                FunctionName,
                PayloadJson,
                RequestJson,
                IdempotencyKey,
                AttemptCount + 1,
                FirstQueuedUnixMs,
                nowUnixMs);
        }
    }
}
