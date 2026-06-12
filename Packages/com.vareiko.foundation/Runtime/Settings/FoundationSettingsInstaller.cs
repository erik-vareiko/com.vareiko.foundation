using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Settings
{
    public static class FoundationSettingsInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<SettingsService>(Lifetime.Singleton).As<ISettingsService>().AsSelf();
        }
    }
}
