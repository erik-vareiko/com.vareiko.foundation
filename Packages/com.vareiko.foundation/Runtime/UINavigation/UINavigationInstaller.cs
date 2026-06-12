using VContainer;

namespace Vareiko.Foundation.UINavigation
{
    public static class FoundationUINavigationInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<UINavigationService>(Lifetime.Singleton).As<IUINavigationService>();
        }
    }
}
