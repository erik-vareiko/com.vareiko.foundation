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
        private readonly ICrashReportingService _crashReportingService;
        private bool _subscribed;

        [Inject]
        public GlobalExceptionHandler(
            [InjectOptional] IFoundationLogger logger = null,
            [InjectOptional] IAppStateMachine appStateMachine = null,
            [InjectOptional] ObservabilityConfig config = null,
            [InjectOptional] SignalBus signalBus = null,
            [InjectOptional] ICrashReportingService crashReportingService = null)
        {
            _signalBus = signalBus;
            _logger = logger;
            _appStateMachine = appStateMachine;
            _config = config;
            _crashReportingService = crashReportingService;
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
            FoundationCrashReport crashReport = new FoundationCrashReport(DateTime.UtcNow, source, message, safeStackTrace, details);

            _signalBus?.Fire(new UnhandledExceptionCapturedSignal(source, message, safeStackTrace));
            _logger?.Error($"Unhandled exception from {source}: {details}", "UnhandledException");
            TrySubmitCrashReport(crashReport);

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

        private void TrySubmitCrashReport(FoundationCrashReport report)
        {
            bool shouldForward = _config == null || _config.ForwardUnhandledExceptionsToCrashReporting;
            if (!shouldForward || _crashReportingService == null || !_crashReportingService.IsEnabled)
            {
                return;
            }

            try
            {
                _crashReportingService.Report(report);
                _signalBus?.Fire(new CrashReportSubmittedSignal(report.Source));
            }
            catch (Exception exception)
            {
                string error = exception != null ? exception.Message : "Crash report submission failed.";
                _signalBus?.Fire(new CrashReportSubmissionFailedSignal(report.Source, error));
                _logger?.Warn($"Crash report submission failed: {error}", "CrashReporting");
            }
        }
    }
}
