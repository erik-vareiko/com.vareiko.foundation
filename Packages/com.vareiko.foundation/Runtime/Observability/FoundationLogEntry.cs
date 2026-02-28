using System;

namespace Vareiko.Foundation.Observability
{
    public readonly struct FoundationLogEntry
    {
        public readonly DateTime TimestampUtc;
        public readonly FoundationLogLevel Level;
        public readonly string Message;
        public readonly string Category;

        public FoundationLogEntry(FoundationLogLevel level, string message, string category, DateTime timestampUtc)
        {
            TimestampUtc = timestampUtc;
            Level = level;
            Message = message ?? string.Empty;
            Category = string.IsNullOrWhiteSpace(category) ? "Foundation" : category;
        }
    }
}
