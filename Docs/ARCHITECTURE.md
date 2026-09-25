# Architecture

## Shape of the project

Void Mart is a **ScriptableObject-event architecture with a service locator**, as the technical
spec asks for. Nothing in gameplay holds a reference to the UI, and nothing in the UI holds a
reference to gameplay — they meet at event channel assets.

```
                     ServiceInstaller  (DontDestroyOnLoad, execution order -10000)
                              │
        ┌────────────┬────────┴────────┬──────────────┬──────────────┐
   SaveService   EconomyService   AudioService    PoolService   Ad / IAP / Platform
        │              │               │               │
        └──────────────┴───── ServiceLocator ──────────┘
                               │
                        GameEventBus (ScriptableObject)
                               │
   ┌───────────────┬───────────┴────────────┬────────────────────┐
PlayerHole     StoreManager            PuzzleController        HUD / modals
```

`ServiceInstaller` lives in both scenes. The Boot copy survives the load and the Game copy
destroys itself if one already exists, which means a designer can press Play straight from
`Game.unity` and still get a fully wired session.

## The event bus

`GameEventBus` is a generated asset holding one channel per signal. Channels are typed
ScriptableObjects (`SignalChannel`, `IntChannel`, `SwallowChannel`, `PuzzleChannel`, …) with
pre-sized listener lists iterated backwards, so dispatch allocates nothing and is safe against
unregistration during a raise.

| Group | Channels |
|-------|----------|
| Economy | `cashChanged`, `gemsChanged`, `xpChanged`, `playerLevelUp`, `productSold` |
| Hole | `propSwallowed`, `holeFillChanged`, `holeTierChanged`, `holeFull`, `unloadRate`, `unloadFinished`, `feverStarted`, `feverEnded` |
| Store | `nodePurchased`, `machineJammed`, `machineRepaired`, `areaChanged` |
| Puzzle | `puzzleRequested`, `puzzleCompleted` |
| Flow | `stateChanged`, `toast`, `saveRequested`, `dataReloaded` |

## The core loop

1. **Gather.** `PlayerHoleController` moves on joystick input and runs one
   `Physics.OverlapSphereNonAlloc` per frame against the `VMSwallowable` layer. Props inside the
   suction field drift toward the rim; props whose footprint fits inside the radius are eaten.
2. **Consume.** `Swallowable` implements the spec's consumption physics: bodies idle as
   kinematic, and on being eaten they wake up, switch to the `SubGround` layer (which collides
   with nothing) and run `Scale(t) = lerp(1, 0, t^2.5)` with a squash-and-stretch term.
3. **Unload.** Standing on a `Hopper` drains the hole into `StoreManager`'s raw-material balance
   at a configurable rate; junk arcs into the funnel and the pneumatic loop's volume follows the
   live transfer rate.
4. **Craft.** `CraftingMachine` pulls raw mass, produces a `ProductDefinition` on a timer and
   flies the finished unit to a `ProductShelf`. Machines jam on a schedule.
5. **Sell.** `CustomerSpawner` feeds shoppers in; `CustomerAgent` walks to a shelf, takes items,
   queues at `Checkout`, pays, and leaves. Cash stacks on the counter until the player (or a
   `RobotHelper`) collects it, at which point bills arc to the collector on a Bezier.
6. **Expand.** `FurnishingZone` pads drain the wallet on an exponential tick while the player
   stands on them, then retire with a particle poof while the structure snaps in on a
   1.0 → 1.25 → 1.0 bounce.
7. **Unjam.** A jammed machine raises `puzzleRequested` when the player comes close;
   `PuzzleController` generates a board, the overlay pops in on EaseOutElastic, and a solve pays
   a cash burst and repairs the machine.

## The hole

Rendered with the stencil technique from the spec, not CSG:

| Object | Shader | Stencil |
|--------|--------|---------|
| Mask quad (tracks the player) | `VoidMart/HoleMask` | `Ref 1, Comp Always, Pass Replace`, `ColorMask 0`, `ZWrite Off`, `ZTest Always`, queue `Geometry-100` |
| Street / shop floor | `VoidMart/ClayLit` | `Ref 1, Comp NotEqual` |
| Pit interior (under the hole) | `VoidMart/PitInterior` | `Ref 1, Comp Equal`, `Cull Front` |

The mask draws before everything else, so the ground refuses to render inside the circle and the
shaft beneath shows through. The one-click setup also disables **depth priming** on every URP
renderer in the project, because the prepass can otherwise swallow the stencil writes.

## Persistence

`SecureSaveEngine` follows section 8 of the spec: a PBKDF2 key (10 000 iterations, SHA-256)
derived from the device identifier, AES-256-GCM, and a temp-file write followed by a replace so a
kill mid-save can never truncate a good file.

Two deliberate deviations, both documented in `SPEC_MAPPING.md`: the `AesGcm` constructor is
resolved by reflection so the same code compiles against .NET Standard 2.1 and .NET 8, and if the
platform has no working AES-GCM the engine transparently falls back to AES-256-CBC with
HMAC-SHA256 (encrypt-then-MAC). A format byte records which path wrote the file.

The serialised payload matches the published schema field for field
(`saveVersion`, `timestamp`, `profile`, `hole`, `store`, `meta`), plus additive `progress` and
`settings` blocks that older readers can ignore.

## Generation order

`VoidMartBuilder.BuildEverything` runs the generators in dependency order:

```
config assets → project settings (layers!) → content tables → meshes
   → font + sprites + ground textures → materials → audio
   → prefabs (need layers, meshes, materials, sprites) → store nodes (need prefabs)
   → scenes (need everything) → build settings
```

`AssetWriter` writes every asset **in place** — meshes have their data copied into the existing
asset, textures overwrite the same PNG path, ScriptableObjects are loaded-or-created — so GUIDs
never change and nothing loses its references on a rebuild.

## Performance notes

- One clay material covers every prop, character and machine; colour comes from baked vertex
  colours, so the street batches into very few draw calls.
- The whole district is a single quad with a tileable texture repeated once per block.
- Props, cash bills, crafted goods, junk, poofs and shoppers all come from `ObjectPool`.
- The city's respawn scan is staggered across frames.
- Physics queries use the `NonAlloc` variants with a pre-sized buffer.
- `NumberFormat` serves small integers from a cache and builds the rest through a shared
  `StringBuilder`, so the HUD does not allocate while counting.

## Checking the sources

There is no .NET toolchain in some environments, so `Tools/cslint.py` performs a structural pass
over the C# sources: delimiter balance, stray non-ASCII in code, duplicate serialised fields, and
types used without a `using` for the namespace that declares them.

```
python3 Tools/cslint.py Assets/VoidMart
```
