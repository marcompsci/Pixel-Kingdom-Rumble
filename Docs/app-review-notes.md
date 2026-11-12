# App Review notes (App Store and TestFlight beta review)

The text in section 1 is what's saved in App Store Connect (App Review Information > Notes, updated 16 Oct 2026). The 12 Nov text in
`Docs/AppStore/REVIEW_NOTES.md` and `beta-review-notes.txt` goes in with the next build. Keep them in sync.

## 1. Notes for the reviewer

Pixel Kingdom Rumble is an original pixel-art zombie survival game, platformer and arena brawler. No account or
sign-in is needed, and the app collects no data.

How to try each mode (all free except Shadow Contracts):
- STORY QUEST (first main-menu button) opens the KINGDOM PLAZA, a street you walk along. Stand at a door and tap
  ENTER. The four world buildings open level select (PLAZA brings you back). The five party-quest buildings (Item Race,
  Balloon Battle, Ring-Out Brawl, Coin Rush, King of the Hill) are matches against CPU rivals, not online.
- Live player counts on the plaza signs come from Game Center matchmaking activity. They are approximate and show "--"
  when not signed in. Nothing goes to the developer.
- QUICK GAME widget (iOS 17+): Home Screen > Edit > Add Widget > Pixel Kingdom Rumble. Tap the arrows to step and
  JUMP to jump. Clears add Star Shards in the game (Story Quest > STORY STATS).
- Seasonal looks (autumn harvest, winter snow) switch on by the device date. All of it is in this build; nothing is
  downloaded or turned on remotely (guideline 2.5.19).
- ZOMBIES (second main-menu button): LAST STAND > SOLO starts a round-based run (pick Hollow Block, Salt Pier or Rail
  Yard). AI SQUAD adds three computer teammates. ZOMBIE LAND is the open-city campaign. DAILY ORDERS, LOCKER and BOARDS
  are at the bottom of the ZOMBIES menu. Online co-op (ONLINE CO-OP) needs a second Game Center account and is optional.
- Arena Clash, Versus, Couch 2P: from the main menu.

In-app purchase "Shadow Contracts" (non-consumable): Main Menu > SHADOW CONTRACTS > UNLOCK ALL CONTRACTS. The first
contract is free. RESTORE PURCHASE is on the same screen.

Star Shards are earned in play only and are never sold. The zombie mystery box costs points earned during the run,
never money.

Game Center: optional leaderboards and achievements. The BOARDS page can show your rank among Game Center friends; iOS
asks for friends access first.

## 2. Reviewer navigation (fastest path to each feature)

| Feature | Path |
|---|---|
| Kingdom Plaza, Story worlds | Launch > STORY QUEST > walk to a door > ENTER |
| Party quests (vs CPU) | STORY QUEST > the party buildings on the plaza (Item Race at the first party door) |
| Story stats (incl. widget stats) | STORY QUEST > a world > STORY STATS |
| Home Screen widget | Home Screen > Edit > Add Widget > Pixel Kingdom Rumble > QUICK GAME (iOS 17+) |
| Zombie survival | Launch > ZOMBIES > LAST STAND > SOLO > (map) |
| AI teammates | ZOMBIES > LAST STAND > AI SQUAD |
| Open-world campaign | ZOMBIES > ZOMBIE LAND |
| Daily orders / cosmetics / leaderboards | ZOMBIES > DAILY ORDERS / LOCKER / BOARDS |
| Purchase + restore | Main Menu > SHADOW CONTRACTS |
| Settings, accessibility (Assist mode, aim assist, HUD size, high contrast) | Pause > SETTINGS |

## 3. Demo account

Not required: the app has no sign-in. Online co-op is optional and uses the reviewer's own Game Center account; every
other mode works offline.

## 4. Things a reviewer might ask about

- **Zombies and violence:** cartoon pixel art; age rating answered as frequent cartoon/fantasy violence and weapons
  (12+).
- **Mystery box:** a random wall weapon bought with in-run points. There is no real money and no odds-based purchase,
  so it isn't a paid loot box.
- **Multiplayer safety:** no chat and no user content. Players see each other's Game Center nicknames only, inside
  matches made by Game Center.
- **Live player counts:** Game Center `queryPlayerGroupActivity` for each place, plus a short hosted matchmaking
  request (cancelled after 6 s, never matches anyone into a game) so the place counts the player. It runs only while
  the player is in the plaza, a Story level or a party quest. It's skipped and never cancels anything while Online
  Co-op is searching or playing.
- **Widget:** WidgetKit extension with App Intents buttons. Its state stays in the app's own App Group on the device.
  No network.
- **Encryption:** only Apple system networking; `ITSAppUsesNonExemptEncryption = NO`.
