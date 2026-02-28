using UnityEngine;

namespace Vareiko.Foundation.Observability
{
    public sealed class UnityConsoleLogSink : IFoundationLogSink
    {
        public void Write(FoundationLogEntry entry)
        {
            string formatted = $"[{entry.Category}] {entry.Message}";
            switch (entry.Level)
            {
                case FoundationLogLevel.Debug:
                case FoundationLogLevel.Info:
                    UnityEngine.Debug.Log(formatted);
                    break;
                case FoundationLogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formatted);
                    break;
                case FoundationLogLevel.Error:
                    UnityEngine.Debug.LogError(formatted);
                    break;
            }
        }
    }
}
