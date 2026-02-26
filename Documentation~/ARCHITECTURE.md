# Foundation Architecture

## Composition Model
- `ProjectContext`: global runtime services.
- `SceneContext`: scene-local UI and startup tasks.
  `FoundationSceneInstaller` can auto-bind `ConfigRegistry` components as bootstrap tasks.

## Runtime Bootstrap Order
1. `FoundationTimeInstaller`
2. `FoundationCommonInstaller`
3. `FoundationBootstrapInstaller`
4. `FoundationAppInstaller`
5. `FoundationConfigInstaller`
6. `FoundationAssetInstaller`
7. `FoundationConnectivityInstaller`
8. `FoundationInputInstaller`
9. `FoundationSceneFlowInstaller`
10. `FoundationLoadingInstaller`
11. `FoundationSaveInstaller`
12. `FoundationConsentInstaller`
13. `FoundationSettingsInstaller`
14. `FoundationEconomyInstaller`
15. `FoundationAudioInstaller`
16. `FoundationAnalyticsInstaller`
17. `FoundationBackendInstaller`
18. `FoundationValidationInstaller`

## Core Principles
- Fail-fast dependency resolution.
- No runtime scene object discovery in installers.
- Interface-first service contracts.
- Provider abstraction for analytics/backend.
- Privacy-by-default analytics gate through consent service.
- Retry + queue wrappers for backend operations (offline-aware cloud functions).
