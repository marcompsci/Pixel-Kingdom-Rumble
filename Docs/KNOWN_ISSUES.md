# Known issues and limits

## Not verified yet (the big one)

- **No Unity compile or play session has happened.** All Unity-side code was syntax-checked with Roslyn and
  its Unity API calls were reviewed against Unity's published C# source, but it has not been compiled against
  UnityEngine. Expect a handful of compile errors and tuning issues on first open. Use [FIRST_RUN.md](FIRST_RUN.md).
- **19 PlayMode tests** are written but have never run.
- **No iOS build, device test or FPS measurement** has been done. The 60 FPS target is unmeasured.
- `PKRHaptics.mm` has not been compiled by Xcode.
- `Packages/manifest.json` targets Unity 6.0 LTS; a newer 6.x Editor may bump package versions.
- `ProjectSettings/` is not in the repo until you commit the one Unity creates (SETUP.md step 6).

## Gameplay limits (current)

- **Nova** and **Brick** are playable. Luma and Rex Rollo are locked in character select.
- Brick is unlocked from the start (no unlock rule yet).
- CPU fighters pick a random playable hero; you can't choose their heroes on the setup screen yet.
- Super armor ignores hazard knockback too, and in Story Quest nothing counts as a launch, so it never breaks there.
- Only one Story level (Sunspire Meadows test) and one arena (Skyforge test). No Clockwork Warden yet.
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
