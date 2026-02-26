using Zenject;

namespace Vareiko.Foundation.SceneFlow
{
    public static class FoundationSceneFlowInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<ISceneFlowService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<SceneLoadStartedSignal>();
            container.DeclareSignal<SceneLoadProgressSignal>();
            container.DeclareSignal<SceneLoadCompletedSignal>();
            container.DeclareSignal<SceneUnloadStartedSignal>();
            container.DeclareSignal<SceneUnloadCompletedSignal>();

            container.Bind<ISceneFlowService>().To<SceneFlowService>().AsSingle();
        }
    }
}
