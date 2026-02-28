using System;

namespace Vareiko.Foundation.Observability
{
    public readonly struct FoundationCrashReport
    {
        public readonly DateTime TimestampUtc;
        public readonly string Source;
        public readonly string Message;
        public readonly string StackTrace;
        public readonly string Details;

        public FoundationCrashReport(DateTime timestampUtc, string source, string message, string stackTrace, string details)
        {
            TimestampUtc = timestampUtc;
            Source = source ?? string.Empty;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            Details = details ?? string.Empty;
        }
    }
}
