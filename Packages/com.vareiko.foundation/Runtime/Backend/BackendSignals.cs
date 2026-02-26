namespace Vareiko.Foundation.Backend
{
    public readonly struct BackendAuthStateChangedSignal
    {
        public readonly bool IsAuthenticated;
        public readonly string PlayerId;

        public BackendAuthStateChangedSignal(bool isAuthenticated, string playerId)
        {
            IsAuthenticated = isAuthenticated;
            PlayerId = playerId;
        }
    }

    public readonly struct BackendOperationRetriedSignal
    {
        public readonly string OperationName;
        public readonly int Attempt;
        public readonly int MaxAttempts;
        public readonly string Error;

        public BackendOperationRetriedSignal(string operationName, int attempt, int maxAttempts, string error)
        {
            OperationName = operationName;
            Attempt = attempt;
            MaxAttempts = maxAttempts;
            Error = error;
        }
    }

    public readonly struct CloudFunctionQueuedSignal
    {
        public readonly string FunctionName;
        public readonly int QueueSize;
        public readonly string Reason;

        public CloudFunctionQueuedSignal(string functionName, int queueSize, string reason)
        {
            FunctionName = functionName;
            QueueSize = queueSize;
            Reason = reason;
        }
    }

    public readonly struct CloudFunctionQueueFlushedSignal
    {
        public readonly int InitialQueueSize;
        public readonly int FlushedCount;
        public readonly int RemainingCount;

        public CloudFunctionQueueFlushedSignal(int initialQueueSize, int flushedCount, int remainingCount)
        {
            InitialQueueSize = initialQueueSize;
            FlushedCount = flushedCount;
            RemainingCount = remainingCount;
        }
    }

    public readonly struct RemoteConfigRefreshedSignal
    {
        public readonly int ValueCount;
        public readonly string Source;

        public RemoteConfigRefreshedSignal(int valueCount, string source)
        {
            ValueCount = valueCount;
            Source = source;
        }
    }

    public readonly struct RemoteConfigRefreshFailedSignal
    {
        public readonly string Error;

        public RemoteConfigRefreshFailedSignal(string error)
        {
            Error = error;
        }
    }
}
