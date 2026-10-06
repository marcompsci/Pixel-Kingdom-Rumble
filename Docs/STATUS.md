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

## Upcoming increments
2. Nova controller + touch controls + Input actions 🟡 (written; awaiting Unity run)
3. Combat runtime (hitbox/hurtbox, attack runner, hit feedback) 🟡 (written; awaiting Unity run)
4. Story test level, enemies, pickups, checkpoints, hazards (+ scene builder) ⬜
5. Pause menu, level-complete screen ⬜
6. Main menu, character select, settings UI ⬜
7. Arena test scene with bots ⬜
8. Docs pass, final status report ⬜
