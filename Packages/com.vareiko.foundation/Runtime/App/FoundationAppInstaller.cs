using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.App
{
    public static class FoundationAppInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<AppStateMachine>(Lifetime.Singleton).As<IAppStateMachine>().AsSelf();

            // IApplicationLifecycleSource is optional/host-provided; when absent the service
            // creates and owns its own Unity source (preserving the Zenject optional behaviour).
            builder.RegisterEntryPoint<ApplicationLifecycleService>(resolver =>
                {
                    resolver.TryResolve<IApplicationLifecycleSource>(out IApplicationLifecycleSource source);
                    return new ApplicationLifecycleService(resolver.Resolve<IFoundationSignalBus>(), source);
                }, Lifetime.Singleton)
                .AsSelf();
        }
    }
}
