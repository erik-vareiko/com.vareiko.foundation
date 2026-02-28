using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Observability
{
    public sealed class GlobalExceptionHandlerTests
    {
        [Test]
        public void Initialize_WhenCaptureDisabled_DoesNotSubscribe()
        {
            ObservabilityConfig config = ScriptableObject.CreateInstance<ObservabilityConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_captureUnhandledExceptions", false);
                GlobalExceptionHandler handler = new GlobalExceptionHandler(null, null, config, null);

                handler.Initialize();

                Assert.That(IsSubscribed(handler), Is.False);
                handler.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void InitializeAndDispose_TogglesSubscription()
        {
            GlobalExceptionHandler handler = new GlobalExceptionHandler();
            try
            {
                handler.Initialize();
                Assert.That(IsSubscribed(handler), Is.True);

                handler.Dispose();
                Assert.That(IsSubscribed(handler), Is.False);
            }
            finally
            {
                handler.Dispose();
            }
        }

        [Test]
        public void ReportUnhandledException_FiresSignal_Logs_AndTransitionsToError()
        {
            SignalBus signalBus = CreateSignalBus();
            UnhandledExceptionCapturedSignal captured = default;
            bool hasSignal = false;
            signalBus.Subscribe<UnhandledExceptionCapturedSignal>(signal =>
            {
                captured = signal;
                hasSignal = true;
            });

            SpyFoundationLogger logger = new SpyFoundationLogger();
            FakeAppStateMachine appState = new FakeAppStateMachine(AppState.Gameplay);
            GlobalExceptionHandler handler = new GlobalExceptionHandler(logger, appState, null, signalBus);

            InvokeReport(handler, "TestSource", new InvalidOperationException("boom"), "stack-trace", null);

            Assert.That(hasSignal, Is.True);
            Assert.That(captured.Source, Is.EqualTo("TestSource"));
            Assert.That(captured.Message, Is.EqualTo("boom"));
            Assert.That(captured.StackTrace, Is.EqualTo("stack-trace"));
            Assert.That(logger.Errors.Count, Is.EqualTo(1));
            Assert.That(logger.Errors[0].Category, Is.EqualTo("UnhandledException"));
            Assert.That(logger.Errors[0].Message, Does.Contain("Unhandled exception from TestSource"));
            Assert.That(appState.TryEnterCalls, Is.EqualTo(1));
            Assert.That(appState.Current, Is.EqualTo(AppState.Error));
        }

        [Test]
        public void ReportUnhandledException_DoesNotTransition_WhenTransitionDisabled()
        {
            ObservabilityConfig config = ScriptableObject.CreateInstance<ObservabilityConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_transitionToErrorStateOnUnhandledException", false);
                FakeAppStateMachine appState = new FakeAppStateMachine(AppState.Gameplay);
                GlobalExceptionHandler handler = new GlobalExceptionHandler(null, appState, config, null);

                InvokeReport(handler, "Test", null, "stack", "message");

                Assert.That(appState.TryEnterCalls, Is.EqualTo(0));
                Assert.That(appState.Current, Is.EqualTo(AppState.Gameplay));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ReportUnhandledException_DoesNotTransition_WhenStateAlreadyError()
        {
            FakeAppStateMachine appState = new FakeAppStateMachine(AppState.Error);
            GlobalExceptionHandler handler = new GlobalExceptionHandler(null, appState, null, null);

            InvokeReport(handler, "Test", new Exception("x"), "stack", null);

            Assert.That(appState.TryEnterCalls, Is.EqualTo(0));
            Assert.That(appState.Current, Is.EqualTo(AppState.Error));
        }

        [Test]
        public void ReportUnhandledException_WithCrashReporter_EmitsSubmittedSignal()
        {
            SignalBus signalBus = CreateSignalBus();
            bool submitted = false;
            signalBus.Subscribe<CrashReportSubmittedSignal>(_ => submitted = true);

            SpyCrashReportingService crashReporting = new SpyCrashReportingService(true);
            GlobalExceptionHandler handler = new GlobalExceptionHandler(null, null, null, signalBus, crashReporting);

            InvokeReport(handler, "TestSource", new InvalidOperationException("boom"), "stack", null);

            Assert.That(crashReporting.Reports.Count, Is.EqualTo(1));
            Assert.That(crashReporting.Reports[0].Source, Is.EqualTo("TestSource"));
            Assert.That(crashReporting.Reports[0].Message, Is.EqualTo("boom"));
            Assert.That(submitted, Is.True);
        }

        [Test]
        public void ReportUnhandledException_WhenCrashReporterThrows_EmitsFailedSignal()
        {
            SignalBus signalBus = CreateSignalBus();
            CrashReportSubmissionFailedSignal failed = default;
            bool hasFailedSignal = false;
            signalBus.Subscribe<CrashReportSubmissionFailedSignal>(signal =>
            {
                failed = signal;
                hasFailedSignal = true;
            });

            SpyCrashReportingService crashReporting = new SpyCrashReportingService(true) { ThrowOnReport = true };
            GlobalExceptionHandler handler = new GlobalExceptionHandler(null, null, null, signalBus, crashReporting);

            InvokeReport(handler, "TaskScheduler", new InvalidOperationException("boom"), "stack", null);

            Assert.That(hasFailedSignal, Is.True);
            Assert.That(failed.Source, Is.EqualTo("TaskScheduler"));
            Assert.That(failed.Error, Does.Contain("submit fail"));
        }

        [Test]
        public void ReportUnhandledException_WhenCrashReportingForwardingDisabled_DoesNotSubmit()
        {
            ObservabilityConfig config = ScriptableObject.CreateInstance<ObservabilityConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_forwardUnhandledExceptionsToCrashReporting", false);
                SpyCrashReportingService crashReporting = new SpyCrashReportingService(true);
                GlobalExceptionHandler handler = new GlobalExceptionHandler(null, null, config, null, crashReporting);

                InvokeReport(handler, "UnityLog", new InvalidOperationException("boom"), "stack", null);

                Assert.That(crashReporting.Reports.Count, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<UnhandledExceptionCapturedSignal>();
            container.DeclareSignal<CrashReportSubmittedSignal>();
            container.DeclareSignal<CrashReportSubmissionFailedSignal>();
            return container.Resolve<SignalBus>();
        }

        private static bool IsSubscribed(GlobalExceptionHandler handler)
        {
            FieldInfo field = typeof(GlobalExceptionHandler).GetField("_subscribed", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && (bool)field.GetValue(handler);
        }

        private static void InvokeReport(
            GlobalExceptionHandler handler,
            string source,
            Exception exception,
            string stackTrace,
            string messageOverride)
        {
            MethodInfo method = typeof(GlobalExceptionHandler).GetMethod("ReportUnhandledException", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(handler, new object[] { source, exception, stackTrace, messageOverride });
        }

        private sealed class FakeAppStateMachine : IAppStateMachine
        {
            public AppState Current { get; private set; }
            public int TryEnterCalls { get; private set; }

            public FakeAppStateMachine(AppState initial)
            {
                Current = initial;
            }

            public bool IsIn(AppState state)
            {
                return Current == state;
            }

            public bool TryEnter(AppState next)
            {
                TryEnterCalls++;
                Current = next;
                return true;
            }

            public void ForceEnter(AppState next)
            {
                Current = next;
            }
        }

        private sealed class SpyFoundationLogger : IFoundationLogger
        {
            public readonly List<LogEntry> Errors = new List<LogEntry>(2);

            public FoundationLogLevel MinimumLevel => FoundationLogLevel.Debug;

            public void Log(FoundationLogLevel level, string message, string category = null)
            {
            }

            public void Debug(string message, string category = null)
            {
            }

            public void Info(string message, string category = null)
            {
            }

            public void Warn(string message, string category = null)
            {
            }

            public void Error(string message, string category = null)
            {
                Errors.Add(new LogEntry(message ?? string.Empty, category ?? string.Empty));
            }
        }

        private readonly struct LogEntry
        {
            public readonly string Message;
            public readonly string Category;

            public LogEntry(string message, string category)
            {
                Message = message;
                Category = category;
            }
        }

        private sealed class SpyCrashReportingService : ICrashReportingService
        {
            public readonly List<FoundationCrashReport> Reports = new List<FoundationCrashReport>(2);
            public bool IsEnabled { get; }
            public bool ThrowOnReport { get; set; }

            public SpyCrashReportingService(bool isEnabled)
            {
                IsEnabled = isEnabled;
            }

            public void Report(FoundationCrashReport report)
            {
                if (ThrowOnReport)
                {
                    throw new InvalidOperationException("submit fail");
                }

                Reports.Add(report);
            }
        }
    }
}
