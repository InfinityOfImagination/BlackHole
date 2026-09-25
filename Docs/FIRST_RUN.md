# First run — what you should see

After **Tools ▸ Void Mart ▸ Build Everything**, the Game scene opens. Press Play.

## Expected

1. **Splash (from `Boot.unity`)** — the wordmark pops in on an elastic scale over a dark void,
   a progress bar fills, then the Game scene fades in. Press Play on `Game.unity` directly and you
   skip straight to the game; both work.
2. **The street** — a dusk-blue block grid with lane markings, buildings with lit windows, parked
   cars, bins, cones, trees, and stickmen who run away from you.
3. **The hole** — a black disc punched through the road with a cyan event-horizon ring. Drive over
   a cone and it should tilt, shrink and drop out of sight with a rubbery pop.
4. **The HUD** — level and XP top-left, cash and gems top-right, a radial capacity gauge at the
   bottom. The gauge fills as you eat; at 100% it pulses magenta and an arrow points north.
5. **The shop** — follow the arrow. Crossing the threshold switches the music and the HUD label
   reads STOREFRONT. Park on the hopper pad: the hole empties, junk arcs into the funnel, and a
   pneumatic loop rises with the transfer rate.
6. **The loop closes** — the crusher turns raw mass into chairs, boxes glide to the shelf,
   shoppers queue at the register, and cash stacks on the counter. Drive over it to collect.
7. **Expansion** — glowing pads show a name and a price. Stand on one and cash drains faster the
   longer you hold it, bills arcing out of you onto the pad. When it completes: a poof, and the
   machine snaps in with a bounce.
8. **A jam** — a machine shakes with a spanner over it. Drive close and the block-sorting overlay
   pops in. One tray piece always completes a line; place it and the machine restarts with a cash
   burst.

## Quick checks

| Check | Where |
|-------|-------|
| Console is clean | No errors after the build or on entering Play |
| Font renders | Any HUD label; if you see nothing, the font atlas did not import |
| Hole punches the road | Look straight down — you should see the shaft, not a black circle |
| Audio plays | Music on the street, a different loop inside, pops as you eat |
| Save works | Play, earn, stop, Play again — cash and hole size persist |
| Tweaker opens | F1 in play mode |

## If something looks wrong

- **The hole is a flat black disc.** Depth priming is re-enabled on a URP renderer. Re-run
  **Tools ▸ Void Mart ▸ Build Everything**, or set Depth Priming Mode to *Disabled* on the
  renderer assets in `Assets/Settings`.
- **Nothing responds to touch or clicks.** The EventSystem's input module lost its actions. Delete
  the EventSystem object and re-run the scene build from the Master Control dashboard.
- **Magenta objects.** A shader failed to compile — check the Console; the shader files live in
  `Assets/VoidMart/Shaders`.
- **No props anywhere.** The prop pools have no prefabs. Rebuild prefabs, then store nodes, from
  the Master Control dashboard.
