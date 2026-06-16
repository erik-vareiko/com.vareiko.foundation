using MessagePipe;
using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.AssetManagement;
using Vareiko.Foundation.Attribution;
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
using Vareiko.Foundation.Monetization;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Push;
using Vareiko.Foundation.Rng;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.SceneFlow;
using Vareiko.Foundation.Settings;
using Vareiko.Foundation.Signals;
using Vareiko.Foundation.Time;
using Vareiko.Foundation.UI;
using Vareiko.Foundation.UINavigation;
using Vareiko.Foundation.Validation;
using VContainer;

namespace Vareiko.Foundation
{
    public static class FoundationRuntimeInstaller
    {
        // Returns the MessagePipeOptions used for the foundation signal brokers so callers can
        // register additional message brokers for their own signal types in the same scope.
        public static MessagePipeOptions InstallProjectServices(
            IContainerBuilder builder,
            AnalyticsConfig analyticsConfig = null,
            AttributionConfig attributionConfig = null,
            BackendConfig backendConfig = null,
            AssetServiceConfig assetConfig = null,
            EconomyConfig economyConfig = null,
            IapConfig iapConfig = null,
            AdsConfig adsConfig = null,
            PushNotificationConfig pushNotificationConfig = null,
            MonetizationPolicyConfig monetizationPolicyConfig = null,
            ConnectivityConfig connectivityConfig = null,
            SaveSchemaConfig saveSchemaConfig = null,
            SaveSecurityConfig saveSecurityConfig = null,
            AutosaveConfig autosaveConfig = null,
            BackendReliabilityConfig backendReliabilityConfig = null,
            BackendCommandConfig backendCommandConfig = null,
            RemoteConfigCacheConfig remoteConfigCacheConfig = null,
            FeatureFlagsConfig featureFlagsConfig = null,
            EnvironmentConfig environmentConfig = null,
            LocalizationConfig localizationConfig = null,
            ObservabilityConfig observabilityConfig = null,
            DeterministicRngConfig deterministicRngConfig = null)
        {
            // MessagePipe + the IFoundationSignalBus facade are registered once here, at the
            // composition root. Each module owns its broker registrations (RegisterSignals),
            // but they all land in THIS scope: the facade publishes through GlobalMessagePipe,
            // whose provider is the project container — that's also why scene-scope modules
            // (Bootstrap, UI, UINavigation) register their signals here, not in the scene scope.
            MessagePipeOptions messagePipeOptions = builder.RegisterMessagePipe();
            builder.RegisterBuildCallback(container => GlobalMessagePipe.SetProvider(container.AsServiceProvider()));
            builder.Register<MessagePipeSignalBus>(Lifetime.Singleton).As<IFoundationSignalBus>();

            FoundationAppInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationBootstrapInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationCommonInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationConfigInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationEnvironmentInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationConnectivityInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationConsentInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationSettingsInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationLocalizationInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationSaveInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationAssetInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationSceneFlowInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationLoadingInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationInputInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationEconomyInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationAnalyticsInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationAttributionInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationIapInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationAdsInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationPushNotificationInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationMonetizationInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationBackendInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationFeatureFlagsInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationObservabilityInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationAudioInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationValidationInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationUIInstaller.RegisterSignals(builder, messagePipeOptions);
            FoundationUINavigationInstaller.RegisterSignals(builder, messagePipeOptions);

            FoundationTimeInstaller.Install(builder);
            FoundationCommonInstaller.Install(builder);
            FoundationRngInstaller.Install(builder, deterministicRngConfig);
            FoundationEnvironmentInstaller.Install(builder, environmentConfig);
            FoundationAppInstaller.Install(builder);
            // Bootstrap is installed in the scene scope (FoundationSceneInstaller), where the
            // IBootstrapTask scene objects live, so BootstrapRunner resolves a populated task list.
            // Installing it here too would create a second runner with an empty task list.
            FoundationConfigInstaller.Install(builder);
            FoundationAssetInstaller.Install(builder, assetConfig);
            FoundationConnectivityInstaller.Install(builder, connectivityConfig);
            FoundationInputInstaller.Install(builder);
            FoundationSceneFlowInstaller.Install(builder);
            FoundationLoadingInstaller.Install(builder);
            FoundationSaveInstaller.Install(builder, saveSchemaConfig, saveSecurityConfig, autosaveConfig);
            FoundationConsentInstaller.Install(builder);
            FoundationSettingsInstaller.Install(builder);
            FoundationLocalizationInstaller.Install(builder, localizationConfig);
            FoundationEconomyInstaller.Install(builder, economyConfig);
            FoundationIapInstaller.Install(builder, iapConfig);
            FoundationAdsInstaller.Install(builder, adsConfig);
            FoundationPushNotificationInstaller.Install(builder, pushNotificationConfig);
            FoundationMonetizationInstaller.Install(builder, monetizationPolicyConfig);
            FoundationAudioInstaller.Install(builder);
            FoundationAnalyticsInstaller.Install(builder, analyticsConfig);
            FoundationAttributionInstaller.Install(builder, attributionConfig);
            FoundationBackendInstaller.Install(builder, backendConfig, backendReliabilityConfig, backendCommandConfig, remoteConfigCacheConfig);
            FoundationFeatureFlagsInstaller.Install(builder, featureFlagsConfig);
            // Cross-module startup rules (they read Save/Backend/Observability configs) are
            // composition-root concerns; the validation module only ships the framework.
            builder.Register<SaveSecurityStartupValidationRule>(Lifetime.Singleton).As<IStartupValidationRule>();
            builder.Register<BackendStartupValidationRule>(Lifetime.Singleton).As<IStartupValidationRule>();
            builder.Register<ObservabilityStartupValidationRule>(Lifetime.Singleton).As<IStartupValidationRule>();
            FoundationValidationInstaller.Install(builder);
            FoundationObservabilityInstaller.Install(builder, observabilityConfig);

            return messagePipeOptions;
        }
    }
}
