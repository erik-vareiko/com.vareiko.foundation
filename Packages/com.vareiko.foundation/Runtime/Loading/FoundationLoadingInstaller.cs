using Zenject;

namespace Vareiko.Foundation.Loading
{
    public static class FoundationLoadingInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<ILoadingService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<LoadingStateChangedSignal>();
            container.BindInterfacesAndSelfTo<LoadingService>().AsSingle().NonLazy();
        }
    }
}
