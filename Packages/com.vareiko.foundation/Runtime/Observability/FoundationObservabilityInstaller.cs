using System.IO;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public static class FoundationObservabilityInstaller
    {
        public static void Install(DiContainer container, ObservabilityConfig config = null)
        {
            if (container.HasBinding<IFoundationLogger>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            container.DeclareSignal<LogMessageEmittedSignal>();
            container.DeclareSignal<DiagnosticsSnapshotUpdatedSignal>();
            container.DeclareSignal<DiagnosticsSnapshotExportedSignal>();
            container.DeclareSignal<DiagnosticsSnapshotExportFailedSignal>();
            container.DeclareSignal<UnhandledExceptionCapturedSignal>();
            container.DeclareSignal<CrashReportSubmittedSignal>();
            container.DeclareSignal<CrashReportSubmissionFailedSignal>();

            if (config == null || config.LogToUnityConsole)
            {
                container.Bind<IFoundationLogSink>().To<UnityConsoleLogSink>().AsSingle().IfNotBound();
            }

            container.Bind<string>()
                .WithId("DiagnosticsExportRootPath")
                .FromInstance(Path.Combine(Application.persistentDataPath, "foundation-diagnostics"))
                .AsSingle()
                .IfNotBound();

            container.Bind<IFoundationLogger>().To<UnityFoundationLogger>().AsSingle();
            container.BindInterfacesAndSelfTo<MonetizationObservabilityService>().AsSingle().NonLazy();
            container.BindInterfacesAndSelfTo<FoundationDiagnosticsService>().AsSingle().NonLazy();
            container.Bind<IDiagnosticsSnapshotExportService>().To<DiagnosticsSnapshotExportService>().AsSingle();
            container.BindInterfacesAndSelfTo<GlobalExceptionHandler>().AsSingle().NonLazy();
        }
    }
}
