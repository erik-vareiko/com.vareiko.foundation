using VContainer;

namespace Vareiko.Foundation.Time
{
    public static class FoundationTimeInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<UnityFoundationTimeProvider>(Lifetime.Singleton).As<IFoundationTimeProvider>();
        }
    }
}
