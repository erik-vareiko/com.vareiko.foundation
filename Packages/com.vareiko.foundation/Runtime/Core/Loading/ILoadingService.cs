namespace Vareiko.Foundation.Loading
{
    public interface ILoadingService
    {
        bool IsLoading { get; }
        float Progress { get; }
        string ActiveOperation { get; }
        void BeginManual(string operationName);
        void SetManualProgress(float progress);
        void CompleteManual();
    }
}
