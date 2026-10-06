using PKR.Core;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Creates default data assets if missing. Never overwrites tuning you've changed in the Inspector;
    /// only fills empty art references.
    /// </summary>
    public static class DataAssets
    {
        public const string CharactersFolder = "Assets/_Project/Data/Characters";

        [MenuItem("PKR/Create Default Data Assets", priority = 3)]
        public static void CreateAll()
        {
            GetOrCreateRoster();
            GetOrCreateCogBeetle();
            GetOrCreateSpringTick();
            GetOrCreateSunspireTestLevel();
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
            return roster;
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

        public static LevelDefinition GetOrCreateSunspireTestLevel()
        {
            EditorUtil.EnsureFolder(LevelsFolder);
            return GetOrCreate<LevelDefinition>($"{LevelsFolder}/SQ_SunspireMeadows_Test.asset", l =>
            {
                l.id = "sq_sunspire_test"; l.displayName = "Sunspire Meadows"; l.biomeName = "Sunspire Meadows";
                l.sceneName = SceneIds.StoryTest; l.parTimeSeconds = 120f;
                l.codexEntry = "Grassland islands stitched together by the Tickworks' oldest machinery. " +
                               "The brass is warm to the touch, and the gears hum just below hearing.";
                l.skyColor = new Color32(255, 200, 150, 255);
            });
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

        static T GetOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
