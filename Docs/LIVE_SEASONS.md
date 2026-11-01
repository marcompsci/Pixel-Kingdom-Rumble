# Live seasons in Story Quest (November 2026)

Requested by Omari on 1 Nov 2026: Story Quest follows the real-world season by itself. It's Harvest Season with
Thanksgiving now; Winterfest snow and Christmas come in December. The existing Holiday Mode levels are unchanged.

## The calendar (device date, Core/Season/SeasonCalendar.cs)

| Season | Dates | Peak (everything turned up) | Look |
| --- | --- | --- | --- |
| Autumn Leaves | 22 Sep – 31 Oct | Halloween Week, 25–31 Oct (jack-o'-lanterns, dusk sky) | Autumn |
| Harvest Season | 1–30 Nov | Thanksgiving Week, Mon–Sun around the 4th Thursday (23–29 Nov 2026) | Autumn |
| Winterfest | 1 Dec – 1 Jan | Christmas 20–26 Dec; New Year 31 Dec – 1 Jan (fireworks) | Winter |
| Frost Season | 2 Jan – end Feb | none | Winter |
| (spring, summer) | 1 Mar – 21 Sep | none | normal world look |

Settings > Display > **Live seasons: ON/OFF** (on by default).

## The ten phases

1. **Season calendar**: real dates, peaks, Thanksgiving maths, save keys per year ("harvest_2026"), countdowns, the Settings switch.
2. **Sky and light**: sky gradient, background layers and the camera colour are warmed for autumn and cooled for winter. Peak evenings (Halloween, Christmas, New Year) turn to dusk.
3. **Weather**: falling leaves or snow around the camera, in two depths. The count follows the graphics tier and the season's intensity (heaviest at Christmas). Reduced motion makes it sparse, slow and straight.
4. **Ledge dressing**: a snow layer on every open ledge in winter, fallen leaves in autumn. Pumpkins, hay bales, corn bundles, snowmen and gifts sit on open floor, never near the start, checkpoints or goal. Decoration only: nothing to stand on.
5. **Holiday decorations**:
   - Checkpoints: pumpkins (lanterns in Halloween Week), hay and corn, a feast table in Thanksgiving Week, a small lit pine in Winterfest, a snowman in Frost.
   - Goal: a feast table in Harvest; a big pine with twinkling lights, a star, gifts (more at Christmas) and a wreath in Winterfest.
6. **Season token**: one hidden per Story level. Maple Leaf, Golden Acorn, Gift or Snowflake, placed in the back half of the level above a Star Shard spot. Already found this season? It shows as a faint outline. The level select shows a badge per node and FOUND / STILL HIDDEN on the card.
7. **Foe outfits**: leaf crowns in autumn, holly beanies in Winterfest, earmuffs in Frost. Hats flip with the foe and hide again in other modes.
8. **Seasonal audio** (synthesized, original):
   - season_chime: a token found in autumn.
   - season_bells: a token found in winter.
   - music_harvest (D major stroll) and music_winterfest (G major, music-box lead with bells) on the menus and world map while a season is live.
9. **Live-season menu and world map**:
   - Main menu banner, e.g. "HARVEST SEASON · LIVE · Golden Acorns 0/22 · Thanksgiving Week in 22 days" or "N rewards to claim".
   - STORY QUEST is tinted for the season.
   - Leaves or snow drift over the menus and the world map.
10. **Season challenges and keepsakes**:
    - Season panel (tap the banner) with four limited challenges: find 3 tokens, clear 5 Story levels, collect 150 Star Shards in Story, defeat 40 foes. They pay 150–200 Star Shards each, once.
    - Find 8 tokens for the season's keepsake (Leaf Crown, Harvest Scarf, Holly Beanie, Frost Earmuffs). The hero wears it in Story Quest (WEAR / WEARING in the panel), and keepsakes are kept forever.

All art is drawn in code at the game's 32 px per tile with the house ink outline (Runtime/Season/SeasonArt.cs). No new data collection: everything stays in the local save.

## Tests
- Core (cloud): 10 SeasonTests covering every season boundary, peaks, Thanksgiving for several years, keys, countdowns, tokens and keepsakes, challenges paying once, sanitising, and the weather maths.
- PlayMode (Mac): SeasonPlayModeTests:
  - Christmas in Sunspire Heights: snow, a gift token, a lit tree.
  - Thanksgiving in Gearfall Caverns: leaves, a feast table, foe hats.
  - A spring day and seasons-off: no dressing.
  - Menus and the world map show the season.
- Every Story level in the smoke tests now runs dressed for Harvest, because the test date is November.

## Verification (1 Nov 2026)
- Cloud core tests: 367/367.
- Mac Unity check at 71e469d: compile 0 errors; EditMode 381/381; PlayMode 153 passed, 0 failed, 2 skipped (as before); Apple compliance gate PASS.
- Simulator screenshots (iPhone 17 Pro Max), checked by eye:
  - Today's date: Harvest menu banner, season panel, map badges, a level with leaves, pumpkins, corn and a warm sky.
  - 26 Nov: Gearfall Caverns with leaf crowns on the foes.
  - 24 Dec: Sunspire Heights and Frostgear Ascent with a dusk sky, snow on the ledges, snowfall, a gift and holly beanies.
  - Fixed after the first look: the menu overflowing, the crushed keepsake row, the smudgy map badges, a greyish autumn sky and the scarf art.
- GitHub main at 71e469d. Installed on Omari's iPhone: launch log clean, menu 60 fps.
- Not done: nobody has played a full level by hand in-season. Token spots and the feel of the snow and leaves on a real phone still need a human look. No TestFlight upload.
- Screenshot runs can preview any date with SCENE#yyyymmdd (e.g. SQ_SunspireHeights#20261224).
