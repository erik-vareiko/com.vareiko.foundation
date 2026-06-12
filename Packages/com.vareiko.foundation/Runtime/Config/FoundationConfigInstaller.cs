using VContainer;

namespace Vareiko.Foundation.Config
{
    public static class FoundationConfigInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<ConfigService>(Lifetime.Singleton).As<IConfigService>();
        }
    }
}
