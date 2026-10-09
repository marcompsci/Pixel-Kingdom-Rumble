# Build status (honest log)

Legend: ✅ verified by actually running · 🟡 written, not yet compiled in Unity · ⬜ not started

## Phase 1 summary (2026-10-06): code-complete, not yet run in Unity

| Area | State | What that means |
|---|---|---|
| Core rules (`PKR.Core`) + 94 EditMode tests | ✅ | Built and run with .NET 8: 94/94 pass. GitHub Actions runs the same tests on every push |
| Unity runtime + editor code (~9,600 lines of C# incl. Core) | 🟡 | Syntax-checked with Roslyn; Unity API usage reviewed against Unity's C# source; **never compiled against UnityEngine** |
| 15 PlayMode tests | 🟡 | Written, never run |
| Scenes, data assets, placeholder art | 🟡 | Produced by PKR menu commands that have never been run |
| iOS build, device test, 60 FPS | ⬜ | Nothing built or measured |

Phase 1 feature list: boot → main menu → character select (Nova playable, 3 locked) → Sunspire Meadows test
level (enemies, pickups, hazards, checkpoints, secret, goal, results) and Skyforge Arena (Stock/Timed/Training,
1-3 CPUs, retracting bridge, results), with touch controls, pause, settings, save, Star Shards and haptics.

Phase 2 has started on top of this (see the Phase 2 log below); it is in the same unverified state.

Next step: [FIRST_RUN.md](FIRST_RUN.md). Known gaps: [KNOWN_ISSUES.md](KNOWN_ISSUES.md).
What comes after: [PHASE2_BACKLOG.md](PHASE2_BACKLOG.md).

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

## Increment 5 — Pause menu, settings, level-complete screen (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: ResultsMath (time format, percent, rank) | ✅ | 5 new tests; 74/74 pass with `dotnet run` |
| UIFactory + UITheme (code-built buttons, toggles, sliders, panels; high contrast) | 🟡 | Syntax-checked + API review; not run in Unity |
| PauseMenu (button, Esc/Start, auto-pause on background, Resume/Restart/Settings/Main Menu) | 🟡 | Same |
| SettingsPanel (volumes, haptics, shake, high contrast, floating stick, control size, edit/reset layout) | 🟡 | Same |
| Touch layout editing from the pause menu (DONE/RESET bar), controls hidden while paused/results | 🟡 | Same |
| LevelCompleteScreen (rank, stats, NEW BEST, reward count-up, Play Again) | 🟡 | Same |
| 3 PlayMode menu tests | 🟡 | Written, not run |

Fixed during review: settings panel narrowed to fit 4:3 iPads; menus pre-select their first button for
keyboard/gamepad; control-size slider refreshes after a layout reset; settings saves are batched instead of
written every slider tick; layout-edit bar got a backing panel and moved below the timer; slider fill lines up
with the handle; resuming with Esc during layout editing exits edit mode cleanly.

Known limits: Main Menu / Next buttons are disabled until increment 6 adds the menu scene; no audio clips yet,
so the volume sliders have nothing audible to change.

## Increment 6 — Boot, main menu, character select (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: RosterSelection | ✅ | 4 new tests; 78/78 pass with `dotnet run` |
| Roster of 4 heroes (Nova playable; Brick, Luma, Rex Rollo locked) with original lore and placeholder art | 🟡 | Data created by editor tool; not run |
| BootLoader, MainMenuUI, CharacterSelectUI, GameSession, CanvasMatchByAspect | 🟡 | Syntax-checked + API review; not run in Unity |
| PKR > Build Menu Scenes (00_Boot first in build), PKR > Build All Scenes | 🟡 | Not run |
| 1 PlayMode character-select test | 🟡 | Written, not run |

Fixed during review: menus switch canvas scaling by screen shape so they fit 4:3 iPads; Build All no longer
re-prompts or half-builds on cancel; choosing a hero always keeps it valid in the save; disabled buttons aren't
pre-selected.

## Increment 7 — Arena Clash: Skyforge Arena with CPU fighters (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: BotBrain (+ difficulty), ArenaMatchConfig, BridgeCycle | ✅ | 16 new tests; 94/94 pass with `dotnet run` |
| ArenaMatchController (spawn, blast-zone KOs with credit, respawn i-frames, clock, rewards) | 🟡 | Syntax-checked + API review; not run in Unity |
| FighterFactory, BotController, SkyforgeBridge (stage event), camera group framing | 🟡 | Same |
| ArenaSetupUI (Stock/Timed/Training, 1-3 CPUs, Easy/Normal/Hard), ArenaHUD, ArenaResultsScreen | 🟡 | Same |
| PKR > Build Arena Test (joins Build All) | 🟡 | Not run |
| 1 PlayMode arena test (KO → elimination → results) | 🟡 | Written, not run |
| Bot difficulty / fun tuning | ⬜ | Needs play-testing |

Fixed during review: runtime-built fighters now wake up with all components present (attacks and dodges no
longer overlap); CPUs treat the open bridge gap like an edge and recover to the nearest platform; respawns drop
over the side platforms instead of the gap; two falls in the same step can't wrongly eliminate someone; CPUs don't
fire a queued attack while recovering; tests no longer add Star Shards to your real save; HUD cards don't double
up for a frame on rematch.

Known limits: every fighter uses Nova's kit (other heroes are locked in Phase 1); no hit/KO sounds yet.

## Increment 8 — Docs pass and Phase 1 report (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| ARCHITECTURE, FIRST_RUN checklist, KNOWN_ISSUES, PHASE2_BACKLOG docs | ✅ | Written; paths and menu names checked against the code |
| GitHub Actions workflow `Core tests` (.NET 8, `DotnetTests/`) | ✅ | First run on GitHub (commit 67c6c3d) completed with success |
| No gameplay code changes | n/a | |

# Phase 2

## 2.1 — Brick joins, generic hero kits, super armor (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: `CombatMath.ApplySuperArmor`, `AttackTimeline.InArmorWindow`, `RosterSelection.PickRandom`, bots recover with mid-air jumps and only zone with projectiles | ✅ | 7 new tests; 101/101 pass with `dotnet run` |
| `HeroAbilities` + `HeroKitDefinition` (replaces Nova-only `NovaAbilities`); Nova and Brick kits | 🟡 | Syntax-checked + independent review against Unity's C# source; not compiled in Unity |
| Brick: 6 moves (Boulder Jab → Quarry Hook, Pillar Uppercut, Rockfall Elbow, Bulwark Charge with super armor, Landslide Shockwave), Stone Step, Granite Guard, Landslide Slam; 6 HP, 4 Guard Pips | 🟡 | Data created by `DataAssets` (also upgrades a Phase 1 locked Brick asset); not run |
| Super armor in `Damageable` (damage applies, knockback/hitstun skipped unless launched) | 🟡 | Same |
| Hero swap at runtime (`HeroLoadout`, `SelectedHeroLoader` on Story/sandbox players); CPU fighters pick random heroes | 🟡 | Same |
| 4 PlayMode tests (armor keeps a move going, a normal hit cancels it, hero swap, mid-air jump) | 🟡 | Written, not run |
| Brick feel / balance vs Nova | ⬜ | Needs play-testing |

Fixed during review: bot recovery jumps were released early (cut short by the jump-cut rule); renamed Nova kit
fields keep any existing Inspector tuning (`FormerlySerializedAs`); Brick CPUs no longer fire Bulwark Charge from
mid range (it could carry them off the stage), since only projectile specials are used for zoning.

## 2.2 — Luma joins, hero unlock rules, traps (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: `TrapLogic` (fall, arm, fire once, expire), `UnlockRules` (clear a level to unlock a hero) | ✅ | 5 new tests; 106/106 pass with `dotnet run` |
| Luma: Wrench Tap → Voltage Swing, Arc Flick, Static Spin, Magnet Tether (pull), Spark Coil (trap), Magnet Hop; unlocked by clearing Sunspire Meadows | 🟡 | Data created by `DataAssets`; syntax-checked + independent review against Unity's C# source; not compiled in Unity |
| `SparkTrap` (pooled, one per attacker, rides moving platforms, cleared on arena rematch) | 🟡 | Same |
| Unlocks on level clear with a "NEW HERO UNLOCKED" banner; Character Select shows the unlock hint and also grants unlocks earned by older saves | 🟡 | Same |
| CPU fighters only use heroes you've unlocked | 🟡 | Same |
| 2 PlayMode tests (tether pulls toward Luma; coil falls, arms, zaps once) | 🟡 | Written, not run |

Fixed during review: a coil thrown while jumping up through a one-way plank landed inside it; coils on moving
platforms floated in place; coils survived into an arena rematch; locked Luma could appear as a CPU; locked-hero
lore text; a level scene built before this change silently unlocked nothing (now warns).

## 2.3 — Rex Rollo joins: momentum, wall ride, speed-scaled hits (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: momentum (coast/skid/overspeed) in `JumpPhysics`, `WallRide` rules, `CombatMath.ScaleBySpeed`, unlock rules with secrets | ✅ | 7 new tests; 113/113 pass with `dotnet run` |
| Motor wall ride + wall jump (off for every hero whose `wallRideTime` is 0) | 🟡 | Syntax-checked + independent review (behavior proven unchanged for the other heroes by reading); not compiled in Unity |
| Rex: Skate Kick → Tail Whip, Flip Kick, Wheel Spin, Momentum Ram (speed-scaled), Grind Drop, Rail Boost; unlocked by clearing Sunspire Meadows with its secret | 🟡 | Data created by `DataAssets`; not run |
| Skid and wall-ride placeholder poses | 🟡 | Same |
| 2 PlayMode tests (wall ride then wall jump; a hero without wall ride never rides) | 🟡 | Written, not run |
| Rex feel tuning | ⬜ | Needs a device |

Fixed during review: a jump pressed during a ride could be eaten by a coyote/air jump in the same step (wall jump now
runs first and blocks coyote jumps after it); the skid pose showed for every hero and over rolls/dodges; a ride could
cut a fresh jump short; riding into a ceiling stuck Rex there; a wall jump flipped a locked attack's facing; Momentum
Ram counted speed in the wrong direction and on moving platforms; chained follow-ups read the lunge speed;
projectile/trap moves now warn that `speedBonus` does nothing for them.

## 2.4 — Clockwork Warden boss (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: `WardenBrain` (phases at 60%/25%, attack patterns, slam tracking then lock-on, stagger-only vulnerability, volley spots) | ✅ | 8 new tests; 121/121 pass with `dotnet run` |
| `ClockworkWarden` (piston slam + shockwave, exposed core hurtbox, gear sweep, cog volley, reset on hero death, completes the level) and `BossHUD` | 🟡 | Syntax-checked + independent review; not compiled in Unity |
| PKR > Build Boss Test (`SQ_ClockworkWarden_Test`, joins Build All); Sunspire's NEXT button opens it | 🟡 | Not run |
| 2 PlayMode tests (core only hittable when staggered, then defeatable; hero death resets the fight) | 🟡 | Written, not run |
| Boss difficulty tuning | ⬜ | Needs play-testing |

Fixed during review: side ledges were too high for most heroes to reach (and their health crystals); the sweep gear
rolled through the wall and parked outside the arena; hazard art didn't match hitbox sizes and was see-through; the
slam warning ring was half the shockwave's width; cog warnings could stay hidden during the drop; the piston snapped
back up in later phases; the boss bar drew badly near empty; building only the boss scene left Sunspire's NEXT disabled.

## 2.5 — Story Quest level select (2026-10-06)

| Item | State | Evidence |
|---|---|---|
| Core: `LevelSelect` (unlock order, cleared levels stay open, suggested level), `SaveData.RecordRank` + best rank per level | ✅ | 5 new tests; 126/126 pass with `dotnet run` |
| `WorldDefinition` (Sunspire Isles: Sunspire Meadows → Clockwork Warden), `LevelSelectUI` (03_LevelSelect, portrait) | 🟡 | Syntax-checked + independent review; not compiled in Unity |
| Flow: Character Select → level select → level; results LEVELS button returns; clear ranks saved | 🟡 | Same |
| 1 PlayMode test (cards built in order, second level locked) | 🟡 | Written, not run |

Fixed during review: a cleared level could show as locked if an earlier one wasn't cleared; shard counts on cards are
capped at the level total.

## 2.6 — New enemies: Gyro Moth and Bolt Knight (2026-10-07)

| Item | State | Evidence |
|---|---|---|
| Core: `FlyerLogic` (hover, windup, swoop, return with a 3 s give-up), `ShieldState` (block / break / back hits, judged from the attacker's position), `CombatMath.BlockedResult` | ✅ | 7 new tests; 133/133 pass with `dotnet run` |
| Gyro Moth (flyer via motor velocity override) and Bolt Knight (`ShieldGuard` + new `IHitFilter` hook in `Damageable`; turns slowly toward the hero) | 🟡 | Syntax-checked + independent review; not compiled in Unity |
| Placed in Sunspire Meadows (two moths, one knight); new placeholder sprites | 🟡 | Not run |
| 2 PlayMode tests (moth hovers without falling; shield blocks light front hits only, breaks on heavy) | 🟡 | Written, not run |

Fixed during review: the shield judged "front" from which way the hero faced instead of where the hero stood (it now
uses the attacker's/projectile's/shockwave's position, and hits from straight above land); the knight turned instantly,
so its back was never reachable (it now takes half a second, and only once you're on the ground); a moth could fly home
into a ceiling forever; swoops rising through one-way planks aborted; blocked hits shook the camera like real hits.

## 2.7 — Star Shard shop: hero palettes (2026-10-07)

| Item | State | Evidence |
|---|---|---|
| Core: `Wardrobe` (buy, one worn palette per hero, unequip) | ✅ | 3 new tests; 136/136 pass with `dotnet run` |
| `CosmeticDefinition`/`CosmeticCatalog` (12 palettes), `ShopPanel` in the main menu, palette shown in Story, arena and Character Select (`HeroLoadout.ApplyTint`, `HitFlash.SetBaseColor`) | 🟡 | Syntax-checked + independent review; not compiled in Unity |
| 1 PlayMode test (only unlocked heroes listed; an unaffordable buy changes nothing) | 🟡 | Written, not run |

Fixed during review: the color swatch covered the start of long item names; the shop listed (and revealed) locked
heroes; the shard-changed event fired with the wrong amount, and on equips.

## 2.8 — Codex (2026-10-07)

| Item | State | Evidence |
|---|---|---|
| Core: `Codex` (unlock rules per category, defeat counts, NEW until opened, paging) + new `SaveData` fields | ✅ | 9 new tests; 145/145 pass with `dotnet run` |
| `CodexDefinition` asset, `CodexPanel` (tabs, paged list, entry pages with silhouettes for locked entries), CODEX button with NEW count, unlocks from `LevelFlowController`, "NEW IN CODEX" banner in `StoryHUD` | 🟡 | Syntax-checked + independent review; not compiled in Unity |
| 2 PlayMode tests (locked entries stay hidden, foe list pages and wraps; hero move list incl. a combo loop) | 🟡 | Written, not run |

Fixed during review: a method and a property shared the name `Tab` (would not compile); a second "NEW IN CODEX"
banner within two seconds was dropped (they now queue); long hero taglines overlapped the lore; Nova read as MEDIUM weight.

## Feature freeze: "Make it real" (2026-10-08)

| Item | State | Evidence |
|---|---|---|
| `Tools/unity_check.command` + `BatchCheck.SetupAndBuild` (batch compile, PKR setup, scene build, EditMode + PlayMode tests → `Logs/check/SUMMARY.txt`) | 🟡 | Shell syntax checked; never run |
| Plan: [VERTICAL_SLICE.md](VERTICAL_SLICE.md) (stages, slice targets, playtest kit, decision rubric); [DEVLOG.md](DEVLOG.md) entry 001 | ✅ | Docs |

Found while checking the Mac: the installed Editor is **Unity 6000.6.3f1** (the manifest targets 6.0 LTS packages, so
the first import will upgrade some packages), and the project has never been opened in Unity (no `ProjectSettings/`).

## Stage 1 result: it compiles, the tests pass, it builds for iPhone (2026-10-08)

First real run, on Omari's Mac with **Unity 6000.6.4f1** via `Tools/unity_check.command` / `unity_check_watch.command`:

| Item | State | Evidence |
|---|---|---|
| Compile (Runtime, Editor, both test assemblies) | ✅ | 0 errors after fixing `GetInstanceID` (obsolete-as-error in 6.6) and deprecated find APIs |
| PKR setup + Build All Scenes in batch mode | ✅ | All 7 scenes + data + sprites generated, after builders were changed to open the new scene before loading assets |
| EditMode tests | ✅ | 145/145 passed in Unity |
| PlayMode tests | ✅ | 39/39 passed in Unity (31 existing + 8 new scene smoke tests: every scene loads and runs 3 s without errors; the Story hero runs right) |
| URP 2D renderer | ✅ | `RenderPipelineSetup` creates/assigns `Settings/Rendering/PKR_URP2D.asset` |
| iOS build | ✅ | Unity Xcode export + `xcodebuild` with automatic signing (team X6LZQ3FS36): BUILD SUCCEEDED, `PixelKingdomRumble.app` |
| Install on iPhone | ⬜ | Both iPhones showed "unavailable" (not plugged in / asleep) |
| Played by a human (Editor or phone) | ⬜ | Nobody has looked at it yet; smoke tests only prove it runs without errors |
| Project files committed | ✅ | `ProjectSettings/`, upgraded `Packages/`, generated Data/Scenes/Art (bd6fdcd); GitHub CI green |


# Phase 3

## 3.1 — Versus mode and ten new fighters (2026-10-09)

- **Versus · 1 on 1** from the main menu: a fighter grid (P1 then CPU pick, random, CPU level), best of three rounds
  with a 60 s clock, health bars, ROUND/FIGHT/K.O. calls, double K.O. and time-up rules (`Core/Arena/VersusMatch`).
- **10 new free fighters** (Kai Tempest, Mara Vex, Volt Ramirez, Sola Brightwing, Grizz, Pixel Pip, Nyx Frost,
  Blaze Torres, Juno Strike, Ollie Kickflip): each has a 3-hit combo ending in a special finisher, up and air
  attacks, neutral/side/down specials and an air special or dive. They are also selectable in Story Quest.
- The CPU uses specials and knows the stage edges. Verified: compile 0 errors, all tests green, installed on iPhone.

## 3.2/3.3 — Stealth and jump-and-run Story levels (2026-10-09)

- Levels are drawn as **text maps** (`Core/Level/StoryLayouts.cs`, legend in `AsciiLevel.cs`) and built into scenes
  by `AsciiLevelBuilder` (**PKR > Build Map Levels**, part of Build All).
- **Jump-and-run:** Sunspire Heights and Gearfall Caverns with spring pads, climbable vines, shard crates you bump
  from below, moving platforms and lifts, spikes, secrets.
- **Stealth:** Rooftop Run and Night Market Heist. Gearwatch Sentries patrol with a visible sight cone (pale →
  amber → red), look back now and then, and chase once alerted. Hide in hay bales, hit a guard from behind before
  it spots you for a **silent takedown**, steal the Gearwright's Ledger, then escape through the gate. The results
  screen grades the run GHOST / SHADOW / AGENT / BRAWLER.
- Route: Sunspire Meadows → Sunspire Heights → Gearfall Caverns → Clockwork Warden → Rooftop Run → Night Market Heist.

| Item | State | Evidence |
|---|---|---|
| Compile | ✅ | 0 errors (Unity 6000.6.4f1, Mac) |
| EditMode tests | ✅ | 168/168 (adds stealth rules, map parsing, every map validates) |
| PlayMode tests | ✅ | 56/56 (adds guard sight/takedown/hiding, spring, vine, crate, and smoke tests for the 4 new scenes) |
| Reachability | 🟡 | A rough script (jump 3 up / 4 across, springs 9 up) finds every goal, checkpoint, shard and secret reachable; not a substitute for playing |
| Played by a human | ⬜ | Needs a playtest: jump distances, guard fairness and timing are untested by people |
