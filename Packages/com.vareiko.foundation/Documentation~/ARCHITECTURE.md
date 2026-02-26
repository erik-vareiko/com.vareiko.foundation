# Foundation Architecture

## Composition Model
- `ProjectContext`: global runtime services.
- `SceneContext`: scene-local UI and startup tasks.
  `FoundationSceneInstaller` can auto-bind `ConfigRegistry` components as bootstrap tasks.
- Scene UI layer:
  - `FoundationUIInstaller` (`IUIService`, `IUIWindowManager`)
  - `FoundationUINavigationInstaller` (`IUINavigationService`)
  - shared registry contract: `UIRegistry` (legacy bridge: `UIScreenRegistry`)

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
17. `FoundationAudioInstaller`
18. `FoundationAnalyticsInstaller`
19. `FoundationBackendInstaller`
20. `FoundationFeatureFlagsInstaller`
21. `FoundationValidationInstaller`
22. `FoundationObservabilityInstaller`

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
