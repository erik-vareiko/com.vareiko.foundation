using Zenject;

namespace Vareiko.Foundation.Iap
{
    public static class FoundationIapInstaller
    {
        public static void Install(DiContainer container, IapConfig config = null)
        {
            if (container.HasBinding<IInAppPurchaseService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<IapInitializedSignal>();
            container.DeclareSignal<IapPurchaseSucceededSignal>();
            container.DeclareSignal<IapPurchaseFailedSignal>();
            container.DeclareSignal<IapRestoreCompletedSignal>();
            container.DeclareSignal<IapRestoreFailedSignal>();
            container.DeclareSignal<IapOperationTelemetrySignal>();

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            if (config != null && config.Provider == InAppPurchaseProviderType.Simulated)
            {
                container.BindInterfacesAndSelfTo<SimulatedInAppPurchaseService>().AsSingle().NonLazy();
            }
            else if (config != null && config.Provider == InAppPurchaseProviderType.UnityIap)
            {
                container.BindInterfacesAndSelfTo<UnityInAppPurchaseService>().AsSingle().NonLazy();
            }
            else
            {
                container.Bind<IInAppPurchaseService>().To<NullInAppPurchaseService>().AsSingle();
            }
        }
    }
}
