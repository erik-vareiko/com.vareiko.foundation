using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Observability
{
    public static class FoundationObservabilityInstaller
    {
        public static void Install(IContainerBuilder builder, ObservabilityConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<ObservabilityConfig>());

            if (config == null || config.LogToUnityConsole)
            {
                builder.Register<UnityConsoleLogSink>(Lifetime.Singleton).As<IFoundationLogSink>();
            }

            string exportRootPath = Path.Combine(Application.persistentDataPath, "foundation-diagnostics");

            builder.Register<UnityFoundationLogger>(resolver => new UnityFoundationLogger(
                    resolver.Resolve<ObservabilityConfig>(),
                    resolver.Resolve<IFoundationSignalBus>(),
                    new List<IFoundationLogSink>(resolver.Resolve<IEnumerable<IFoundationLogSink>>())),
                Lifetime.Singleton)
                .As<IFoundationLogger>();
            builder.RegisterEntryPoint<FoundationDiagnosticsService>(Lifetime.Singleton).As<IDiagnosticsService>().AsSelf();
            builder.Register<DiagnosticsSnapshotExportService>(Lifetime.Singleton).As<IDiagnosticsSnapshotExportService>().WithParameter<string>(exportRootPath);

            // ICrashReportingService has no foundation implementation (host-provided); resolve it
            // leniently so the global exception handler still composes when none is bound.
            builder.RegisterEntryPoint<GlobalExceptionHandler>(resolver =>
                {
                    resolver.TryResolve<ICrashReportingService>(out ICrashReportingService crashReportingService);
                    return new GlobalExceptionHandler(
                        resolver.Resolve<IFoundationLogger>(),
                        resolver.Resolve<IAppStateMachine>(),
                        resolver.Resolve<ObservabilityConfig>(),
                        resolver.Resolve<IFoundationSignalBus>(),
                        crashReportingService);
                }, Lifetime.Singleton)
                .AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<LogMessageEmittedSignal>(signalOptions);
            builder.RegisterMessageBroker<DiagnosticsSnapshotUpdatedSignal>(signalOptions);
            builder.RegisterMessageBroker<DiagnosticsSnapshotExportedSignal>(signalOptions);
            builder.RegisterMessageBroker<DiagnosticsSnapshotExportFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<UnhandledExceptionCapturedSignal>(signalOptions);
            builder.RegisterMessageBroker<CrashReportSubmittedSignal>(signalOptions);
            builder.RegisterMessageBroker<CrashReportSubmissionFailedSignal>(signalOptions);
        }
    }
}
