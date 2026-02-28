# Foundation Architecture

## Composition Model
- `ProjectContext`: global runtime services.
- `SceneContext`: scene-local UI and startup tasks.
  `FoundationSceneInstaller` can auto-bind `ConfigRegistry` components as bootstrap tasks.
- Scene UI layer:
  - `FoundationUIInstaller` (`IUIService`, `IUIWindowManager`)
  - `FoundationUINavigationInstaller` (`IUINavigationService`)
  - shared registry contract: `UIRegistry` (legacy bridge: `UIScreenRegistry`)
- Input layer:
  - `FoundationInputInstaller` binds adapters in priority order.
  - `NewInputSystemAdapter` is preferred when `ENABLE_INPUT_SYSTEM` is defined.
  - `LegacyKeyboardInputAdapter` + `NullInputAdapter` remain fallback.
  - `IInputRebindService` persists binding overrides through `IInputRebindStorage`.
- Practical onboarding flow for a fresh host project is documented in `Documentation~/STARTER_FLOW.md`.

## Starter Runtime Flow
1. Boot scene creates `ProjectContext` and installs global services.
2. `FoundationRuntimeInstaller` installs core modules in deterministic order.
3. Bootstrap tasks run through `BootstrapRunner`.
4. Startup validation emits `StartupValidationCompletedSignal`.
5. App transitions into initial state or safe `AppState.Error` fallback on fatal boot failure.
6. Gameplay scenes provide `SceneContext` with scene-local UI/navigation bindings.

## Runtime Bootstrap Order
1. `FoundationTimeInstaller`
2. `FoundationCommonInstaller`
3. `FoundationEnvironmentInstaller`
4. `FoundationAppInstaller`
5. `FoundationBootstrapInstaller`
6. `FoundationConfigInstaller`
7. `FoundationAssetInstaller`
8. `FoundationConnectivityInstaller`
9. `FoundationInputInstaller`
10. `FoundationSceneFlowInstaller`
11. `FoundationLoadingInstaller`
12. `FoundationSaveInstaller`
13. `FoundationConsentInstaller`
14. `FoundationSettingsInstaller`
15. `FoundationLocalizationInstaller`
16. `FoundationEconomyInstaller`
17. `FoundationIapInstaller`
18. `FoundationAdsInstaller`
19. `FoundationPushNotificationInstaller`
20. `FoundationMonetizationInstaller`
21. `FoundationAudioInstaller`
22. `FoundationAnalyticsInstaller`
23. `FoundationAttributionInstaller`
24. `FoundationBackendInstaller`
25. `FoundationFeatureFlagsInstaller`
26. `FoundationValidationInstaller`
27. `FoundationObservabilityInstaller`

## Core Principles
- Fail-fast dependency resolution.
- No runtime scene object discovery in installers.
- Interface-first service contracts.
- Provider abstraction for analytics/backend.
- Privacy-by-default analytics gate through consent service.
- Retry + queue wrappers for backend operations (offline-aware cloud functions).
- Remote-config first feature rollout through feature flags service.
- Unified runtime observability layer (logger + diagnostics snapshot).
- Fallback to `AppState.Error` on boot failure and unhandled runtime exceptions.
- Save reliability baseline with atomic writes, rolling backups and restore fallback.
- Asset lifecycle baseline with reference tracking and leak diagnostics signals.
- Localization lookup fallback chain (`current language -> fallback language -> key/default`).
- Unified UI primitives for screens/windows/panels/items/buttons with queue-based window sequencing.
- Input abstraction supports runtime adapter fallback and persisted binding overrides.
- Startup validation includes baseline production-safety rules for save security, backend config and observability config.
- Backend remote-config layer supports explicit cache invalidation and forced refresh control via `IRemoteConfigCacheService`.
- Backend auth/data result models include normalized error mapping (`BackendErrorCode`) and retryability metadata.
- Save layer includes cloud sync orchestration (`ICloudSaveSyncService`) with resolver-based conflict choices (`KeepLocal` / `UseCloud` / `Merge`).
- Observability logger supports structured sink fan-out via `IFoundationLogSink` and `FoundationLogEntry`.
- Unhandled-exception boundary supports optional crash-provider forwarding via `ICrashReportingService` and `FoundationCrashReport`.
- Diagnostics service supports snapshot export to a local QA/support file path via `IDiagnosticsSnapshotExportService`.
- Editor scaffolder can bootstrap runtime module code plus optional test and sample-installer stubs for faster project wiring.
- Editor project validator includes release-gate checks (package version/dependencies, script `.meta` completeness, merge markers and Unity version parse).
- Environment module includes starter profile presets (`dev`, `stage`, `prod`) for quick project bootstrap.
- IAP module provides provider abstraction with a simulated baseline and safe null fallback.
- IAP module includes Unity IAP adapter path (`UnityInAppPurchaseService`) with dependency guard when `UNITY_PURCHASING` is unavailable.
- Ads module provides rewarded/interstitial abstraction with consent-aware gate and simulated/null providers plus external bridge adapter path (`ExternalAdsBridgeService`).
- Push notifications module provides consent-aware permission/topic abstraction with simulated/null providers.
- Push notifications module includes Unity adapter path (`UnityPushNotificationService`) guarded by `FOUNDATION_UNITY_NOTIFICATIONS`.
- Monetization module provides centralized cooldown/session-cap policy for ad and IAP operations via `IMonetizationPolicyService`.
- Attribution module provides provider abstraction with host-bridge integration (`ExternalAttributionBridgeService`) and consent-aware tracking gate.
- Observability diagnostics snapshot includes revenue/comms counters and latency metrics sourced from monetization telemetry signals.
