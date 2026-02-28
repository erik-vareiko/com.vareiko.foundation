using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.AssetManagement;
using Vareiko.Foundation.Audio;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Common;
using Vareiko.Foundation.Config;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Economy;
using Vareiko.Foundation.Environment;
using Vareiko.Foundation.Features;
using Vareiko.Foundation.Iap;
using Vareiko.Foundation.Input;
using Vareiko.Foundation.Localization;
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
            IapConfig iapConfig = null,
            AdsConfig adsConfig = null,
            ConnectivityConfig connectivityConfig = null,
            SaveSchemaConfig saveSchemaConfig = null,
            SaveSecurityConfig saveSecurityConfig = null,
            AutosaveConfig autosaveConfig = null,
            BackendReliabilityConfig backendReliabilityConfig = null,
            RemoteConfigCacheConfig remoteConfigCacheConfig = null,
            FeatureFlagsConfig featureFlagsConfig = null,
            EnvironmentConfig environmentConfig = null,
            LocalizationConfig localizationConfig = null,
            ObservabilityConfig observabilityConfig = null)
        {
            FoundationTimeInstaller.Install(container);
            FoundationCommonInstaller.Install(container);
            FoundationEnvironmentInstaller.Install(container, environmentConfig);
            FoundationAppInstaller.Install(container);
            FoundationBootstrapInstaller.Install(container);
            FoundationConfigInstaller.Install(container);
            FoundationAssetInstaller.Install(container, assetConfig);
            FoundationConnectivityInstaller.Install(container, connectivityConfig);
            FoundationInputInstaller.Install(container);
            FoundationSceneFlowInstaller.Install(container);
            FoundationLoadingInstaller.Install(container);
            FoundationSaveInstaller.Install(container, saveSchemaConfig, saveSecurityConfig, autosaveConfig);
            FoundationConsentInstaller.Install(container);
            FoundationSettingsInstaller.Install(container);
            FoundationLocalizationInstaller.Install(container, localizationConfig);
            FoundationEconomyInstaller.Install(container, economyConfig);
            FoundationIapInstaller.Install(container, iapConfig);
            FoundationAdsInstaller.Install(container, adsConfig);
            FoundationAudioInstaller.Install(container);
            FoundationAnalyticsInstaller.Install(container, analyticsConfig);
            FoundationBackendInstaller.Install(container, backendConfig, backendReliabilityConfig, remoteConfigCacheConfig);
            FoundationFeatureFlagsInstaller.Install(container, featureFlagsConfig);
            FoundationValidationInstaller.Install(container);
            FoundationObservabilityInstaller.Install(container, observabilityConfig);
        }
    }
}
