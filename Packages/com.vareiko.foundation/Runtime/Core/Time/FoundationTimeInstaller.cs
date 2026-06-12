using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Time
{
    public static class FoundationTimeInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<UnityFoundationTimeProvider>(Lifetime.Singleton).As<IFoundationTimeProvider>();
            // Entry point so the container's player-loop drives Tick in play mode; tests drive
            // TickService.Advance manually.
            builder.RegisterEntryPoint<TickService>(Lifetime.Singleton).As<ITickService>().AsSelf();
        }
    }
}
