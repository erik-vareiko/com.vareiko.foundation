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
            container.DeclareSignal<UnhandledExceptionCapturedSignal>();

            if (config == null || config.LogToUnityConsole)
            {
                container.Bind<IFoundationLogSink>().To<UnityConsoleLogSink>().AsSingle().IfNotBound();
            }

            container.Bind<IFoundationLogger>().To<UnityFoundationLogger>().AsSingle();
            container.BindInterfacesAndSelfTo<FoundationDiagnosticsService>().AsSingle().NonLazy();
            container.BindInterfacesAndSelfTo<GlobalExceptionHandler>().AsSingle().NonLazy();
        }
    }
}
