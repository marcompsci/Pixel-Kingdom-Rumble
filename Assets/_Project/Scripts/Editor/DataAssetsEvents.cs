using PKR.Core;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>Maze levels, Holiday Mode and Halloween Mode data: levels, holiday critters and the zombie cast.</summary>
    public static partial class DataAssets
    {
        // ---- mazes (one per Story world) ---------------------------------------------------------------------------

        public static LevelDefinition GetOrCreateGearworkMaze() => GetOrCreateMapLevel(
            "SQ_GearworkMaze", "sq_gearwork_maze", "Gearwork Maze", "Tickworks Engine Hall", SceneIds.GearworkMaze,
            SceneIds.BossTest, "Maze", 140f, new Color32(60, 50, 70, 255),
            "The engine hall's service maze. Keys open the locked doors, floor switches swap the sun blocks for the moon " +
            "blocks, and the old presses still slam on schedule. Think fast, then move faster.");

        public static LevelDefinition GetOrCreateFrozenVault() => GetOrCreateMapLevel(
            "SQ_FrozenVault", "sq_frozen_vault", "The Frozen Vault", "Frostgear Peaks", SceneIds.FrozenVault,
            SceneIds.EmberWastes, "Maze", 160f, new Color32(140, 190, 230, 255),
            "A cold-storage vault cut into the glacier. Climb the switch tower for the first key, duck the saws in the " +
            "low corridor, time the four laser gates and slip under twin crushers to the vault door.");

        public static LevelDefinition GetOrCreateShatteredLabyrinth() => GetOrCreateMapLevel(
            "SQ_ShatteredLabyrinth", "sq_shattered_labyrinth", "The Shattered Labyrinth", "Shattered Crossing", SceneIds.ShatteredLabyrinth,
            SceneIds.FourfoldSovereign, "Maze", 180f, new Color32(40, 30, 64, 255),
            "The last maze before the Sovereign: lava bridges that exist in one colour at a time, a key you can only reach " +
            "by riding a crusher, and a gauntlet of saws and lasers at the end.");

        // ---- Story world 4: the Starfall Observatory ------------------------------------------------------------

        public static LevelDefinition GetOrCreateStarfallSteps() => GetOrCreateMapLevel(
            "SQ_StarfallSteps", "sq_starfall_steps", "Starfall Steps", "Starfall Observatory", SceneIds.StarfallSteps,
            SceneIds.OrreryGardens, "Platform", 190f, new Color32(26, 22, 64, 255),
            "The stairway to the observatory, built over nothing at all. Star updrafts carry you over the spike pits, " +
            "if you trust them. Comet wisps don't like visitors.");

        public static LevelDefinition GetOrCreateOrreryGardens() => GetOrCreateMapLevel(
            "SQ_OrreryGardens", "sq_orrery_gardens", "Orrery Gardens", "Starfall Observatory", SceneIds.OrreryGardens,
            SceneIds.CelestialMaze, "Platform", 210f, new Color32(30, 24, 70, 255),
            "Brass rings and drifting plates circle the observatory like moons. Lens golems keep the paths; the laser " +
            "telescopes still fire on the hour.");

        public static LevelDefinition GetOrCreateCelestialMaze() => GetOrCreateMapLevel(
            "SQ_CelestialMaze", "sq_celestial_maze", "The Celestial Maze", "Starfall Observatory", SceneIds.CelestialMaze,
            SceneIds.CometRun, "Maze", 170f, new Color32(24, 20, 60, 255),
            "The orrery's service maze: ride an updraft shaft to the first key, cross the sun and moon bridges, then the " +
            "laser hall under the old press.");

        public static LevelDefinition GetOrCreateCometRun() => GetOrCreateMapLevel(
            "SQ_CometRun", "sq_comet_run", "Comet Run", "Starfall Observatory", SceneIds.CometRun,
            SceneIds.OrrerySentinel, "Platform", 230f, new Color32(20, 16, 52, 255),
            "The last climb to the great orrery, with comets falling all around. Everything you've learned, all at once.");

        public static LevelDefinition GetOrCreateOrreryLevel() => GetOrCreateMapLevel(
            "SQ_OrrerySentinel", "sq_orrery_sentinel", "The Orrery Sentinel", "Starfall Observatory", SceneIds.OrrerySentinel,
            "", "Boss", 160f, new Color32(16, 12, 44, 255),
            "The keeper of the great orrery, the fastest machine in the isles. It fights like the Warden and the Sovereign, " +
            "only more so. Wait for the slam, then hit the core.");

        public static WorldDefinition GetOrCreateStarfallObservatory()
        {
            EditorUtil.EnsureFolder(LevelsFolder);
            var route = new[]
            {
                GetOrCreateStarfallSteps(), GetOrCreateOrreryGardens(), GetOrCreateCelestialMaze(), GetOrCreateCometRun(),
                GetOrCreateOrreryLevel()
            };
            var world = GetOrCreate<WorldDefinition>($"{LevelsFolder}/World_StarfallObservatory.asset", w =>
            {
                w.displayName = "Starfall Observatory";
                w.description = "Above the clouds the stars are falling. Climb the observatory and set the great orrery right.";
            });
            world.levels.Clear();
            world.levels.AddRange(route);
            EditorUtility.SetDirty(world);
            return world;
        }

        public static EnemyDefinition GetOrCreateCometWisp() => EventFoe("CometWisp", "enemy_comet_wisp", "Comet Wisp",
            "A tiny comet with a temper. Hovers in the dark and dives at anything that glows, which includes you.",
            EnemyBehavior.Flyer, new Vector2(0.7f, 0.6f), 1, 0f, 2,
            d => { d.aggroRange = 7f; d.swoopSpeed = 10f; d.swoopTime = 0.8f; d.swoopWindup = 0.45f; d.flyerReturnSpeed = 4.5f; d.swoopCooldown = 1.3f; d.weight = 0.6f; });

        public static EnemyDefinition GetOrCreateLensGolem() => EventFoe("LensGolem", "enemy_lens_golem", "Lens Golem",
            "An old observatory telescope that grew legs. Slow and sturdy; three good hits crack the lens.",
            EnemyBehavior.Walker, new Vector2(0.8f, 0.85f), 4, 0.26f, 3, d => { d.weight = 1.6f; d.contactHit = Bite(1, 8f); });

        public static EnemyDefinition GetOrCreateStarHopper() => EventFoe("StarHopper", "enemy_star_hopper", "Star Hopper",
            "A fallen star that refuses to stay down. Bounces at you from surprising distances.",
            EnemyBehavior.Hopper, new Vector2(0.7f, 0.7f), 2, 0f, 3, d => { d.hopCooldown = 1.1f; d.hopRange = 8f; d.hopDrift = 0.7f; });

        // ---- Holiday Mode -----------------------------------------------------------------------------------------

        static LevelDefinition EventLevel(string file, string id, string name, string biome, string scene, float par, Color32 sky,
                                          string treats, float timeLimit, string codex)
        {
            var lvl = GetOrCreateMapLevel(file, id, name, biome, scene, "", "Event", par, sky, codex);
            if (!lvl.isEvent || lvl.collectibleName != treats || !Mathf.Approximately(lvl.timeLimitSeconds, timeLimit))
            {
                lvl.isEvent = true;
                lvl.collectibleName = treats;
                lvl.timeLimitSeconds = timeLimit;
                EditorUtility.SetDirty(lvl);
            }
            return lvl;
        }

        public static LevelDefinition GetOrCreateWinterGiftRun() => EventLevel(
            "EV_WinterGiftRun", "ev_winter_gift_run", "Winter Gift Run", "Winter Village", SceneIds.WinterGiftRun, 150f,
            new Color32(150, 190, 235, 255), "GIFTS", 0f,
            "The village's gifts blew out of the toy workshop in a snowstorm. Gather them up before the snowmen do.");

        public static LevelDefinition GetOrCreateSweetheartSprint() => EventLevel(
            "EV_SweetheartSprint", "ev_sweetheart_sprint", "Sweetheart Sprint", "Sweetheart Gardens", SceneIds.SweetheartSprint, 150f,
            new Color32(255, 196, 220, 255), "HEARTS", 0f,
            "The rose gardens are full of sun and moon blocks that swap every time a switch is pressed. Cupid-bots patrol the sky.");

        public static LevelDefinition GetOrCreateHarvestHustle() => EventLevel(
            "EV_HarvestHustle", "ev_harvest_hustle", "Harvest Hustle", "Harvest Hollow", SceneIds.HarvestHustle, 160f,
            new Color32(255, 200, 140, 255), "PIES", 0f,
            "Harvest day at the mill: the belts are running, the cider presses are pounding and the turkeys have escaped.");

        public static LevelDefinition GetOrCreateCountdownClimb() => EventLevel(
            "EV_CountdownClimb", "ev_countdown_climb", "Countdown Climb", "Midnight City", SceneIds.CountdownClimb, 150f,
            new Color32(24, 22, 56, 255), "STARS", 180f,
            "The party is on the tallest rooftop in town and the countdown has started. Climb before the clock strikes midnight!");

        public static LevelDefinition GetOrCreateHauntedEscape() => EventLevel(
            "EV_HauntedEscape", "ev_haunted_escape", "Haunted Escape", "Haunted Hollow", SceneIds.HauntedEscape, 170f,
            new Color32(28, 22, 48, 255), "CANDY", 0f,
            "Something woke the graveyard. Run! The horde is right behind you, and the only way out is through the crypt.");

        public static LevelDefinition GetOrCreateZombieSurvivalLevel() => EventLevel(
            "EV_ZombieSurvival", "ev_zombie_survival", "Zombie Survival", "Moonlit Manor", SceneIds.ZombieSurvival, 600f,
            new Color32(22, 18, 40, 255), "", 0f,
            "Hold the manor through endless rounds of zombies. Guns on the walls, a mystery crate, perk shrines, the upgrade " +
            "forge, and something about those jack-o'-lanterns...");

        // ---- holiday critters and the zombie cast -------------------------------------------------------------------

        static EnemyDefinition EventFoe(string file, string id, string name, string codex, EnemyBehavior behavior, Vector2 size,
                                        int hp, float speed, int drop, System.Action<EnemyDefinition> extra = null)
        {
            EditorUtil.EnsureFolder(EnemiesFolder);
            var e = GetOrCreate<EnemyDefinition>($"{EnemiesFolder}/{file}.asset", d =>
            {
                d.id = id; d.displayName = name; d.codexEntry = codex;
                d.bodySize = size; d.maxHealth = hp; d.weight = 1f; d.moveSpeed = speed;
                d.behavior = behavior; d.shardDrop = drop;
                extra?.Invoke(d);
            });
            if (e.sprite == null)
            {
                // The first idle frame doubles as the still sprite.
                string art = PixelArt.EnemyArtName(e);
                e.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArt.Root}/Enemies/{art}/{art}_idle_0.png");
                EditorUtility.SetDirty(e);
            }
            return e;
        }

        static HitData Bite(int damage, float knock) => new HitData
        {
            damage = damage, pipDamage = 1, baseKnockback = knock, exposedMultiplier = 1f, angleDegrees = 45f,
            baseHitstunFrames = 18, hitstopFrames = 4, isHeavy = damage > 1
        };

        public static EnemyDefinition GetOrCreateSnowman() => EventFoe("Snowman", "enemy_snowman", "Grumpy Snowman",
            "Built by the village children, brought to life by a stray spark from the toy workshop, and very cross about it.",
            EnemyBehavior.Walker, new Vector2(0.75f, 0.95f), 2, 0.3f, 2);

        public static EnemyDefinition GetOrCreateCupidBot() => EventFoe("CupidBot", "enemy_cupid_bot", "Cupid-Bot",
            "A heart-shaped courier drone from the rose gardens. It still dives at anyone it thinks needs a hug.",
            EnemyBehavior.Flyer, new Vector2(0.8f, 0.6f), 1, 0f, 2,
            d => { d.aggroRange = 6.5f; d.swoopSpeed = 8.5f; d.swoopTime = 0.8f; d.swoopWindup = 0.5f; d.flyerReturnSpeed = 3.8f; d.swoopCooldown = 1.6f; d.weight = 0.6f; });

        public static EnemyDefinition GetOrCreateTurkey() => EventFoe("Turkey", "enemy_turkey", "Runaway Turkey",
            "Escaped the harvest pen and refuses to go back. Hops at anyone who gets close.",
            EnemyBehavior.Hopper, new Vector2(0.75f, 0.75f), 2, 0f, 3,
            d => { d.hopCooldown = 1.3f; d.hopRange = 7f; d.hopDrift = 0.6f; });

        public static EnemyDefinition GetOrCreateFirecracker() => EventFoe("Firecracker", "enemy_firecracker", "Fizzer",
            "A firecracker that lit itself early and went for a walk. It goes off when it's knocked down, so hit and step back.",
            EnemyBehavior.Walker, new Vector2(0.55f, 0.95f), 2, 0.45f, 3, d => { d.burstOnDeath = true; });

        public static EnemyDefinition GetOrCreateZombieShambler() => EventFoe("ZombieShambler", "enemy_zombie_shambler", "Shambler",
            "Slow, hungry and never alone. Arms out, always walking straight at you.",
            EnemyBehavior.Chaser, new Vector2(0.62f, 1.2f), 2, 0.32f, 1, d => { d.contactHit = Bite(1, 7f); });

        public static EnemyDefinition GetOrCreateZombieRunner() => EventFoe("ZombieRunner", "enemy_zombie_runner", "Runner",
            "Fresh, fast and very rude. Runners sprint across the yard and hop anything in their way.",
            EnemyBehavior.Chaser, new Vector2(0.6f, 1.2f), 2, 0.68f, 1, d => { d.contactHit = Bite(1, 8f); d.weight = 0.9f; });

        public static EnemyDefinition GetOrCreateZombieBloater() => EventFoe("ZombieBloater", "enemy_zombie_bloater", "Bloater",
            "Swollen with swamp gas. It bursts when it goes down, so finish it from range.",
            EnemyBehavior.Chaser, new Vector2(0.78f, 1.2f), 4, 0.26f, 1, d => { d.burstOnDeath = true; d.contactHit = Bite(1, 7f); d.weight = 1.4f; });

        public static EnemyDefinition GetOrCreateZombieSpitter() => EventFoe("ZombieSpitter", "enemy_zombie_spitter", "Spitter",
            "Hangs back and lobs glowing goo. Close the gap or keep moving.",
            EnemyBehavior.Chaser, new Vector2(0.6f, 1.2f), 3, 0.32f, 1, d => { d.spitEvery = 2.6f; d.contactHit = Bite(1, 6f); });

        public static EnemyDefinition GetOrCreateZombieBrute() => EventFoe("ZombieBrute", "enemy_zombie_brute", "Brute",
            "A wall of muscle in a bandana. Hits for two hearts and shrugs off the small guns.",
            EnemyBehavior.Chaser, new Vector2(0.85f, 1.35f), 9, 0.34f, 2, d => { d.contactHit = Bite(2, 11f); d.weight = 2f; });

        public static EnemyDefinition GetOrCreateZombieBat() => EventFoe("ZombieBat", "enemy_zombie_bat", "Grave Bat",
            "Roosts on the manor roof and dives at anyone in the yard. Aim up!",
            EnemyBehavior.Flyer, new Vector2(0.75f, 0.5f), 1, 0f, 1,
            d => { d.aggroRange = 15f; d.swoopSpeed = 9f; d.swoopTime = 1.1f; d.swoopWindup = 0.45f; d.flyerReturnSpeed = 4f; d.swoopCooldown = 1.4f; d.weight = 0.5f; });

        // ---- October 2026 chibi foes ----------------------------------------------------------------------------
        public static EnemyDefinition GetOrCreateGearGoon() => EventFoe("GearGoon", "enemy_gear_goon", "Gear Goon",
            "Tickworks maintenance crew who decided breaking things is more fun than fixing them. Big wrench, bigger attitude.",
            EnemyBehavior.Walker, new Vector2(0.8f, 1.1f), 4, 0.32f, 3, d => { d.contactHit = Bite(1, 8f); d.weight = 1.4f; });

        public static EnemyDefinition GetOrCreatePrankBandit() => EventFoe("PrankBandit", "enemy_prank_bandit", "Prank Bandit",
            "A masked mischief-maker on spring shoes. Bounces at you, steals nothing, laughs a lot.",
            EnemyBehavior.Hopper, new Vector2(0.6f, 0.9f), 2, 0f, 3, d => { d.hopCooldown = 1.2f; d.hopRange = 7.5f; d.hopDrift = 0.65f; });

        public static EnemyDefinition GetOrCreateHexHatter() => EventFoe("HexHatter", "enemy_hex_hatter", "Hex Hatter",
            "A stage magician whose act got out of hand. Throws enchanted cards from a distance, then follows you.",
            EnemyBehavior.Chaser, new Vector2(0.62f, 1.2f), 3, 0.3f, 4,
            d => { d.spitEvery = 2.4f; d.chaseRange = 8f; d.contactHit = Bite(1, 7f); });

        public static EnemyDefinition GetOrCreatePumpkinKing() => EventFoe("PumpkinKing", "enemy_pumpkin_king", "The Pumpkin King",
            "Every fifth night he climbs out of the patch to lead the horde. Huge, tough and furious. Bring the big guns.",
            EnemyBehavior.Chaser, new Vector2(1f, 1.6f), 40, 0.42f, 10, d => { d.contactHit = Bite(2, 13f); d.weight = 2.5f; });
    }
}
