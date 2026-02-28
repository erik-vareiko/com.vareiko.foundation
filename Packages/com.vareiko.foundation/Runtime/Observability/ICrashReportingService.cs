namespace Vareiko.Foundation.Observability
{
    public interface ICrashReportingService
    {
        bool IsEnabled { get; }
        void Report(FoundationCrashReport report);
    }
}
