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
}
