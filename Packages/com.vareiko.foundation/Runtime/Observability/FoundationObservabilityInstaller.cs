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

            container.Bind<IFoundationLogger>().To<UnityFoundationLogger>().AsSingle();
            container.BindInterfacesAndSelfTo<FoundationDiagnosticsService>().AsSingle().NonLazy();
        }
    }
}
