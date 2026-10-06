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
            GetOrCreateNova();
            AssetDatabase.SaveAssets();
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
