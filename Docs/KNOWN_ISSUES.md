# Known issues and limits

## Not verified yet (the big one)

- **No Unity compile or play session has happened.** All Unity-side code was syntax-checked with Roslyn and
  its Unity API calls were reviewed against Unity's published C# source, but it has not been compiled against
  UnityEngine. Expect a handful of compile errors and tuning issues on first open. Use [FIRST_RUN.md](FIRST_RUN.md).
- **26 PlayMode tests** are written but have never run.
- **No iOS build, device test or FPS measurement** has been done. The 60 FPS target is unmeasured.
- `PKRHaptics.mm` has not been compiled by Xcode.
- `Packages/manifest.json` targets Unity 6.0 LTS; a newer 6.x Editor may bump package versions.
- `ProjectSettings/` is not in the repo until you commit the one Unity creates (SETUP.md step 6).

## Gameplay limits (current)

- All four heroes are playable: Nova and Brick from the start, Luma after clearing Sunspire Meadows, Rex Rollo after
  clearing it with the secret found.
- Rex's momentum and wall ride are the biggest feel risk: numbers are first guesses and need on-device tuning.
- CPU fighters don't wall ride on purpose and don't build speed before Momentum Ram.
- CPU fighters pick a random hero you've unlocked; you can't choose their heroes on the setup screen yet.
- CPU fighters never lay Spark Coils and ignore coils on the ground.
- Spark Coils are per attacker and survive until they fire or time out (6 s); they are cleared on rematch.
- Super armor ignores hazard knockback too, and in Story Quest nothing counts as a launch, so it never breaks there.
- One Story level (Sunspire Meadows test), the Clockwork Warden boss arena, and one arena (Skyforge test).
- The Warden is built from placeholder shapes (gear sprites, circles); no intro/outro cutscene or boss music.
- Warden timings and 24 HP are first guesses; tune `WardenTuning` and the component fields after play-testing.
- The level select is a simple list of cards, not a drawn world map yet.
- Levels cleared before the level select existed show rank "-" until replayed (ranks weren't saved before).
- Shard counts include enemy drops, so the results screen can show more than the level's total (the level select caps it).
- **Codex** button in the main menu is a placeholder ("later").
- Star Shards accumulate but there is **no cosmetics shop** to spend them in yet.
- **No audio clips**: AudioService and the volume sliders work, but there is nothing to hear.
- Single local player only (no local or online multiplayer).

## Rough edges to expect

- Movement, combat and bot numbers are starting values; they need play-testing on a device.
- Hit flash is a color tint, not a white-flash shader.
- Levels are sprite-tiled GameObjects rather than Tilemaps.
- Spring Ticks can hop off ledges.
- CPU fighters are simple (no shielding, combos beyond jab → sweep, or edge-guarding).
- In the movement sandbox, the debug panel's buttons overlap the joystick zone.
- Orientation switching forces a rotation, then re-enables auto-rotate 0.25 s later; check on device.
- Touch controls only appear on devices / in the Device Simulator (or with *Force Show* on `TouchControls`).
