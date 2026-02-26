using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.App;
using Vareiko.Foundation.AssetManagement;
using Vareiko.Foundation.Audio;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Common;
using Vareiko.Foundation.Config;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Economy;
using Vareiko.Foundation.Features;
using Vareiko.Foundation.Input;
using Vareiko.Foundation.Loading;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.SceneFlow;
using Vareiko.Foundation.Settings;
using Vareiko.Foundation.Time;
using Vareiko.Foundation.Validation;
using Zenject;

namespace Vareiko.Foundation
{
    public static class FoundationRuntimeInstaller
    {
        public static void InstallProjectServices(
            DiContainer container,
            AnalyticsConfig analyticsConfig = null,
            BackendConfig backendConfig = null,
            AssetServiceConfig assetConfig = null,
            EconomyConfig economyConfig = null,
            ConnectivityConfig connectivityConfig = null,
            SaveSchemaConfig saveSchemaConfig = null,
            SaveSecurityConfig saveSecurityConfig = null,
            BackendReliabilityConfig backendReliabilityConfig = null,
            RemoteConfigCacheConfig remoteConfigCacheConfig = null,
            FeatureFlagsConfig featureFlagsConfig = null,
            ObservabilityConfig observabilityConfig = null)
        {
            FoundationTimeInstaller.Install(container);
            FoundationCommonInstaller.Install(container);
            FoundationBootstrapInstaller.Install(container);
            FoundationAppInstaller.Install(container);
            FoundationConfigInstaller.Install(container);
            FoundationAssetInstaller.Install(container, assetConfig);
            FoundationConnectivityInstaller.Install(container, connectivityConfig);
            FoundationInputInstaller.Install(container);
            FoundationSceneFlowInstaller.Install(container);
            FoundationLoadingInstaller.Install(container);
            FoundationSaveInstaller.Install(container, saveSchemaConfig, saveSecurityConfig);
            FoundationConsentInstaller.Install(container);
            FoundationSettingsInstaller.Install(container);
            FoundationEconomyInstaller.Install(container, economyConfig);
            FoundationAudioInstaller.Install(container);
            FoundationAnalyticsInstaller.Install(container, analyticsConfig);
            FoundationBackendInstaller.Install(container, backendConfig, backendReliabilityConfig, remoteConfigCacheConfig);
            FoundationFeatureFlagsInstaller.Install(container, featureFlagsConfig);
            FoundationValidationInstaller.Install(container);
            FoundationObservabilityInstaller.Install(container, observabilityConfig);
        }
    }
}
