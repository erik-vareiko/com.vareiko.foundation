# Changelog

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
