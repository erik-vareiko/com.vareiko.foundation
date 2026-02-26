using Zenject;

namespace Vareiko.Foundation.Analytics
{
    public static class FoundationAnalyticsInstaller
    {
        public static void Install(DiContainer container, AnalyticsConfig config = null)
        {
            if (container.HasBinding<IAnalyticsService>())
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

            container.DeclareSignal<AnalyticsEventTrackedSignal>();
            container.DeclareSignal<AnalyticsEventDroppedSignal>();
            container.Bind<IAnalyticsService>().To<AnalyticsService>().AsSingle();
        }
    }
}
