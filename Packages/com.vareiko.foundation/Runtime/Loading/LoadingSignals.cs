namespace Vareiko.Foundation.Loading
{
    public readonly struct LoadingStateChangedSignal
    {
        public readonly bool IsLoading;
        public readonly float Progress;
        public readonly string OperationName;

        public LoadingStateChangedSignal(bool isLoading, float progress, string operationName)
        {
            IsLoading = isLoading;
            Progress = progress;
            OperationName = operationName;
        }
    }
}
