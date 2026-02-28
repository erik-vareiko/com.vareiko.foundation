namespace Vareiko.Foundation.Observability
{
    public readonly struct LogMessageEmittedSignal
    {
        public readonly FoundationLogLevel Level;
        public readonly string Message;
        public readonly string Category;

        public LogMessageEmittedSignal(FoundationLogLevel level, string message, string category)
        {
            Level = level;
            Message = message;
            Category = category;
        }
    }

    public readonly struct DiagnosticsSnapshotUpdatedSignal
    {
        public readonly DiagnosticsSnapshot Snapshot;

        public DiagnosticsSnapshotUpdatedSignal(DiagnosticsSnapshot snapshot)
        {
            Snapshot = snapshot;
        }
    }

    public readonly struct DiagnosticsSnapshotExportedSignal
    {
        public readonly string FilePath;

        public DiagnosticsSnapshotExportedSignal(string filePath)
        {
            FilePath = filePath ?? string.Empty;
        }
    }

    public readonly struct DiagnosticsSnapshotExportFailedSignal
    {
        public readonly string Error;

        public DiagnosticsSnapshotExportFailedSignal(string error)
        {
            Error = error ?? string.Empty;
        }
    }

    public readonly struct UnhandledExceptionCapturedSignal
    {
        public readonly string Source;
        public readonly string Message;
        public readonly string StackTrace;

        public UnhandledExceptionCapturedSignal(string source, string message, string stackTrace)
        {
            Source = source ?? string.Empty;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
        }
    }

    public readonly struct CrashReportSubmittedSignal
    {
        public readonly string Source;

        public CrashReportSubmittedSignal(string source)
        {
            Source = source ?? string.Empty;
        }
    }

    public readonly struct CrashReportSubmissionFailedSignal
    {
        public readonly string Source;
        public readonly string Error;

        public CrashReportSubmissionFailedSignal(string source, string error)
        {
            Source = source ?? string.Empty;
            Error = error ?? string.Empty;
        }
    }
}
