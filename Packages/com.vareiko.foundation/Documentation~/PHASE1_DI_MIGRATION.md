# Phase 1 — DI Migration (Zenject → VContainer, SignalBus → MessagePipe)

> Companion to `REFACTOR_PLAN_3_0.md`. This is the migration spec: translation
> rules, before/after, the two open decisions, and the execution order.

## Translation table

| Zenject / SignalBus | VContainer / MessagePipe |
|---|---|
| `static Install(DiContainer c)` per module | `static Install(IContainerBuilder b)` per module (keep the modular static-installer shape) |
| `MonoInstaller` + `InstallBindings()` | `LifetimeScope` + `Configure(IContainerBuilder builder)` |
| `container.Bind<IFoo>().To<Foo>().AsSingle()` | `builder.Register<Foo>(Lifetime.Singleton).As<IFoo>()` |
| `BindInterfacesAndSelfTo<Foo>().AsSingle()` | `builder.Register<Foo>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()` |
| `BindInterfacesAndSelfTo<Foo>().AsSingle().NonLazy()` where `Foo : IInitializable/ITickable` | `builder.RegisterEntryPoint<Foo>(Lifetime.Singleton).AsSelf()` (+ `.As<IFoo>()` if a typed handle is needed) |
| `container.BindInstance(config)` | `builder.RegisterInstance(config)` (`.AsSelf()` / `.As<T>()`) |
| `container.HasBinding<T>()` guard | `// not needed` — each installer is called once from one scope (see "Idempotency") |
| `[Inject]` constructor | plain constructor — VContainer injects the (single) ctor automatically; the attribute can stay or go |
| `[InjectOptional] Dep d = null` | **no native equivalent** → Decision B below |
| `SignalBus` injected | `IFoundationSignalBus` facade (Decision A) |
| `container.DeclareSignal<T>()` | `builder.RegisterMessageBroker<T>(options)` |
| `_signalBus?.Fire(new T(...))` | `_signalBus.Publish(new T(...))` |
| `_signalBus.Subscribe<T>(h)` / `Unsubscribe` | `_signalBus.Subscribe<T>(h)` → returns `IDisposable`; dispose to unsubscribe |
| Zenject `IInitializable` / `ITickable` / `IDisposable` | `VContainer.Unity.IInitializable` / `ITickable` / `System.IDisposable` — same method names (`Initialize`/`Tick`/`Dispose`), only the `using` changes |
| `ProjectContext` prefab | root `LifetimeScope` (parent scope) |
| `SceneContext` | scene `LifetimeScope` (child scope) |

## Root composition setup (once)

```csharp
// In the root LifetimeScope.Configure(builder):
var options = builder.RegisterMessagePipe();
builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
builder.Register<IFoundationSignalBus, MessagePipeSignalBus>(Lifetime.Singleton);

// All foundation signal types get a broker (1:1 with today's DeclareSignal calls),
// centralized in one place instead of scattered across installers:
FoundationSignalBrokers.Register(builder, options); // RegisterMessageBroker<T> for each T
```

## Decision A — signal replacement strategy

**Option A1 (recommended): thin facade.** Keep the small-diff path.

```csharp
public interface IFoundationSignalBus
{
    void Publish<T>(T message);
    IDisposable Subscribe<T>(Action<T> handler);
}

public sealed class MessagePipeSignalBus : IFoundationSignalBus
{
    public void Publish<T>(T message) => GlobalMessagePipe.GetPublisher<T>().Publish(message);
    public IDisposable Subscribe<T>(Action<T> handler) => GlobalMessagePipe.GetSubscriber<T>().Subscribe(handler);
}
```

- Diff in ~42 services is mechanical: `[InjectOptional] SignalBus` → `IFoundationSignalBus`,
  `_signalBus?.Fire(x)` → `_signalBus.Publish(x)`.
- Cost: one indirection layer + relies on `GlobalMessagePipe` provider being set first.

**Option A2: idiomatic per-type.** Inject `IPublisher<T>` / `ISubscriber<T>` directly.

- Pure MessagePipe, explicit dependencies, best perf.
- Cost: a service firing N signal types injects N publishers; large, hand-written diff
  across 42 services. Heavy for a migration whose goal is parity, not redesign.

> Recommendation: **A1 now**, optionally refactor hot paths to A2 later. Either way every
> signal type needs `RegisterMessageBroker<T>` — that burden is identical to today's
> `DeclareSignal<T>` and is just moved into `FoundationSignalBrokers`.

## Decision B — optional-dependency strategy

Zenject `[InjectOptional]` has no VContainer equivalent; an unregistered ctor param throws.

**Option B1 (recommended): always-bind defaults (fail-fast).**
- `SignalBus` optionality disappears — the facade is always registered.
- Optional configs → register a default instance when the serialized field is null:
  `builder.RegisterInstance(_config != null ? _config : ScriptableObject.CreateInstance<XConfig>())`.
- Optional services (e.g. `ISaveMigrationService`) → bind a `Null*` default.
- Net effect: constructors take non-optional deps; the `_x?.` guards can be dropped over
  time. This finally realizes the "fail-fast dependency resolution" principle the docs
  already claim (today it is fail-*silent*).

**Option B2: preserve optionality.** Add a `TryResolve`-style wrapper / register `Option<T>`.
- Smallest behavioral change, but keeps the fail-silent ergonomics and adds a helper layer.

> Recommendation: **B1**, applied with parity in mind (defaults reproduce today's
> null-guarded behavior), module by module during the sweep.

## Before / after

### 1. Static module installer (`FoundationConsentInstaller`)

```csharp
// BEFORE (Zenject)
public static void Install(DiContainer container)
{
    if (container.HasBinding<IConsentService>()) return;
    if (!container.HasBinding<SignalBus>()) SignalBusInstaller.Install(container);
    FoundationSaveInstaller.Install(container);
    container.DeclareSignal<ConsentLoadedSignal>();
    container.DeclareSignal<ConsentChangedSignal>();
    container.BindInterfacesAndSelfTo<ConsentService>().AsSingle().NonLazy();
}

// AFTER (VContainer + MessagePipe)
public static void Install(IContainerBuilder builder)
{
    FoundationSaveInstaller.Install(builder);
    // ConsentLoaded/ConsentChanged brokers live in FoundationSignalBrokers (registered once).
    builder.RegisterEntryPoint<ConsentService>(Lifetime.Singleton).As<IConsentService>().AsSelf();
}
```

### 2. Service using signals (`ConsentService`)

```csharp
// BEFORE
using Zenject;
[Inject] public ConsentService(ISaveService saveService, [InjectOptional] SignalBus signalBus = null)
...
_signalBus?.Fire(new ConsentLoadedSignal(_state.IsCollected));

// AFTER
using VContainer.Unity; // for IInitializable
public ConsentService(ISaveService saveService, IFoundationSignalBus signalBus)
...
_signalBus.Publish(new ConsentLoadedSignal(_state.IsCollected));
```

### 3. Composition root (`FoundationProjectInstaller : MonoInstaller`)

```csharp
// BEFORE
public sealed class FoundationProjectInstaller : MonoInstaller
{
    [SerializeField] private AnalyticsConfig _analyticsConfig;
    // ...
    public override void InstallBindings()
        => FoundationRuntimeInstaller.InstallProjectServices(Container, _analyticsConfig, ...);
}

// AFTER
public sealed class FoundationProjectScope : LifetimeScope
{
    [SerializeField] private AnalyticsConfig _analyticsConfig;
    // ...
    protected override void Configure(IContainerBuilder builder)
        => FoundationRuntimeInstaller.InstallProjectServices(builder, _analyticsConfig, ...);
}
```

`FoundationRuntimeInstaller.InstallProjectServices` keeps its shape; only the first
parameter changes `DiContainer container` → `IContainerBuilder builder`, and it also does
the one-time MessagePipe / facade / broker registration shown in "Root composition setup".

### 4. Entry points (`BootstrapRunner`, `ConnectivityService`)

`IInitializable`/`ITickable`/`IDisposable` keep their method bodies; swap
`using Zenject;` → `using VContainer.Unity;`, and register via `RegisterEntryPoint<T>`.

## Idempotency note

Today every installer guards with `HasBinding<T>()` because the runtime list of installers
could double up. Under VContainer each module installer is invoked exactly once from a
single `LifetimeScope.Configure`, so the guards are dropped. (This is why the Phase 0
idempotency test was intentionally omitted.)

## Test migration

`FoundationCompositionTests` and the signal-emission tests are rewritten against VContainer:

```csharp
var builder = new ContainerBuilder();
FoundationRuntimeInstaller.InstallProjectServices(builder);
using var container = builder.Build();           // IObjectResolver
GlobalMessagePipe.SetProvider(container.AsServiceProvider());
Assert.That(container.Resolve<IAppStateMachine>(), Is.Not.Null);
```

The transition/state tests (no SignalBus) carry over unchanged. The signal tests move from
`SignalBus` to `IFoundationSignalBus` / MessagePipe. **These tests must stay green — they
are the parity proof.**

## Execution order (de-risked)

- **Phase 1a — pilot (reference implementation).** Decisions locked: **A1 facade**,
  **B1 always-bind defaults**. Status (2026-06-08): **GREEN** — all 3 pilot tests pass
  in Unity 6.3 alongside the Zenject Phase 0 tests, validating the full
  VContainer + MessagePipe + facade stack live. (27 unrelated pre-existing EditMode
  failures are frozen in `TEST_BASELINE.md`.)
  - `Runtime/Signals/IFoundationSignalBus.cs` + `MessagePipeSignalBus.cs` — the facade.
  - `Tests/Runtime/Composition/VContainerPilotTests.cs` — isolated smoke test of the new
    stack (facade publish/subscribe via brokers + `GlobalMessagePipe`, entry-point
    register/resolve, default-instance binding). Additive: no Zenject code touched, so the
    Phase 0 baseline stays green alongside it.
  - asmdefs (`Vareiko.Foundation`, `Vareiko.Foundation.Tests`) now reference VContainer +
    MessagePipe **alongside** Zenject for the transition.
  - This validates the uncertain API edges before committing everywhere. (The real
    App+Consent+Save conversion + root `LifetimeScope` lands at the start of Phase 1b,
    once the pilot is green.)
- **Phase 1b — incremental service migration (stays green on Zenject).** To avoid a
  non-compiling half-migrated monolith, the container stays Zenject during 1b. A
  transitional `ZenjectFoundationSignalBus : IFoundationSignalBus` (bound in
  `FoundationCommonInstaller`) wraps the live Zenject `SignalBus`, so services can move to
  the facade **one module at a time** while still composing through Zenject and staying
  green. Per module: `SignalBus` field/ctor → `IFoundationSignalBus` (non-optional, per B1);
  `_signalBus?.Fire(x)` → `_signalBus.Publish(x)`; `Subscribe`/`Unsubscribe` →
  `Subscribe(...)` returning `IDisposable`. Installers keep their `DiContainer` signature and
  `DeclareSignal<T>` calls for now. Tests that construct services directly switch to the
  DI-agnostic `FakeSignalBus` test double.
  - Status (2026-06-08): **ALL services migrated to `IFoundationSignalBus`** (~50 files:
    App, Consent, Analytics, Environment, Features, Settings, Economy, Localization,
    SceneFlow, Config, Ads, Attribution, Iap, Push, Backend, Save, Monetization, Common,
    Validation, Bootstrap, Connectivity, Input, AssetManagement, UI, UINavigation, Audio,
    Loading, Observability). Fire-only services use `_signalBus?.Publish(...)`; subscribers
    track `IDisposable`s (single field or `List<IDisposable>`) and dispose instead of
    unsubscribe. All affected tests moved to the DI-agnostic `FakeSignalBus`.
  - Only Zenject usage left in the package: the 28 module installers (`SignalBusInstaller` +
    `DeclareSignal`), the transitional `ZenjectFoundationSignalBus` adapter, and two tests
    that intentionally exercise the still-Zenject installers/composition. These all convert
    in Phase 1c.
- **Phase 1c — cutover (atomic).** Once every service is on the facade: convert installers
  `DiContainer` → `IContainerBuilder`, replace the two `MonoInstaller`s with root/scene
  `LifetimeScope`s, register MessagePipe + brokers (`FoundationSignalBrokers`) + the
  `MessagePipeSignalBus`, switch Zenject `IInitializable`/`ITickable` usings to
  `VContainer.Unity`, delete `ZenjectFoundationSignalBus`, rewrite the composition/signal
  tests against VContainer, then swap asmdef references and `package.json` deps and remove
  `net.bobbo.extenject`. Full composition test green against the frozen `TEST_BASELINE.md` = done.

## Validated VContainer patterns (proven green in `VContainerPilotTests`, 2026-06-08)

Use these exact forms when converting installers — each is confirmed at runtime:

```csharp
// interface binding
builder.Register<Foo>(Lifetime.Singleton).As<IFoo>();
// concrete + all interfaces (for IInitializable/ITickable services)
builder.Register<Foo>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
// lifecycle entry point (Initialize/Tick dispatched by the LifetimeScope at runtime)
builder.RegisterEntryPoint<Foo>(Lifetime.Singleton);
// config / default instance (ex-[InjectOptional] config → always register, real or default)
builder.RegisterInstance(_config != null ? _config : ScriptableObject.CreateInstance<FooConfig>());
// string id parameter (e.g. SaveRootPath, DiagnosticsExportRootPath)
builder.Register<Foo>(Lifetime.Singleton).WithParameter<string>(path);
// MessagePipe facade + brokers (once, in the root)
var options = builder.RegisterMessagePipe();
builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
builder.Register<MessagePipeSignalBus>(Lifetime.Singleton).As<IFoundationSignalBus>();
FoundationSignalBrokers.Register(builder, options);
```

**Decorator chains (Backend retry/queue/cache wrappers) — DELEGATE REGISTRATION ONLY.**
`WithParameter` (by type or by name) does NOT break the `IFoo → Wrapper → IFoo` cycle when
the wrapper is registered `.As<IFoo>()` (confirmed: "Circular dependency detected"). Register
the inner as its concrete type and build the wrapper with a factory lambda:

```csharp
builder.Register<PlayFabBackendService>(Lifetime.Singleton);            // inner, concrete only
builder.Register<IBackendService>(
    r => new RetryingBackendService(r.Resolve<PlayFabBackendService>(), config, r.Resolve<IFoundationSignalBus>()),
    Lifetime.Singleton);                                               // wrapper via delegate
```

**Minimal-cutover refinement (lower risk):** keep the Zenject asmdef reference during the
cutover (dead, unused) so the ~50 services keep their `[Inject]`/`[InjectOptional]` attributes
(VContainer ignores them) and `using Zenject;`. Only change service class declarations that
implement lifecycle interfaces to fully-qualified `VContainer.Unity.IInitializable` /
`VContainer.Unity.ITickable` (avoids the Zenject-vs-VContainer ambiguity). `[InjectOptional]`
params still need their dependency registered — register config defaults / Null services in the
installers. Strip Zenject attributes/usings and remove the package entirely in a later cleanup
pass once composition runs on VContainer.

## Done when
- No `Zenject` reference remains in any package asmdef or `using`.
- `FoundationCompositionTests` (VContainer) and all signal tests are green.
- Sample scene boots through the new root `LifetimeScope`.
