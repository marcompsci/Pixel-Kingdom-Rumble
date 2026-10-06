# Naming registry

Every player-facing name lives in data (ScriptableObjects) or in this table's listed location, so the game can be
renamed or reskinned without code changes. Internal IDs (lowercase) never change; display names can.

| Internal ID | Display name (working) | Kind | Defined in |
|---|---|---|---|
| `nova` | Nova | Hero | `Data/Characters/Nova.asset` (increment 2); `SaveData.DefaultCharacterId` |
| `brick` | Brick | Hero (locked in Phase 1) | `Data/Characters/Brick.asset` |
| `luma` | Luma | Hero (locked in Phase 1) | `Data/Characters/Luma.asset` |
| `rex_rollo` | Rex Rollo | Hero (locked in Phase 1) | `Data/Characters/RexRollo.asset` |
| `sunspire_meadows` | Sunspire Meadows | Biome | `Data/Levels/` |
| `sq_sunspire_test` | Sunspire Meadows (Test) | Story level | `Data/Levels/` |
| `skyforge_arena` | Skyforge Arena | Arena stage | `Data/Arena/` |
| `clockwork_warden` | The Clockwork Warden | Boss (Phase 2) | — |
| `nova_courier_jab` | Courier Jab | Nova move | `Data/Moves/Nova/` |
| `nova_comet_sweep` | Comet Sweep | Nova move | `Data/Moves/Nova/` |
| `nova_rising_arc` | Rising Arc | Nova move | `Data/Moves/Nova/` |
| `nova_tailspin` | Tailspin | Nova move | `Data/Moves/Nova/` |
| `nova_comet_bolt` | Comet Bolt | Nova move | `Data/Moves/Nova/` |
| `nova_meteor_shockwave` | Meteor Shockwave | Nova ability hit | `Data/Moves/Nova/` |
| `enemy_cog_beetle` | Cog Beetle | Enemy (walker) | `Data/Enemies/CogBeetle.asset` |
| `enemy_spring_tick` | Spring Tick | Enemy (hopper) | `Data/Enemies/SpringTick.asset` |
| `secret_lift_room` | (secret room) | Secret | `StoryLevelBuilder.cs` |
| — | Tickworks | Lore: the ancient sky-machine | Character/level lore text |
| — | Star Shards | Currency | `Economy.cs` comments; UI strings |
| — | Guard Pips / Exposed | Arena mechanic | UI strings |
| — | Pixel Kingdom Rumble | Product name | `ProjectConfigurator.ProductName` |

Before release: run a trademark search on the final title and hero names.
