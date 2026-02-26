using Zenject;

namespace Vareiko.Foundation.Economy
{
    public static class FoundationEconomyInstaller
    {
        public static void Install(DiContainer container, EconomyConfig config = null)
        {
            if (container.HasBinding<IEconomyService>())
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

            container.DeclareSignal<CurrencyBalanceChangedSignal>();
            container.DeclareSignal<InventoryItemChangedSignal>();
            container.DeclareSignal<EconomyOperationFailedSignal>();
            container.BindInterfacesAndSelfTo<InMemoryEconomyService>().AsSingle().NonLazy();
        }
    }
}
