# Changelog

## Unreleased
- Core primitives (Phase 3, second batch):
  - `Result` / `Result<T>` (`Vareiko.Foundation`) — canonical success/failure primitive (factories, `TryGetValue`, `GetValueOrDefault`, `AsResult`); the default for new foundation APIs. `IDiagnosticsSnapshotExportService.ExportAsync` migrated from its ad-hoc result struct to `Result<string>` (breaking); domain results carrying codes/flags (backend, cloud sync) intentionally keep their shapes.
  - `StateMachine<TState>` (`Vareiko.Foundation`) — minimal generic FSM (transition guard, `TryEnter`/`ForceEnter`, change event, custom comparer).
  - **`AppState` is no longer an enum** (breaking for `switch`/casts, source-compatible otherwise): it is a string-backed struct with the same well-known states (`Boot`…`Shutdown`), so hosts add custom states via `new AppState("MetaShop")`. `AppStateMachine` is rebuilt on `StateMachine<AppState>` with the same default lifecycle rules; custom states flow through them.
  - Disposable utilities (`Vareiko.Foundation`) — `DisposableAction` (idempotent) and `CompositeDisposable` (the signal-subscription bag pattern).
- Core primitives (Phase 3 of the 3.0 refactor, first batch):
  - `ITickService` / `TickService` (`Vareiko.Foundation.Time`) — central ordered update loop: per-frame listeners with explicit ordering, `Delay`/`Repeat`/`NextFrame` timers (scaled or unscaled time), pause support, exception isolation per listener, `IDisposable` handles everywhere; driven by the container player-loop in play mode, manually via `Advance` in tests. Registered by `FoundationTimeInstaller` and resolvable from the project scope.
  - Object pooling (`Vareiko.Foundation.Pooling`) — `IObjectPool<T>` with `ObjectPool<T>` for plain classes (factory, get/release/destroy callbacks, max size, prewarm, double-release detection) and `ComponentPool<T>` for prefab instances (activate/deactivate lifecycle, reparenting, overflow destruction, tolerance to externally destroyed instances), plus a `using`-scoped `GetScoped` helper.
- Asmdef modularization (Phase 2 of the 3.0 refactor) — the monolithic `Vareiko.Foundation` assembly is split into 10 runtime assemblies:
  - `Vareiko.Foundation.Core` (signals facade, time, common, app+bootstrap, config, connectivity, environment, input, loading, RNG, scene flow, validation framework), `…Persistence` (save+settings+consent), `…Audio`, `…UI` (incl. UINavigation), `…Assets`, `…Backend`, `…Features`, `…Monetization` (ads/IAP/push/attribution/analytics/economy/policy), `…Observability`, and the `Vareiko.Foundation` umbrella (composition root; references all modules)
  - a host can now reference Core + a subset of modules and compile without the rest; the umbrella keeps the all-in `InstallProjectServices` path working unchanged
  - `FOUNDATION_ADDRESSABLES` is now auto-defined via asmdef `versionDefines` when `com.unity.addressables` is installed (previously the define could not work in package form — the assembly had no Addressables reference)
  - signal brokers are registered per module (`Foundation<Module>Installer.RegisterSignals`); the central `FoundationSignalBrokers` registry is removed
  - ownership moves to keep the assembly graph acyclic: `CloudSaveSyncService` → backend module, `MonetizationObservabilityService` → monetization module (contract stays in observability), cross-module startup validation rules → composition root
  - namespaces are unchanged — consumer code keeps compiling after adding the module references; hosts referencing the `Vareiko.Foundation` asmdef by name keep working via the umbrella
- DI migration Phase 1c — VContainer/MessagePipe cutover (composition now runs on VContainer):
  - all 31 module installers converted `DiContainer` → `IContainerBuilder`; `FoundationRuntimeInstaller` registers MessagePipe + the `IFoundationSignalBus` facade (`MessagePipeSignalBus`) + every signal broker once at the root
  - root `FoundationProjectInstaller` / `FoundationSceneInstaller` / `FoundationDomainInstaller` are now VContainer `LifetimeScope`s (`Configure(IContainerBuilder)`)
  - lifecycle services register via `RegisterEntryPoint`; `IInitializable`/`ITickable` fully-qualified to `VContainer.Unity.*`
  - ex-`[InjectOptional]` parity: configs always bound (real or default), `List<T>` collection deps resolved via `IEnumerable<T>` factory registration, host-optional deps (`IApplicationLifecycleSource`, `ICrashReportingService`, `UIRegistry`, input adapter) resolved with `TryResolve`
  - Backend decorator chains rebuilt with delegate registration; transitional `ZenjectFoundationSignalBus` removed; composition tests rewritten against VContainer
- DI migration — scene MonoBehaviour injection (Zenject `SceneContext` parity):
  - the 12 scene-placed `[Inject] Construct(...)` components (UI value binders, window button actions, `UIConfirmDialogPresenter`, `LoadingOverlayPresenter`, `DiagnosticsOverlayView`) ported to VContainer method injection with required dependencies
  - `FoundationSceneInstaller` now injects every scene root GameObject hierarchy at container build (before entry points run); opt-out via the `_injectSceneObjects` inspector flag
  - `FoundationSceneInstaller` defaults its parent scope to `FoundationProjectInstaller` so scene services resolve project services without inspector wiring
- DI migration — Zenject fully removed:
  - stripped `using Zenject;` and `[Inject]`/`[InjectOptional]` attributes from all remaining services (VContainer ignored them; no behavior change)
  - removed the `Zenject` reference from all asmdefs and `net.bobbo.extenject` from `package.json` (replaced by `jp.hadashikick.vcontainer` + `com.cysharp.messagepipe`)
  - `FoundationRuntimeInstaller.InstallProjectServices` now returns the `MessagePipeOptions` so hosts can register message brokers for their own signal types
  - module scaffolding templates and the BasicSetup sample rewritten for VContainer (`LifetimeScope` roots instead of `ProjectContext`/`SceneContext`)
- DI migration — verification closed (Phase 1 done):
  - EditMode: 214 passed / 27 failed, all 27 the frozen `TEST_BASELINE.md` set, zero new failures
  - new `FoundationCompositionPlayModeTests` boots the real project + scene scopes in play mode and asserts single bootstrap run, scene-wide injection (incl. inactive objects), and parent-scope resolution
  - `AudioServicePlayModeSmokeTests` updated to the 2.0 lazy audio-root behavior (was stale)
- Changed default save storage:
  - `FoundationSaveInstaller` now binds `ISaveStorage` to `PlayerPrefsSaveStorage` by default
  - `PlayerPrefsSaveStorage` stores one JSON payload per `slot/key` pair using stable relative PlayerPrefs keys
  - `FileSaveStorage` remains available as an alternative backend for projects that pre-bind `ISaveStorage`
- Extended `UIButtonView`:
  - optional UGUI `Button` bridge with lifecycle-safe click subscription
  - `SetInteractable(...)` now syncs an assigned `Button.interactable`
  - added owned click action API (`SetClickAction` / `ClearClickAction`) without clearing external `OnClicked` listeners

## 2.0.0
- Breaking UI cleanup:
  - removed legacy UI bridge APIs (`UIScreenRegistry`, `IUiService`, `UiService`, `Ui*` signals and navigation aliases)
  - `UIService` now depends only on `UIRegistry`
  - `UINavigationService` now exposes only `IUINavigationService` and `UINavigationChangedSignal`
- Hardened UI registry and runtime visibility:
  - duplicate non-empty UI ids now fail fast instead of silently overwriting registry entries
  - `UIScreen` and `UIWindow` require non-empty ids
  - repeated `Show`/`Hide` calls no longer emit duplicate visibility signals
- Hardened UI binding lifecycle:
  - value binders use `IUIValueEventService` subscriptions without touching `SignalBus` unsubscribe paths
  - invalid keys no longer create fallback subscriptions
  - repeated enable/disable cycles clear previous subscriptions before attaching new ones
- Added UI validation coverage:
  - non-interactive validation API for CI/editor tests
  - duplicate/empty id checks, collection template checks, runtime layout warnings and decorative raycast warnings
- Added migration guide:
  - `Documentation~/MIGRATION_2_0.md`

- Fixed Zenject 6 singleton compatibility in primitive path bindings:
  - `FoundationSaveInstaller` now binds `SaveRootPath` via `BindInstance(...).WithId(...)` instead of `Bind<string>().AsSingle()`
  - `FoundationObservabilityInstaller` now binds `DiagnosticsExportRootPath` via `BindInstance(...).WithId(...)` instead of `Bind<string>().AsSingle()`
- Fixed Unity 6 startup/thread-safety issue in `AudioService`:
  - runtime audio root and `AudioSource` components are now created lazily on first actual audio use
  - startup initialization no longer creates Unity audio objects eagerly during container initialization

## 1.1.0
- Added typed cloud-command contracts and service:
  - `CloudCommandRequest`
  - `CloudCommandResponse`
  - `ICloudCommandService`
  - `CloudCommandService`
  - `BackendCommandConfig`
  - `ICloudCommandRetryClassifier` / `CloudCommandRetryClassifier`
- Added command reliability queue v2 with legacy migration:
  - `CloudCommandQueueItem`
  - `ICloudCommandQueueStore`
  - `PlayerPrefsCloudCommandQueueStore`
  - queue metadata persistence (`AttemptCount`, `FirstQueuedUnixMs`, `LastAttemptUnixMs`)
  - loss-tolerant legacy queue migration from `cloud_function_queue` format
  - queue TTL enforcement via `BackendCommandConfig.QueueTtlHours`
- Added deterministic RNG module:
  - `IDeterministicRngService`
  - `IDeterministicRngStream`
  - `RngStreamState`
  - `DeterministicRngService` (`PCG32` stream generator)
  - `DeterministicRngConfig`
  - `FoundationRngInstaller`
- Updated runtime composition and installers:
  - `FoundationRuntimeInstaller` now installs RNG module
  - `FoundationProjectInstaller` now exposes:
    - `BackendCommandConfig`
    - `DeterministicRngConfig`
  - `FoundationBackendInstaller` now binds command service and command queue store
- Added runtime coverage:
  - `CloudCommandServiceTests`
  - `CloudCommandRetryClassifierTests`
  - `PlayerPrefsCloudCommandQueueStoreTests`
  - `DeterministicRngServiceTests`

## 1.0.2
- Added printable PDF guide artifact:
  - `Documentation~/USAGE_GUIDE.pdf`
- Added reproducible PDF generator script:
  - `Tools/docs/build_usage_guide_pdf.py`
- Updated `README.md` with direct PDF guide link.

## 1.0.1
- Added detailed usage guide:
  - `Documentation~/USAGE_GUIDE.md`
  - step-by-step setup flow, module usage patterns, bridge integration examples, testing and release checklists
- Updated `README.md` with direct link to the full usage guide.

## 1.0.0
- First stable release of `com.vareiko.foundation`.
- Closed `v1.0` monetization/comms roadmap:
  - IAP abstraction + Unity IAP adapter path
  - ads abstraction + external ads bridge path
  - push notifications abstraction + Unity push adapter path
  - monetization policy hardening
  - revenue/comms observability metrics
  - attribution abstraction + external bridge path
- Includes release hardening from `0.3.46` (telemetry compile fix in IAP/push services).

## 0.3.46
- Fixed compile regression in telemetry timing for IAP and push services:
  - replaced ambiguous `Time.realtimeSinceStartup` usages with `UnityEngine.Time.realtimeSinceStartup`
  - affected services:
    - `UnityInAppPurchaseService`
    - `UnityPushNotificationService`

## 0.3.45
- Added attribution abstraction baseline:
  - `IAttributionService`
  - `AttributionConfig`
  - `AttributionProviderType`
  - attribution track/revenue result models and signals
- Added external attribution bridge adapter path:
  - `ExternalAttributionBridge` (host callbacks for initialize/event/revenue/user-id)
  - `ExternalAttributionBridgeService` (`AttributionProviderType.ExternalBridge`)
- Integrated attribution module into runtime install pipeline:
  - `FoundationRuntimeInstaller`
  - `FoundationProjectInstaller` (`AttributionConfig` reference)
- Added runtime coverage:
  - `ExternalAttributionBridgeServiceTests`
- Updated docs for attribution module onboarding and starter flow.

## 0.3.44
- Added external ads bridge adapter baseline (Unity Ads independent):
  - `ExternalAdsBridge` (host-registered initialize/load/show handlers)
  - `ExternalAdsBridgeService` (`IAdsService` provider for `AdsProviderType.ExternalBridge`)
- Updated ads installer selection:
  - `FoundationAdsInstaller` now binds `ExternalAdsBridgeService` when provider is `ExternalBridge`
- Added runtime coverage:
  - `ExternalAdsBridgeServiceTests`
- Updated docs for external ads bridge provider path.

## 0.3.43
- Closed release docs baseline for package onboarding:
  - added starter flow guide (`Documentation~/STARTER_FLOW.md`) with clean-project wiring and first-run checklist
  - expanded `README.md` with a practical "first 15 minutes" setup path and config profile guidance
  - updated architecture docs with direct starter-flow reference and runtime startup sequence summary
- Updated backlog release-docs milestone status to done.

## 0.3.42
- Added revenue/comms observability baseline:
  - `IMonetizationObservabilityService`
  - `MonetizationObservabilityService`
  - `MonetizationObservabilitySnapshot`
- Extended diagnostics snapshot with monetization counters and latency fields:
  - IAP purchase success/failure + latency
  - ad show success/failure + latency
  - push permission/topic subscription counters + latency
- Added telemetry signals for monetization operations:
  - `IapOperationTelemetrySignal`
  - `AdsOperationTelemetrySignal`
  - `PushOperationTelemetrySignal`
- Wired telemetry emission in production/runtime services:
  - `UnityInAppPurchaseService`
  - `UnityPushNotificationService`
  - `SimulatedAdsService`
- Added runtime coverage:
  - `MonetizationObservabilityServiceTests`
  - expanded `FoundationDiagnosticsServiceTests`

## 0.3.41
- Added monetization policy hardening baseline:
  - `IMonetizationPolicyService`
  - `MonetizationPolicyService`
  - `MonetizationPolicyConfig`
  - `MonetizationAdDecision` / `MonetizationIapDecision`
  - `MonetizationPolicyBlockReason`
- Added policy signals:
  - `MonetizationAdBlockedSignal`
  - `MonetizationAdRecordedSignal`
  - `MonetizationIapBlockedSignal`
  - `MonetizationIapRecordedSignal`
  - `MonetizationSessionResetSignal`
- Integrated monetization policy module into runtime install pipeline:
  - `FoundationRuntimeInstaller`
  - `FoundationProjectInstaller` (`MonetizationPolicyConfig` reference)
- Added runtime coverage:
  - `MonetizationPolicyServiceTests`

## 0.3.40
- Added Unity push notifications provider adapter path:
  - `UnityPushNotificationService`
  - `UnityPushNotificationBridge`
  - integrated into `FoundationPushNotificationInstaller` for `PushNotificationProviderType.UnityNotifications`
- Added dependency-safe fallback behavior:
  - when `FOUNDATION_UNITY_NOTIFICATIONS` is unavailable, initialization and permission flow return provider-unavailable failures with explicit diagnostics
- Added runtime coverage:
  - `UnityPushNotificationServiceTests`

## 0.3.39
- Added Unity IAP provider adapter path:
  - `UnityInAppPurchaseService`
  - integrated into `FoundationIapInstaller` for `InAppPurchaseProviderType.UnityIap`
- Added dependency-safe fallback behavior:
  - when `UNITY_PURCHASING` is unavailable, initialization/purchase/restore return provider-unavailable failures with explicit diagnostics
- Added runtime coverage:
  - `UnityInAppPurchaseServiceTests`

## 0.3.38
- Added push notifications abstraction baseline with consent-aware permission/topic flow:
  - `IPushNotificationService`
  - `PushNotificationConfig`
  - `PushNotificationProviderType`
  - `PushNotificationPermissionStatus`
  - `PushNotificationErrorCode`
  - `PushInitializeResult` / `PushPermissionResult` / `PushDeviceTokenResult` / `PushTopicResult`
- Added push notifications runtime providers:
  - `SimulatedPushNotificationService`
  - `NullPushNotificationService`
- Added installer/signal layer:
  - `FoundationPushNotificationInstaller`
  - `PushInitializedSignal`
  - `PushPermissionChangedSignal`
  - `PushTokenUpdatedSignal`
  - `PushTopicSubscribedSignal`
  - `PushTopicSubscriptionFailedSignal`
  - `PushTopicUnsubscribedSignal`
  - `PushTopicUnsubscriptionFailedSignal`
- Integrated push notifications module into runtime install pipeline:
  - `FoundationRuntimeInstaller`
  - `FoundationProjectInstaller` (`PushNotificationConfig` reference)
- Added runtime coverage:
  - `SimulatedPushNotificationServiceTests`

## 0.3.37
- Added ads abstraction baseline (rewarded/interstitial) with consent gating:
  - `IAdsService`
  - `AdsConfig`
  - `AdsProviderType`
  - `AdPlacementType`
  - `AdsErrorCode`
  - `AdLoadResult` / `AdShowResult` / `AdsInitializeResult`
- Added ads runtime providers:
  - `SimulatedAdsService`
  - `NullAdsService`
- Added installer/signal layer:
  - `FoundationAdsInstaller`
  - `AdsInitializedSignal`
  - `AdLoadedSignal`
  - `AdLoadFailedSignal`
  - `AdShownSignal`
  - `AdShowFailedSignal`
  - `AdRewardGrantedSignal`
- Integrated ads module into runtime install pipeline:
  - `FoundationRuntimeInstaller`
  - `FoundationProjectInstaller` (`AdsConfig` reference)
- Added runtime coverage:
  - `SimulatedAdsServiceTests`

## 0.3.36
- Added IAP abstraction + provider baseline:
  - `IInAppPurchaseService`
  - `IapConfig`
  - `InAppPurchase*` result/error/product models
  - `InAppPurchaseProviderType` (`None`, `Simulated`, `UnityIap`)
- Added IAP runtime providers:
  - `SimulatedInAppPurchaseService` (development/store-flow baseline)
  - `NullInAppPurchaseService` (safe fallback)
- Added installer/signal layer:
  - `FoundationIapInstaller`
  - `IapInitializedSignal`
  - `IapPurchaseSucceededSignal`
  - `IapPurchaseFailedSignal`
  - `IapRestoreCompletedSignal`
  - `IapRestoreFailedSignal`
- Integrated IAP module into runtime install pipeline:
  - `FoundationRuntimeInstaller`
  - `FoundationProjectInstaller` (`IapConfig` reference)
- Added runtime coverage:
  - `SimulatedInAppPurchaseServiceTests`

## 0.3.35
- Added starter environment profile presets baseline (`dev/stage/prod`):
  - `EnvironmentConfig.ApplyStarterPresets()`
  - built-in preset constants:
    - `EnvironmentConfig.StarterProfileDev`
    - `EnvironmentConfig.StarterProfileStage`
    - `EnvironmentConfig.StarterProfileProd`
- Added editor tooling for starter presets:
  - menu: `Tools/Vareiko/Foundation/Create Starter Environment Config`
  - context action: `Apply Starter Presets (dev/stage/prod)` on `EnvironmentConfig`
- Added runtime coverage:
  - `EnvironmentConfigStarterPresetsTests`

## 0.3.34
- Upgraded editor project validator with release-gate checks (`Tools/Vareiko/Foundation/Validate Project`):
  - package/changelog version alignment validation
  - required package dependency validation
  - missing `.meta` detection for package scripts
  - merge conflict marker scan in release-critical roots (`Packages`, `ProjectSettings`, `Tools`, `.github`)
  - Unity editor version parse validation from `ProjectSettings/ProjectVersion.txt`
- Added release-gate result codes (`REL-*`) to validation report output.

## 0.3.33
- Upgraded module scaffolder for developer-experience baseline:
  - added optional test stub generation (`Tests/{{MODULE_NAME}}ServiceTests.cs`)
  - added optional integration sample installer generation (`Sample/{{MODULE_NAME}}SampleInstaller.cs`)
- Updated `FoundationModuleScaffolder` UI:
  - `Generate Test Stub` toggle
  - `Generate Integration Sample` toggle
- Added new scaffolder templates:
  - `Tests.cs.txt`
  - `SampleInstaller.cs.txt`

## 0.3.32
- Added diagnostics snapshot export baseline for QA/support:
  - `IDiagnosticsSnapshotExportService`
  - `DiagnosticsSnapshotExportService`
  - `DiagnosticsSnapshotExportResult`
- Added diagnostics export observability signals:
  - `DiagnosticsSnapshotExportedSignal`
  - `DiagnosticsSnapshotExportFailedSignal`
- Updated `FoundationObservabilityInstaller`:
  - registers diagnostics export signals
  - binds default diagnostics export path (`Application.persistentDataPath/foundation-diagnostics`)
  - binds `IDiagnosticsSnapshotExportService`
- Added runtime coverage:
  - `DiagnosticsSnapshotExportServiceTests`

## 0.3.31
- Added optional crash-reporting adapter contracts:
  - `FoundationCrashReport`
  - `ICrashReportingService`
- Updated global unhandled-exception flow:
  - `GlobalExceptionHandler` now forwards unhandled exceptions to optional crash-reporting service
  - forwarding is configurable via `ObservabilityConfig.ForwardUnhandledExceptionsToCrashReporting`
- Added crash-reporting observability signals:
  - `CrashReportSubmittedSignal`
  - `CrashReportSubmissionFailedSignal`
- Updated `FoundationObservabilityInstaller` signal registration for new crash-reporting signals.
- Expanded runtime coverage:
  - `GlobalExceptionHandlerTests` now includes crash-report submission/failure/config-disable scenarios.

## 0.3.30
- Added structured logging sink abstraction:
  - `FoundationLogEntry`
  - `IFoundationLogSink`
  - `UnityConsoleLogSink`
- Updated `UnityFoundationLogger`:
  - routes logs through configured sink bindings
  - keeps fallback Unity-console behavior when no sinks are bound
  - preserves `LogMessageEmittedSignal` emission flow
- Updated `FoundationObservabilityInstaller`:
  - binds default `UnityConsoleLogSink` when console logging is enabled
- Added runtime coverage:
  - `UnityFoundationLoggerTests`

## 0.3.29
- Added cloud save sync baseline on top of Save + Backend abstractions:
  - `ICloudSaveSyncService`
  - `CloudSaveSyncService`
  - `CloudSaveSyncResult` / `CloudSaveSyncAction`
- Added cloud save observability signals:
  - `SaveCloudPushedSignal`
  - `SaveCloudPulledSignal`
  - `SaveCloudConflictResolvedSignal`
  - `SaveCloudSyncFailedSignal`
- Updated `FoundationSaveInstaller`:
  - registers cloud save signals
  - binds `ICloudSaveSyncService`
- Added runtime coverage:
  - `CloudSaveSyncServiceTests`

## 0.3.28
- Added New Input System baseline with persistent rebind support:
  - `NewInputSystemAdapter`
  - `IInputRebindService` / `InputRebindService`
  - `IInputRebindStorage` / `PlayerPrefsInputRebindStorage`
  - runtime tests: `InputRebindServiceTests`, `InputRebindStorageTests`
  - docs updates (`README`, `ARCHITECTURE`, `BACKLOG`)
- Fixed New Input System compile setup for `NewInputSystemAdapter`:
  - added `Unity.InputSystem` reference to `Vareiko.Foundation.asmdef`
  - added `com.unity.inputsystem` package dependency in `package.json`
- Expanded startup validation baseline:
  - added built-in rules:
    - `SaveSecurityStartupValidationRule`
    - `BackendStartupValidationRule`
    - `ObservabilityStartupValidationRule`
  - added runtime coverage: `FoundationStartupValidationRulesTests`
- Remote config cache hardening:
  - added `IRemoteConfigCacheService` (`ForceRefreshAsync`, `InvalidateCache`)
  - updated `CachedRemoteConfigService` with refresh source tracking and cache invalidation signal
  - added runtime coverage: `CachedRemoteConfigServiceTests`
- PlayFab integration hardening:
  - added mapped backend error model (`BackendErrorCode`, `ErrorCode`, `IsRetryable`)
  - updated `PlayFabBackendService` with strict config/custom-id validation and normalized auth-state signaling
  - updated `PlayFabCloudFunctionService` with function-name and PlayFab-config validation
  - updated `PlayFabRemoteConfigService` with explicit readiness handling and guarded refresh behavior
  - expanded PlayFab smoke coverage for validation and auth-state transition scenarios

## 0.3.27
- Fixed runtime test compile compatibility in `SettingsServiceTests`:
  - removed invalid null-coalescing usage between `GameSettings` and generic `T` in fake `ISaveService.LoadAsync<T>` implementation.

## 0.3.26
- Added integration smoke coverage for external entry points:
  - `PlayFabServicesSmokeTests`
  - `AddressablesAssetProviderTests`
- Added dedicated PlayMode smoke assembly and test:
  - `Vareiko.Foundation.PlayModeTests`
  - `AudioServicePlayModeSmokeTests`
- Upgraded CI workflow:
  - Unity test matrix now runs both `editmode` and `playmode` when `UNITY_LICENSE` is configured.
- Expanded sample quality:
  - `Samples~/BasicSetup/Scenes/FoundationSampleScene.unity`
  - `Samples~/BasicSetup/Scripts/FoundationSampleSceneBootstrap.cs`
  - updated sample docs/metadata for ready scene wiring flow.

## 0.3.25
- Added runtime tests for remaining bootstrap/observability gaps:
  - `ConfigRegistryTests`
  - `GlobalExceptionHandlerTests`
- Covered key baseline scenarios:
  - config registry entry execution, default id normalization, task name fallback, and cancellation handling
  - global exception capture configuration gate, subscribe/dispose lifecycle, error-signal/log emission, and error-state transition guards

## 0.3.24
- Expanded runtime test coverage for remaining core modules:
  - `ConfigServiceTests`
  - `InputServiceTests`
  - `RetryPolicyTests`
  - `HealthCheckRunnerTests`
  - `FoundationDiagnosticsServiceTests`
  - `AudioServiceTests`
- Covered key baseline scenarios:
  - config register/get/unregister and missing-config signaling
  - input adapter resolution, preferred scheme switching, and fallback behavior
  - retry policy clamping, retry callback behavior, and null-argument guards
  - health-check pass/fail/exception paths and emitted check signals
  - diagnostics snapshot refresh cadence and boot-state signal handling
  - audio volume initialization/signal updates and clamping behavior

## 0.3.23
- Expanded runtime core test coverage:
  - `EnvironmentServiceTests`
  - `InMemoryEconomyServiceTests`
  - `SettingsServiceTests`
  - `AnalyticsServiceTests`
- Covered key baseline scenarios:
  - environment profile loading and typed parsing
  - economy seed initialization and operation validation paths
  - settings load/apply/save flow behavior
  - analytics consent gating, property merge behavior, and event buffer cap

## 0.3.22
- Added GitHub Actions CI workflow:
  - preflight package validation (`Tools/ci/validate-package.ps1`)
  - Unity EditMode tests via `game-ci/unity-test-runner` when `UNITY_LICENSE` is configured
  - explicit CI skip job when Unity license secret is absent
- Added CI validation script checks:
  - package/changelog version alignment
  - required package dependencies presence
  - `.meta` existence for package C# scripts
  - merge-conflict marker scan in tracked project directories
  - Unity version parse validation from `ProjectSettings/ProjectVersion.txt`

## 0.3.21
- Backend reliability:
  - added persistent cloud function queue storage contracts:
    - `ICloudFunctionQueueStore`
    - `PlayerPrefsCloudFunctionQueueStore`
    - `CloudFunctionQueueItem`
  - updated `ReliableCloudFunctionService`:
    - restores queued cloud functions on initialize
    - persists queue after enqueue/flush/dispose
    - emits `CloudFunctionQueueRestoredSignal`
  - updated backend installer to bind queue store by default.
- Connectivity recovery:
  - added network reachability abstraction:
    - `INetworkReachabilityProvider`
    - `UnityNetworkReachabilityProvider`
  - updated `ConnectivityService`:
    - optional focus-regained refresh via `IApplicationLifecycleService`
    - refresh cooldown from `ConnectivityConfig`
- Validation readiness:
  - added severity model: `StartupValidationSeverity`
  - added warning and completion signals:
    - `StartupValidationWarningSignal`
    - `StartupValidationCompletedSignal`
  - updated `StartupValidationRunner` to produce aggregated summary counts.
- Added runtime tests:
  - `ReliableCloudFunctionServiceTests`
  - `ConnectivityServiceTests`
  - `StartupValidationRunnerTests`

## 0.3.20
- Added application lifecycle baseline:
  - `IApplicationLifecycleService`
  - `ApplicationLifecycleService`
  - `IApplicationLifecycleSource`
  - `UnityApplicationLifecycleSource`
  - lifecycle signals: pause/focus/quit
- Updated `FoundationAppInstaller`:
  - registers lifecycle signals
  - binds `ApplicationLifecycleService`
- Updated `AutosaveService` to consume `IApplicationLifecycleService` when available (with fallback to previous direct Unity hook path).
- Added runtime tests:
  - `ApplicationLifecycleServiceTests`
  - autosave pause lifecycle integration coverage in `AutosaveServiceTests`

## 0.3.19
- Added reusable confirm dialog layer:
  - `IUIConfirmDialogService`
  - `UIConfirmDialogService`
  - `UIConfirmDialogPresenter`
  - `UIConfirmDialogRequest`
- Registered `UIConfirmDialogService` in `FoundationUIInstaller`.
- Added runtime tests:
  - `UIConfirmDialogPresenterTests`
  - `UIConfirmDialogServiceTests`

## 0.3.18
- Added typed window payload helpers:
  - `UIWindowResultPayload.Serialize<T>`
  - `UIWindowResultPayload.TryDeserialize<T>`
  - `UIWindowResult` extensions: `TryGetPayload<T>`, `GetPayloadOrDefault<T>`, `WithPayload<T>`
- Added runtime tests:
  - `UIWindowResultPayloadTests`

## 0.3.17
- Added awaitable window result flow:
  - `IUIWindowResultService`
  - `UIWindowResultStatus`
  - `UIWindowResult`
  - `UIWindowResolveButtonAction`
- Updated `UIWindowManager` to support:
  - `EnqueueAndWaitAsync`
  - `TryResolveCurrent`
  - `TryResolve`
  - `UIWindowResolvedSignal`
- Added runtime tests:
  - `UIWindowResultServiceTests`
  - `UIWindowResolveButtonActionTests`

## 0.3.16
- Fixed editor assembly references:
  - added `Zenject` reference to `Vareiko.Foundation.Editor.asmdef`
  - resolved compile errors in `FoundationProjectValidator` when using installer types derived from `MonoInstaller`

## 0.3.15
- Added UI button baseline components:
  - `UIWindowOpenButtonAction`
  - `UIWindowCloseButtonAction`
  - `UIBoolButtonInteractableBinder`
- Added runtime tests:
  - `UIWindowButtonActionsTests`
  - `UIBoolButtonInteractableBinderTests`

## 0.3.14
- Added UI collection binding baseline:
  - `UIItemCollectionBinder` (pooling + show/hide + optional destroy on shrink)
  - `UIItemCountBinder` (reactive count binding from `IUIValueEventService`)
- Added runtime tests:
  - `UIItemCollectionBinderTests`
  - `UIItemCountBinderTests`

## 0.3.13
- Added editor project validation tool:
  - menu: `Tools/Vareiko/Foundation/Validate Project`
  - validates scenes/prefabs for baseline Foundation wiring (`FoundationSceneInstaller`, `FoundationProjectInstaller` candidates)
  - validates UI baseline (`UIRegistry` presence when `UIElement` exists, empty/duplicate UI ids)
- Updated editor assembly references to include runtime assembly (`Vareiko.Foundation`).

## 0.3.12
- Added built-in reactive operators for `IReadOnlyValueStream<T>`:
  - `Map`
  - `Combine`
- Added `ComputedValueStream<T>` and `IComputedValueStream<T>` with disposable upstream subscriptions.
- Added runtime tests for reactive operators:
  - `ValueStreamOperatorsTests`

## 0.3.11
- Added built-in dependency-free reactive primitives:
  - `IReadOnlyValueStream<T>`
  - `IValueStream<T>`
  - `ValueStream<T>`
- Extended `IUIValueEventService` with reactive observers:
  - `ObserveInt`
  - `ObserveFloat`
  - `ObserveBool`
  - `ObserveString`
- Updated `UIValueEventService` to publish both:
  - `SignalBus` signals
  - `ValueStream<T>` updates
- Updated UI binders to prefer reactive stream subscription with SignalBus fallback.
- Extended `UIValueEventServiceTests` with stream behavior coverage.

## 0.3.10
- Added event-driven UI value bridge:
  - `IUIValueEventService` / `UIValueEventService`
  - typed value signals (`UIIntValueChangedSignal`, `UIFloatValueChangedSignal`, `UIBoolValueChangedSignal`, `UIStringValueChangedSignal`)
  - installer integration via `FoundationUIInstaller`
- Added ready UI binders for push-based updates:
  - `UIIntTextBinder`
  - `UIFloatTextBinder`
  - `UIStringTextBinder`
  - `UIBoolGameObjectBinder`
- Added runtime tests for value service behavior:
  - `UIValueEventServiceTests`

## 0.3.9
- Standardized primary UI API naming to `UI`:
  - added `IUIService` / `UIService`
  - added `IUINavigationService` / `UINavigationService`
  - added `FoundationUIInstaller` / `FoundationUINavigationInstaller`
  - added `UI*` signals (`UIReadySignal`, `UIElementShownSignal`, `UINavigationChangedSignal`, etc.)
- Kept `Ui*` contracts/signals/installers as `[Obsolete]` compatibility aliases.
- Added shared UI base layer components:
  - `UIElement`
  - `UIWindow`
  - `UIPanel`
  - `UIItemView`
  - `UIButtonView`
  - `UIRegistry` (with legacy `UIScreenRegistry` bridge)
- Added window sequencing manager:
  - `IUIWindowManager`
  - `UIWindowManager`
  - window queue lifecycle signals (`UIWindowQueuedSignal`, `UIWindowShownSignal`, `UIWindowClosedSignal`, `UIWindowQueueDrainedSignal`)
- Updated scene installer to use `FoundationUIInstaller` + `FoundationUINavigationInstaller` and `UIRegistry`.
- Added runtime tests for window queue behavior:
  - `UIWindowManagerTests`

## 0.3.8
- Added localization baseline module:
  - `LocalizationConfig`
  - `ILocalizationService` / `LocalizationService`
  - `FoundationLocalizationInstaller`
  - localization signals (`LanguageChangedSignal`, `LocalizationKeyMissingSignal`)
- Integrated localization config into:
  - `FoundationRuntimeInstaller`
  - `FoundationProjectInstaller`
- Added runtime smoke tests for localization fallback and language switch flow:
  - `LocalizationServiceTests`
- Added editor tooling for feature/module scaffolding:
  - `Vareiko.Foundation.Editor` assembly
  - `Tools/Vareiko/Foundation/Create Runtime Module` window
  - templates in `Editor/Scaffolding/Templates/*`

## 0.3.7
- Added package runtime test assembly `Vareiko.Foundation.Tests` (`Tests/Runtime`).
- Added test doubles for deterministic tests:
  - `FakeTimeProvider`
  - `InMemorySaveStorage`
  - `FakeConnectivityService`
  - `FakeRemoteConfigService`
- Added baseline tests for core runtime modules:
  - `BootstrapRunnerTests`
  - `SaveServiceTests`
  - `AutosaveServiceTests`
  - `LoadingServiceTests`
  - `FeatureFlagServiceTests`
  - `AssetServiceTests`
  - `SceneFlowServiceTests` (argument/contract validation smoke checks)

## 0.3.6
- Added save hardening baseline:
  - atomic write strategy in `FileSaveStorage`
  - rolling backups and restore fallback in `SaveService`
  - backup-related options in `SaveSecurityConfig`
  - save backup/restore signals
- Added autosave module:
  - `AutosaveConfig`
  - `AutosaveService`
  - autosave lifecycle signals
  - installer integration via `FoundationSaveInstaller`
- Added asset lifecycle tracking baseline:
  - reference counting and tracked release flow in `AssetService`
  - new asset diagnostics signals (`AssetReferenceChangedSignal`, `AssetReleasedSignal`, `AssetLeakDetectedSignal`)
  - diagnostics snapshot fields for tracked assets and references
- Updated project/runtime installers and docs to expose `AutosaveConfig`.

## 0.3.5
- Added environment profile module:
  - `EnvironmentConfig`
  - `IEnvironmentService` / `EnvironmentService`
  - `FoundationEnvironmentInstaller`
  - environment signals for profile change and missing keys
- Added global unhandled exception boundary in observability:
  - `GlobalExceptionHandler`
  - `UnhandledExceptionCapturedSignal`
  - new observability config toggles for exception capture and error-state fallback
- Added boot/runtime fallback to `AppState.Error` when boot task fails or unhandled exception is captured.
- Updated runtime installer and project installer to support `EnvironmentConfig`.

## 0.3.4
- Switched Zenject dependency from `com.gamesoft.zenject` to `net.bobbo.extenject` (OpenUPM).
- Updated versions to:
  - `com.cysharp.unitask` `2.5.10`
  - `net.bobbo.extenject` `9.3.2`
- Updated README dependency setup and manifest examples accordingly.

## 0.3.3
- Restored explicit package dependencies in `package.json` for OpenUPM:
  - `com.cysharp.unitask` `2.5.10`
  - `com.gamesoft.zenject` `9.2.2`
- Updated README dependency setup to OpenUPM flow (`openupm add ...`).

## 0.3.2
- Removed hard package dependencies on `UniTask` and `Extenject` from `package.json`.
- Dependency installation is now explicit and manual in the host project after adding `com.vareiko.foundation`.

## 0.3.1
- Fixed package dependency resolution for Extenject:
  - updated `com.svermeulen.extenject` from `9.2.0` to `9.2.0-stcf3` (OpenUPM available version).
- Kept `com.cysharp.unitask` on `2.5.10`.

## 0.3.0
- Added remote-config baseline for backend:
  - `RemoteConfigCacheConfig`
  - `CachedRemoteConfigService`
  - `PlayFabRemoteConfigService` integration point.
- Added feature flags module:
  - `IFeatureFlagService`
  - `FeatureFlagsConfig`
  - `RefreshFeatureFlagsBootstrapTask`.
- Added observability module:
  - `IFoundationLogger` and `UnityFoundationLogger`
  - diagnostics snapshot service
  - runtime diagnostics overlay view.
- Added save hardening baseline:
  - `SaveSecurityConfig`
  - `SecureSaveSerializer` (encryption + integrity hash + legacy fallback)
  - save conflict resolver contracts.
- Updated `FoundationRuntimeInstaller` and `FoundationProjectInstaller` with new module configs.

## 0.2.0
- Added package-level common runtime module:
  - retry policy
  - health-check contracts and runner
- Added connectivity runtime module (`IConnectivityService` + signals).
- Added loading orchestration module (`ILoadingService`) integrated with scene flow signals.
- Added consent/privacy runtime module (`IConsentService`) with persistent save-backed state.
- Updated analytics with privacy-first consent gate and dropped-event signal.
- Added save schema versioning and migration pipeline contracts.
- Added backend reliability layer:
  - retrying backend decorator
  - reliable cloud function service with offline queue
  - backend reliability config
- Updated composition root (`FoundationRuntimeInstaller`) and project installers to include new module configs.
- Updated architecture and backlog docs to reflect new baseline.

## 0.1.0
- Initial package structure.
- Added foundation runtime modules:
  - bootstrap
  - app state
  - asset management
  - config registry
  - input abstraction
  - scene flow
  - save/settings
  - economy
  - audio
  - UI and UI navigation
  - analytics abstraction
  - backend abstraction with PlayFab integration point
  - startup validation
- Added `FoundationProjectInstaller` and `FoundationSceneInstaller`.
