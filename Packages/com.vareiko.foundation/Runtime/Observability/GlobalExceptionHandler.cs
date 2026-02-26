using System;
using System.Threading.Tasks;
using Vareiko.Foundation.App;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public sealed class GlobalExceptionHandler : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly IFoundationLogger _logger;
        private readonly IAppStateMachine _appStateMachine;
        private readonly ObservabilityConfig _config;
        private bool _subscribed;

        [Inject]
        public GlobalExceptionHandler(
            [InjectOptional] IFoundationLogger logger = null,
            [InjectOptional] IAppStateMachine appStateMachine = null,
            [InjectOptional] ObservabilityConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _signalBus = signalBus;
            _logger = logger;
            _appStateMachine = appStateMachine;
            _config = config;
        }

        public void Initialize()
        {
            if (_config != null && !_config.CaptureUnhandledExceptions)
            {
                return;
            }

            Subscribe();
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            Application.logMessageReceived += OnUnityLogMessageReceived;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            Application.logMessageReceived -= OnUnityLogMessageReceived;
            _subscribed = false;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception exception = args != null ? args.ExceptionObject as Exception : null;
            ReportUnhandledException("AppDomain", exception, exception != null ? exception.StackTrace : string.Empty);
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            Exception exception = args != null ? args.Exception : null;
            ReportUnhandledException("TaskScheduler", exception, exception != null ? exception.StackTrace : string.Empty);
            args?.SetObserved();
        }

        private void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }

            ReportUnhandledException("UnityLog", null, stackTrace, condition);
        }

        private void ReportUnhandledException(string source, Exception exception, string stackTrace, string messageOverride = null)
        {
            string message = !string.IsNullOrWhiteSpace(messageOverride)
                ? messageOverride
                : (exception != null ? exception.Message : "Unhandled exception");
            string details = exception != null ? exception.ToString() : message;
            string safeStackTrace = stackTrace ?? string.Empty;

            _signalBus?.Fire(new UnhandledExceptionCapturedSignal(source, message, safeStackTrace));
            _logger?.Error($"Unhandled exception from {source}: {details}", "UnhandledException");

            bool shouldTransition = _config == null || _config.TransitionToErrorStateOnUnhandledException;
            if (!shouldTransition ||
                _appStateMachine == null ||
                _appStateMachine.IsIn(AppState.Shutdown) ||
                _appStateMachine.IsIn(AppState.Error))
            {
                return;
            }

            _appStateMachine.TryEnter(AppState.Error);
        }
    }
}
