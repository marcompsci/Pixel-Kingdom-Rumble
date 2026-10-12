# Rumble Rating, Daily Rumble and Couch Play

## Why these features
Research (October 2026) on App Store brawlers and community complaints:

| Complaint about other brawlers | What Pixel Kingdom Rumble does |
|---|---|
| Ranked scoring feels unfair; a 2nd place can out-score 1st | Placement points with a capped style bonus, so a better placement **always** earns more. Every point is shown on the results screen. |
| Losing streaks and unfair matchmaking | **Auto** CPU level adapts to your results. Bronze never loses points, promotions are shielded for 3 matches, and losses are halved after 2 bad matches in a row. |
| Touch players are at a disadvantage; controllers recommended | Controller support plus movable/resizable buttons (already in) and couch play on one device. |
| Pay-to-win / energy timers / ads | None. The Daily Rumble is free and unlimited to practise. |
| Same match every time | Daily Rumble twists, also selectable as a TWIST in custom matches. |
| No local multiplayer on mobile | Couch 2P on one iPhone or iPad. |

## Rumble Points (per match, solo vs CPU)
- **Placement:** 2 fighters 120/0; 3 fighters 130/70/0; 4 fighters 130/80/40/0.
- **Style:**
  - +8 per KO, -4 per fall, and an extra -4 for a self-destruct.
  - +5 First Blood, +4 per Revenge KO, +10 Untouchable (no falls).
  - +8 Comeback (won after being last), +6 Clutch (won on your last stock).
  - Style is clamped to 0..35, and 35 is smaller than every placement gap.
- **CPU multiplier:** 0.70x at the easiest level up to 1.35x at the hardest (Normal = 1.025x).
- **Daily Rumble:** +20 on the first attempt each day, +5 per streak day (up to +40).

## Rating and tiers
- **Tier thresholds:** Bronze 0, Silver 250, Gold 600, Platinum 1100, Diamond 1700, Legend 2400.
- **Rating change:** points earned minus the tier's entry cost (40/55/70/80/90/100).
- **Protection:** Bronze can't lose points. A promotion shields you for 3 matches. Losses are halved after 2 bottom-half finishes in a row.
- **Leaderboard:** Game Center `pkr.lb.rumble` (best score, high to low). This leaderboard must exist in App Store Connect.
- **Counted matches:** Arena Clash vs CPU, Versus, and the Daily Rumble. Training and couch matches are unrated.

## Auto CPU level
The skill estimate is a 0..1 value that starts at 0.35.
- It moves by 0.07 x the placement score after each rated match (+1 for 1st, -1 for last).
- CPU tuning blends Easy → Normal → Hard along that value.
- The setup screens show it as `AUTO (NORMAL)`.

## Daily Rumble
- The same twist for everyone on a calendar day, chosen deterministically from the date.
- The 7 twists combine Low Gravity, Heavy Hitters (+35% knockback), Giants (+30% size) and One Stock.
- One rated attempt per day. Replays are practice.

## Couch play (2P)
- **Two controllers:** one each.
- **One controller:** P1 plays on touch, P2 on the controller.
- **No controllers:** split-screen touch, with P1 on the left half and P2 on the right (controls shrink to 80%).
- P2 picks a fighter on the setup screen. Add 0–2 CPUs.

## Code
- Core (pure, tested):
  - `Core/Arena/RumbleRating.cs`: RumblePoints, RumbleTiers, RumbleRating, AdaptiveDifficulty, RumbleTracker, DailyRumble.
  - `ArenaMatchConfig`: humans, p2CharacterId, autoLevel, mutators, daily.
  - `SaveData.rumble`.
- Runtime:
  - `RumbleRecorder` / `RumbleUI`.
  - `ArenaMods` (gravity, knockback, size).
  - `CouchInput`.
  - `PlayerInputRouter.Bind` (per-player devices).
  - `TouchControlsUI.SetRegion` (split touch).
  - Setup and results screens.
- Tests: `Tests/EditMode/RumbleRatingTests.cs` and the new Arena PlayMode tests.
