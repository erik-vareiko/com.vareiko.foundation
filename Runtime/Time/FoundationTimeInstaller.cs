using Zenject;

namespace Vareiko.Foundation.Time
{
    public static class FoundationTimeInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IFoundationTimeProvider>())
            {
                return;
            }

            container.Bind<IFoundationTimeProvider>().To<UnityFoundationTimeProvider>().AsSingle();
        }
    }
}
