# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

A Unity project whose real product is the embedded package
**`com.vareiko.foundation`** (`Packages/com.vareiko.foundation/`) — a universal
infrastructure "level-0" template that any Unity project pulls in to skip startup
wiring. The surrounding Unity project (`Assets/`, `ProjectSettings/`) is the host
used to develop and test the package; the package is the thing that ships.

Unity: `2022.3`. Package version: `2.0.0` (evolving toward `3.0.0`, see below).

## Source layout

- `Packages/com.vareiko.foundation/Runtime/` — all runtime code (~296 .cs files),
  one folder per module (App, Bootstrap, Save, UI, Backend, Ads, Iap, Analytics, …).
- `Packages/com.vareiko.foundation/Editor/` — scaffolding + project validator.
- `Packages/com.vareiko.foundation/Documentation~/` — architecture & roadmap docs.
- `Packages/com.vareiko.foundation/Samples~/BasicSetup/` — onboarding sample.
- Top-level `Assembly-*.csproj`, `Unity.*.csproj`, `Library/`, `obj/`, `Logs/`,
  `*.sln` are Unity-generated — **never hand-edit, never commit them as changes**.
- `Docs/`, `PROJECT_CONTEXT.md` — project-level process notes.

Ignore `Library/`, `obj/`, `Logs/`, `UserSettings/`, `.idea/` — generated/IDE state.

## Tech stack (current)

- **DI:** Zenject / `net.bobbo.extenject` — *being migrated to VContainer* (see below).
- **Messaging:** Zenject `SignalBus` — *being migrated to MessagePipe*.
- **Async:** UniTask (`Cysharp.Threading.Tasks`) — keep.
- **Input:** Unity Input System behind `IInputService` — keep.
- **Configs:** ScriptableObject `*Config` assets injected into installers — keep.
- **Assets:** Addressables behind `IAssetService` (Resources fallback) — keep.

## Architecture conventions (match these)

- **Interface-first.** Every service is `I<Name>Service` + impl; bindings go through
  a `Foundation<Module>Installer`. Provider modules ship a triad:
  `Null<X>` (safe no-op), `Simulated<X>` (editor/dev), and a real SDK-backed impl.
- **Composition:** `FoundationRuntimeInstaller.InstallProjectServices` installs all
  modules in a **deterministic order** (see `Documentation~/ARCHITECTURE.md`). Do not
  reorder casually — boot depends on it.
- **Bootstrap:** `IBootstrapTask`s run through `BootstrapRunner`; fatal failure falls
  back to `AppState.Error`. No runtime scene-object discovery in installers.
- **Events:** services fire signals via the message bus, guarded
  (`_signalBus?.Fire(new XSignal(...))`). One `*Signals.cs` per module.
- **Optional deps:** heavy use of `[InjectOptional]` + null-guards so modules degrade
  rather than hard-fail when a dependency isn't bound.
- **Async:** return `UniTask`/`UniTask<T>`, thread `CancellationToken`, `.Forget()`
  fire-and-forget loops.
- **Style:** explicit types (not `var`) is the prevailing convention here; Pascal'd
  signal/config types; `sealed` classes by default. Mirror the surrounding file.

## Active work — v3.0 refactor

The package is mid-refactor toward a true universal level-0. **Read
`Packages/com.vareiko.foundation/Documentation~/REFACTOR_PLAN_3_0.md` before
architectural work** — it holds the locked decisions, gap list, and phase plan.

Headline decisions: migrate **Zenject → VContainer** and **SignalBus → MessagePipe**;
split the monolithic asmdef into `Foundation.Core` + opt-in modules; add Core
primitives (object pool, `ITickService`, generic FSM, `Result<T>`). Phases run in
order — safety-net tests → DI migration → asmdef split → Core primitives → maturity.

When adding code during the transition, prefer patterns that survive the DI move
(constructor injection, explicit dependencies) over Zenject-specific constructs.

## Build / test

- Open the project in Unity 2022.3 to compile; there is no standalone `dotnet build`
  path (the `.csproj` files are Unity-generated).
- Tests live in `Vareiko.Foundation.Tests` (EditMode), `…PlayModeTests`,
  `…EditorTests`. Run via Unity Test Runner (Window → General → Test Runner).
- `Tools/ci/validate-package.ps1` runs package release-gate checks; CI is
  `.github/workflows/ci.yml`.

## Conventions for changes

- Touch only `Packages/com.vareiko.foundation/**` for package work; don't commit
  Unity-generated files or `Library/obj/Logs` churn.
- Keep every new `.cs` paired with a Unity `.meta` (Unity creates it; don't delete).
- Update `CHANGELOG.md` and, for architecture shifts, `Documentation~/ARCHITECTURE.md`
  and the refactor plan's "Done when" checkboxes.
- Commit messages follow the existing `feat(scope):` / `fix:` / `docs:` style.
