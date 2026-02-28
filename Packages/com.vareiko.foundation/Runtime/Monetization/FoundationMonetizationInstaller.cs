using Zenject;

namespace Vareiko.Foundation.Monetization
{
    public static class FoundationMonetizationInstaller
    {
        public static void Install(DiContainer container, MonetizationPolicyConfig config = null)
        {
            if (container.HasBinding<IMonetizationPolicyService>())
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

            container.DeclareSignal<MonetizationAdBlockedSignal>();
            container.DeclareSignal<MonetizationAdRecordedSignal>();
            container.DeclareSignal<MonetizationIapBlockedSignal>();
            container.DeclareSignal<MonetizationIapRecordedSignal>();
            container.DeclareSignal<MonetizationSessionResetSignal>();

            container.BindInterfacesAndSelfTo<MonetizationPolicyService>().AsSingle().NonLazy();
        }
    }
}
