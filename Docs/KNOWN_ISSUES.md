# Known issues and limits

## Verified 2026-10-08

Compiles in Unity 6000.6.4f1, all scenes build, 145 EditMode + 39 PlayMode tests pass in Unity, and the signed iOS
build succeeds. See STATUS.md "Stage 1 result". Everything below that says "never compiled" is now outdated for
compiling and tests; **no human has played it yet**, and nothing has run on an iPhone.

## Not verified yet (the big one)

- **No Unity compile or play session has happened.** All Unity-side code was syntax-checked with Roslyn and
  its Unity API calls were reviewed against Unity's published C# source, but it has not been compiled against
  UnityEngine. Expect a handful of compile errors and tuning issues on first open. Use [FIRST_RUN.md](FIRST_RUN.md).
- **29 PlayMode tests** are written but have never run.
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
- Gyro Moths have 1 HP; a tougher flyer would fall under gravity while knocked back (fine for now).
- Bolt Knight shields only exist in Story Quest; arena fighters have no shields.
- Levels cleared before the level select existed show rank "-" until replayed (ranks weren't saved before).
- Shard counts include enemy drops, so the results screen can show more than the level's total (the level select caps it).
- The Codex covers heroes, foes and places; there are no separate lore pages yet, and places have no art (a sky-colored dot).
- After updating, pages you had already earned (heroes unlocked, levels cleared) show as NEW once.
- Defeat counts start at zero for foes defeated before the Codex existed.
- The shop sells palettes (sprite tints) only; trails, victory poses and other cosmetics are still to come.
- Palettes for a hero appear in the shop only once that hero is unlocked.
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


## Phase 3 (Versus, stealth and platform levels)

- **Not playtested by a person yet.** Fighter balance, CPU difficulty, guard sight ranges and jump gaps were tuned
  by numbers and a rough reachability script, not by hand.
- Fighters are paper-doll placeholder sprites without animation frames; specials reuse the existing hitbox and
  projectile systems.
- Guards' line of sight is blocked by any ground, including one-way planks.
- The Rooftop Run secret is entered by dropping onto the low plank in the gap between the second and third roofs.

## Shadow Contracts (2026-10-10)

- The $3.99 unlock can't be bought on a device until the in-app purchase exists in App Store Connect (see SHADOW_CONTRACTS.md). Tester builds have an unlock button instead.
- Purchases use Unity IAP 5.0 (moved from the deprecated IAP 4 on 2026-10-10).
- Fixed: vines, springs, crates, hay, objectives, relics and scrolls were missing from built scenes, because Unity only saves a component whose file has the same name. Each is now in its own file, and a smoke test guards this.

## Phase 4 (2026-10-11)

- Game Center: scores and achievements only post once the leaderboard `pkr.lb.contracts` and the 10 achievements in
  `Core/Save/Achievements.cs` exist in App Store Connect (Features > Game Center) with exactly those IDs.
- The upload warned that the UnityRuntime dSYM is missing, so crash reports for engine code won't be symbolicated. The game's own code is unaffected.
- Audio is synthesized placeholder chiptune; volumes were set by ear in code, not on a phone.
- First-time tips are remembered per save; resetting the save shows them again.

## Zombie update 2 (2026-10-17): co-op, performance, accessibility, content

- **Online co-op is still untested on two real phones.** The new parts (ping, interpolation, ordering, drop and
  rejoin, lobby roster, version check) are covered by in-memory tests only. GameKit only re-invites a dropped player
  in two-player matches; in 3-4 player matches a dropped player stays with the AI until the run ends.
- **Frame rate on a device is not measured yet.** The game now logs a `[PKR Perf]` line every 30 s (fps, 95th
  percentile frame, missed frames, tier, thermal state); read it from a device log to get real numbers.
- Colour-vision palettes cover danger, health, status and squad colours; the art itself (sprites, tiles) is unchanged.
- Weapon mastery only counts kills on the host and in solo play; guests in online co-op don't earn finishes yet.
- Splitter and burrower use tinted existing zombie sprites (no new animation frames yet).
- The new leaderboard (`pkr.lb.gravemoor`) and four achievements only work once they exist in App Store Connect
  (Tools/asc_fix.py gc_leaderboard / gc_achievement) and are added to the version's Game Center section.
- New music and sounds are synthesized placeholders; volumes were set by numbers, not by ear on a phone.
