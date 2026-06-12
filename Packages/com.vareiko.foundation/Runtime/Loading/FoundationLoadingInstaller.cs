using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Loading
{
    public static class FoundationLoadingInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LoadingService>(Lifetime.Singleton).As<ILoadingService>().AsSelf();
        }
    }
}
