# Story Quest World 3: The Shattered Realms

Under the Clockwork Glacier sat the Tickworks' mainspring. Freeing it cracked the engine's heart into four sealed realms, each with its own broken rule. The core mind that split them waits where they meet.

On the level select, World 3 is its own page ("<" / ">"). The four realms are drawn as one map split into four colored sections, with the crossing and the boss underneath. Clockwork Glacier's NEXT button leads straight into World 3.

| # | Level | Rule | New foe |
|---|---|---|---|
| 1 | Ember Wastes | Lava pits (`a`), crumbling basalt bridges (`c`), fire geysers on a timer (`f`), a ferry over the lava lake | Cinder Crawler (`Y`) |
| 2 | Sky Gardens | Tailwinds over the gaps (`>`), a headwind up high (`<`), vines | Puff Glider (`Q`) |
| 3 | Sunken Clockworks | Water currents that carry you over spike channels (`)`), an undertow that pushes back (`(`), drowned gears and lifts | Rust Crab (`D`) |
| 4 | Toy Fortress | Conveyor floors (`}` / `{`), trapdoors over spike pits (`c`) | Wind-up Soldier (`W`) |
| 5 | The Shattered Crossing | All four rules in one 236-wide run, in four sections split by stone gates with a checkpoint at each | All four |
| 6 | The Fourfold Sovereign (boss) | Runs on the Warden's fight engine, but harder: | — |

The Sovereign compared with the Warden:
- 25% faster (`WardenTuning.Scaled(1.25)`).
- 36 HP.
- Two more cogs per volley.
- Its body changes realm color each phase.
- Its stagger window shrinks less than its attacks, so it stays fair.

## How the new pieces work
- `ForceZone` (wind / current / conveyor):
  - Wind and current add acceleration in `PlatformerMotor2D.AddPush`. It is full strength in the air and 45% on the ground.
  - Extra speed is capped at run speed + 6.
  - Holding against the wind still makes headway.
  - Currents add a little lift.
  - Conveyors carry a grounded hero like a moving platform (3.2 u/s).
- `CrumblePlatform`: falls 0.5 s after you land, comes back after 3 s, and never reappears inside the hero.
- `FireGeyser`: 2.2 s dormant, 0.8 s warning, 1.3 s eruption (Core `BridgeCycle`). The column is 4 cells tall and hurts and launches like spikes.
- Lava: hurts and launches like spikes, but harder.

## Checks
- Every map passes `AsciiLevel.Validate` and the layout tests.
- Every map passed the cell-by-cell reachability check with the push zones switched off. Wind and currents only make jumps easier or a little harder.
- `ShatteredRealmsTests` covers each realm's rule, the crossing's section order, the new tiles and the Sovereign tuning.
