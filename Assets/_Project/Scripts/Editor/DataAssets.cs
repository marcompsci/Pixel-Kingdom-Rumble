using PKR.Core;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Creates default data assets if missing. Never overwrites tuning you've changed in the Inspector;
    /// only fills empty art references.
    /// </summary>
    public static partial class DataAssets
    {
        public const string CharactersFolder = "Assets/_Project/Data/Characters";

        [MenuItem("PKR/Create Default Data Assets", priority = 3)]
        public static void CreateAll()
        {
            GetOrCreateRoster();
            GetOrCreateCogBeetle();
            GetOrCreateSpringTick();
            GetOrCreateGyroMoth();
            GetOrCreateBoltKnight();
            GetOrCreateSunspireTestLevel();
            GetOrCreateWardenLevel();
            GetOrCreateGearwatchSentry();
            GetOrCreateGearwatchCaptain();
            GetOrCreateFrostMite();
            GetOrCreateGearwatchElite();
            GetOrCreateCinderCrawler(); GetOrCreatePuffGlider(); GetOrCreateRustCrab(); GetOrCreateWindupSoldier();
            GetOrCreateStoryWorld();
            GetOrCreateShatteredRealms();
            GetOrCreateCodex();
            AssetDatabase.SaveAssets();
        }

        /// <summary>A hero that is listed but not playable yet (no move kit). Lore and art are final-ish placeholders.</summary>
        static CharacterDefinition GetOrCreateLockedHero(string file, string id, string name, string tagline, string lore,
                                                         Color32 color, float weight, float armor, System.Func<Sprite> sprite)
        {
            EditorUtil.EnsureFolder(CharactersFolder);
            var def = GetOrCreate<CharacterDefinition>($"{CharactersFolder}/{file}.asset", d =>
            {
                d.id = id; d.displayName = name; d.tagline = tagline; d.lore = lore;
                d.unlockedByDefault = false; d.playableInThisBuild = false;
                d.placeholderColor = color; d.weight = weight; d.armorPercent = armor;
            });
            if (def.bodySprite == null) { def.bodySprite = sprite(); EditorUtility.SetDirty(def); }
            return def;
        }

        public static CharacterRoster GetOrCreateRoster()
        {
            var nova = GetOrCreateNova();
            var brick = GetOrCreateBrick();
            var luma = GetOrCreateLuma();
            var rex = GetOrCreateRex();

            var roster = GetOrCreate<CharacterRoster>($"{CharactersFolder}/Roster.asset", r => { });
            if (roster.heroes.Count == 0)
            {
                roster.heroes.Add(nova); roster.heroes.Add(brick); roster.heroes.Add(luma); roster.heroes.Add(rex);
                EditorUtility.SetDirty(roster);
            }
            if (roster.cosmetics == null) { roster.cosmetics = GetOrCreateCosmetics(); EditorUtility.SetDirty(roster); }
            // Phase 3.1: the ten free Versus fighters join the roster after the original four.
            foreach (var f in VersusFighters.GetOrCreateAll())
                if (!roster.heroes.Contains(f)) { roster.heroes.Add(f); EditorUtility.SetDirty(roster); }
            // Generated pixel art: animation frames, signature weapons and projectile art for every hero.
            foreach (var h in roster.heroes) PixelArt.ApplyFighter(h);
            return roster;
        }

        public const string CosmeticsFolder = "Assets/_Project/Data/Cosmetics";

        /// <summary>Star Shard shop: three original palettes per hero. Names and prices are starting values.</summary>
        public static CosmeticCatalog GetOrCreateCosmetics()
        {
            EditorUtil.EnsureFolder(CosmeticsFolder);
            var catalog = GetOrCreate<CosmeticCatalog>($"{CosmeticsFolder}/Cosmetics.asset", c => { });
            void Add(string id, string name, string hero, int cost, Color32 tint)
            {
                var item = GetOrCreate<CosmeticDefinition>($"{CosmeticsFolder}/{id}.asset", d =>
                {
                    d.id = id; d.displayName = name; d.heroId = hero; d.cost = cost; d.tint = tint;
                });
                if (!catalog.items.Contains(item)) { catalog.items.Add(item); EditorUtility.SetDirty(catalog); }
            }
            Add("pal_nova_dawn", "Dawn Courier", "nova", 60, new Color32(255, 214, 150, 255));
            Add("pal_nova_nebula", "Nebula Night", "nova", 120, new Color32(190, 150, 255, 255));
            Add("pal_nova_glacier", "Glacier Post", "nova", 200, new Color32(170, 230, 255, 255));
            Add("pal_brick_moss", "Moss Guardian", "brick", 60, new Color32(170, 230, 150, 255));
            Add("pal_brick_basalt", "Basalt", "brick", 120, new Color32(130, 130, 150, 255));
            Add("pal_brick_sandstone", "Sandstone", "brick", 200, new Color32(255, 220, 160, 255));
            Add("pal_luma_copper", "Copper Coil", "luma", 60, new Color32(255, 180, 130, 255));
            Add("pal_luma_mint", "Mint Circuit", "luma", 120, new Color32(170, 255, 210, 255));
            Add("pal_luma_midnight", "Midnight Lab", "luma", 200, new Color32(150, 160, 230, 255));
            Add("pal_rex_lava", "Lava Skate", "rex_rollo", 60, new Color32(255, 160, 120, 255));
            Add("pal_rex_arctic", "Arctic Roller", "rex_rollo", 120, new Color32(220, 240, 255, 255));
            Add("pal_rex_neon", "Neon Rink", "rex_rollo", 200, new Color32(255, 150, 240, 255));
            // October 2026 cast
            Add("pal_juni_autumn", "Autumn Leaves", "juni_bloom", 60, new Color32(255, 190, 130, 255));
            Add("pal_juni_lilac", "Lilac Bloom", "juni_bloom", 120, new Color32(220, 180, 255, 255));
            Add("pal_juni_frost", "Frost Fern", "juni_bloom", 200, new Color32(180, 230, 255, 255));
            Add("pal_marco_sepia", "Sepia Sketch", "marco_brio", 60, new Color32(240, 210, 170, 255));
            Add("pal_marco_neon", "Neon Graffiti", "marco_brio", 120, new Color32(170, 255, 200, 255));
            Add("pal_marco_noir", "Charcoal", "marco_brio", 200, new Color32(170, 170, 190, 255));
            Add("pal_tamsin_mint", "Mint Margins", "tamsin_reed", 60, new Color32(190, 255, 220, 255));
            Add("pal_tamsin_midnight", "Midnight Library", "tamsin_reed", 120, new Color32(160, 160, 240, 255));
            Add("pal_tamsin_gilded", "Gilded Edition", "tamsin_reed", 200, new Color32(255, 230, 150, 255));
            Add("pal_dex_ice", "Ice Tour", "dex_ortega", 60, new Color32(180, 230, 255, 255));
            Add("pal_dex_lava", "Lava Amp", "dex_ortega", 120, new Color32(255, 170, 130, 255));
            Add("pal_dex_gold", "Gold Record", "dex_ortega", 200, new Color32(255, 225, 140, 255));
            Add("pal_pepper_basil", "Basil", "pepper_quinn", 60, new Color32(190, 240, 170, 255));
            Add("pal_pepper_berry", "Berry Jam", "pepper_quinn", 120, new Color32(240, 170, 220, 255));
            Add("pal_pepper_smoke", "Smokehouse", "pepper_quinn", 200, new Color32(200, 190, 180, 255));
            Add("pal_bo_storm", "Storm Watch", "bo_tidewell", 60, new Color32(180, 200, 230, 255));
            Add("pal_bo_coral", "Coral Reef", "bo_tidewell", 120, new Color32(255, 190, 170, 255));
            Add("pal_bo_kelp", "Kelp Forest", "bo_tidewell", 200, new Color32(180, 240, 190, 255));
            Add("pal_zuri_sunrise", "Sunrise", "zuri_starling", 60, new Color32(255, 210, 170, 255));
            Add("pal_zuri_nebula", "Nebula", "zuri_starling", 120, new Color32(230, 170, 255, 255));
            Add("pal_zuri_aurora", "Aurora", "zuri_starling", 200, new Color32(170, 255, 230, 255));
            Add("pal_otto_copper", "Copper Works", "otto_brass", 60, new Color32(255, 190, 150, 255));
            Add("pal_otto_silver", "Silver Hands", "otto_brass", 120, new Color32(210, 220, 240, 255));
            Add("pal_otto_emerald", "Emerald Escapement", "otto_brass", 200, new Color32(170, 240, 190, 255));
            return catalog;
        }

        public const string EnemiesFolder = "Assets/_Project/Data/Enemies";
        public const string LevelsFolder = "Assets/_Project/Data/Levels";

        public static EnemyDefinition GetOrCreateCogBeetle()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/CogBeetle.asset", d =>
            {
                d.id = "enemy_cog_beetle"; d.displayName = "Cog Beetle";
                d.codexEntry = "Maintenance drones of the Tickworks. Their shells still turn with the great gears, " +
                               "and they patrol the same path they were wound for centuries ago.";
                d.bodySize = new Vector2(0.9f, 0.7f); d.maxHealth = 2; d.weight = 1f; d.moveSpeed = 0.28f;
                d.behavior = EnemyBehavior.Walker; d.shardDrop = 2;
            });
            if (e.sprite == null) { e.sprite = PlaceholderArt.CogBeetle(); EditorUtility.SetDirty(e); }
            return e;
        }

        public static EnemyDefinition GetOrCreateSpringTick()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/SpringTick.asset", d =>
            {
                d.id = "enemy_spring_tick"; d.displayName = "Spring Tick";
                d.codexEntry = "A wind-up sentry that coils tight and launches itself at intruders. " +
                               "Wait for it to land, then strike before it rewinds.";
                d.bodySize = new Vector2(0.7f, 0.75f); d.maxHealth = 2; d.weight = 0.9f; d.moveSpeed = 0f;
                d.behavior = EnemyBehavior.Hopper; d.hopCooldown = 1.5f; d.hopRange = 7f; d.hopDrift = 0.5f;
                d.shardDrop = 3;
            });
            if (e.sprite == null) { e.sprite = PlaceholderArt.SpringTick(); EditorUtility.SetDirty(e); }
            return e;
        }

        public static EnemyDefinition GetOrCreateGyroMoth()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/GyroMoth.asset", d =>
            {
                d.id = "enemy_gyro_moth"; d.displayName = "Gyro Moth";
                d.codexEntry = "Tickworks pollinators that once carried oil between the great gears. Now they guard the gaps " +
                               "between islands, hovering in place until something warm comes close, then diving straight at it.";
                d.bodySize = new Vector2(0.8f, 0.6f); d.maxHealth = 1; d.weight = 0.7f; d.moveSpeed = 0f;
                d.behavior = EnemyBehavior.Flyer;
                d.aggroRange = 6f; d.swoopSpeed = 9f; d.swoopTime = 0.8f; d.swoopWindup = 0.55f;
                d.flyerReturnSpeed = 4f; d.swoopCooldown = 1.6f;
                d.shardDrop = 2;
            });
            if (e.sprite == null) { e.sprite = PlaceholderArt.GyroMoth(); EditorUtility.SetDirty(e); }
            return e;
        }

        public static EnemyDefinition GetOrCreateBoltKnight()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/BoltKnight.asset", d =>
            {
                d.id = "enemy_bolt_knight"; d.displayName = "Bolt Knight";
                d.codexEntry = "Gate guards riveted together from spare boiler plate. Their tower shields shrug off light blows; " +
                               "a heavy hit knocks the shield aside, and their backs were never armored at all.";
                d.bodySize = new Vector2(0.9f, 1.25f); d.maxHealth = 4; d.weight = 1.6f; d.moveSpeed = 0.18f;
                d.behavior = EnemyBehavior.ShieldWalker;
                d.shieldBreakDuration = 1.6f; d.guardRange = 4f;
                d.shardDrop = 4;
            });
            bool dirty = false;
            if (e.sprite == null) { e.sprite = PlaceholderArt.BoltKnight(); dirty = true; }
            if (e.shieldSprite == null) { e.shieldSprite = PlaceholderArt.BoltShield(); dirty = true; }
            if (dirty) EditorUtility.SetDirty(e);
            return e;
        }

        public static LevelDefinition GetOrCreateSunspireTestLevel()
        {
            EditorUtil.EnsureFolder(LevelsFolder);
            var lvl = GetOrCreate<LevelDefinition>($"{LevelsFolder}/SQ_SunspireMeadows_Test.asset", l =>
            {
                l.id = "sq_sunspire_test"; l.displayName = "Sunspire Meadows"; l.biomeName = "Sunspire Meadows";
                l.sceneName = SceneIds.StoryTest; l.parTimeSeconds = 120f;
                l.codexEntry = "Grassland islands stitched together by the Tickworks' oldest machinery. " +
                               "The brass is warm to the touch, and the gears hum just below hearing.";
                l.skyColor = new Color32(255, 200, 150, 255);
            });
            // Phase 3.3: Meadows now leads into Sunspire Heights (it used to go straight to the Warden).
            if (lvl.nextSceneName != SceneIds.SunspireHeights || lvl.modeTag != "Platform")
            {
                lvl.nextSceneName = SceneIds.SunspireHeights; lvl.modeTag = "Platform"; EditorUtility.SetDirty(lvl);
            }
            return lvl;
        }

        public static EnemyDefinition GetOrCreateGearwatchSentry()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/GearwatchSentry.asset", d =>
            {
                d.id = "enemy_gearwatch_sentry"; d.displayName = "Gearwatch Sentry";
                d.codexEntry = "Lantern-carrying night watch of the Brasslight Quarter. They see far down the street they face " +
                               "and nothing behind them; stay out of the light, wait in the hay, and a tap on the back ends their shift.";
                d.bodySize = new Vector2(0.8f, 1.3f); d.maxHealth = 4; d.weight = 1.3f; d.moveSpeed = 0.2f;
                d.behavior = EnemyBehavior.Guard;
                d.sightRange = 6f; d.sightHalfAngle = 30f; d.lookBackInterval = 4f; d.lookBackPause = 1.1f; d.chaseSpeed = 0.6f;
                d.shardDrop = 2;
            });
            if (e.sprite == null) { e.sprite = PlaceholderArt.GearwatchSentry(); EditorUtility.SetDirty(e); }
            return e;
        }

        // ---- Shadow Contracts ---------------------------------------------------------------------------

        public const string ContractsFolder = "Assets/_Project/Data/Contracts";
        public const string BoardAssetPath = ContractsFolder + "/MissionBoard.asset";

        /// <summary>The four worlds of the board. Each door opens when the world before it is cleared.</summary>
        public static readonly (string name, string tagline, Color32 color)[] ContractWorlds =
        {
            ("Brasslight Quarter", "The rooftops and docks of the city where the Order began.", new Color32(124, 104, 214, 255)),
            ("Frost Citadel", "A fortress carved into the glacier. Frost Mites gnaw the halls and the moats are full of ice spikes.", new Color32(86, 164, 220, 255)),
            ("Sky Foundry", "Floating forges above the clouds. Ride steam springs and drifting plates past the Gearwatch Elite.", new Color32(220, 120, 60, 255)),
            ("Shadow Spire", "The Gearwatch's own tower. Elites on every floor, spikes below, and the Grandmaster at the top.", new Color32(120, 60, 170, 255)),
        };

        /// <summary>Everything the builder and the board need for one contract.</summary>
        public class ContractSpec
        {
            public string id, displayName, sceneName, briefing, objectiveName, objectiveDisplay, containsClue, revealedByClue, clueHint;
            /// <summary>World on the board (0 = Brasslight Quarter) and the biome name that picks the terrain tint.</summary>
            public int world;
            public string biome = "Brasslight Quarter";
            public ContractKind kind;
            public bool free;
            public float par;
            public Color32 sky;
            public string[] rows;
        }

        public static readonly ContractSpec[] ContractSpecs =
        {
            new ContractSpec
            {
                id = "ms_first_light", displayName = "First Light", sceneName = "MS_FirstLight", kind = ContractKind.Main, free = true,
                par = 120f, sky = new Color32(46, 40, 92, 255), rows = ContractLayouts.FirstLight,
                objectiveName = "the Dawn Lantern", objectiveDisplay = "Dawn Lantern",
                briefing = "Your first night with the Order of the Hidden Gear. A merchant on the low roofs has stolen the " +
                           "Dawn Lantern. Hide in the hay, climb the vines, slip past the watch and bring it back. " +
                           "Hit a guard from behind before it sees you for a silent takedown."
            },
            new ContractSpec
            {
                id = "ms_bell_keeper", displayName = "The Bell Keeper", sceneName = "MS_BellKeeper", kind = ContractKind.Main,
                par = 150f, sky = new Color32(36, 30, 72, 255), rows = ContractLayouts.BellKeeper, containsClue = "clue_cipher_crypt",
                objectiveName = "the target", objectiveDisplay = "Target",
                briefing = "Captain Ruskin rings the alarm bell whenever the Order moves. Climb the bell tower and take him " +
                           "down, then vanish into the crowd. Rumor says a back room on this street holds a coded scroll."
            },
            new ContractSpec
            {
                id = "ms_brass_vault", displayName = "Vault of the Brass Kings", sceneName = "MS_BrassVault", kind = ContractKind.Main,
                par = 180f, sky = new Color32(54, 34, 70, 255), rows = ContractLayouts.BrassVault,
                objectiveName = "the Brass Crown", objectiveDisplay = "Brass Crown",
                briefing = "The old kings' crown sits in a stepped vault, guarded day and night. Cross the gap, climb the " +
                           "vault walls and lift the Brass Crown from its pedestal. Ancient relics line every ledge."
            },
            new ContractSpec
            {
                id = "ms_lantern_harbor", displayName = "Harbor of Lanterns", sceneName = "MS_LanternHarbor", kind = ContractKind.Main,
                par = 170f, sky = new Color32(28, 36, 74, 255), rows = ContractLayouts.LanternHarbor, containsClue = "clue_clocktower",
                objectiveName = "the target", objectiveDisplay = "Target",
                briefing = "A smuggler captain ships stolen relics out of the lantern harbor tonight. Cross the docks and " +
                           "stop him on the warehouse roof. Check the boathouse: smugglers hide more than cargo."
            },
            new ContractSpec
            {
                id = "ms_gearworks_foundry", displayName = "The Gearworks Foundry", sceneName = "MS_GearworksFoundry", kind = ContractKind.Main,
                par = 190f, sky = new Color32(70, 34, 30, 255), rows = ContractLayouts.GearworksFoundry,
                objectiveName = "the Forge Seal", objectiveDisplay = "Forge Seal",
                briefing = "The foundry captain stamps every forged pass in the city with the Forge Seal. Ride the steam " +
                           "spring to the furnace roof, lift the seal, then take the captain down before the night shift ends."
            },
            new ContractSpec
            {
                id = "ms_skybridge_gala", displayName = "The Skybridge Gala", sceneName = "MS_SkybridgeGala", kind = ContractKind.Main,
                par = 175f, sky = new Color32(60, 40, 100, 255), rows = ContractLayouts.SkybridgeGala,
                objectiveName = "the target", objectiveDisplay = "Target",
                briefing = "The rival guild's paymaster is hosting a gala on the lantern bridges above the square. Cross " +
                           "the bridges, stay in the shadows of the hay carts and take him down. Leave no witnesses awake."
            },
            new ContractSpec
            {
                id = "ms_cipher_crypt", displayName = "The Cipher Crypt", sceneName = "MS_CipherCrypt", kind = ContractKind.Secret,
                par = 160f, sky = new Color32(20, 18, 40, 255), rows = ContractLayouts.CipherCrypt, revealedByClue = "clue_cipher_crypt",
                clueHint = "Search the back rooms of The Bell Keeper",
                objectiveName = "the Cipher Key", objectiveDisplay = "Cipher Key",
                briefing = "The scroll was a map. Beneath the market lie catacombs packed with relics of the first kings, " +
                           "and the Cipher Key that opens the Order's oldest door. The crypt has its own guards."
            },
            new ContractSpec
            {
                id = "ms_clocktower_shadow", displayName = "Clocktower Shadow", sceneName = "MS_ClocktowerShadow", kind = ContractKind.Secret,
                par = 180f, sky = new Color32(40, 24, 64, 255), rows = ContractLayouts.ClocktowerShadow, revealedByClue = "clue_clocktower",
                clueHint = "A scroll hides in the Harbor of Lanterns boathouse",
                objectiveName = "the Master Key", objectiveDisplay = "Master Key",
                briefing = "The last captain of the rival guild waits at the top of the great clock with the Master Key. " +
                           "Three roofs, three climbs, one target. Take him down and take the key."
            },

            // ---- World 2: Frost Citadel ----
            new ContractSpec
            {
                id = "ms_frozen_gate", displayName = "The Frozen Gate", sceneName = "MS_FrozenGate", kind = ContractKind.Main,
                world = 1, biome = "Frost Citadel", par = 200f, sky = new Color32(24, 40, 78, 255), rows = ContractLayouts.FrozenGate,
                objectiveName = "the Rime Seal", objectiveDisplay = "Rime Seal",
                briefing = "Through the door lies the Frost Citadel. Cross the spiked moat, climb the gatehouse and lift the " +
                           "Rime Seal that keeps its doors frozen shut. Frost Mites patrol the ice: they don't look, but they bite."
            },
            new ContractSpec
            {
                id = "ms_hall_of_icicles", displayName = "Hall of Icicles", sceneName = "MS_HallOfIcicles", kind = ContractKind.Main,
                world = 1, biome = "Frost Citadel", par = 210f, sky = new Color32(18, 34, 66, 255), rows = ContractLayouts.HallOfIcicles,
                objectiveName = "the target", objectiveDisplay = "Target",
                briefing = "A long frozen hall under a low ceiling of ice. The Ice Warden walks the upper gallery. Take him down " +
                           "without waking the whole hall."
            },
            new ContractSpec
            {
                id = "ms_glacier_vault", displayName = "The Glacier Vault", sceneName = "MS_GlacierVault", kind = ContractKind.Main,
                world = 1, biome = "Frost Citadel", par = 230f, sky = new Color32(30, 46, 90, 255), rows = ContractLayouts.GlacierVault,
                objectiveName = "the Frost Crown", objectiveDisplay = "Frost Crown",
                briefing = "The citadel's treasury: three tiers of ice and stone. Steal the Frost Crown from the top and take down " +
                           "the Vault Keeper. Clear this and the door to the Sky Foundry opens."
            },
            // ---- World 3: Sky Foundry ----
            new ContractSpec
            {
                id = "ms_cloud_docks", displayName = "Cloud Docks", sceneName = "MS_CloudDocks", kind = ContractKind.Main,
                world = 2, biome = "Sky Foundry", par = 200f, sky = new Color32(110, 150, 210, 255), rows = ContractLayouts.CloudDocks,
                objectiveName = "the Foundry Pass", objectiveDisplay = "Foundry Pass",
                briefing = "Airship docks hanging over open sky. Ride the drifting plates, spring onto the cargo tower and steal " +
                           "the Foundry Pass. Watch for the Gearwatch Elite: sharper eyes, faster to notice."
            },
            new ContractSpec
            {
                id = "ms_bellows", displayName = "The Bellows", sceneName = "MS_Bellows", kind = ContractKind.Main,
                world = 2, biome = "Sky Foundry", par = 230f, sky = new Color32(150, 90, 60, 255), rows = ContractLayouts.Bellows,
                objectiveName = "the target", objectiveDisplay = "Target",
                briefing = "The foundry's lungs: steam springs, a lift and floors of spikes. The Bellows Master runs the furnace " +
                           "from the top deck. Take him down."
            },
            new ContractSpec
            {
                id = "ms_furnace_crown", displayName = "Furnace Crown", sceneName = "MS_FurnaceCrown", kind = ContractKind.Main,
                world = 2, biome = "Sky Foundry", par = 260f, sky = new Color32(120, 60, 40, 255), rows = ContractLayouts.FurnaceCrown,
                objectiveName = "the Ember Key", objectiveDisplay = "Ember Key",
                briefing = "The top of the foundry. Climb the three-tier crown, steal the Ember Key and take down the Foundry Lord. " +
                           "Beyond it waits the door to the Shadow Spire."
            },
            // ---- World 4: Shadow Spire ----
            new ContractSpec
            {
                id = "ms_spire_gate", displayName = "The Spire Gate", sceneName = "MS_SpireGate", kind = ContractKind.Main,
                world = 3, biome = "Shadow Spire", par = 240f, sky = new Color32(26, 16, 40, 255), rows = ContractLayouts.SpireGate,
                objectiveName = "the Spire Sigil", objectiveDisplay = "Spire Sigil",
                briefing = "The Gearwatch's own fortress. Elites on every wall and hardly any hay to hide in. Steal the Spire " +
                           "Sigil from the gatehouse roof and get inside."
            },
            new ContractSpec
            {
                id = "ms_mirror_galleries", displayName = "The Mirror Galleries", sceneName = "MS_MirrorGalleries", kind = ContractKind.Main,
                world = 3, biome = "Shadow Spire", par = 260f, sky = new Color32(20, 12, 34, 255), rows = ContractLayouts.MirrorGalleries,
                objectiveName = "the targets", objectiveDisplay = "Targets",
                briefing = "Vaulted galleries under a low ceiling, a lift between floors and two marked inquisitors. Both must " +
                           "fall before the far door will open."
            },
            new ContractSpec
            {
                id = "ms_last_contract", displayName = "The Last Contract", sceneName = "MS_LastContract", kind = ContractKind.Main,
                world = 3, biome = "Shadow Spire", par = 300f, sky = new Color32(34, 10, 30, 255), rows = ContractLayouts.LastContract,
                objectiveName = "the Grandmaster's Gear", objectiveDisplay = "Grandmaster's Gear",
                briefing = "The top of the Shadow Spire. Climb the three tiers, take the Grandmaster's Gear and take down the " +
                           "Grandmaster himself. End the Gearwatch, and walk out a legend of the Order."
            },
        };

        public static LevelDefinition GetOrCreateContractLevel(ContractSpec s)
        {
            EditorUtil.EnsureFolder(ContractsFolder);
            var lvl = GetOrCreate<LevelDefinition>($"{ContractsFolder}/{s.sceneName}.asset", l =>
            {
                l.id = s.id; l.displayName = s.displayName; l.biomeName = s.biome;
                l.sceneName = s.sceneName; l.parTimeSeconds = s.par; l.skyColor = s.sky;
                l.codexEntry = s.briefing;
            });
            if (lvl.modeTag != "Contract" || !string.IsNullOrEmpty(lvl.nextSceneName) || lvl.biomeName != s.biome)
            {
                lvl.modeTag = "Contract"; lvl.nextSceneName = ""; lvl.biomeName = s.biome; EditorUtility.SetDirty(lvl);
            }
            return lvl;
        }

        public static MissionBoardDefinition GetOrCreateMissionBoard()
        {
            EditorUtil.EnsureFolder(ContractsFolder);
            var board = GetOrCreate<MissionBoardDefinition>($"{ContractsFolder}/MissionBoard.asset", b =>
            {
                b.title = "SHADOW CONTRACTS";
                b.description = "Missions for the Order of the Hidden Gear.";
            });
            board.worlds.Clear();
            foreach (var w in ContractWorlds) board.worlds.Add(new ContractWorld { name = w.name, tagline = w.tagline, color = w.color });
            board.contracts.Clear();
            foreach (var s in ContractSpecs)
            {
                var m = GetOrCreate<MissionDefinition>($"{ContractsFolder}/Contract_{s.sceneName}.asset", d => { });
                m.id = s.id; m.displayName = s.displayName; m.briefing = s.briefing; m.kind = s.kind; m.free = s.free;
                m.revealedByClue = s.revealedByClue ?? ""; m.clueHint = s.clueHint ?? ""; m.containsClue = s.containsClue ?? "";
                m.world = s.world;
                m.level = GetOrCreateContractLevel(s);
                m.totalRelics = new AsciiLevel(s.rows).Count('R');
                EditorUtility.SetDirty(m);
                board.contracts.Add(m);
            }
            EditorUtility.SetDirty(board);
            return board;
        }

        public static EnemyDefinition GetOrCreateGearwatchElite()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/GearwatchElite.asset", d =>
            {
                d.id = "enemy_gearwatch_elite"; d.displayName = "Gearwatch Elite";
                d.codexEntry = "The Spire's own guard. They see farther, notice faster and look back more often than any sentry, " +
                               "and they chase hard. Their armor still has no eyes in the back.";
                d.bodySize = new Vector2(0.85f, 1.35f); d.maxHealth = 5; d.weight = 1.4f; d.moveSpeed = 0.22f;
                d.behavior = EnemyBehavior.Guard;
                d.sightRange = 7.5f; d.sightHalfAngle = 36f; d.lookBackInterval = 2.8f; d.lookBackPause = 1.2f; d.chaseSpeed = 0.72f;
                d.noticeRate = 2.4f; d.shardDrop = 4;
            });
            if (e.sprite == null) { e.sprite = PlaceholderArt.GearwatchElite(); EditorUtility.SetDirty(e); }
            return e;
        }

        public static EnemyDefinition GetOrCreateGearwatchCaptain()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/GearwatchCaptain.asset", d =>
            {
                d.id = "enemy_gearwatch_captain"; d.displayName = "Gearwatch Captain";
                d.codexEntry = "Officers of the night watch, marked for the Order's contracts. Tougher than a sentry and quick " +
                               "to raise the alarm, but just as blind behind.";
                d.bodySize = new Vector2(0.85f, 1.35f); d.maxHealth = 6; d.weight = 1.5f; d.moveSpeed = 0.16f;
                d.behavior = EnemyBehavior.Guard;
                d.sightRange = 6.5f; d.sightHalfAngle = 32f; d.lookBackInterval = 3.2f; d.lookBackPause = 1.3f; d.chaseSpeed = 0.68f;
                d.shardDrop = 5;
            });
            bool dirty = false;
            if (!e.isMissionTarget) { e.isMissionTarget = true; dirty = true; }
            if (e.sprite == null) { e.sprite = PlaceholderArt.GearwatchCaptain(); dirty = true; }
            if (dirty) EditorUtility.SetDirty(e);
            return e;
        }

        /// <summary>One of the text-map Story Quest levels (Phase 3.2/3.3).</summary>
        static LevelDefinition GetOrCreateMapLevel(string file, string id, string name, string biome, string scene, string next,
                                                   string tag, float par, Color32 sky, string codex)
        {
            EditorUtil.EnsureFolder(LevelsFolder);
            var lvl = GetOrCreate<LevelDefinition>($"{LevelsFolder}/{file}.asset", l =>
            {
                l.id = id; l.displayName = name; l.biomeName = biome; l.sceneName = scene; l.parTimeSeconds = par;
                l.codexEntry = codex; l.skyColor = sky;
            });
            if (lvl.nextSceneName != next || lvl.modeTag != tag) { lvl.nextSceneName = next; lvl.modeTag = tag; EditorUtility.SetDirty(lvl); }
            return lvl;
        }

        public static LevelDefinition GetOrCreateSunspireHeights() => GetOrCreateMapLevel(
            "SQ_SunspireHeights", "sq_sunspire_heights", "Sunspire Heights", "Sunspire Meadows", SceneIds.SunspireHeights,
            SceneIds.GearfallCaverns, "Platform", 150f, new Color32(150, 200, 255, 255),
            "The windy upper meadows: spring pads, vine-covered pillars and crates packed with shards by careless couriers.");

        public static LevelDefinition GetOrCreateGearfallCaverns() => GetOrCreateMapLevel(
            "SQ_GearfallCaverns", "sq_gearfall_caverns", "Gearfall Caverns", "Under the Isles", SceneIds.GearfallCaverns,
            SceneIds.GearworkMaze, "Platform", 170f, new Color32(48, 44, 70, 255),
            "Hollow roots of the floating isles where broken gears rain down for centuries. The floor is all spikes; the way " +
            "through is up, on lifts and drifting plates.");

        public static LevelDefinition GetOrCreateRooftopRun() => GetOrCreateMapLevel(
            "SQ_RooftopRun", "sq_rooftop_run", "Rooftop Run", "Brasslight Quarter", SceneIds.RooftopRun,
            SceneIds.NightMarketHeist, "Stealth", 160f, new Color32(40, 46, 88, 255),
            "Tiled roofs of the Brasslight Quarter by night. The Gearwatch walks every ridge; someone has hidden the " +
            "Gearwright's Ledger up here, and it explains why the Warden went wrong.");

        public static LevelDefinition GetOrCreateNightMarketHeist() => GetOrCreateMapLevel(
            "SQ_NightMarketHeist", "sq_night_market", "Night Market Heist", "Brasslight Quarter", SceneIds.NightMarketHeist,
            SceneIds.FrostgearAscent, "Stealth", 180f, new Color32(60, 36, 72, 255),
            "Lantern stalls, hay carts and a counting house on stilts. The real ledger is in the vault above the market, " +
            "and the whole night watch is between you and it.");

        // ---- World 2: Frostgear Peaks (Phase 5) ----------------------------------------------------------------
        public static LevelDefinition GetOrCreateFrostgearAscent() => GetOrCreateMapLevel(
            "SQ_FrostgearAscent", "sq_frostgear_ascent", "Frostgear Ascent", "Frostgear Peaks", SceneIds.FrostgearAscent,
            SceneIds.ClockworkGlacier, "Platform", 190f, new Color32(170, 210, 240, 255),
            "The ledger points north, to the peaks where the Tickworks' cooling gears turn under the ice. Frost Mites " +
            "chew the frozen teeth, and the old shelves are only reachable on steam springs and drifting plates.");

        public static LevelDefinition GetOrCreateClockworkGlacier() => GetOrCreateMapLevel(
            "SQ_ClockworkGlacier", "sq_clockwork_glacier", "Clockwork Glacier", "Frostgear Peaks", SceneIds.ClockworkGlacier,
            SceneIds.FrozenVault, "Platform", 200f, new Color32(120, 170, 220, 255),
            "A whole engine floor frozen solid, its spikes rimed with ice. Lifts still rise and plates still drift on " +
            "their old schedules; ride them tower to tower to reach the heart of the cold.");

        public static EnemyDefinition GetOrCreateFrostMite()
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/FrostMite.asset", d =>
            {
                d.id = "enemy_frost_mite"; d.displayName = "Frost Mite";
                d.codexEntry = "Ice-shelled mites that gnaw the frozen gears of the Frostgear Peaks. Quicker than a Cog " +
                               "Beetle and a little tougher; they skitter back and forth across their patch of ice.";
                d.bodySize = new Vector2(0.85f, 0.65f); d.maxHealth = 3; d.weight = 1f; d.moveSpeed = 0.4f;
                d.behavior = EnemyBehavior.Walker; d.shardDrop = 3;
            });
            if (e.sprite == null) { e.sprite = PlaceholderArt.FrostMite(); EditorUtility.SetDirty(e); }
            return e;
        }

        // ---- World 3: The Shattered Realms -----------------------------------------------------------------------
        // Under the glacier sat the Tickworks' mainspring. Freeing it cracked the engine's heart into four sealed
        // realms, each running on its own broken rule, and the core mind that split them waits where they meet.
        public static readonly (string name, string tagline, Color32 color)[] ShatteredRealmSections =
        {
            ("EMBER WASTES", "Lava, falling basalt, fire geysers", new Color32(232, 96, 48, 255)),
            ("SKY GARDENS", "Wind that carries you off ledges", new Color32(120, 196, 240, 255)),
            ("SUNKEN CLOCKWORKS", "Currents and drowned gears", new Color32(48, 170, 160, 255)),
            ("TOY FORTRESS", "Conveyors, trapdoors, wind-up guards", new Color32(214, 84, 150, 255)),
        };

        public static LevelDefinition GetOrCreateEmberWastes() => GetOrCreateMapLevel(
            "SQ_EmberWastes", "sq_ember_wastes", "Ember Wastes", "Ember Wastes", SceneIds.EmberWastes,
            SceneIds.SkyGardens, "Platform", 200f, new Color32(70, 26, 24, 255),
            "The first realm the mainspring cracked open: a basalt desert over a sea of lava. The bridges crumble under " +
            "your boots, geysers keep time like a broken clock, and Cinder Crawlers drag their glowing backs across it all.");

        public static LevelDefinition GetOrCreateSkyGardens() => GetOrCreateMapLevel(
            "SQ_SkyGardens", "sq_sky_gardens", "Sky Gardens", "Sky Gardens", SceneIds.SkyGardens,
            SceneIds.SunkenClockworks, "Platform", 200f, new Color32(168, 214, 255, 255),
            "Hanging gardens torn loose and set adrift. The Tickworks' fans still blow, so the wind decides where a jump " +
            "lands. Puff Gliders ride the gusts; vines are the only things that hold still.");

        public static LevelDefinition GetOrCreateSunkenClockworks() => GetOrCreateMapLevel(
            "SQ_SunkenClockworks", "sq_sunken_clockworks", "Sunken Clockworks", "Sunken Clockworks", SceneIds.SunkenClockworks,
            SceneIds.ToyFortress, "Platform", 210f, new Color32(20, 64, 86, 255),
            "A machine city that sank into the engine's coolant. Currents tug you along the old pipe-streets, drowned " +
            "gears still turn as platforms, and Rust Crabs guard every dry ledge.");

        public static LevelDefinition GetOrCreateToyFortress() => GetOrCreateMapLevel(
            "SQ_ToyFortress", "sq_toy_fortress", "Toy Fortress", "Toy Fortress", SceneIds.ToyFortress,
            SceneIds.ShatteredCrossing, "Platform", 210f, new Color32(48, 30, 60, 255),
            "The Gearwright's old toy workshop, wound up and left running for a hundred years. Belts carry you toward " +
            "trapdoors, and the Wind-up Soldiers march their rounds whether anyone is watching or not.");

        public static LevelDefinition GetOrCreateShatteredCrossing() => GetOrCreateMapLevel(
            "SQ_ShatteredCrossing", "sq_shattered_crossing", "The Shattered Crossing", "Shattered Crossing", SceneIds.ShatteredCrossing,
            SceneIds.ShatteredLabyrinth, "Platform", 260f, new Color32(40, 30, 64, 255),
            "Where the four realms grind against each other: lava gives way to wind, wind to water, water to the toy " +
            "belts. One long run through every rule at once, to the door of the engine's heart.");

        public static LevelDefinition GetOrCreateSovereignLevel() => GetOrCreateMapLevel(
            "SQ_FourfoldSovereign", "sq_fourfold_sovereign", "The Fourfold Sovereign", "Sovereign's Core", SceneIds.FourfoldSovereign,
            SceneIds.StarfallSteps, "Boss", 150f, new Color32(26, 16, 36, 255),
            "The Tickworks' core mind, split four ways. It fights like the Warden it once commanded, only faster, and it " +
            "changes realms as it weakens. Strike its core while it's stuck after a slam.");

        static EnemyDefinition GetOrCreateRealmFoe(string file, string id, string name, string codex, EnemyBehavior behavior,
                                                   Vector2 size, int hp, float speed, int drop, System.Func<Sprite> art,
                                                   System.Action<EnemyDefinition> extra = null)
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/{file}.asset", d =>
            {
                d.id = id; d.displayName = name; d.codexEntry = codex;
                d.bodySize = size; d.maxHealth = hp; d.weight = 1f; d.moveSpeed = speed;
                d.behavior = behavior; d.shardDrop = drop;
                extra?.Invoke(d);
            });
            if (e.sprite == null) { e.sprite = art(); EditorUtility.SetDirty(e); }
            return e;
        }

        public static EnemyDefinition GetOrCreateCinderCrawler() => GetOrCreateRealmFoe("CinderCrawler", "enemy_cinder_crawler",
            "Cinder Crawler", "Basalt slugs that grew in the Ember Wastes' heat. Slow and stubborn, with a back too hot to stand on; " +
            "a solid hit cracks the crust.", EnemyBehavior.Walker, new Vector2(1f, 0.6f), 3, 0.22f, 3, PlaceholderArt.CinderCrawler);

        public static EnemyDefinition GetOrCreatePuffGlider() => GetOrCreateRealmFoe("PuffGlider", "enemy_puff_glider",
            "Puff Glider", "Weather drones from the Sky Gardens, still sowing clouds over gardens that drifted away long ago. " +
            "They hang on the wind and dive at anything that jumps past.", EnemyBehavior.Flyer, new Vector2(0.9f, 0.6f), 1, 0f, 2,
            PlaceholderArt.PuffGlider, d => { d.aggroRange = 6.5f; d.swoopSpeed = 8f; d.swoopTime = 0.9f; d.swoopWindup = 0.6f;
                                              d.flyerReturnSpeed = 3.5f; d.swoopCooldown = 1.8f; d.weight = 0.6f; });

        public static EnemyDefinition GetOrCreateRustCrab() => GetOrCreateRealmFoe("RustCrab", "enemy_rust_crab",
            "Rust Crab", "Repair crabs of the sunken machine city. The coolant rusted their shells but not their routines: " +
            "they scuttle their ledge end to end, quicker than they look.", EnemyBehavior.Walker, new Vector2(0.95f, 0.7f), 3, 0.45f, 3,
            PlaceholderArt.RustCrab);

        public static EnemyDefinition GetOrCreateWindupSoldier() => GetOrCreateRealmFoe("WindupSoldier", "enemy_windup_soldier",
            "Wind-up Soldier", "Toy guards from the Gearwright's workshop, wound so tight they never stopped marching. " +
            "Tougher than they look; two good hits knock the key loose.", EnemyBehavior.Walker, new Vector2(0.7f, 1.1f), 4, 0.35f, 4,
            PlaceholderArt.WindupSoldier);

        /// <summary>Story world 3 on the level select: four realms, the crossing that joins them, and the boss.</summary>
        public static WorldDefinition GetOrCreateShatteredRealms()
        {
            EditorUtil.EnsureFolder(LevelsFolder);
            var route = new[]
            {
                GetOrCreateEmberWastes(), GetOrCreateSkyGardens(), GetOrCreateSunkenClockworks(), GetOrCreateToyFortress(),
                GetOrCreateShatteredCrossing(), GetOrCreateShatteredLabyrinth(), GetOrCreateSovereignLevel()
            };
            var world = GetOrCreate<WorldDefinition>($"{LevelsFolder}/World_ShatteredRealms.asset", w =>
            {
                w.displayName = "The Shattered Realms";
                w.description = "The mainspring cracked the engine's heart into four sealed realms. Cross them all, then wind the Sovereign back.";
            });
            world.levels.Clear();
            world.levels.AddRange(route);
            world.sectioned = true;
            world.sectionColors.Clear();
            world.sectionNotes.Clear();
            foreach (var sec in ShatteredRealmSections) { world.sectionColors.Add(sec.color); world.sectionNotes.Add(sec.tagline); }
            EditorUtility.SetDirty(world);
            return world;
        }

        /// <summary>The Story Quest route shown on the level select, in play order.</summary>
        public static WorldDefinition GetOrCreateStoryWorld()
        {
            EditorUtil.EnsureFolder(LevelsFolder);
            var route = new[]
            {
                GetOrCreateSunspireTestLevel(), GetOrCreateSunspireHeights(), GetOrCreateGearfallCaverns(), GetOrCreateGearworkMaze(),
                GetOrCreateWardenLevel(), GetOrCreateRooftopRun(), GetOrCreateNightMarketHeist(),
                GetOrCreateFrostgearAscent(), GetOrCreateClockworkGlacier(), GetOrCreateFrozenVault()
            };
            var world = GetOrCreate<WorldDefinition>($"{LevelsFolder}/World_SunspireIsles.asset", w =>
            {
                w.displayName = "Sunspire Isles";
                w.description = "Floating meadows held up by the Tickworks. Something in the engine hall is grinding backward.";
            });
            // Play order is fixed by the route above (rebuilt every time so new levels slot in where they belong).
            world.levels.Clear();
            world.levels.AddRange(route);
            EditorUtility.SetDirty(world);
            return world;
        }

        public const string CodexFolder = "Assets/_Project/Data/Codex";

        /// <summary>Codex contents: the roster, every foe type and the Story Quest world. Missing links are filled in.</summary>
        public static CodexDefinition GetOrCreateCodex()
        {
            EditorUtil.EnsureFolder(CodexFolder);
            var codex = GetOrCreate<CodexDefinition>($"{CodexFolder}/Codex.asset", c => { });
            if (codex.roster == null) codex.roster = GetOrCreateRoster();
            if (codex.world == null) codex.world = GetOrCreateStoryWorld();
            if (!codex.moreWorlds.Contains(GetOrCreateShatteredRealms())) codex.moreWorlds.Add(GetOrCreateShatteredRealms());
            if (!codex.moreWorlds.Contains(GetOrCreateStarfallObservatory())) codex.moreWorlds.Add(GetOrCreateStarfallObservatory());
            foreach (var e in new[] { GetOrCreateCogBeetle(), GetOrCreateSpringTick(), GetOrCreateGyroMoth(), GetOrCreateBoltKnight(), GetOrCreateGearwatchSentry(), GetOrCreateGearwatchCaptain(),
                                      GetOrCreateCinderCrawler(), GetOrCreatePuffGlider(), GetOrCreateRustCrab(), GetOrCreateWindupSoldier() })
                if (!codex.enemies.Contains(e)) codex.enemies.Add(e);
            EditorUtility.SetDirty(codex);
            return codex;
        }

        public static LevelDefinition GetOrCreateWardenLevel()
        {
            EditorUtil.EnsureFolder(LevelsFolder);
            var lvl = GetOrCreate<LevelDefinition>($"{LevelsFolder}/SQ_ClockworkWarden_Test.asset", l =>
            {
                l.id = "sq_clockwork_warden"; l.displayName = "Clockwork Warden"; l.biomeName = "Tickworks Engine Hall";
                l.sceneName = SceneIds.BossTest; l.parTimeSeconds = 90f;
                l.codexEntry = "A towering guardian built to keep the Tickworks turning. Its memory has worn down with its " +
                               "gears, and it now guards the engine hall against everyone, even the heroes it was built to help.";
                l.skyColor = new Color32(70, 52, 60, 255);
            });
            // Phase 3.2: after the Warden, the story moves to the Brasslight Quarter rooftops.
            if (lvl.nextSceneName != SceneIds.RooftopRun || lvl.modeTag != "Boss")
            {
                lvl.nextSceneName = SceneIds.RooftopRun; lvl.modeTag = "Boss"; EditorUtility.SetDirty(lvl);
            }
            return lvl;
        }

        public static CharacterDefinition GetOrCreateNova()
        {
            EditorUtil.EnsureFolder(CharactersFolder);
            var kit = GetOrCreate<NovaKitDefinition>($"{CharactersFolder}/NovaKit.asset", _ => { });
            var nova = GetOrCreate<CharacterDefinition>($"{CharactersFolder}/Nova.asset", d =>
            {
                d.id = "nova";
                d.displayName = "Nova";
                d.tagline = "Sky courier. Never late, rarely on the ground.";
                d.lore = "Nova carries messages between the drifting Sunspire Isles, vaulting gaps no bridge reaches. " +
                         "Her comet staff was cut from a fallen star-rail of the Tickworks, and it still hums when the " +
                         "great gears turn backward. She took up the Star Shard hunt because a courier who stops moving " +
                         "is a courier whose island has already fallen.";
                d.unlockedByDefault = true;
                d.playableInThisBuild = true;
                d.placeholderColor = new Color32(46, 168, 178, 255);
                d.maxHealth = 5;
                d.weight = 0.9f;   // light: travels far when launched
                d.armorPercent = 0f;
                d.guardPips = 3;
            });
            if (nova.kit == null) nova.kit = kit;
            if (nova.bodySprite == null) nova.bodySprite = PlaceholderArt.Nova();
            if (nova.moveset == null) nova.moveset = GetOrCreateNovaMoveset();
            EditorUtility.SetDirty(nova);
            return nova;
        }

        public const string BrickMovesFolder = "Assets/_Project/Data/Moves/Brick";

        /// <summary>
        /// Brick, the stone guardian: slow, heavy, armored. Playable from Phase 2. Also upgrades a Brick asset made by an
        /// older build (locked, no kit) without touching tuning that already exists.
        /// </summary>
        public static CharacterDefinition GetOrCreateBrick()
        {
            EditorUtil.EnsureFolder(CharactersFolder);
            var kit = GetOrCreate<BrickKitDefinition>($"{CharactersFolder}/BrickKit.asset", _ => { });
            var brick = GetOrCreate<CharacterDefinition>($"{CharactersFolder}/Brick.asset", d =>
            {
                d.id = "brick";
                d.displayName = "Brick";
                d.placeholderColor = new Color32(140, 136, 150, 255);
                ApplyBrickStats(d);
            });
            bool upgrade = brick.kit == null; // created by Phase 1 as a locked silhouette
            if (upgrade) ApplyBrickStats(brick);
            brick.tagline = string.IsNullOrEmpty(brick.tagline) ? "Stone guardian. Slow to move, impossible to move." : brick.tagline;
            if (string.IsNullOrEmpty(brick.lore))
                brick.lore = "Brick was carved to hold up a bridge that fell centuries ago, and he has been standing guard over the gap " +
                             "ever since. When the islands began to drift he finally stepped off his post, shield stance first.";
            if (brick.kit == null) brick.kit = kit;
            if (brick.bodySprite == null) brick.bodySprite = PlaceholderArt.Brick();
            if (brick.moveset == null) brick.moveset = GetOrCreateBrickMoveset();
            brick.playableInThisBuild = true;
            brick.unlockedByDefault = true;
            EditorUtility.SetDirty(brick);
            return brick;
        }

        static void ApplyBrickStats(CharacterDefinition d)
        {
            d.maxHealth = 6;
            d.weight = 1.6f;       // heavy: hard to launch
            d.armorPercent = 0.3f;
            d.guardPips = 4;       // one more pip than everyone else
            d.dodgeSpeed = 8.5f; d.dodgeDuration = 0.26f; d.dodgeInvulnerability = 0.2f; d.dodgeCooldown = 0.45f;
            d.movement = new MovementStats
            {
                runSpeed = 5.8f, groundAcceleration = 50f, groundDeceleration = 70f,
                airAcceleration = 32f, airDeceleration = 20f,
                jumpHeight = 2.6f, timeToApex = 0.4f, fallGravityMultiplier = 1.9f, maxFallSpeed = 20f,
                airJumps = 1 // Stone Step: he kicks off a conjured slab once per airtime
            };
        }

        /// <summary>Brick's stone-fist moves. Original names and starting values; tune in the Inspector.</summary>
        public static Moveset GetOrCreateBrickMoveset()
        {
            EditorUtil.EnsureFolder(BrickMovesFolder);
            string F(string n) => $"{BrickMovesFolder}/{n}.asset";

            var hook = GetOrCreate<MoveDefinition>(F("Brick_QuarryHook"), m =>
            {
                m.id = "brick_quarry_hook"; m.displayName = "Quarry Hook";
                m.description = "A slow, heavy follow-up hook. Launches Exposed foes a long way.";
                m.frames = new FrameData(7, 4, 18, 6);
                m.hitboxOffset = new Vector2(0.9f, 0.05f); m.hitboxSize = new Vector2(1.2f, 1.0f);
                m.hit = new HitData { damage = 2, pipDamage = 2, baseKnockback = 8.5f, exposedMultiplier = 3.2f,
                                      angleDegrees = 40f, baseHitstunFrames = 15, hitstopFrames = 7, isHeavy = true };
                m.lungeSpeed = 3f; m.haptic = HapticStrength.Medium; m.shakeAmplitude = 0.15f;
            });
            var jab = GetOrCreate<MoveDefinition>(F("Brick_BoulderJab"), m =>
            {
                m.id = "brick_boulder_jab"; m.displayName = "Boulder Jab";
                m.description = "A short stone punch. Press Attack again to follow with Quarry Hook.";
                m.frames = new FrameData(5, 3, 14, 8);
                m.hitboxOffset = new Vector2(0.8f, 0.05f); m.hitboxSize = new Vector2(1.0f, 0.7f);
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 4.5f, exposedMultiplier = 1.5f,
                                      angleDegrees = 15f, baseHitstunFrames = 11, hitstopFrames = 4, isHeavy = false };
                m.lungeSpeed = 2f;
            });
            if (jab.followUp == null) { jab.followUp = hook; EditorUtility.SetDirty(jab); }

            var pillar = GetOrCreate<MoveDefinition>(F("Brick_PillarUppercut"), m =>
            {
                m.id = "brick_pillar_uppercut"; m.displayName = "Pillar Uppercut";
                m.description = "Up + Attack. A column of stone punches up in front of Brick.";
                m.frames = new FrameData(7, 5, 18, 0);
                m.hitboxOffset = new Vector2(0.4f, 1.1f); m.hitboxSize = new Vector2(1.2f, 1.3f);
                m.hit = new HitData { damage = 2, pipDamage = 1, baseKnockback = 9f, exposedMultiplier = 2.6f,
                                      angleDegrees = 85f, baseHitstunFrames = 15, hitstopFrames = 6, isHeavy = true };
                m.lungeSpeed = 0f; m.haptic = HapticStrength.Medium;
            });
            var elbow = GetOrCreate<MoveDefinition>(F("Brick_RockfallElbow"), m =>
            {
                m.id = "brick_rockfall_elbow"; m.displayName = "Rockfall Elbow";
                m.description = "Air Attack. A downward elbow that knocks foes toward the ground.";
                m.frames = new FrameData(5, 5, 14, 0);
                m.hitboxOffset = new Vector2(0.45f, -0.55f); m.hitboxSize = new Vector2(1.0f, 0.9f);
                m.hit = new HitData { damage = 2, pipDamage = 1, baseKnockback = 6f, exposedMultiplier = 2.2f,
                                      angleDegrees = -50f, baseHitstunFrames = 12, hitstopFrames = 5, isHeavy = false };
                m.rootedOnGround = false; m.endsOnLanding = true; m.haptic = HapticStrength.Medium;
            });
            var charge = GetOrCreate<MoveDefinition>(F("Brick_BulwarkCharge"), m =>
            {
                m.id = "brick_bulwark_charge"; m.displayName = "Bulwark Charge";
                m.description = "Ground Special. Brick raises his stone guard and barrels forward. Hits don't stop him " +
                                "(he still takes the damage) unless he is Exposed and hit hard.";
                m.frames = new FrameData(8, 14, 18, 0);
                m.hitboxOffset = new Vector2(0.7f, 0f); m.hitboxSize = new Vector2(0.9f, 1.3f);
                m.hit = new HitData { damage = 2, pipDamage = 2, baseKnockback = 8f, exposedMultiplier = 3f,
                                      angleDegrees = 25f, baseHitstunFrames = 14, hitstopFrames = 6, isHeavy = true };
                m.lungeSpeed = 9f; m.superArmor = true; m.armorStartFrame = 0; m.armorEndFrame = -1;
                m.haptic = HapticStrength.Medium; m.shakeAmplitude = 0.12f;
            });
            var landslide = GetOrCreate<MoveDefinition>(F("Brick_LandslideShockwave"), m =>
            {
                m.id = "brick_landslide_shockwave"; m.displayName = "Landslide Shockwave";
                m.description = "The landing blast of Landslide Slam. Wider and heavier than most.";
                m.frames = new FrameData(0, 1, 0, 0);
                m.shape = HitShape.Circle; m.hitboxOffset = new Vector2(0f, -0.3f); m.hitboxRadius = 2.1f;
                m.hit = new HitData { damage = 3, pipDamage = 2, baseKnockback = 9.5f, exposedMultiplier = 3f,
                                      angleDegrees = 55f, baseHitstunFrames = 16, hitstopFrames = 8, isHeavy = true };
                m.haptic = HapticStrength.Heavy; m.shakeAmplitude = 0.35f; m.shakeDuration = 0.22f;
            });

            return GetOrCreate<Moveset>(F("Brick_Moveset"), ms =>
            {
                ms.groundAttack = jab; ms.groundUpAttack = pillar; ms.airAttack = elbow;
                ms.groundSpecial = charge; ms.airSpecial = null; ms.abilityImpact = landslide;
            });
        }

        public const string LumaMovesFolder = "Assets/_Project/Data/Moves/Luma";

        /// <summary>
        /// Luma, the magnet-glove inventor: average weight, pulls foes in and lays traps. Playable from Phase 2.2,
        /// unlocked by clearing Sunspire Meadows. Upgrades a Luma asset made by an older build (locked, no kit).
        /// </summary>
        public static CharacterDefinition GetOrCreateLuma()
        {
            EditorUtil.EnsureFolder(CharactersFolder);
            var kit = GetOrCreate<LumaKitDefinition>($"{CharactersFolder}/LumaKit.asset", _ => { });
            var luma = GetOrCreate<CharacterDefinition>($"{CharactersFolder}/Luma.asset", d =>
            {
                d.id = "luma";
                d.displayName = "Luma";
                d.placeholderColor = new Color32(240, 132, 52, 255);
                ApplyLumaStats(d);
            });
            if (luma.kit == null) { ApplyLumaStats(luma); luma.kit = kit; }
            if (string.IsNullOrEmpty(luma.tagline)) luma.tagline = "Inventor. Her gloves pull sparks out of thin air.";
            if (string.IsNullOrEmpty(luma.lore))
                luma.lore = "Luma repairs Tickworks relays for a living and builds things she shouldn't in her spare time. Her magnet " +
                            "gloves can drag loose energy shards toward her or lock them into a barrier for a few precious seconds.";
            if (luma.bodySprite == null) luma.bodySprite = PlaceholderArt.Luma();
            if (luma.moveset == null) luma.moveset = GetOrCreateLumaMoveset();
            luma.playableInThisBuild = true;
            luma.unlockedByDefault = false;
            if (string.IsNullOrEmpty(luma.unlockByClearingLevelId)) luma.unlockByClearingLevelId = "sq_sunspire_test";
            if (string.IsNullOrEmpty(luma.unlockHint)) luma.unlockHint = "Clear Sunspire Meadows";
            EditorUtility.SetDirty(luma);
            return luma;
        }

        static void ApplyLumaStats(CharacterDefinition d)
        {
            d.maxHealth = 5;
            d.weight = 0.95f;
            d.armorPercent = 0f;
            d.guardPips = 3;
            d.movement = new MovementStats { runSpeed = 7.2f, jumpHeight = 3.0f, timeToApex = 0.37f };
        }

        /// <summary>Luma's magnet-glove moves. Original names and starting values; tune in the Inspector.</summary>
        public static Moveset GetOrCreateLumaMoveset()
        {
            EditorUtil.EnsureFolder(LumaMovesFolder);
            string F(string n) => $"{LumaMovesFolder}/{n}.asset";
            var spark = new Color32(120, 200, 255, 255);

            var swing = GetOrCreate<MoveDefinition>(F("Luma_VoltageSwing"), m =>
            {
                m.id = "luma_voltage_swing"; m.displayName = "Voltage Swing";
                m.description = "A charged backhand with the glove. Launches Exposed foes.";
                m.frames = new FrameData(6, 4, 16, 6);
                m.hitboxOffset = new Vector2(0.85f, 0.05f); m.hitboxSize = new Vector2(1.2f, 0.9f);
                m.hit = new HitData { damage = 2, pipDamage = 2, baseKnockback = 7.5f, exposedMultiplier = 3f,
                                      angleDegrees = 35f, baseHitstunFrames = 14, hitstopFrames = 6, isHeavy = true };
                m.lungeSpeed = 3f; m.haptic = HapticStrength.Medium; m.shakeAmplitude = 0.12f;
            });
            var tap = GetOrCreate<MoveDefinition>(F("Luma_WrenchTap"), m =>
            {
                m.id = "luma_wrench_tap"; m.displayName = "Wrench Tap";
                m.description = "A quick tap with her pocket wrench. Press Attack again for Voltage Swing.";
                m.frames = new FrameData(3, 3, 11, 8);
                m.hitboxOffset = new Vector2(0.7f, 0.05f); m.hitboxSize = new Vector2(0.85f, 0.6f);
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 4f, exposedMultiplier = 1.5f,
                                      angleDegrees = 15f, baseHitstunFrames = 10, hitstopFrames = 3, isHeavy = false };
                m.lungeSpeed = 2.5f;
            });
            if (tap.followUp == null) { tap.followUp = swing; EditorUtility.SetDirty(tap); }

            var flick = GetOrCreate<MoveDefinition>(F("Luma_ArcFlick"), m =>
            {
                m.id = "luma_arc_flick"; m.displayName = "Arc Flick";
                m.description = "Up + Attack. A spark jumps between her gloves overhead.";
                m.frames = new FrameData(4, 4, 14, 0);
                m.hitboxOffset = new Vector2(0.2f, 1.0f); m.hitboxSize = new Vector2(1.3f, 0.9f);
                m.hit = new HitData { damage = 2, pipDamage = 1, baseKnockback = 7.5f, exposedMultiplier = 2.5f,
                                      angleDegrees = 78f, baseHitstunFrames = 13, hitstopFrames = 5, isHeavy = true };
                m.lungeSpeed = 0f; m.haptic = HapticStrength.Medium;
            });
            var spin = GetOrCreate<MoveDefinition>(F("Luma_StaticSpin"), m =>
            {
                m.id = "luma_static_spin"; m.displayName = "Static Spin";
                m.description = "Air Attack. She spins with gloves out, zapping both sides.";
                m.frames = new FrameData(3, 8, 10, 0);
                m.shape = HitShape.Circle; m.hitboxOffset = Vector2.zero; m.hitboxRadius = 1.0f;
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 4.5f, exposedMultiplier = 2f,
                                      angleDegrees = 30f, baseHitstunFrames = 10, hitstopFrames = 3, isHeavy = false };
                m.rootedOnGround = false; m.endsOnLanding = true;
            });
            var tether = GetOrCreate<MoveDefinition>(F("Luma_MagnetTether"), m =>
            {
                m.id = "luma_magnet_tether"; m.displayName = "Magnet Tether";
                m.description = "Ground Special. A magnetic bolt that yanks whoever it hits toward Luma.";
                m.frames = new FrameData(9, 1, 20, 0);
                // Angle 170 = backward and slightly up: the target is pulled toward the attacker.
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 8f, exposedMultiplier = 1.2f,
                                      angleDegrees = 170f, baseHitstunFrames = 18, hitstopFrames = 4, isHeavy = false };
                m.spawnsProjectile = true; m.projectileSpeed = 15f; m.projectileLifetime = 0.45f;
                m.projectileRadius = 0.3f; m.projectileColor = spark; m.lungeSpeed = 0f;
            });
            var coil = GetOrCreate<MoveDefinition>(F("Luma_SparkCoil"), m =>
            {
                m.id = "luma_spark_coil"; m.displayName = "Spark Coil";
                m.description = "Air Special. Drops a coil that arms on the ground and zaps the first foe to touch it. One at a time.";
                m.frames = new FrameData(6, 1, 16, 0);
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 6f, exposedMultiplier = 2f,
                                      angleDegrees = 80f, baseHitstunFrames = 30, hitstopFrames = 5, isHeavy = false };
                m.spawnsTrap = true; m.projectileSpawnOffset = new Vector2(0.2f, -0.4f); m.projectileColor = spark;
                m.trapArmDelay = 0.35f; m.trapLifetime = 6f; m.trapRadius = 0.55f; m.trapFallSpeed = 14f;
                m.rootedOnGround = false; m.endsOnLanding = false; m.lungeSpeed = 0f;
            });

            return GetOrCreate<Moveset>(F("Luma_Moveset"), ms =>
            {
                ms.groundAttack = tap; ms.groundUpAttack = flick; ms.airAttack = spin;
                ms.groundSpecial = tether; ms.airSpecial = coil; ms.abilityImpact = null;
            });
        }

        public const string RexMovesFolder = "Assets/_Project/Data/Moves/RexRollo";

        /// <summary>
        /// Rex Rollo, the roller-skating lizard: fast, slippery, hits harder the faster he goes. Playable from Phase 2.3,
        /// unlocked by clearing Sunspire Meadows with its secret found. Upgrades a locked asset from older builds.
        /// </summary>
        public static CharacterDefinition GetOrCreateRex()
        {
            EditorUtil.EnsureFolder(CharactersFolder);
            var kit = GetOrCreate<RexKitDefinition>($"{CharactersFolder}/RexKit.asset", _ => { });
            var rex = GetOrCreate<CharacterDefinition>($"{CharactersFolder}/RexRollo.asset", d =>
            {
                d.id = "rex_rollo";
                d.displayName = "Rex Rollo";
                d.placeholderColor = new Color32(92, 196, 96, 255);
                ApplyRexStats(d);
            });
            if (rex.kit == null) { ApplyRexStats(rex); rex.kit = kit; }
            if (string.IsNullOrEmpty(rex.tagline)) rex.tagline = "Roller-skating lizard. Brakes are optional.";
            if (string.IsNullOrEmpty(rex.lore))
                rex.lore = "Rex Rollo learned to skate on the brass rails that ring the floating islands and never saw a reason to stop. " +
                           "The faster he goes, the harder he hits; the trick is getting him to turn.";
            if (rex.bodySprite == null) rex.bodySprite = PlaceholderArt.RexRollo();
            if (rex.moveset == null) rex.moveset = GetOrCreateRexMoveset();
            rex.playableInThisBuild = true;
            rex.unlockedByDefault = false;
            if (string.IsNullOrEmpty(rex.unlockByClearingLevelId)) { rex.unlockByClearingLevelId = "sq_sunspire_test"; rex.unlockMinSecrets = 1; }
            if (string.IsNullOrEmpty(rex.unlockHint)) rex.unlockHint = "Clear Sunspire Meadows and find its secret";
            EditorUtility.SetDirty(rex);
            return rex;
        }

        static void ApplyRexStats(CharacterDefinition d)
        {
            d.maxHealth = 5;
            d.weight = 1.0f;
            d.armorPercent = 0f;
            d.guardPips = 3;
            d.dodgeSpeed = 12.5f; d.dodgeDuration = 0.24f;
            d.movement = new MovementStats
            {
                runSpeed = 9.5f, groundAcceleration = 28f, groundDeceleration = 60f,
                coastDeceleration = 7f,   // glides when you let go
                skidDeceleration = 45f,   // turning around takes a skid
                airAcceleration = 30f, airDeceleration = 10f,
                jumpHeight = 3.0f, timeToApex = 0.36f,
                wallRideTime = 0.45f, wallRideSpeed = 6.5f, wallRideMinSpeed = 4f, wallJumpSpeedX = 9f
            };
        }

        /// <summary>Rex Rollo's skate moves. Original names and starting values; tune in the Inspector.</summary>
        public static Moveset GetOrCreateRexMoveset()
        {
            EditorUtil.EnsureFolder(RexMovesFolder);
            string F(string n) => $"{RexMovesFolder}/{n}.asset";

            var whip = GetOrCreate<MoveDefinition>(F("Rex_TailWhip"), m =>
            {
                m.id = "rex_tail_whip"; m.displayName = "Tail Whip";
                m.description = "A full spin that cracks his tail around both sides. Launches Exposed foes.";
                m.frames = new FrameData(6, 5, 16, 6);
                m.shape = HitShape.Circle; m.hitboxOffset = new Vector2(0f, -0.1f); m.hitboxRadius = 1.1f;
                m.hit = new HitData { damage = 2, pipDamage = 2, baseKnockback = 7.5f, exposedMultiplier = 3f,
                                      angleDegrees = 40f, baseHitstunFrames = 14, hitstopFrames = 6, isHeavy = true };
                m.lungeSpeed = 1.5f; m.haptic = HapticStrength.Medium; m.shakeAmplitude = 0.12f;
            });
            var kick = GetOrCreate<MoveDefinition>(F("Rex_SkateKick"), m =>
            {
                m.id = "rex_skate_kick"; m.displayName = "Skate Kick";
                m.description = "A quick wheel-first kick. Press Attack again for Tail Whip.";
                m.frames = new FrameData(3, 3, 12, 8);
                m.hitboxOffset = new Vector2(0.75f, -0.2f); m.hitboxSize = new Vector2(0.95f, 0.6f);
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 4f, exposedMultiplier = 1.5f,
                                      angleDegrees = 20f, baseHitstunFrames = 10, hitstopFrames = 3, isHeavy = false };
                m.lungeSpeed = 3.5f;
            });
            if (kick.followUp == null) { kick.followUp = whip; EditorUtility.SetDirty(kick); }

            var flip = GetOrCreate<MoveDefinition>(F("Rex_FlipKick"), m =>
            {
                m.id = "rex_flip_kick"; m.displayName = "Flip Kick";
                m.description = "Up + Attack. A backflip that kicks straight up.";
                m.frames = new FrameData(5, 4, 15, 0);
                m.hitboxOffset = new Vector2(0.3f, 1.0f); m.hitboxSize = new Vector2(1.2f, 1.0f);
                m.hit = new HitData { damage = 2, pipDamage = 1, baseKnockback = 8f, exposedMultiplier = 2.5f,
                                      angleDegrees = 82f, baseHitstunFrames = 14, hitstopFrames = 5, isHeavy = true };
                m.lungeSpeed = 0f; m.haptic = HapticStrength.Medium;
            });
            var wheel = GetOrCreate<MoveDefinition>(F("Rex_WheelSpin"), m =>
            {
                m.id = "rex_wheel_spin"; m.displayName = "Wheel Spin";
                m.description = "Air Attack. Spins with skates out, clipping both sides.";
                m.frames = new FrameData(3, 7, 10, 0);
                m.shape = HitShape.Circle; m.hitboxOffset = Vector2.zero; m.hitboxRadius = 0.95f;
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 5f, exposedMultiplier = 2f,
                                      angleDegrees = 30f, baseHitstunFrames = 10, hitstopFrames = 3, isHeavy = false };
                m.rootedOnGround = false; m.endsOnLanding = true;
            });
            var ram = GetOrCreate<MoveDefinition>(F("Rex_MomentumRam"), m =>
            {
                m.id = "rex_momentum_ram"; m.displayName = "Momentum Ram";
                m.description = "Ground Special. Tucks and rams forward. The faster Rex was skating, the harder it hits " +
                                "(up to 2.5x knockback, +1 damage and Guard Pip at top speed).";
                m.frames = new FrameData(4, 10, 16, 0);
                m.hitboxOffset = new Vector2(0.75f, 0f); m.hitboxSize = new Vector2(1.0f, 1.1f);
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 5f, exposedMultiplier = 2.5f,
                                      angleDegrees = 30f, baseHitstunFrames = 12, hitstopFrames = 5, isHeavy = true };
                m.lungeSpeed = 10f; m.speedBonus = 1.5f; m.haptic = HapticStrength.Medium; m.shakeAmplitude = 0.15f;
            });
            var grind = GetOrCreate<MoveDefinition>(F("Rex_GrindDropShockwave"), m =>
            {
                m.id = "rex_grind_drop_shockwave"; m.displayName = "Grind Drop Shockwave";
                m.description = "The landing of Grind Drop. Small but quick.";
                m.frames = new FrameData(0, 1, 0, 0);
                m.shape = HitShape.Circle; m.hitboxOffset = new Vector2(0f, -0.3f); m.hitboxRadius = 1.3f;
                m.hit = new HitData { damage = 2, pipDamage = 2, baseKnockback = 8f, exposedMultiplier = 2.8f,
                                      angleDegrees = 60f, baseHitstunFrames = 14, hitstopFrames = 6, isHeavy = true };
                m.haptic = HapticStrength.Heavy; m.shakeAmplitude = 0.25f; m.shakeDuration = 0.18f;
            });

            return GetOrCreate<Moveset>(F("Rex_Moveset"), ms =>
            {
                ms.groundAttack = kick; ms.groundUpAttack = flip; ms.airAttack = wheel;
                ms.groundSpecial = ram; ms.airSpecial = null; ms.abilityImpact = grind;
            });
        }

        public const string NovaMovesFolder = "Assets/_Project/Data/Moves/Nova";

        /// <summary>Nova's comet-staff moves. All names and numbers are original starting values; tune in the Inspector.</summary>
        public static Moveset GetOrCreateNovaMoveset()
        {
            EditorUtil.EnsureFolder(NovaMovesFolder);
            string F(string n) => $"{NovaMovesFolder}/{n}.asset";

            var sweep = GetOrCreate<MoveDefinition>(F("Nova_CometSweep"), m =>
            {
                m.id = "nova_comet_sweep"; m.displayName = "Comet Sweep";
                m.description = "A wide follow-up swing that trails starlight. Launches Exposed foes.";
                m.frames = new FrameData(5, 4, 16, 6);
                m.hitboxOffset = new Vector2(0.85f, 0f); m.hitboxSize = new Vector2(1.3f, 0.9f);
                m.hit = new HitData { damage = 2, pipDamage = 2, baseKnockback = 7.5f, exposedMultiplier = 3f,
                                      angleDegrees = 35f, baseHitstunFrames = 14, hitstopFrames = 6, isHeavy = true };
                m.lungeSpeed = 4f; m.haptic = HapticStrength.Medium; m.shakeAmplitude = 0.12f;
            });
            var jab = GetOrCreate<MoveDefinition>(F("Nova_CourierJab"), m =>
            {
                m.id = "nova_courier_jab"; m.displayName = "Courier Jab";
                m.description = "A quick staff poke. Press Attack again to follow with Comet Sweep.";
                m.frames = new FrameData(3, 3, 12, 8);
                m.hitboxOffset = new Vector2(0.75f, 0.05f); m.hitboxSize = new Vector2(0.9f, 0.6f);
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 4f, exposedMultiplier = 1.5f,
                                      angleDegrees = 15f, baseHitstunFrames = 10, hitstopFrames = 3, isHeavy = false };
                m.lungeSpeed = 3f;
            });
            if (jab.followUp == null) { jab.followUp = sweep; EditorUtility.SetDirty(jab); }

            var rising = GetOrCreate<MoveDefinition>(F("Nova_RisingArc"), m =>
            {
                m.id = "nova_rising_arc"; m.displayName = "Rising Arc";
                m.description = "Up + Attack. An overhead arc that pops foes into the air.";
                m.frames = new FrameData(5, 4, 14, 0);
                m.hitboxOffset = new Vector2(0.3f, 1.0f); m.hitboxSize = new Vector2(1.4f, 0.9f);
                m.hit = new HitData { damage = 2, pipDamage = 1, baseKnockback = 8f, exposedMultiplier = 2.5f,
                                      angleDegrees = 80f, baseHitstunFrames = 14, hitstopFrames = 5, isHeavy = true };
                m.lungeSpeed = 0f; m.haptic = HapticStrength.Medium;
            });
            var tailspin = GetOrCreate<MoveDefinition>(F("Nova_Tailspin"), m =>
            {
                m.id = "nova_tailspin"; m.displayName = "Tailspin";
                m.description = "Air Attack. Nova spins her staff around her body, hitting both sides.";
                m.frames = new FrameData(3, 6, 10, 0);
                m.shape = HitShape.Circle; m.hitboxOffset = Vector2.zero; m.hitboxRadius = 0.95f;
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 5f, exposedMultiplier = 2f,
                                      angleDegrees = 30f, baseHitstunFrames = 10, hitstopFrames = 3, isHeavy = false };
                m.rootedOnGround = false; m.endsOnLanding = true;
            });
            var bolt = GetOrCreate<MoveDefinition>(F("Nova_CometBolt"), m =>
            {
                m.id = "nova_comet_bolt"; m.displayName = "Comet Bolt";
                m.description = "Ground Special. Flicks a spark of comet-light straight ahead.";
                m.frames = new FrameData(8, 1, 18, 0);
                m.hit = new HitData { damage = 1, pipDamage = 1, baseKnockback = 3.5f, exposedMultiplier = 1.5f,
                                      angleDegrees = 10f, baseHitstunFrames = 8, hitstopFrames = 3, isHeavy = false };
                m.spawnsProjectile = true; m.projectileSpeed = 13f; m.projectileLifetime = 0.55f;
                m.projectileRadius = 0.28f; m.lungeSpeed = 0f;
            });
            var meteor = GetOrCreate<MoveDefinition>(F("Nova_MeteorShockwave"), m =>
            {
                m.id = "nova_meteor_shockwave"; m.displayName = "Meteor Shockwave";
                m.description = "The landing blast of Meteor Drop. Pushes everything nearby away.";
                m.frames = new FrameData(0, 1, 0, 0);
                m.shape = HitShape.Circle; m.hitboxOffset = new Vector2(0f, -0.3f); m.hitboxRadius = 1.6f;
                m.hit = new HitData { damage = 3, pipDamage = 2, baseKnockback = 9f, exposedMultiplier = 3f,
                                      angleDegrees = 60f, baseHitstunFrames = 16, hitstopFrames = 7, isHeavy = true };
                m.haptic = HapticStrength.Heavy; m.shakeAmplitude = 0.3f; m.shakeDuration = 0.2f;
            });

            var set = GetOrCreate<Moveset>(F("Nova_Moveset"), ms =>
            {
                ms.groundAttack = jab; ms.groundUpAttack = rising; ms.airAttack = tailspin;
                ms.groundSpecial = bolt; ms.airSpecial = null; ms.abilityImpact = meteor;
            });
            return set;
        }

        internal static T GetOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                init(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            if (asset is EnemyDefinition enemy) PixelArt.ApplyEnemy(enemy); // animation frames (Art/Pixel/Enemies)
            return asset;
        }
    }
}
