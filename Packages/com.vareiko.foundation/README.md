# Vareiko Foundation

Reusable Zenject-first runtime architecture package for Unity projects.

## Included Modules
- Bootstrap pipeline (`IBootstrapTask`, `BootstrapRunner`, lifecycle signals).
- Common runtime helpers (`RetryPolicy`, health checks).
- Composition helpers (`FoundationDomainInstaller`).
- App state machine (`IAppStateMachine`).
- Application lifecycle service (`IApplicationLifecycleService`) with pause/focus/quit events.
- Asset management (`IAssetService`, Resources/Addressables providers, reference tracking).
- Config registry (`IConfigService`, `ConfigRegistry` bootstrap task).
- Runtime environments (`IEnvironmentService`, profile-based key/value access, starter presets for `dev/stage/prod`).
- Connectivity monitoring (`IConnectivityService`) with focus-regained refresh policy.
- Feature flags (`IFeatureFlagService`) with remote-config fallback and local overrides.
- Localization baseline (`ILocalizationService`, language switching and fallback lookups).
- Input service (`IInputService`, adapter-based architecture) with New Input System adapter and rebind API (`IInputRebindService`).
- Scene flow (`ISceneFlowService`).
- Loading state orchestration (`ILoadingService`) with scene-signal integration.
- UI loading presenter (`LoadingOverlayPresenter`).
- Save system (`ISaveService`, atomic file storage, JSON serializer, rolling backups, autosave scheduler).
- Cloud save sync (`ICloudSaveSyncService`) with conflict resolution over backend player-data storage.
- Save schema versioning/migration contracts (`ISaveMigration`) and security layer (`SaveSecurityConfig` + `SecureSaveSerializer`).
- IAP abstraction (`IInAppPurchaseService`) with simulated and null providers baseline.
- Ads abstraction (`IAdsService`) for rewarded/interstitial placements with consent-aware gating, including an external bridge provider path for non-Unity ad SDKs.
- Push notifications abstraction (`IPushNotificationService`) with simulated/null providers and Unity adapter path.
- Monetization policy service (`IMonetizationPolicyService`) with cooldown/session-cap guards for ad shows and IAP flow.
- Attribution abstraction (`IAttributionService`) with external bridge provider path for host SDKs (AppsFlyer/Adjust/etc.).
- Privacy and consent (`IConsentService`).
- Settings system (`ISettingsService`).
- Economy service (`IEconomyService`, in-memory baseline).
- Audio service (`IAudioService`).
- Observability (`IFoundationLogger`, `IFoundationLogSink`, `ICrashReportingService`, diagnostics snapshot service, diagnostics snapshot export service, diagnostics overlay view, global exception boundary, asset/save diagnostics signals).
- Revenue/comms observability (`IMonetizationObservabilityService`) with counters and latency metrics for IAP/Ads/Push operations.
- Startup validation (`IStartupValidationRule`, `StartupValidationRunner`) with summary signal (`StartupValidationCompletedSignal`).
- UI base and navigation (`UIElement`, `UIScreen`, `UIWindow`, `UIPanel`, `UIItemView`, `UIButtonView`, `IUIService`, `IUINavigationService`, `IUIWindowManager`).
- UI button actions (`UIWindowOpenButtonAction`, `UIWindowCloseButtonAction`) and button-state binding (`UIBoolButtonInteractableBinder`).
- UI modal results (`IUIWindowResultService`, `UIWindowResult`, `UIWindowResolveButtonAction`) for awaitable confirm/cancel flows.
- Confirm dialog template (`IUIConfirmDialogService`, `UIConfirmDialogPresenter`, `UIConfirmDialogRequest`) for reusable modal confirmation flows.
- Analytics abstraction (`IAnalyticsService`).
- Backend abstraction (`IBackendService`, `IRemoteConfigService`, `ICloudFunctionService`) with PlayFab entry adapter, retry and persistent cloud-function queue.
- Editor tooling: module scaffolder (`Tools/Vareiko/Foundation/Create Runtime Module`).

## Installation
1. Add a `ProjectContext` in your bootstrap scene.
2. Attach `FoundationProjectInstaller` to `ProjectContext`.
3. Add a `SceneContext` in gameplay scenes.
4. Attach `FoundationSceneInstaller` to `SceneContext`.
5. Optionally assign:
- `AnalyticsConfig`
- `AttributionConfig`
- `BackendConfig`
- `BackendReliabilityConfig`
- `AssetServiceConfig`
- `EconomyConfig`
- `IapConfig`
- `AdsConfig`
- `PushNotificationConfig`
- `MonetizationPolicyConfig`
- `ConnectivityConfig`
- `SaveSchemaConfig`
- `SaveSecurityConfig`
- `AutosaveConfig`
- `RemoteConfigCacheConfig`
- `FeatureFlagsConfig`
- `EnvironmentConfig`
- `LocalizationConfig`
- `ObservabilityConfig`
- `UIRegistry` (or legacy `UIScreenRegistry`)
- `ConfigRegistry[]` (optional, auto-registered as bootstrap tasks)
- `IBootstrapTask` MonoBehaviours

## Quick Start (First 15 Minutes)
1. Create one bootstrap scene and one gameplay scene.
2. In bootstrap scene add:
- `ProjectContext` + `FoundationProjectInstaller`.
- `EnvironmentConfig` asset with `ApplyStarterPresets()` (`dev/stage/prod`).
3. In gameplay scene add:
- `SceneContext` + `FoundationSceneInstaller`.
- UI canvas with loading overlay presenter (optional but recommended).
4. In `FoundationProjectInstaller` assign only minimum configs first:
- `EnvironmentConfig`
- `ObservabilityConfig`
- `SaveSecurityConfig`
- `AutosaveConfig`
5. Leave provider-heavy modules in safe fallback mode for first run:
- backend -> `None`
- IAP -> `None` or `Simulated`
- ads -> `None` or `Simulated`
- push -> `None` or `Simulated`
- attribution -> `None`
6. Enter Play Mode and verify:
- boot reaches your expected app state
- no startup validation errors
- diagnostics snapshot is available from `IFoundationDiagnosticsService`

Detailed flow with checklist: `Documentation~/STARTER_FLOW.md`.
Full practical guide: `Documentation~/USAGE_GUIDE.md`.

## Dependencies
- Zenject (`Zenject` asmdef, OpenUPM package `net.bobbo.extenject`)
- UniTask (`UniTask` asmdef, OpenUPM package `com.cysharp.unitask`)
- Optional: Addressables (`FOUNDATION_ADDRESSABLES` define to enable provider)
- Unity Input System (`Unity.InputSystem` asmdef / package `com.unity.inputsystem`)
- Optional: Unity IAP (`UNITY_PURCHASING` define from package `com.unity.purchasing`)
- Optional: Unity push notifications adapter (`FOUNDATION_UNITY_NOTIFICATIONS` define + host push SDK integration)

`com.vareiko.foundation` declares Zenject/UniTask as package dependencies.
Host project must have OpenUPM scoped registry configured.

OpenUPM commands:

```bash
openupm add com.cysharp.unitask
openupm add net.bobbo.extenject
```

Example `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp",
        "net.bobbo"
      ]
    }
  ],
  "dependencies": {
    "com.cysharp.unitask": "2.5.10",
    "net.bobbo.extenject": "9.3.2"
  }
}
```

## Notes
- `PlayFabBackendService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- `PlayFabCloudFunctionService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- `PlayFabRemoteConfigService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- By default backend services run in safe null mode (`NullBackendService`, `NullRemoteConfigService`, `NullCloudFunctionService`).
- Analytics is privacy-first by default: events are blocked until consent is explicitly collected and granted.
- Runtime smoke tests are included in `Tests/Runtime` (`Vareiko.Foundation.Tests` assembly).
- Runtime tests now include core modules (`Environment`, `Economy`, `Iap`, `Ads`, `Push`, `Monetization`, `Settings`, `Analytics`, `Config`, `Input`, `Common`, `Observability`, `Audio`) and infrastructure guards (`ConfigRegistry`, `GlobalExceptionHandler`) in addition to previously covered areas.
- Runtime module scaffolder generates `IService/Service/Config/Signals/Installer` and can optionally generate a test stub plus integration sample installer into your `Assets` folder.
- Project validator menu: `Tools/Vareiko/Foundation/Validate Project` (scene wiring + release-gate checks).
- Starter environment presets menu: `Tools/Vareiko/Foundation/Create Starter Environment Config`.
- CI workflow is available at `.github/workflows/ci.yml`; set `UNITY_LICENSE` secret to enable Unity EditMode + PlayMode tests in GitHub Actions.
- Sample `Basic Setup` now includes a ready scene (`FoundationSampleScene`) and bootstrap helper (`FoundationSampleSceneBootstrap`).

## Starter Environment Presets
- `EnvironmentConfig.ApplyStarterPresets()` fills baseline profiles:
  - `dev`
  - `stage`
  - `prod`
- Editor helpers:
  - create preset asset: `Tools/Vareiko/Foundation/Create Starter Environment Config`
  - apply presets to existing asset via context menu on `EnvironmentConfig`

## Input Rebinds (New Input System)
- `FoundationInputInstaller` binds `NewInputSystemAdapter` first when `ENABLE_INPUT_SYSTEM` is available.
- `LegacyKeyboardInputAdapter` and `NullInputAdapter` remain as fallback adapters.
- Use `IInputRebindService` to:
  - `TryApplyBindingOverride(actionName, bindingIndex, overridePath)`
  - `TryRemoveBindingOverride(actionName, bindingIndex)`
  - `ResetAllBindingOverrides()`
  - `ExportOverridesJson()` / `ImportOverridesJson(json)`
- Rebind overrides are persisted through `IInputRebindStorage` (`PlayerPrefsInputRebindStorage` by default).

## Startup Validation Baseline
- `FoundationValidationInstaller` now registers baseline runtime rules:
  - `SaveSecurityStartupValidationRule`
  - `BackendStartupValidationRule`
  - `ObservabilityStartupValidationRule`
- Typical findings:
  - save encryption enabled with default/empty secret key -> `Error`
  - PlayFab backend selected with empty `TitleId` -> `Error`
  - missing configs / disabled safety toggles -> `Warning`

## Remote Config Cache Control
- `CachedRemoteConfigService` now implements `IRemoteConfigCacheService`.
- New control API:
  - `ForceRefreshAsync()` - bypass passive cadence and refresh immediately.
  - `InvalidateCache(reason)` - clear cached values and schedule next auto-refresh.
- Signals:
  - `RemoteConfigRefreshedSignal` now includes source (`initialize`, `auto`, `manual`, `forced`).
  - `RemoteConfigCacheInvalidatedSignal` notifies cache clear events.

## PlayFab Hardening Baseline
- `PlayFabBackendService` now validates provider/title/custom-id and normalizes auth-state transitions.
- `BackendAuthResult` and `BackendPlayerDataResult` now expose:
  - `ErrorCode` (`BackendErrorCode`)
  - `IsRetryable`
- `NullBackendService` and PlayFab adapters now return consistent mapped backend errors.

## Cloud Save Sync Baseline
- `ICloudSaveSyncService` provides:
  - `PushAsync(slot, key)` - local save -> cloud backend player data.
  - `PullAsync(slot, key)` - cloud backend player data -> local save.
  - `SyncAsync(slot, key)` - conflict-aware two-way sync using `ISaveConflictResolver`.
- Cloud save keys are mapped as `foundation.save.{slot}.{key}` in backend player data.

## IAP Baseline
- `IInAppPurchaseService` exposes:
  - `InitializeAsync()`
  - `GetCatalogAsync()`
  - `PurchaseAsync(productId)`
  - `RestorePurchasesAsync()`
- `IapConfig.Provider` supports:
  - `None` (safe fallback to `NullInAppPurchaseService`)
  - `Simulated` (`SimulatedInAppPurchaseService` for development flows)
  - `UnityIap` (`UnityInAppPurchaseService` adapter; requires Unity IAP package)
- `UnityInAppPurchaseService` maps Unity Purchasing initialize/purchase/restore callbacks into `InAppPurchase*` models and IAP signals.
- Signals:
  - `IapInitializedSignal`
  - `IapPurchaseSucceededSignal`
  - `IapPurchaseFailedSignal`
  - `IapRestoreCompletedSignal`
  - `IapRestoreFailedSignal`

## Ads Baseline
- `IAdsService` exposes:
  - `InitializeAsync()`
  - `GetPlacementIdsAsync()`
  - `LoadAsync(placementId)`
  - `ShowAsync(placementId)`
- `AdsConfig.Provider` supports:
  - `None` (safe fallback to `NullAdsService`)
  - `Simulated` (`SimulatedAdsService` for development flows)
  - `UnityAds` (reserved provider id for custom Unity Ads wiring)
  - `ExternalBridge` (`ExternalAdsBridgeService` + `ExternalAdsBridge` handlers for host mediation SDK integration)
- `SimulatedAdsService` supports rewarded/interstitial placement simulation and respects advertising consent gate (`ConsentScope.Advertising`) when enabled in config.
- `ExternalAdsBridgeService` expects host runtime callbacks:
  - `ExternalAdsBridge.SetInitializeHandler(...)` (optional)
  - `ExternalAdsBridge.SetLoadHandler(...)` (required)
  - `ExternalAdsBridge.SetShowHandler(...)` (required)
  - `ExternalAdsBridge.ClearHandlers()` on shutdown/domain reload
- Signals:
  - `AdsInitializedSignal`
  - `AdLoadedSignal`
  - `AdLoadFailedSignal`
  - `AdShownSignal`
  - `AdShowFailedSignal`
  - `AdRewardGrantedSignal`

## Push Notifications Baseline
- `IPushNotificationService` exposes:
  - `InitializeAsync()`
  - `RequestPermissionAsync()`
  - `GetDeviceTokenAsync()`
  - `SubscribeAsync(topic)`
  - `UnsubscribeAsync(topic)`
  - `GetSubscribedTopicsAsync()`
- `PushNotificationConfig.Provider` supports:
  - `None` (safe fallback to `NullPushNotificationService`)
  - `Simulated` (`SimulatedPushNotificationService` for development flows)
  - `UnityNotifications` (`UnityPushNotificationService` adapter path; requires `FOUNDATION_UNITY_NOTIFICATIONS`)
- `SimulatedPushNotificationService` supports consent-aware permission gate (`ConsentScope.PushNotifications`), topic subscription flow, and simulated token delivery.
- `UnityPushNotificationService` provides production adapter path with dependency guard and topic flow.
- Use `UnityPushNotificationBridge.ReportDeviceToken(token)` from your host push SDK callback to forward runtime device token into foundation layer.
- Signals:
  - `PushInitializedSignal`
  - `PushPermissionChangedSignal`
  - `PushTokenUpdatedSignal`
  - `PushTopicSubscribedSignal`
  - `PushTopicSubscriptionFailedSignal`
  - `PushTopicUnsubscribedSignal`
  - `PushTopicUnsubscriptionFailedSignal`

## Monetization Policy Baseline
- `IMonetizationPolicyService` exposes:
  - `CanShowAdAsync(placementId, placementType)`
  - `RecordAdShownAsync(placementId, placementType)`
  - `CanStartPurchaseAsync(productId)`
  - `RecordPurchaseAsync(productId)`
  - `ResetSessionAsync()`
- `MonetizationPolicyConfig` supports:
  - default cooldown/session-cap rules for interstitial/rewarded placements and IAP products
  - explicit policy mode (`RequireExplicitPlacementPolicy`, `RequireExplicitProductPolicy`)
  - per-placement and per-product overrides
- Signals:
  - `MonetizationAdBlockedSignal`
  - `MonetizationAdRecordedSignal`
  - `MonetizationIapBlockedSignal`
  - `MonetizationIapRecordedSignal`
  - `MonetizationSessionResetSignal`

## Revenue/Comms Observability Baseline
- `IMonetizationObservabilityService` aggregates:
  - IAP purchase success/failure counters and purchase latency (`last/avg`)
  - ad show success/failure counters and show latency (`last/avg`)
  - push permission granted/denied counters and permission latency (`last/avg`)
  - push topic subscription success/failure counters
- Telemetry signals:
  - `IapOperationTelemetrySignal`
  - `AdsOperationTelemetrySignal`
  - `PushOperationTelemetrySignal`
- Diagnostics integration:
  - `DiagnosticsSnapshot` now includes monetization/comms metrics for QA/support export and runtime overlay consumers.

## Attribution Baseline
- `IAttributionService` exposes:
  - `InitializeAsync()`
  - `SetUserId(userId)`
  - `TrackEventAsync(eventName, properties)`
  - `TrackRevenueAsync(revenueData)`
- `AttributionConfig.Provider` supports:
  - `None` (safe fallback to `NullAttributionService`)
  - `ExternalBridge` (`ExternalAttributionBridgeService` + `ExternalAttributionBridge` host callbacks)
- `ExternalAttributionBridgeService` expects host runtime callbacks:
  - `ExternalAttributionBridge.SetInitializeHandler(...)` (optional)
  - `ExternalAttributionBridge.SetUserIdHandler(...)` (optional)
  - `ExternalAttributionBridge.SetTrackEventHandler(...)` (required)
  - `ExternalAttributionBridge.SetTrackRevenueHandler(...)` (required)
  - `ExternalAttributionBridge.ClearHandlers()` on shutdown/domain reload
- Signals:
  - `AttributionInitializedSignal`
  - `AttributionEventTrackedSignal`
  - `AttributionEventTrackFailedSignal`
  - `AttributionRevenueTrackedSignal`
  - `AttributionRevenueTrackFailedSignal`

## Structured Logging Sinks
- `UnityFoundationLogger` now writes through `IFoundationLogSink` bindings using structured `FoundationLogEntry`.
- Default binding is `UnityConsoleLogSink` (enabled when `ObservabilityConfig.LogToUnityConsole` is `true`).
- You can add custom sinks (for example file/http adapters) by binding additional `IFoundationLogSink` implementations.

## Optional Crash Reporting Adapter
- Implement and bind `ICrashReportingService` to route unhandled exceptions into your crash provider (for example Firebase/Sentry/Backtrace adapters).
- `GlobalExceptionHandler` sends `FoundationCrashReport` payloads when:
  - `ObservabilityConfig.CaptureUnhandledExceptions` is `true`
  - `ObservabilityConfig.ForwardUnhandledExceptionsToCrashReporting` is `true`
  - bound `ICrashReportingService.IsEnabled` is `true`
- Signals:
  - `CrashReportSubmittedSignal`
  - `CrashReportSubmissionFailedSignal`

## Diagnostics Snapshot Export
- `IDiagnosticsSnapshotExportService.ExportAsync(label)` writes a JSON snapshot for QA/support.
- Default export directory is `Application.persistentDataPath/foundation-diagnostics`.
- Signals:
  - `DiagnosticsSnapshotExportedSignal`
  - `DiagnosticsSnapshotExportFailedSignal`

## Event-Driven UI Template
- Publish values from domain/services through `IUIValueEventService`:
  - `SetInt("hud.coins", value)`
  - `SetFloat("hud.hp", value)`
  - `SetBool("hud.alive", value)`
  - `SetString("hud.player_name", value)`
- Bind values on scene objects with ready components:
  - `UIIntTextBinder`
  - `UIFloatTextBinder`
  - `UIStringTextBinder`
  - `UIBoolGameObjectBinder`
  - `UIBoolButtonInteractableBinder`
  - `UIItemCountBinder` (binds item collection size from int key)
- All binder components consume `SignalBus` + key matching, so UI updates are push-based without per-frame polling.

### Button Actions
- `UIWindowOpenButtonAction`:
  - subscribes to `UIButtonView.OnClicked`
  - enqueues target window in `IUIWindowManager` with configured priority/duplicate policy
- `UIWindowCloseButtonAction`:
  - closes current window by default
  - can close a specific window id when configured
- `UIWindowResolveButtonAction`:
  - resolves current (or specific) window with status (`Confirmed/Canceled/Closed`) and optional payload

### Window Result Flow
- Use `IUIWindowResultService.EnqueueAndWaitAsync(windowId)` when you need modal-like UI flow.
- Resolve from UI button:
  - set `UIWindowResolveButtonAction` status to `Confirmed`/`Canceled`
  - optionally set payload (for example selected reward/item id)
- Runtime receives `UIWindowResult` with:
  - `WindowId`
  - `Status`
  - `Payload`

### Typed Window Payload
- Use `UIWindowResultPayload` helper for safe typed payload work:
  - `UIWindowResultPayload.Serialize(payloadDto)`
  - `UIWindowResultPayload.TryDeserialize<T>(raw, out value)`
  - `result.TryGetPayload<T>(out value)`
  - `result.GetPayloadOrDefault<T>(fallback)`
- Primitive payloads (`int`, `bool`, `float`, enums) are also parsed from raw strings for backward compatibility.

### Confirm Dialog Service
- `IUIConfirmDialogService.ShowAsync(windowId, request)`:
  - applies request data to `UIConfirmDialogPresenter` on target window
  - opens window via `IUIWindowResultService.EnqueueAndWaitAsync`
  - returns `UIWindowResult`
- `UIConfirmDialogPresenter` updates title/message/labels and resolves:
  - `Confirmed` with confirm payload
  - `Canceled` with cancel payload

### Collection Binding
- `UIItemCollectionBinder` manages pooled `UIItemView` instances and supports:
  - grow/shrink by count
  - hide extra items or destroy on shrink
  - retrieving active item views for data rendering
- Typical flow:
  - `UIItemCountBinder` listens to `IUIValueEventService` key (for example `inventory.count`)
  - updates collection size reactively
  - presenter fills visible items with domain data

### Reactive Streams (Built-in)
- `IUIValueEventService` also exposes reactive streams:
  - `ObserveInt(key)`
  - `ObserveFloat(key)`
  - `ObserveBool(key)`
  - `ObserveString(key)`
- Example:
```csharp
private IDisposable _coinsSubscription;

void Bind(IUIValueEventService values)
{
    _coinsSubscription = values.ObserveInt("hud.coins")
        .Subscribe(v => coinsText.text = v.ToString(), true);
}

void Unbind()
{
    _coinsSubscription?.Dispose();
    _coinsSubscription = null;
}
```

- Compose derived streams without extra dependencies:
```csharp
var hpPercent = values.ObserveFloat("hud.hp")
    .Combine(values.ObserveFloat("hud.hp_max"), (hp, max) => max <= 0f ? 0f : hp / max);

var hpLabel = hpPercent.Map(p => $"{p * 100f:0}%");
```
