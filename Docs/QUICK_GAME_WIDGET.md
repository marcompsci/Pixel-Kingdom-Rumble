# Quick Game widget + Story Quest improvements (1 Nov 2026)

Requested by Omari: an interactive widget that plays a small Story Quest stage without opening the app, with left/right
movement, jump and double jump, and its stats tracked in the app under Story Quest.

Apple's widgets only take plain button taps (no touch-and-hold, double-tap gestures or real-time play), and each tap
redraws the widget. Omari picked the **step platformer**: every tap is one step of game time.

## Controls
- Tap the **left half** to step left and the **right half** to step right.
- Tap **JUMP** to jump up and forward. Tap it again in the air for a **double jump** (the button reads DOUBLE).
- Each step the hero falls one tile if nothing is underneath. Foes walk every other step. Landing on a foe stomps it.
- A pit or spikes costs a heart and sends the hero back to the last flag. Three hearts per run.
- Reaching the gate clears the stage, and NEXT STAGE moves on. Out of hearts shows TRY AGAIN.

## The ten phases
1. **Quick Game engine** (Native/QuickGameWidget/QuickGameEngine.swift):
   - Pure Swift with 8 original stages: Meadow Hop, Acorn Steps, Beetle Bridge, Spike Meadow, High Road, Double Gap, Tower Climb, Last Light.
   - The day picks the starting stage.
   - A solver proves every stage can be cleared (32–34 taps) and every shard can be reached.
2. **Widget look**:
   - Small, Medium and Large Home Screen widgets drawn with the game's own art: your selected hero, a foe, shards, flags, the gate and the tiles.
   - Tiles follow the live seasons (harvest tiles in autumn, snow tiles in winter).
   - A Lock Screen widget shows today's stage, shards, clears and streak.
3. **Tap controls**: App Intents for left, right, jump/double jump, TRY AGAIN and NEXT STAGE. They run in the background, so the app never opens.
4. **In the iPhone build** (IOSPostBuild):
   - A WidgetKit extension target (iOS 17+; the app still supports iOS 15).
   - A shared App Group (group.com.marcompsci.pixelkingdomrumble), privacy manifests for UserDefaults (reason 1C8F.1), and versions matching the app.
   - The compliance gate checks all of this.
5. **Game sync** (QuickGameSync):
   - Whenever the game opens, it reads the widget's stats and pays 20 Star Shards per new widget clear (up to 200 per visit).
   - It tells the widget which hero to draw and whether live seasons are on. Nothing leaves the phone.
6. **Story Stats page** (Level Select → STORY STATS):
   - Story Quest: levels cleared, shards, secrets, time medals, foes defeated and season tokens.
   - Quick Game: clears, runs, shards, stomps, days in a row, taps and fewest taps per stage, plus how to add the widget.
7. **Time medals**: GOLD at par or faster, SILVER within 25% over par, BRONZE for any clear. Shown on the results screen, the level card and the stats page.
8. **Still to find** on the results screen: missing Star Shards, secrets, the season token and the next medal's target time.
9. **Pause screen progress** in Story levels: shards, secrets, time against par and the season token so far.
10. **Medal badges** on each cleared level's map node.

## Verification
- Cloud core tests: 373/373.
- Mac Unity check at f1e6a75:
  - Compile: 0 errors. EditMode 387/387. PlayMode 155 passed, 0 failed, 2 skipped.
  - Compliance gate: PASS.
  - Widget Swift type-check against the iOS 17 SDK: OK. Engine tests: 60/60.
- iPhone build: signed and installed on Omari's iPhone.
  - QuickGameWidget.appex is embedded, with its asset catalog and App Intents metadata.
  - Both provisioning profiles include the App Group; Xcode registered com.marcompsci.pixelkingdomrumble.quickgame automatically.
  - Launch log: no exceptions.
- **Not yet seen on a Home Screen.** The widget's look and taps on a real phone still need checking: add it and play a stage. No TestFlight upload.

## Files
- Native/QuickGameWidget/: Swift sources, Info.plist, entitlements, privacy manifest, Assets.xcassets (crops of the game's art, 4x nearest-neighbour).
- Tools/QuickGameTests/main.swift and Tools/widget_check.sh (Logs/widget_request runs it alone).
- Assets/Plugins/iOS/PKRWidget.mm, Runtime/QuickGame/, Core/QuickGame/ (rewards, medals, checklist, TinyJson).
