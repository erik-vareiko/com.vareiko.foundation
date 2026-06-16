using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Economy
{
    public static class FoundationEconomyInstaller
    {
        public static void Install(IContainerBuilder builder, EconomyConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<EconomyConfig>());
            builder.RegisterEntryPoint<InMemoryEconomyService>(Lifetime.Singleton).As<IEconomyService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<CurrencyBalanceChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<InventoryItemChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<EconomyOperationFailedSignal>(signalOptions);
        }
    }
}
