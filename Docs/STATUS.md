# Build status (honest log)

Legend: ✅ verified by actually running · 🟡 written, not yet compiled in Unity · ⬜ not started

## Increment 1 — skeleton, Core rules, services (2026-10-05)

| Item | State | Evidence |
|---|---|---|
| Core library (combat, pips, jump assist, save, settings, economy, match rules) | ✅ | Compiled with .NET 8 (C# 9, warnings as errors) |
| 43 Core unit tests | ✅ | `dotnet run` in `DotnetTests/`: 43 passed, 0 failed |
| Same tests inside Unity Test Runner | 🟡 | Not run: Unity is not available in the cloud build environment |
| Runtime services (bootstrap, scenes, orientation, pause, save, settings, audio, haptics) | 🟡 | C# syntax-checked with Roslyn; NOT compiled against UnityEngine |
| iOS haptics plugin (`PKRHaptics.mm`) | 🟡 | Not compiled; needs an Xcode build |
| iOS Player Settings tool | 🟡 | Not run |
| iOS build / device test / FPS | ⬜ | Nothing claimed |

Known risks to check on first open:
- Package versions in `Packages/manifest.json` target Unity 6.0 LTS; a newer 6.x Editor may bump them.
- Orientation switching forces the rotation, then re-enables auto-rotate 0.25 s later; verify on device.

## Increment 2 — Nova controller, touch controls, movement sandbox (2026-10-05)

| Item | State | Evidence |
|---|---|---|
| Core: MovementStats, JumpPhysics, ActionBuffer | ✅ | 11 new tests; 54/54 pass with `dotnet run` |
| PlatformerMotor2D, NovaAbilities (roll, air dash, meteor drop), FighterVisual, CameraFollow2D | 🟡 | Syntax-checked; Unity APIs verified against Unity's published C# source; NOT compiled in Unity |
| Touch controls (floating stick, 4 buttons, safe area, high contrast, drag-to-edit layout saved to settings) | 🟡 | Same as above; needs Device Simulator / iPhone test |
| Keyboard/gamepad bindings (code-defined Input System actions) | 🟡 | Same as above |
| Movement sandbox builder (layers, placeholder sprites, Nova assets, scene) | 🟡 | Not run |
| 5 PlayMode motor tests | 🟡 | Written, not run |
| Feel tuning on device | ⬜ | Numbers are starting values; expect to tune |

Fixed during review (before anything ran): jumping up through a one-way platform could count as landing;
knockback didn't cancel Nova's abilities; rolling off a ledge glided; orientation force could be cancelled on iOS;
PlayMode tests sampled the interpolated transform instead of the physics body.

Known limits: Special on the ground does nothing yet (comet-staff attacks come in increment 3);
the debug panel's buttons sit over the joystick zone, so tapping them can also start the floating stick.

## Increment 3 — Combat runtime (2026-10-05)

| Item | State | Evidence |
|---|---|---|
| Core: AttackTimeline (phases, cancel windows), SwingHitLog | ✅ | 7 new tests; 61/61 pass with `dotnet run` |
| MoveDefinition / Moveset data, Nova's 6 moves (Courier Jab → Comet Sweep, Rising Arc, Tailspin, Comet Bolt, Meteor Shockwave) | 🟡 | Written; assets are created by the editor tool, not yet run |
| Damageable (HP + Guard Pips), Hurtbox, CombatQuery, AttackRunner, Projectile + PoolService, HitStop, HitFlash, StatusPips, TrainingDummy | 🟡 | Syntax-checked; Unity APIs checked against Unity's C# source by a review pass; NOT compiled in Unity |
| Sandbox: two training dummies (HP and Guard Pips) | 🟡 | Builder updated, not run |
| 3 PlayMode combat tests | 🟡 | Written, not run |

Fixed during review: attacks now process their first frame before the motor (a jump buffered on the same frame
could turn a ground jab into an air move); hit stop restores the previous time scale; dummies can't get stuck
mid-respawn; hitboxes use the physics body position instead of the interpolated transform.

Known limits: hit flash is a color tint (proper white-flash shader comes with real art); no hit sounds yet
(silent placeholders); Nova can't be damaged in the sandbox because nothing attacks her yet (enemies in increment 4).

## Increment 4 — Story Quest test level (2026-10-05)

| Item | State | Evidence |
|---|---|---|
| Core: PatrolLogic, HopperLogic, LevelRun | ✅ | 8 new tests; 69/69 pass with `dotnet run` |
| Enemies (Cog Beetle walker, Spring Tick hopper) built from data and pooled; contact damage; shard drops | 🟡 | Syntax-checked + API review; not run in Unity |
| Pickups (Star Shards, health crystals) pooled; puff effects pooled | 🟡 | Same |
| Moving platforms with rider carry (motor change), spikes, pits, 2 checkpoints, secret room with fake wall, goal gate | 🟡 | Same |
| LevelFlowController (death/respawn, enemy reset, run stats, save + reward on clear), StoryHUD | 🟡 | Same |
| PKR > Build Story Test Level | 🟡 | Not run |
| 2 PlayMode tests (platform carry, spikes + i-frames) | 🟡 | Written, not run |

Fixed during review: platforms now update after riders (rising/slowing lifts no longer drop "grounded");
landing on a moving platform no longer double-counts its speed; hoppers can't get stuck after being hit
mid-hop; the player can't die after reaching the goal; a lethal hit during a pit respawn now resets properly;
dropped shards fall back down instead of freezing mid-air; the lift starts flush with the floor.

Known limits: blocks are sprite-tiled GameObjects rather than a Tilemap (simpler to generate; fine at this size);
the level-complete banner is temporary (full screen with buttons in increment 5); Spring Ticks can hop off ledges.

## Upcoming increments
2. Nova controller + touch controls + Input actions 🟡 (written; awaiting Unity run)
3. Combat runtime (hitbox/hurtbox, attack runner, hit feedback) 🟡 (written; awaiting Unity run)
4. Story test level, enemies, pickups, checkpoints, hazards (+ scene builder) 🟡 (written; awaiting Unity run)
5. Pause menu, level-complete screen ⬜
6. Main menu, character select, settings UI ⬜
7. Arena test scene with bots ⬜
8. Docs pass, final status report ⬜
