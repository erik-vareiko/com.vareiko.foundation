# Vertical Slice — level-0 dogfood

Proves the "boots a project in minutes" claim: one empty scene + one component gives
you a full foundation boot (project scope → scene scope → bootstrap pipeline →
custom app state → ticking gameplay loop → autosaved progress).

## Run it

1. Import this sample via Package Manager (Samples → Vertical Slice).
2. Create an **empty scene**.
3. Add an empty GameObject and attach `VerticalSliceBootstrap`.
4. Press Play.

## What it demonstrates

| Piece | Where |
|---|---|
| Project + scene `LifetimeScope` wiring from code | `VerticalSliceBootstrap` |
| `IBootstrapTask` loading a save profile before gameplay | `LoadProfileBootstrapTask` |
| Custom `AppState` (`new AppState("Run")`) flowing through `IAppStateMachine` | `SliceGameplayDriver` |
| `ITickService` listener + `Repeat` timer instead of `Update()`/coroutines | `SliceGameplayDriver` |
| `ComponentPool<T>` spawn/despawn of primitives | `SliceGameplayDriver` |
| Dictionary-bearing save model (Newtonsoft default serializer) | `SliceProfile`, autosave on interval |
| Scene-wide injection (`[Inject] Construct` on a scene MonoBehaviour) | `SliceGameplayDriver` |

Watch the Console: boot signals, state transitions and autosave writes are logged.
Stop and replay — the run counter persists through `ISaveService`.
