# Void Mart — working notes

Unity 6 (URP) hybrid-casual game. Read `README.md` first, then `Docs/ARCHITECTURE.md`.

## Ground rules

- **Everything under `Assets/VoidMart/Generated/` is output.** Never hand-edit it; change the
  generator or the config and rebuild (`Tools ▸ Void Mart ▸ Build Everything`).
- **`GameConfig` is the single source of truth** for tunable values. Add a new knob there, mark
  it `[Tweakable("Category", min, max)]`, and it appears automatically in both the Master Control
  window and the in-game debug overlay. Do not scatter magic numbers through components.
- **Art is resolved by key** through `GameAssets` (`GetMesh`, `GetSprite`, `GetMaterial`,
  `GetPrefab`, `GetClip`). Prefabs should not hold hard links to generated art where a key works.
- **Systems talk through `GameEventBus` channels**, not direct references. UI must not reach into
  gameplay classes.
- Gameplay loops must not allocate: pool objects, use the `NonAlloc` physics queries, avoid LINQ
  and string concatenation in `Update`.

## Before you commit

There is no .NET toolchain in the cloud container, so run the structural checker:

```
python3 Tools/cslint.py Assets/VoidMart
```

It catches delimiter imbalance, stray non-ASCII in code, duplicate serialised fields and types
used without the right `using`. It is not a compiler — if you have Unity available, open the
project and check the Console too.

## Gotchas worth remembering

- **Listeners added from editor scripts are not serialised.** Wire `Button.onClick` at runtime
  (see `HUDController.WireButtons`), never in the generators.
- **A modal must not deactivate its own GameObject**, or its `Start`/`Update` stop running and it
  can never hear the event that opens it. `ModalPanel` deactivates the panel child instead.
- **The stencil hole needs URP depth priming off**; `ProjectSetup.ConfigureUniversalRenderPipeline`
  does that on every renderer asset in the project.
- **The street texture tiles once per block pitch**, so the ground plane must stay an exact
  multiple of `blockSize + roadWidth`.
- `AssetWriter` rewrites assets in place on purpose — that is what keeps GUIDs, and therefore
  scene and prefab references, stable across rebuilds.
