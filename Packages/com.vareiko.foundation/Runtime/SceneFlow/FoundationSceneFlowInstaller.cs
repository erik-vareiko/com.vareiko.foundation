using VContainer;

namespace Vareiko.Foundation.SceneFlow
{
    public static class FoundationSceneFlowInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<SceneFlowService>(Lifetime.Singleton).As<ISceneFlowService>();
        }
    }
}
