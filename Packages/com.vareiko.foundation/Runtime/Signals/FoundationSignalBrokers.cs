using MessagePipe;
using VContainer;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.App;
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
using Vareiko.Foundation.Loading;
using Vareiko.Foundation.Localization;
using Vareiko.Foundation.Monetization;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Push;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.SceneFlow;
using Vareiko.Foundation.Settings;
using Vareiko.Foundation.UI;
using Vareiko.Foundation.UINavigation;
using Vareiko.Foundation.Validation;

namespace Vareiko.Foundation.Signals
{
    /// <summary>
    /// Central registration of every foundation signal type as a MessagePipe broker.
    /// This replaces the per-installer Zenject <c>DeclareSignal&lt;T&gt;</c> calls (1:1).
    /// Called once from the root <c>LifetimeScope</c> after <c>RegisterMessagePipe</c>.
    /// </summary>
    public static class FoundationSignalBrokers
    {
        public static void Register(IContainerBuilder builder, MessagePipeOptions options)
        {
            // App / Bootstrap
            builder.RegisterMessageBroker<AppStateChangedSignal>(options);
            builder.RegisterMessageBroker<ApplicationPauseChangedSignal>(options);
            builder.RegisterMessageBroker<ApplicationFocusChangedSignal>(options);
            builder.RegisterMessageBroker<ApplicationQuitSignal>(options);
            builder.RegisterMessageBroker<ApplicationBootStartedSignal>(options);
            builder.RegisterMessageBroker<ApplicationBootTaskStartedSignal>(options);
            builder.RegisterMessageBroker<ApplicationBootTaskCompletedSignal>(options);
            builder.RegisterMessageBroker<ApplicationBootCompletedSignal>(options);
            builder.RegisterMessageBroker<ApplicationBootFailedSignal>(options);

            // Common / Config / Environment / Connectivity
            builder.RegisterMessageBroker<HealthCheckPassedSignal>(options);
            builder.RegisterMessageBroker<HealthCheckFailedSignal>(options);
            builder.RegisterMessageBroker<ConfigRegisteredSignal>(options);
            builder.RegisterMessageBroker<ConfigMissingSignal>(options);
            builder.RegisterMessageBroker<EnvironmentProfileChangedSignal>(options);
            builder.RegisterMessageBroker<EnvironmentValueMissingSignal>(options);
            builder.RegisterMessageBroker<ConnectivityChangedSignal>(options);

            // Consent
            builder.RegisterMessageBroker<ConsentLoadedSignal>(options);
            builder.RegisterMessageBroker<ConsentChangedSignal>(options);

            // Settings / Localization
            builder.RegisterMessageBroker<SettingsLoadedSignal>(options);
            builder.RegisterMessageBroker<SettingsChangedSignal>(options);
            builder.RegisterMessageBroker<LanguageChangedSignal>(options);
            builder.RegisterMessageBroker<LocalizationKeyMissingSignal>(options);

            // Save
            builder.RegisterMessageBroker<SaveWrittenSignal>(options);
            builder.RegisterMessageBroker<SaveDeletedSignal>(options);
            builder.RegisterMessageBroker<SaveLoadFailedSignal>(options);
            builder.RegisterMessageBroker<SaveBackupWrittenSignal>(options);
            builder.RegisterMessageBroker<SaveRestoredFromBackupSignal>(options);
            builder.RegisterMessageBroker<SaveMigratedSignal>(options);
            builder.RegisterMessageBroker<AutosaveTriggeredSignal>(options);
            builder.RegisterMessageBroker<AutosaveCompletedSignal>(options);
            builder.RegisterMessageBroker<AutosaveFailedSignal>(options);
            builder.RegisterMessageBroker<SaveCloudPushedSignal>(options);
            builder.RegisterMessageBroker<SaveCloudPulledSignal>(options);
            builder.RegisterMessageBroker<SaveCloudConflictResolvedSignal>(options);
            builder.RegisterMessageBroker<SaveCloudSyncFailedSignal>(options);

            // AssetManagement
            builder.RegisterMessageBroker<AssetLoadedSignal>(options);
            builder.RegisterMessageBroker<AssetLoadFailedSignal>(options);
            builder.RegisterMessageBroker<AssetReleasedSignal>(options);
            builder.RegisterMessageBroker<AssetReferenceChangedSignal>(options);
            builder.RegisterMessageBroker<AssetWarmupCompletedSignal>(options);
            builder.RegisterMessageBroker<AssetLeakDetectedSignal>(options);

            // SceneFlow / Loading
            builder.RegisterMessageBroker<SceneLoadStartedSignal>(options);
            builder.RegisterMessageBroker<SceneLoadProgressSignal>(options);
            builder.RegisterMessageBroker<SceneLoadCompletedSignal>(options);
            builder.RegisterMessageBroker<SceneUnloadStartedSignal>(options);
            builder.RegisterMessageBroker<SceneUnloadCompletedSignal>(options);
            builder.RegisterMessageBroker<LoadingStateChangedSignal>(options);

            // Input
            builder.RegisterMessageBroker<InputSchemeChangedSignal>(options);

            // Economy
            builder.RegisterMessageBroker<CurrencyBalanceChangedSignal>(options);
            builder.RegisterMessageBroker<InventoryItemChangedSignal>(options);
            builder.RegisterMessageBroker<EconomyOperationFailedSignal>(options);

            // Analytics / Attribution
            builder.RegisterMessageBroker<AnalyticsEventTrackedSignal>(options);
            builder.RegisterMessageBroker<AnalyticsEventDroppedSignal>(options);
            builder.RegisterMessageBroker<AttributionInitializedSignal>(options);
            builder.RegisterMessageBroker<AttributionEventTrackedSignal>(options);
            builder.RegisterMessageBroker<AttributionEventTrackFailedSignal>(options);
            builder.RegisterMessageBroker<AttributionRevenueTrackedSignal>(options);
            builder.RegisterMessageBroker<AttributionRevenueTrackFailedSignal>(options);

            // Iap
            builder.RegisterMessageBroker<IapInitializedSignal>(options);
            builder.RegisterMessageBroker<IapPurchaseSucceededSignal>(options);
            builder.RegisterMessageBroker<IapPurchaseFailedSignal>(options);
            builder.RegisterMessageBroker<IapRestoreCompletedSignal>(options);
            builder.RegisterMessageBroker<IapRestoreFailedSignal>(options);
            builder.RegisterMessageBroker<IapOperationTelemetrySignal>(options);

            // Ads
            builder.RegisterMessageBroker<AdsInitializedSignal>(options);
            builder.RegisterMessageBroker<AdLoadedSignal>(options);
            builder.RegisterMessageBroker<AdLoadFailedSignal>(options);
            builder.RegisterMessageBroker<AdShownSignal>(options);
            builder.RegisterMessageBroker<AdShowFailedSignal>(options);
            builder.RegisterMessageBroker<AdRewardGrantedSignal>(options);
            builder.RegisterMessageBroker<AdsOperationTelemetrySignal>(options);

            // Push
            builder.RegisterMessageBroker<PushInitializedSignal>(options);
            builder.RegisterMessageBroker<PushPermissionChangedSignal>(options);
            builder.RegisterMessageBroker<PushTokenUpdatedSignal>(options);
            builder.RegisterMessageBroker<PushTopicSubscribedSignal>(options);
            builder.RegisterMessageBroker<PushTopicUnsubscribedSignal>(options);
            builder.RegisterMessageBroker<PushTopicSubscriptionFailedSignal>(options);
            builder.RegisterMessageBroker<PushTopicUnsubscriptionFailedSignal>(options);
            builder.RegisterMessageBroker<PushOperationTelemetrySignal>(options);

            // Monetization
            builder.RegisterMessageBroker<MonetizationAdRecordedSignal>(options);
            builder.RegisterMessageBroker<MonetizationIapRecordedSignal>(options);
            builder.RegisterMessageBroker<MonetizationSessionResetSignal>(options);
            builder.RegisterMessageBroker<MonetizationAdBlockedSignal>(options);
            builder.RegisterMessageBroker<MonetizationIapBlockedSignal>(options);

            // Backend
            builder.RegisterMessageBroker<BackendAuthStateChangedSignal>(options);
            builder.RegisterMessageBroker<BackendOperationRetriedSignal>(options);
            builder.RegisterMessageBroker<CloudFunctionQueuedSignal>(options);
            builder.RegisterMessageBroker<CloudFunctionQueueFlushedSignal>(options);
            builder.RegisterMessageBroker<CloudFunctionQueueRestoredSignal>(options);
            builder.RegisterMessageBroker<RemoteConfigRefreshedSignal>(options);
            builder.RegisterMessageBroker<RemoteConfigRefreshFailedSignal>(options);
            builder.RegisterMessageBroker<RemoteConfigCacheInvalidatedSignal>(options);

            // Features
            builder.RegisterMessageBroker<FeatureFlagsRefreshedSignal>(options);
            builder.RegisterMessageBroker<FeatureFlagOverriddenSignal>(options);

            // Observability
            builder.RegisterMessageBroker<LogMessageEmittedSignal>(options);
            builder.RegisterMessageBroker<DiagnosticsSnapshotUpdatedSignal>(options);
            builder.RegisterMessageBroker<DiagnosticsSnapshotExportedSignal>(options);
            builder.RegisterMessageBroker<DiagnosticsSnapshotExportFailedSignal>(options);
            builder.RegisterMessageBroker<UnhandledExceptionCapturedSignal>(options);
            builder.RegisterMessageBroker<CrashReportSubmittedSignal>(options);
            builder.RegisterMessageBroker<CrashReportSubmissionFailedSignal>(options);

            // Audio
            builder.RegisterMessageBroker<AudioVolumesChangedSignal>(options);

            // Validation
            builder.RegisterMessageBroker<StartupValidationPassedSignal>(options);
            builder.RegisterMessageBroker<StartupValidationWarningSignal>(options);
            builder.RegisterMessageBroker<StartupValidationFailedSignal>(options);
            builder.RegisterMessageBroker<StartupValidationCompletedSignal>(options);

            // UI
            builder.RegisterMessageBroker<UIReadySignal>(options);
            builder.RegisterMessageBroker<UIElementShownSignal>(options);
            builder.RegisterMessageBroker<UIElementHiddenSignal>(options);
            builder.RegisterMessageBroker<UIScreenShownSignal>(options);
            builder.RegisterMessageBroker<UIScreenHiddenSignal>(options);
            builder.RegisterMessageBroker<UIWindowQueuedSignal>(options);
            builder.RegisterMessageBroker<UIWindowShownSignal>(options);
            builder.RegisterMessageBroker<UIWindowClosedSignal>(options);
            builder.RegisterMessageBroker<UIWindowResolvedSignal>(options);
            builder.RegisterMessageBroker<UIWindowQueueDrainedSignal>(options);
            builder.RegisterMessageBroker<UINavigationChangedSignal>(options);
            builder.RegisterMessageBroker<UIIntValueChangedSignal>(options);
            builder.RegisterMessageBroker<UIFloatValueChangedSignal>(options);
            builder.RegisterMessageBroker<UIBoolValueChangedSignal>(options);
            builder.RegisterMessageBroker<UIStringValueChangedSignal>(options);
        }
    }
}
