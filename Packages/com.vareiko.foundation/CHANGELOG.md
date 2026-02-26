# Changelog

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
