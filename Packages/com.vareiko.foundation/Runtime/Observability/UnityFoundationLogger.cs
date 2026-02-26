using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public sealed class UnityFoundationLogger : IFoundationLogger
    {
        private readonly ObservabilityConfig _config;
        private readonly SignalBus _signalBus;

        [Inject]
        public UnityFoundationLogger([InjectOptional] ObservabilityConfig config = null, [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _signalBus = signalBus;
        }

        public FoundationLogLevel MinimumLevel => _config != null ? _config.MinimumLogLevel : FoundationLogLevel.Info;

        public void Log(FoundationLogLevel level, string message, string category = null)
        {
            if (level < MinimumLevel)
            {
                return;
            }

            string scope = string.IsNullOrWhiteSpace(category) ? "Foundation" : category;
            string formatted = $"[{scope}] {message}";
            if (_config == null || _config.LogToUnityConsole)
            {
                switch (level)
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

            _signalBus?.Fire(new LogMessageEmittedSignal(level, message ?? string.Empty, scope));
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
    }
}
