using System;
using System.Collections.Generic;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public sealed class UnityFoundationLogger : IFoundationLogger
    {
        private readonly ObservabilityConfig _config;
        private readonly SignalBus _signalBus;
        private readonly List<IFoundationLogSink> _sinks;

        [Inject]
        public UnityFoundationLogger(
            [InjectOptional] ObservabilityConfig config = null,
            [InjectOptional] SignalBus signalBus = null,
            [InjectOptional] List<IFoundationLogSink> sinks = null)
        {
            _config = config;
            _signalBus = signalBus;
            _sinks = sinks ?? new List<IFoundationLogSink>(0);
        }

        public FoundationLogLevel MinimumLevel => _config != null ? _config.MinimumLogLevel : FoundationLogLevel.Info;

        public void Log(FoundationLogLevel level, string message, string category = null)
        {
            if (level < MinimumLevel)
            {
                return;
            }

            string scope = string.IsNullOrWhiteSpace(category) ? "Foundation" : category;
            string safeMessage = message ?? string.Empty;
            FoundationLogEntry entry = new FoundationLogEntry(level, safeMessage, scope, DateTime.UtcNow);

            bool hasSink = _sinks.Count > 0;
            if (hasSink)
            {
                for (int i = 0; i < _sinks.Count; i++)
                {
                    IFoundationLogSink sink = _sinks[i];
                    sink?.Write(entry);
                }
            }
            else if (_config == null || _config.LogToUnityConsole)
            {
                // Backward-compatible fallback when installer bindings are customized.
                WriteToUnityConsole(entry);
            }

            _signalBus?.Fire(new LogMessageEmittedSignal(level, safeMessage, scope));
        }

        public void Debug(string message, string category = null)
        {
            Log(FoundationLogLevel.Debug, message, category);
        }

        public void Info(string message, string category = null)
        {
            Log(FoundationLogLevel.Info, message, category);
        }

        public void Warn(string message, string category = null)
        {
            Log(FoundationLogLevel.Warning, message, category);
        }

        public void Error(string message, string category = null)
        {
            Log(FoundationLogLevel.Error, message, category);
        }

        private static void WriteToUnityConsole(FoundationLogEntry entry)
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
