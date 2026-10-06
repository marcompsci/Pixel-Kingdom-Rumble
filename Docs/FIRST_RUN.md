# First-run checklist (Unity + iPhone)

None of the Unity-side code has been compiled or run yet (see [STATUS.md](STATUS.md)). This checklist is the
fastest way to find out what works. Tick items as you go and note anything that fails; paste errors back to
Claude with the file and line.

## A. Open the project (about 15 minutes)

- [ ] Create the project as in [SETUP.md](SETUP.md) (Universal 2D template + this repo).
- [ ] Unity opens with **0 compile errors** in the Console. *If not, this is the first thing to fix:
      copy the first red error (file + line).*
- [ ] Accept the Input System restart prompt if shown; Active Input Handling = Input System Package (New).
- [ ] **PKR > Setup Layers**: layers 8–13 are Ground, Player, Enemy, Hurtbox, Pickup, Hazard.
- [ ] **PKR > Configure iOS Player Settings** runs without errors.

## B. Tests

- [ ] Test Runner > **EditMode**: 113 tests pass (they already pass under .NET; a failure here means a Unity
      difference worth reporting).
- [ ] Test Runner > **PlayMode**: 23 tests. These are the first real check of the physics code.
      Most likely to need tuning: `MotorPlayModeTests` (jump heights) and `ArenaPlayModeTests` (timing).

## C. Generate content

- [ ] **PKR > Build All Scenes**. Expect log lines for the sandbox, story level, menus and arena.
- [ ] `Assets/_Project/Data` contains the roster, all four heroes with their kits and moves, two enemies and the level.
- [ ] `Assets/_Project/Art/Placeholder` contains the `ph_*.png` sprites and they look like pixel art
      (Point filter, no blur).
- [ ] File > Build Profiles: `00_Boot` is first in the scene list.

## D. Play in the Editor (Device Simulator, iPhone)

Window > General > Device Simulator, pick an iPhone, open `00_Boot`, press Play.

- [ ] Title → main menu in **portrait**; Star Shard total shows.
- [ ] Story Quest → character select: Nova and Brick selectable; Luma shows "LOCKED: CLEAR SUNSPIRE MEADOWS"; Rex Rollo shows his secret hint.
- [ ] Pick **Brick**: Sunspire Meadows loads with Brick (grey stone sprite, 6 HP). Try Boulder Jab → Quarry Hook,
      Pillar Uppercut, Rockfall Elbow, Bulwark Charge (walk into an enemy mid-charge: he keeps going),
      Stone Step (jump again in the air), Granite Guard (air dodge), Landslide Slam (air special).
- [ ] Sunspire Meadows loads in **landscape**; joystick + 4 buttons appear; touch with the mouse works.
- [ ] Movement: run, short hop vs held jump, coyote jump just off a ledge, jump pressed just before landing.
- [ ] Nova: jab → sweep, Up+Attack, air Tailspin, Comet Bolt, Meteor Drop, roll, 8-way air dash.
- [ ] Enemies patrol/hop, take hits, drop shards. Spikes and pits hurt. Checkpoints save position.
- [ ] Moving platform carries Nova. Secret room behind the fake wall in section D.
- [ ] Goal gate → level-complete screen with rank and reward; Play Again / Main Menu work.
- [ ] First clear shows "NEW HERO UNLOCKED: LUMA!". Pick Luma: Magnet Tether pulls enemies in; Spark Coil
      (air special) blinks, arms and zaps a Cog Beetle; Magnet Hop (air dodge) pops her up.
- [ ] Clear again with the secret room found: "NEW HERO UNLOCKED: REX ROLLO!". Pick Rex: he glides and skids;
      skate into the lift-shaft wall (section D) and jump into it holding toward it: he rides up, Jump kicks off;
      Momentum Ram from a standstill vs at full speed (the full-speed one sends enemies much farther).
- [ ] Nova, Brick and Luma still stop crisply and never wall ride (their movement must be unchanged).
- [ ] Pause (II button and Esc): Resume, Restart, Settings, Main Menu. Edit the control layout and see it saved.
- [ ] Settings: high contrast changes UI and controls; screen shake off stops shake.
- [ ] Arena Clash: setup screen, Stock with 3 CPUs on Normal. CPUs move, attack, recover, avoid the gap.
- [ ] CPUs are a mix of your unlocked heroes (Nova, Brick, then Luma and Rex once unlocked); Brick CPUs recover with Stone Step and show 4 Guard Pips.
- [ ] Bridge flashes then retracts; KOs credit the last attacker; respawn blinks; results screen + Rematch.
- [ ] Timed and Training modes start and end correctly.
- [ ] Quit and replay: Star Shards and best times persisted.

## E. iPhone

Follow the iOS section of [SETUP.md](SETUP.md).

- [ ] Xcode build succeeds (watch for `PKRHaptics.mm` errors; it needs UIKit, which Unity links by default).
- [ ] Launches in portrait, rotates to landscape for gameplay and back for menus.
- [ ] UI stays inside the notch / Dynamic Island / home-bar safe area in both orientations.
- [ ] Multi-touch: hold the stick and press buttons at the same time.
- [ ] Haptics on hits and button taps (Settings > Haptics on).
- [ ] Backgrounding the app pauses the game.
- [ ] **Performance**: non-development build; Xcode Debug navigator FPS gauge in the arena with 3 CPUs.
      Record the device and the number in STATUS.md. Target 60 FPS.

## When something fails

Note: scene, steps, expected vs actual, and the first Console error. Most issues will be a number to tune
in a data asset (`Assets/_Project/Data`) rather than code; builders never overwrite tuned data assets.
