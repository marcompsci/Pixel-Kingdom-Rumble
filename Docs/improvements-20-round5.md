# Pixel Kingdom Rumble: 20 improvements, round 5

All content is original. No new tracking, analytics or data collection: everything is kept in the local save.

## Badges and goals
1. **Badge progress**: every badge tracks how far along you are.
2. **6 new badges (18 total)**: STAR PUPIL (10 S ranks), LIONHEART (10 BRAVE clears), SECRET KEEPER (10 secrets), GRAVEKEEPER (2,000 zombies), RIVALRY (beat 6 Versus heroes), KINGDOM CITIZEN (10 hours played).
3. **NEXT BADGE on the main menu**: the badge you're closest to, with progress ("TRAILBLAZER · Clear 10 Story levels (7/10)").
4. **Profile badges**: shows the badges you've earned and the three you're closest to, with progress.
5. **Haptic tap** when you earn a badge.

## Story Quest
6. **Tries per level**: every start of a level is counted.
7. **Clear screen**: "Cleared on attempt 4!" (or "first try!") on a first clear, and the attempt number after that.
8. **Level select**: uncleared levels show "3 TRIES SO FAR".
9. **World completion %** on the level select title (clears, shards and secrets).
10. **Speedrun total**: Story Stats adds up your best times across all cleared levels.
11. **Pause menu** shows your BEST time for the level.

## Versus
12. **Your record with each hero** is saved after every Versus match.
13. **Versus select**: your record with that hero appears next to its mastery.
14. **Profile**: your top Versus hero and its record.

## Zombies (Last Stand)
15. **YOUR BEST** to beat is called out at the start of round 1.
16. **NEW BEST!** callout, toast and haptic the moment you pass your best round.
17. **Game over**: how this run compares with your average on the map.

## Meta
18. **Tip of the day**: tap it to see the next tip.
19. **ABOUT** (from Profile): the version number, plus that all content is original, there are no ads or tracking, and progress is saved on the device.
20. **Welcome-back gift**: +25 Star Shards when you return after a week or more away. The What's New card is refreshed for this update (1.2).

## Tests
- EditMode: PlayerMeta3Tests (7 tests: attempts, completion and speedrun total, hero records, Last Stand pace, welcome-back gift, badge progress and next goal, tips).
- PlayMode: Round5PlayModeTests (story attempt counted, level select completion, NEXT BADGE and tip cycling, profile badge progress and ABOUT, Versus hero records).
