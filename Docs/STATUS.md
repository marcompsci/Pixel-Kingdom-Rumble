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
- Orientation switching uses the documented force-then-autorotate pattern; verify on device.

## Upcoming increments
2. Nova controller + touch controls + Input actions ⬜
3. Combat runtime (hitbox/hurtbox, attack runner, hit feedback) ⬜
4. Story test level, enemies, pickups, checkpoints, hazards (+ scene builder) ⬜
5. Pause menu, level-complete screen ⬜
6. Main menu, character select, settings UI ⬜
7. Arena test scene with bots ⬜
8. Docs pass, final status report ⬜
