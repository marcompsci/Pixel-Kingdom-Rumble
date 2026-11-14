# Pixel Kingdom Rumble: 15 improvements across the game (13 Nov 2026)

These are for the next update; version 1.0 (build 2133) is in App Store review. Everything is original.

## Arena Clash & Versus
1. **Items in Arena Clash.** ITEMS: ON drops the party item boxes into a normal match: Dash Fizz, Bubble Shield, Seeker Star, Slick Puddle, Thunder Bell and Gust Fan. Items are luck, so those matches don't change your Rumble Rating.
2. **Choose the CPU fighters.** CPU HEROES: RANDOM, or ALL followed by a hero you've unlocked.
3. **Training lab.** CPU ACTS: STAND / WALK / JUMP / FIGHT BACK. A combo counter shows:
   - your current and best combo
   - total hits
   - Guard Pips broken
   - launches
   - a "4 HIT COMBO!" flash
4. **Edge-guarding CPUs.** When you're knocked off the stage, a CPU walks to your ledge, waits and swings as you climb back.
   - Hard CPUs do this 75% of the time and Normal 30%. Easy never does.
   - Auto difficulty blends between the levels.
   - CPUs never step off the ledge themselves.
5. **Versus ladder.** After choosing your fighter, tap LADDER to face 3–5 rivals in a row.
   - Each rival is best of 3. The first is Easy, the middle ones Normal and the last one or two Hard.
   - A loss lets you TRY AGAIN on that rung.
   - Clearing it pays 100 + 40 Star Shards per rung.
   - The save keeps your clears, your best climb and the fighters you've cleared with.

## Zombies
6. **Bargain Bell power-up.** Last Stand only. For 20 seconds every mystery box spin costs 10 points.
   - It shows a HUD timer and is synced to online co-op guests.
   - New 20×20 art (pu_bargain), drawn by a script in this repo.
7. **Field Guide.** ZOMBIES > FIELD GUIDE lists all 14 kinds of the dead with a tip for each.
   - Each kind moves from SEEN to STUDIED to MASTERED with kills. Rare elites need fewer.
   - Mastering a kind pays 50 Star Shards once.
   - Kills count in both Last Stand and Zombie Land, and the first kill of a kind shows a "new entry" message.

## Story Quest
8. **Time-trial ghost.** In a level you've cleared, a see-through copy of your best clear runs beside you. A faster clear saves a new ghost. It can be turned off in Settings > STORY TIME-TRIAL GHOST.
9. **FLAWLESS.** Clear a level without losing a heart for 20 Star Shards, once per level. It shows on the results screen.
10. **Shard compass.** Replaying a cleared level with Star Shards still missing, an arrow at the screen edge points to the nearest shard or unopened shard crate.
11. **Story game speed (accessibility).** Settings > STORY GAME SPEED: 100 / 90 / 80 / 70%. It takes effect immediately, even mid-level from Pause. Slowed clears still count, but don't set best times, medals or ghosts.

## Whole game
12. **Welcome guide.** Brand-new players see 3 short cards on the main menu: where to start, other modes, and comfort settings. It's skippable, shown once, and ends with START STORY QUEST.
13. **ALSO TODAY.** The DAILY page now also lists:
    - the Story Quest party challenge
    - the Arena Clash Daily Rumble
    - the Zombies daily orders, each marked DONE when finished
14. **Fairness and safety.** Item matches are unrated, and slowed Story clears don't count as records. New save data is cleaned on load: ladder records, ghosts (60 levels at most), Field Guide counts and FLAWLESS.
15. **Tests.** 12 engine-free tests: edge-guarding, the combo counter, the ladder, the Field Guide, the Bargain Bell table and network flag, ghosts, FLAWLESS and game speed. Also 7 PlayMode tests covering arena items, CPU hero picks, the training lab, the ladder's first rival, Story extras, the welcome guide and the Field Guide / ALSO TODAY pages.

## Checked before building (already in the game, so not redone)
- Weapon mastery
- The Last Stand end-of-run report
- The arrow pointing to the last zombies of a round
- The boss intro banner and boss music
- Best-of-3 Versus rounds

## Not verified
- Not played by a person on a phone yet.
- The tuning numbers are first guesses: edge-guard chance, ladder rewards, Field Guide kill targets and the Bargain Bell drop rate (about 10% of Last Stand power-ups).
- The Bargain Bell hasn't been tried in online co-op on two devices.
