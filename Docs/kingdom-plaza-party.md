# Pixel Kingdom Rumble: Kingdom Plaza and Party Quests (10 phases)

Story Quest now opens into **Kingdom Plaza**, a world you can walk around. It has nine buildings: the four Story worlds and five party quests. Every mode is original and inspired by kart-racing and platform-fighter party games, with no third-party names, characters or tracks.

## Phases
1. **Kingdom Plaza.** A street you can roam (SQ_KingdomPlaza) with 9 building facades. Each building has a sign above it showing its name, your progress and its live count. Stand at a door to open an ENTER card.
2. **Live counts.**
   - Counts come from Game Center player-group activity (PKRPresence.mm and LivePresence).
   - The plaza banner shows the kingdom total, and each sign shows the count for that quest.
   - Counts are approximate and refresh every 30 s. They show `--` when unknown: off iOS, when signed out, or with no data. They are never invented.
   - No new data collection: the game only does the Game Center player-group ping and query.
3. **Item boxes and 5 items.** Dash Fizz, Bubble Shield, Seeker Star, Slick Puddle and Thunder Bell. The odds favour players behind, and the leader never gets the Bell.
4. **Item Race** (SQ_ItemRace). A 220-tile course with boost belts, springs and high planks. You race 3 CPU racers with a 3-2-1-GO start, see your live place, and get a results screen when you finish.
5. **Balloon Battle.** Everyone has 3 balloons. A launch or 3 hits pops one, and the last fighter with balloons wins.
6. **Ring-Out Brawl.** Stock brawl with items. After 60 s the blast zone closes in to 72% over 30 s.
7. **Coin Rush.** A 90 s match. Coins rain down, and getting hit or falling drops some of yours.
8. **King of the Hill.** A 90 s match. Only a fighter alone in the crown zone scores, and the zone moves every 15 s.
9. **Party HUD, results and rewards.** Placement pays 40/25/15/8 Star Shards. Story Stats has a PARTY QUESTS section showing matches, wins, podiums and your best race time.
10. **Navigation.**
    - Story worlds open level select on that world's page.
    - The PLAZA button returns you to the plaza, standing at the door you left from.
    - The plaza follows the season and plays seasonal music.
    - EditMode and PlayMode tests cover all of it.

## Honest limits
- Party quests are played against CPU rivals. They are not online multiplayer.
- Live counts only show real numbers on a device signed in to Game Center. They haven't been checked yet with other real players online.
- Nothing has been uploaded to TestFlight or the App Store.
