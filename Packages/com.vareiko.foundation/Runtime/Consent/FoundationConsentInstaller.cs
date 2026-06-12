using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Consent
{
    public static class FoundationConsentInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ConsentService>(Lifetime.Singleton).As<IConsentService>().AsSelf();
        }
    }
}
