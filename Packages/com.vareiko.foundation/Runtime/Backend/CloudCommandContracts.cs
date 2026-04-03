using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Backend
{
    [Serializable]
    public readonly struct CloudCommandRequest
    {
        public readonly string CommandName;
        public readonly string IdempotencyKey;
        public readonly string CorrelationId;
        public readonly string RequestVersion;
        public readonly string PayloadJson;
        public readonly string PlayerId;
        public readonly long ClientUnixMs;
        public readonly IReadOnlyDictionary<string, string> Meta;

        public CloudCommandRequest(
            string commandName,
            string idempotencyKey,
            string correlationId,
            string requestVersion,
            string payloadJson,
            string playerId,
            long clientUnixMs,
            IReadOnlyDictionary<string, string> meta = null)
        {
            CommandName = commandName ?? string.Empty;
            IdempotencyKey = idempotencyKey ?? string.Empty;
            CorrelationId = correlationId ?? string.Empty;
            RequestVersion = requestVersion ?? string.Empty;
            PayloadJson = payloadJson;
            PlayerId = playerId ?? string.Empty;
            ClientUnixMs = clientUnixMs;
            Meta = meta;
        }
    }

    [Serializable]
    public readonly struct CloudCommandResponse
    {
        public readonly bool Success;
        public readonly bool IsRetryable;
        public readonly string ErrorCode;
        public readonly string ErrorMessage;
        public readonly string ResponseJson;
        public readonly string ProcessedIdempotencyKey;
        public readonly long ServerUnixMs;

        public CloudCommandResponse(
            bool success,
            bool isRetryable,
            string errorCode,
            string errorMessage,
            string responseJson,
            string processedIdempotencyKey,
            long serverUnixMs)
        {
            Success = success;
            IsRetryable = !success && isRetryable;
            ErrorCode = errorCode ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
            ResponseJson = responseJson ?? string.Empty;
            ProcessedIdempotencyKey = processedIdempotencyKey ?? string.Empty;
            ServerUnixMs = serverUnixMs;
        }

        public static CloudCommandResponse Succeed(string responseJson, string processedIdempotencyKey, long serverUnixMs)
        {
            return new CloudCommandResponse(true, false, string.Empty, string.Empty, responseJson, processedIdempotencyKey, serverUnixMs);
        }

        public static CloudCommandResponse Fail(string errorCode, string errorMessage, bool isRetryable = false)
        {
            return new CloudCommandResponse(false, isRetryable, errorCode, errorMessage, string.Empty, string.Empty, 0);
        }
    }

    public interface ICloudCommandService
    {
        UniTask<CloudCommandResponse> ExecuteAsync(CloudCommandRequest request, CancellationToken cancellationToken = default);
    }
}
