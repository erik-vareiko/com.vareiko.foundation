using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.UI
{
    public static class FoundationUIInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            // UIRegistry is host-provided (a scene MonoBehaviour) and therefore optional; resolve
            // it leniently so UIService composes whether or not a registry is bound.
            builder.RegisterEntryPoint<UIService>(resolver =>
                {
                    resolver.TryResolve<UIRegistry>(out UIRegistry registry);
                    return new UIService(registry, resolver.Resolve<IFoundationSignalBus>());
                }, Lifetime.Singleton)
                .AsSelf();
            builder.Register<UIWindowManager>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UIConfirmDialogService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UIValueEventService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
