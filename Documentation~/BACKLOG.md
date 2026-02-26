# Foundation Backlog

## Core Runtime
1. Deterministic startup test harness.
2. Extended domain installer templates (stateful feature toggles + optional scene services).
3. Add `ScriptableObjectInstaller` variants for package configs.

## UI and UX Platform
1. Screen transition pipeline.
2. Input-aware back-navigation policies.
3. Advanced loading presenter kit (progress animation profiles + localization-ready status labels).

## Assets and Config
1. Real Addressables provider (labels, groups, dependency tracking).
2. Asset reference counting and leak diagnostics.
3. Config schema validation and environment overrides.

## Save and Settings
1. Cloud/local conflict resolver.
2. Secure storage adapters.
3. Optional encryption and integrity hash for save payloads.

## Audio
1. AudioMixer integration and snapshots.
2. Event-based routing profiles.
3. Dynamic ducking policies.

## Analytics
1. Unity Analytics adapter.
2. Custom HTTP analytics adapter.
3. Event taxonomy registry and governance.
4. Consent scope UI kit + legal text localization pipeline.

## Backend and PlayFab
1. Full PlayFab adapter implementation (auth, data, economy).
2. Remote config service with cache and kill switches.
3. PlayFab cloud-function adapter with typed request/response contracts.
4. Persistent offline queue (disk-backed) for backend operations.

## Input and Economy
1. New Input System adapter with rebinding persistence.
2. Gamepad and touch adapters.
3. Backend-backed economy adapter.

## QA and Observability
1. Smoke tests for boot/state/scene/save/settings.
2. Integration tests for analytics/backend contracts.
3. Runtime diagnostics dashboard (health checks, connectivity, queue, save schema).
