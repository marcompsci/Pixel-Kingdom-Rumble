# Pixel Kingdom Rumble: 25 improvements, round 6

All content is original. No new tracking, analytics or data collection: everything is kept in the local save.

## Story Quest
1. **Tries on cleared levels**: level select cards show TRIES N for cleared levels too.
2. **Time vs your best** on the clear screen ("1.5s FASTER THAN YOUR OLD BEST!" / "0.8s OFF YOUR BEST").
3. **CLOSE CALL**: the clear screen calls out a finish on your last heart.
4. **ON A ROLL**: a toast after 3, 5, 10… clears in a row this session without a fall.
5. **Pause menu**: shows TRY N.
6. **Story Stats**: total tries and your toughest level (the one you've started most).
7. **Profile**: S-rank count and total tries.
8. **Profile**: your fastest clear.

## Versus
9. **Win streaks** (current and best) are saved.
10. **Streak toasts** at 3, 5 and every 5 after that ("5 WINS IN A ROW!").
11. **FIRST WIN VS <HERO>** toast the first time you beat a rival.
12. **Versus select**: STREAK N next to your hero's mastery.
13. **Profile**: your best Versus streak (and the current one).

## Zombies
14. **Last Stand button**: your last run ("last run: round 9 on Salt Pier").
15. **Game over**: kills per round.
16. **Profile**: the number of Last Stand runs you've played.
17. **Badges are checked at game over**.

## Badges and meta
18. **Badges pop when you earn them**: after a Story clear, a Versus match or a Last Stand run, not only on the main menu.
19. **4 new badges (22 total)**: ON FIRE (5 Versus wins in a row), LOYAL SUBJECT (30 days played), NEVER GIVE UP (100 Story tries), NIGHT SHIFT (50 Last Stand runs).
20. **THIS SESSION**: back on the main menu, a toast sums up what you've done since opening the game.
21. **Greeting** on the main menu that matches the time of day.
22. **PROFILE 9/22** on the menu button (or PROFILE · NEW when there's something new to see).
23. **Settings > ASSISTS > Menu tips & goals**: turn off the greeting, the NEXT BADGE line and the tip.
24. **Profile**: days played.
25. **What's New 1.3** card covering rounds 4-6.

## Tests
- EditMode: PlayerMeta4Tests (8 tests: Versus streak, clean streak and close call, time vs best, story records, greeting, session summary, last run and kills per round, new badges).
- PlayMode: Round6PlayModeTests (Versus streak, greeting and PROFILE count and the setting that hides them, pause TRY count, the Zombies menu's last run, the ASSISTS toggle).
