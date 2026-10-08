# "Make it real": vertical slice, playtest and decisions

Feature freeze is on. No new features until this page's three stages are done. The backlog
([PHASE2_BACKLOG.md](PHASE2_BACKLOG.md)) waits for the playtest results.

## Stage 1: it compiles and the tests pass ✅ (done 2026-10-08, except a human play-through)

1. Push the waiting commits (GitHub Desktop > **Push origin**).
2. Close this project in Unity if it's open, then double-click **`Tools/unity_check.command`** in Finder.
   It imports the project, compiles, builds every scene and runs both test suites without opening the
   Editor window, and writes `Logs/check/SUMMARY.txt`. First run: 10–20 minutes.
   **Hands-off option:** double-click `Tools/unity_check_watch.command` instead and leave its Terminal window open.
   It reruns the check whenever Claude drops a request file after pulling a fix, so nobody has to click.
3. Tell Claude "check done". Claude reads the logs, fixes, and sends a new commit. Repeat until the summary says
   **0 compile errors, scenes built OK, all EditMode and PlayMode tests passed**.
4. Then open the project in the Editor (Unity Hub > Add > `~/GameDev/Pixel-Kingdom-Rumble`) and work through
   [FIRST_RUN.md](FIRST_RUN.md) section D in the Device Simulator.

Note: the batch check creates default `ProjectSettings/`. Before the iPhone build, set the URP 2D renderer as in
[SETUP.md](SETUP.md) and commit `ProjectSettings/`.

**Put it on your iPhone:** plug the iPhone into the Mac, unlock it (Developer Mode on), then double-click
`Tools/ios_build.command` (or, with the watcher running, Claude creates `Logs/ios_request`). It builds, signs,
installs and launches the game.

**Done when:** 0 errors, green tests, and you can play Boot → Sunspire Meadows → Clockwork Warden → results
in the Editor without a crash.

## Stage 2: the vertical slice (next week)

### Scope (only this gets polish)

Boot → main menu → Character Select (**Nova and Brick**) → level select → **Sunspire Meadows** →
**Clockwork Warden** → results. Luma, Rex, Arena Clash, Shop and Codex stay in the build but get no polish
time. If they break the slice, hide their buttons.

### Done when (each item checked on a real iPhone, not the simulator)

| Area | Target |
|---|---|
| Performance | 60 FPS in Sunspire with all enemies on screen (Xcode FPS gauge); no visible hitches |
| Controls | Stick and buttons sit where thumbs rest; buttons ≥ 88 pt; you can hold the stick and press two buttons at once |
| Movement | Run → jump → land feels snappy; coyote jump and jump buffer noticeable; no "I pressed jump and nothing happened" |
| Combat | Every hit has hit stop, flash and knockback; you can tell when an enemy is hurt vs blocking |
| First 30 seconds | A new player moves, jumps and attacks without being told how (on-screen hints in section A) |
| Death | Back in control within 2 seconds of dying; checkpoints never feel unfair |
| Boss | Every Warden attack is readable (telegraph before damage); a first-time player can win in ≤ 5 tries |
| Length | Slice takes 6–10 minutes for a new player |
| Stability | 10 full runs in a row without a crash or soft-lock |

### Daily rhythm

Play the slice for 15 minutes on the phone → write the 3 worst things → send them to Claude → Claude fixes and
sends a build → repeat. Tune numbers in `Assets/_Project/Data` first; change code only when tuning can't fix it.

## Stage 3: five-person playtest

**Who:** 5 people who have never seen the game. Mix: 2 who play mobile games, 2 who rarely play games,
1 who plays console/PC action games.

**Setup:** fresh install (delete the app and reinstall so progress starts empty), sound on, landscape, no explanations.
Say only: "Play until you'd normally stop. Think out loud if you can."

**Watch and write down (don't help unless they're stuck for 60 s):**

| Measure | Record |
|---|---|
| Time until they figure out jump + attack | seconds |
| Deaths in Sunspire / Warden tries | counts |
| Where they got stuck or confused | place + what they said |
| Did they reach the Warden? Beat it? | yes / no |
| When they stopped, and why | minute + reason |
| Did they ask to play again or ask about the game? | yes / no |

**After they play, ask (in this order):**

1. What was the most fun moment?
2. What was the most annoying moment?
3. If this were free on the App Store, would you keep it on your phone? Why / why not?
4. Would you rather play it on a phone, a computer, or a console with a controller?
5. If you could fight your friends in this, would you? (shows the arena idea, which they haven't played)

## The three decisions (made with the playtest notes)

| Decision | Choose A if… | Choose B if… |
|---|---|---|
| **Core: platformer or brawler?** | Platformer if ≥ 3/5 reached the Warden and named a Story moment as the most fun | Brawler if testers light up at Q5 and Story felt like a chore; then the next slice is Arena with a friend |
| **iPhone only, or web/PC demo too?** | iPhone only if controls scored well (few control complaints, ≥ 3/5 would keep it) | Add a web/PC demo if ≥ 2/5 struggled with touch or ≥ 3/5 prefer a controller (Unity builds both from this project) |
| **Which retro game gets main focus?** | Pixel Kingdom Rumble if ≥ 3/5 would keep it or asked to play again | Otherwise, run the same five-person test on Retro Sk8 / Retro Hoops and back the strongest |

Write the results and decisions in [STATUS.md](STATUS.md) under a "Playtest 1" heading. Only then reopen the
backlog, in the order the playtest points to (likely: whatever testers complained about, then real art and audio
for the slice, then Game Center).
