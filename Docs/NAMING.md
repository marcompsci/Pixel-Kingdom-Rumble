# Naming registry

Every player-facing name lives in data (ScriptableObjects) or in this table's listed location, so the game can be
renamed or reskinned without code changes. Internal IDs (lowercase) never change; display names can.

| Internal ID | Display name (working) | Kind | Defined in |
|---|---|---|---|
| `nova` | Nova | Hero | `Data/Characters/Nova.asset` (increment 2); `SaveData.DefaultCharacterId` |
| `brick` | Brick | Hero (playable from Phase 2.1) | `Data/Characters/Brick.asset` |
| `luma` | Luma | Hero (playable from Phase 2.2; unlocked by clearing Sunspire Meadows) | `Data/Characters/Luma.asset` |
| `rex_rollo` | Rex Rollo | Hero (playable from Phase 2.3; clear Sunspire Meadows with its secret) | `Data/Characters/RexRollo.asset` |
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
| `brick_boulder_jab` | Boulder Jab | Brick move | `Data/Moves/Brick/` |
| `brick_quarry_hook` | Quarry Hook | Brick move | `Data/Moves/Brick/` |
| `brick_pillar_uppercut` | Pillar Uppercut | Brick move | `Data/Moves/Brick/` |
| `brick_rockfall_elbow` | Rockfall Elbow | Brick move | `Data/Moves/Brick/` |
| `brick_bulwark_charge` | Bulwark Charge | Brick move (super armor) | `Data/Moves/Brick/` |
| `brick_landslide_shockwave` | Landslide Shockwave | Brick ability hit | `Data/Moves/Brick/` |
| — | Stone Step, Granite Guard, Landslide Slam | Brick abilities | `Data/Characters/BrickKit.asset`, Brick's `movement.airJumps` |
| `luma_wrench_tap` | Wrench Tap | Luma move | `Data/Moves/Luma/` |
| `luma_voltage_swing` | Voltage Swing | Luma move | `Data/Moves/Luma/` |
| `luma_arc_flick` | Arc Flick | Luma move | `Data/Moves/Luma/` |
| `luma_static_spin` | Static Spin | Luma move | `Data/Moves/Luma/` |
| `luma_magnet_tether` | Magnet Tether | Luma move (pull projectile) | `Data/Moves/Luma/` |
| `luma_spark_coil` | Spark Coil | Luma move (trap) | `Data/Moves/Luma/` |
| — | Magnet Hop | Luma ability | `Data/Characters/LumaKit.asset` |
| `rex_skate_kick` | Skate Kick | Rex move | `Data/Moves/RexRollo/` |
| `rex_tail_whip` | Tail Whip | Rex move | `Data/Moves/RexRollo/` |
| `rex_flip_kick` | Flip Kick | Rex move | `Data/Moves/RexRollo/` |
| `rex_wheel_spin` | Wheel Spin | Rex move | `Data/Moves/RexRollo/` |
| `rex_momentum_ram` | Momentum Ram | Rex move (speed-scaled) | `Data/Moves/RexRollo/` |
| `rex_grind_drop_shockwave` | Grind Drop Shockwave | Rex ability hit | `Data/Moves/RexRollo/` |
| — | Rail Boost, Grind Drop, Wall Ride | Rex abilities | `Data/Characters/RexKit.asset`, Rex's `movement` |
| `enemy_cog_beetle` | Cog Beetle | Enemy (walker) | `Data/Enemies/CogBeetle.asset` |
| `enemy_spring_tick` | Spring Tick | Enemy (hopper) | `Data/Enemies/SpringTick.asset` |
| `secret_lift_room` | (secret room) | Secret | `StoryLevelBuilder.cs` |
| — | Tickworks | Lore: the ancient sky-machine | Character/level lore text |
| — | Star Shards | Currency | `Economy.cs` comments; UI strings |
| — | Guard Pips / Exposed | Arena mechanic | UI strings |
| — | Pixel Kingdom Rumble | Product name | `ProjectConfigurator.ProductName` |

Before release: run a trademark search on the final title and hero names.
