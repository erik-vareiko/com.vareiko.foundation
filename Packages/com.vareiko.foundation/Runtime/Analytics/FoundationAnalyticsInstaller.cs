using UnityEngine;
using VContainer;

namespace Vareiko.Foundation.Analytics
{
    public static class FoundationAnalyticsInstaller
    {
        public static void Install(IContainerBuilder builder, AnalyticsConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AnalyticsConfig>());
            builder.Register<AnalyticsService>(Lifetime.Singleton).As<IAnalyticsService>();
        }
    }
}
