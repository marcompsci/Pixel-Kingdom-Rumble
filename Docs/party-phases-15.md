# Pixel Kingdom Rumble: 15 party and plaza phases (13 Nov 2026)

These build on Kingdom Plaza and the party quests and are meant for a 1.1 update. Version 1.0 is in App Store review with build 2133. Everything is original: no third-party names, characters or tracks.

## Phases
1. **CPU difficulty (Easy / Normal / Hard).**
   - Chosen on the notice board and saved.
   - In the arena quests it sets the CPU level.
   - In the Item Race it sets CPU speed. The catch-up stays: CPUs far ahead slow down, and CPUs far behind speed up.
   - Rewards: Easy pays ×0.75, Hard pays ×1.25.
2. **Notice board.** It stands by the plaza entrance and holds:
   - today's challenge
   - the CPU difficulty
   - the Kingdom Cup
   - Auto-run
3. **Daily party challenge.**
   - One challenge a day, based on the date. Examples: "Grab 12+ coins in Coin Rush", "Finish Item Race in under 1:15".
   - Pays 60 Star Shards once a day.
   - A streak counts days in a row.
   - The building it's for shows "DAILY!" on its sign.
4. **Kingdom Cup.**
   - Four quests in a row against the same three CPUs: Item Race, then Balloon Battle, then Coin Rush, then King of the Hill.
   - Cup points by place: 10 / 6 / 3 / 1.
   - Final place earns a trophy (gold, silver or bronze) and bonus shards (150 / 80 / 40 / 15, scaled by difficulty).
   - The best trophy is kept for each difficulty.
   - The results screen offers NEXT EVENT or QUIT CUP.
5. **Party ranks.**
   - Each quest has its own rank: Rookie, then Contender, Star, Champion and Legend.
   - A win is worth 3 points and any other podium finish 1.
   - Ranks show on the building signs, in Story Stats, and on the results screen when you rank up.
6. **Item Race ghost.**
   - Your best run is saved (about 4 bytes a sample).
   - It races beside you as a see-through hero with no collider.
   - Results say "NEW BEST TIME!" when you beat it.
7. **Gust Fan.** A sixth item that shoves every rival within 4.5 tiles away from you. A Bubble Shield blocks it, and the odds favour the back of the pack.
8. **Share results.** A SHARE button on every party result. It sends a short text through the iOS share sheet; the player chooses where it goes, and nothing is collected.
9. **Townsfolk.** Five original townspeople stroll the plaza. They stop, face you and give a tip in a speech bubble. They have no collider and can't be hit.
10. **Plaza map and fast travel.** A MAP button lists the notice board and all 9 buildings in their colours. Tap one to jump to its door.
11. **How-to-play cards.** The first time you play each quest (once per save), a card explains the rules.
12. **Auto-run (accessibility).**
    - A switch on the notice board.
    - In the Item Race the hero runs right on their own, so you only need to jump.
    - Pushing left still works.
13. **Performance.** Coin Rush coins are pooled, and the coin touch check reuses one buffer, so a 90-second round with dozens of coins creates no garbage.
14. **Story Stats.** The party section now shows:
    - your rank in each quest
    - cups played and the best trophy per difficulty
    - daily challenges done and your streak
15. **Tests and polish.**
    - 10 new engine-free tests: difficulty, daily challenge and streak, cup scoring and ties, ranks, the ghost codec, save sanitising.
    - New PlayMode tests: the notice board, townsfolk, map fast travel and the ghost.
    - From the simulator screenshots, the plaza cards were moved off the touch controls.

## Verified
- **Mac Unity check (`14ccc20`):** 0 compile errors.
  - EditMode 406/406.
  - PlayMode 166 passed, 0 failed, 2 skipped (balance labs).
  - Compliance gate PASS.
  - Widget tests 60/60.
- **Core tests in the cloud:** 392/392.
- **Simulator screenshots:** notice board, plaza map, Coin Rush.

## Not verified
- Not played by a person on a phone yet.
- Difficulty speeds and daily targets (for example 75 s, 12 coins, 25 s) are first guesses; tune them after play-testing.
- Party quests are still against CPUs, not online.
