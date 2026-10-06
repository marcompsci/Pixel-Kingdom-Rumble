# Phase 2 backlog

Ordered by what unblocks the most. Step 0 comes first because everything else builds on Phase 1 actually running.

## 0. Make Phase 1 real (before new features)
- Work through [FIRST_RUN.md](FIRST_RUN.md); fix compile errors and failing PlayMode tests.
- Commit Unity's `ProjectSettings/` and template assets.
- First iPhone build; record FPS in STATUS.md; tune movement and touch feel on device.
- Bot difficulty pass (Easy should be beatable by a new player, Hard should feel fair).

## 1. Heroes
| Hero | Kit idea (original) | Tech needed |
|---|---|---|
| ~~**Brick** (stone guardian)~~ ✅ 2.1 | Slow, heavy; armored charge; ground-slam shockwave; 4 Guard Pips; can't air dash but has a stone-step double jump | Done: super armor, `HeroKitDefinition`, `HeroAbilities` |
| ~~**Luma** (magnet-glove inventor)~~ ✅ 2.2 | Magnet tether pulls foes; deployable spark coil trap; magnet hop | Done: pull projectile, pooled `SparkTrap`; zipping to platforms is a possible later upgrade |
| ~~**Rex Rollo** (roller-skating lizard)~~ ✅ 2.3 | Momentum: keeps speed, wall-ride, tail-whip spin, skid-turn | Done: coast/skid in `MovementStats`, `WallRide` in the motor, speed-scaled hits |

Also: ~~generic hero kits~~ ✅, ~~CPUs pick random heroes~~ ✅; ~~unlock rules~~ ✅ (clear a level); still to do: choosing CPU heroes on the setup screen, CPUs using traps.

## 2. Story Quest
- **Clockwork Warden** boss: multi-phase (gear sweep, piston slam, weak-point core exposed after a stagger),
  boss health bar, arena-style room, intro/outro. Core: a boss phase state machine with .NET tests.
- Real Sunspire Meadows levels (1-1 to 1-3) + a world map, switched to **Tilemaps** with rule tiles.
- New enemy types (flying Gyro Moth, shielded Bolt Knight).
- Level select with ranks and secrets per level.

## 3. Arena Clash
- More stages (each with one original stage event), stage select.
- Items toggle (a few original arena items).
- Smarter CPUs: edge-guarding, shielding/dodging projectiles, per-hero tactics.
- Local multiplayer with controllers (2–4 players) — the `FighterIntent` design already supports it.

## 4. Meta
- **Codex**: heroes, enemies (text already in `EnemyDefinition`), stages, lore entries unlocked by play.
- **Cosmetics shop** for Star Shards (palettes, trails, victory poses). Cosmetic-only; `Economy` already handles purchases.
- **Game Center**: sign-in, leaderboards (best times), achievements.
- Cloud save (iCloud key-value or Game Center saved games).

## 5. Presentation
- **Real pixel art** for heroes, enemies, tiles, UI (commissioned or made in Aseprite; record in CREDITS_AND_LICENSES.md).
- Animations (sprite sheets / Animator) replacing squash-and-stretch placeholders.
- **Audio**: original music for menus, meadow, arena, boss; SFX for hits, KOs, pickups, UI.
- White-flash hit shader, particles, parallax backgrounds.
- Accessibility: colorblind palettes, button remapping on gamepad, reduced motion.

## 6. Shipping
- App icon, launch screen, App Store screenshots.
- Privacy manifest (`PrivacyInfo.xcprivacy`) and age rating.
- TestFlight beta; crash reporting.
- Unity CI build (e.g. GameCI) in addition to the .NET Core tests.
- Final name: rename via [NAMING.md](NAMING.md).
