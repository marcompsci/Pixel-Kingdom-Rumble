using System;
using System.Collections.Generic;
using PKR.Core;
using UnityEditor;
using UnityEngine;
using Look = PKR.EditorTools.FighterArt.Look;
using B = PKR.EditorTools.FighterArt.Build;
using Hd = PKR.EditorTools.FighterArt.Head;
using Ex = PKR.EditorTools.FighterArt.Extra;

namespace PKR.EditorTools
{
    /// <summary>
    /// Phase 3.1: ten free, original fighters for Versus (and every other mode). Each has its own look, stats,
    /// movement kit and moves: a three-hit Attack combo with a SPECIAL finisher, Up + Attack, Air Attack, neutral,
    /// side and down Specials, an air dodge and (most) a dive. Starting values only; tune the assets in the
    /// Inspector. Existing assets are never overwritten.
    /// </summary>
    public static class VersusFighters
    {
        public const string Folder = "Assets/_Project/Data/Characters/Versus";
        public const string MovesRoot = "Assets/_Project/Data/Moves/Versus";

        public enum Kit { Dash, Guard, Hop, Boost }

        // ---- Hit helpers -------------------------------------------------------------------------

        static HitData L(int dmg, float kb, float angle, int stun = 11, int stop = 3) => new HitData
        {
            damage = dmg, pipDamage = 1, baseKnockback = kb, exposedMultiplier = 1.6f,
            angleDegrees = angle, baseHitstunFrames = stun, hitstopFrames = stop, isHeavy = false
        };

        static HitData H(int dmg, float kb, float angle, int stun = 15, int stop = 6, int pips = 2) => new HitData
        {
            damage = dmg, pipDamage = pips, baseKnockback = kb, exposedMultiplier = 3f,
            angleDegrees = angle, baseHitstunFrames = stun, hitstopFrames = stop, isHeavy = true
        };

        static FrameData Fd(int s, int a, int r, int c = 0) => new FrameData(s, a, r, c);

        /// <summary>Creates one fighter's move assets under MovesRoot/heroFolder.</summary>
        class Book
        {
            readonly string _folder, _prefix;
            public Book(string heroFolder, string prefix)
            {
                _folder = $"{MovesRoot}/{heroFolder}";
                _prefix = prefix;
                EditorUtil.EnsureFolder(_folder);
            }

            MoveDefinition Make(string key, string name, string desc, Action<MoveDefinition> init) =>
                DataAssets.GetOrCreate<MoveDefinition>($"{_folder}/{_prefix}_{key}.asset", m =>
                {
                    m.id = $"{_prefix.ToLowerInvariant()}_{key.ToLowerInvariant()}";
                    m.displayName = name;
                    m.description = desc;
                    init(m);
                });

            public MoveDefinition Strike(string key, string name, string desc, FrameData f, Vector2 off, Vector2 size, HitData h,
                                         float lunge = 2f, Action<MoveDefinition> x = null) => Make(key, name, desc, m =>
            {
                m.frames = f; m.shape = HitShape.Box; m.hitboxOffset = off; m.hitboxSize = size; m.hit = h; m.lungeSpeed = lunge;
                m.haptic = h.isHeavy ? HapticStrength.Medium : HapticStrength.Light;
                if (h.isHeavy) m.shakeAmplitude = 0.12f;
                x?.Invoke(m);
            });

            public MoveDefinition Ring(string key, string name, string desc, FrameData f, Vector2 off, float radius, HitData h,
                                       Action<MoveDefinition> x = null) => Make(key, name, desc, m =>
            {
                m.frames = f; m.shape = HitShape.Circle; m.hitboxOffset = off; m.hitboxRadius = radius; m.hit = h; m.lungeSpeed = 0f;
                m.haptic = h.isHeavy ? HapticStrength.Heavy : HapticStrength.Light;
                if (h.isHeavy) { m.shakeAmplitude = 0.2f; m.shakeDuration = 0.16f; }
                x?.Invoke(m);
            });

            public MoveDefinition Air(string key, string name, string desc, FrameData f, Vector2 off, float radius, HitData h) =>
                Ring(key, name, desc, f, off, radius, h, m => { m.rootedOnGround = false; m.endsOnLanding = true; });

            public MoveDefinition Shot(string key, string name, string desc, FrameData f, HitData h, float speed, float life,
                                       float radius, Color32 color) => Make(key, name, desc, m =>
            {
                m.frames = f; m.hit = h; m.spawnsProjectile = true; m.projectileSpeed = speed; m.projectileLifetime = life;
                m.projectileRadius = radius; m.projectileColor = color; m.lungeSpeed = 0f; m.haptic = HapticStrength.Light;
            });

            public MoveDefinition Trap(string key, string name, string desc, HitData h, Color32 color) => Make(key, name, desc, m =>
            {
                m.frames = Fd(7, 1, 16); m.hit = h; m.spawnsTrap = true; m.projectileSpawnOffset = new Vector2(0.9f, -0.4f);
                m.projectileColor = color; m.trapArmDelay = 0.35f; m.trapLifetime = 6f; m.trapRadius = 0.6f; m.trapFallSpeed = 14f;
                m.lungeSpeed = 0f;
            });

            public MoveDefinition Impact(string key, string name, float radius, HitData h) => Ring(key, name,
                "The landing blast of the dive.", Fd(0, 1, 0), new Vector2(0f, -0.3f), radius, h);
        }

        static void Link(MoveDefinition a, MoveDefinition next, MoveDefinition specialNext = null)
        {
            bool dirty = false;
            if (a.followUp == null && next != null) { a.followUp = next; dirty = true; }
            if (a.specialFollowUp == null && specialNext != null) { a.specialFollowUp = specialNext; dirty = true; }
            if (dirty) EditorUtility.SetDirty(a);
        }

        /// <summary>
        /// Runs on every build (also for assets made by an older version): makes every combo link a true combo
        /// (the target is still in hitstun when the next hit lands) and uses the moveset's dive impact radius for
        /// the kit's landing shockwave.
        /// </summary>
        static void Tune(CharacterDefinition def)
        {
            var ms = def.moveset;
            if (ms == null) return;
            var seen = new HashSet<MoveDefinition>();
            for (var m = ms.groundAttack; m != null && seen.Add(m); m = m.followUp)
            {
                int need = 0;
                foreach (var next in new[] { m.followUp, m.specialFollowUp })
                    if (next != null)
                        need = Math.Max(need, m.frames.active + m.frames.recovery - m.frames.cancelWindow + next.frames.startup + 3);
                if (need > m.hit.baseHitstunFrames)
                {
                    var h = m.hit;
                    h.baseHitstunFrames = need;
                    m.hit = h;
                    EditorUtility.SetDirty(m);
                }
            }
            if (def.kit is HeroKitDefinition kit && kit.hasDive && ms.abilityImpact != null &&
                !Mathf.Approximately(kit.diveShockwaveRadius, ms.abilityImpact.hitboxRadius))
            {
                kit.diveShockwaveRadius = ms.abilityImpact.hitboxRadius;
                EditorUtility.SetDirty(kit);
            }
        }

        static Moveset Set(string heroFolder, string prefix, MoveDefinition atk, MoveDefinition up, MoveDefinition air,
                           MoveDefinition sp, MoveDefinition side, MoveDefinition down, MoveDefinition airSp, MoveDefinition impact)
        {
            var ms = DataAssets.GetOrCreate<Moveset>($"{MovesRoot}/{heroFolder}/{prefix}_Moveset.asset", s =>
            {
                s.groundAttack = atk; s.groundUpAttack = up; s.airAttack = air; s.groundSpecial = sp;
                s.sideSpecial = side; s.downSpecial = down; s.airSpecial = airSp; s.abilityImpact = impact;
            });
            return ms;
        }

        // ---- Fighter assembly --------------------------------------------------------------------

        static CharacterDefinition Fighter(string file, string id, string name, string tagline, string lore, Look look,
                                           float weight, MovementStats move, int hp, int pips, float armor,
                                           Kit kit, string dodgeName, string diveName, Func<Moveset> moves,
                                           Action<HeroKitDefinition> tuneKit = null)
        {
            EditorUtil.EnsureFolder(Folder);
            HeroKitDefinition k;
            string kitPath = $"{Folder}/{file}Kit.asset";
            Action<HeroKitDefinition> init = kd =>
            {
                kd.airDodgeName = dodgeName;
                kd.diveName = diveName ?? "";
                kd.hasDive = !string.IsNullOrEmpty(diveName);
                tuneKit?.Invoke(kd);
            };
            switch (kit)
            {
                case Kit.Guard: k = DataAssets.GetOrCreate<BrickKitDefinition>(kitPath, kd => init(kd)); break;
                case Kit.Hop: k = DataAssets.GetOrCreate<LumaKitDefinition>(kitPath, kd => init(kd)); break;
                case Kit.Boost: k = DataAssets.GetOrCreate<RexKitDefinition>(kitPath, kd => init(kd)); break;
                default: k = DataAssets.GetOrCreate<NovaKitDefinition>(kitPath, kd => init(kd)); break;
            }

            var def = DataAssets.GetOrCreate<CharacterDefinition>($"{Folder}/{file}.asset", d =>
            {
                d.id = id; d.displayName = name; d.tagline = tagline; d.lore = lore;
                d.unlockedByDefault = true; d.playableInThisBuild = true;
                d.placeholderColor = look.top;
                d.weight = weight; d.movement = move; d.maxHealth = hp; d.guardPips = pips; d.armorPercent = armor;
            });
            bool dirty = false;
            if (def.kit == null) { def.kit = k; dirty = true; }
            if (def.bodySprite == null) { def.bodySprite = FighterArt.Make("ph_vs_" + id, look); dirty = true; }
            if (def.moveset == null) { def.moveset = moves(); dirty = true; }
            if (dirty) EditorUtility.SetDirty(def);
            Tune(def);
            return def;
        }

        static MovementStats Move(float run, float jumpHeight, float apex, int airJumps = 0, float coast = -1f, float skid = -1f,
                                  float wallRide = 0f) => new MovementStats
        {
            runSpeed = run, jumpHeight = jumpHeight, timeToApex = apex, airJumps = airJumps,
            coastDeceleration = coast, skidDeceleration = skid, wallRideTime = wallRide
        };

        static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        /// <summary>All ten, in select-screen order. Safe to call repeatedly.</summary>
        public static List<CharacterDefinition> GetOrCreateAll() => new List<CharacterDefinition>
        {
            KaiTempest(), MaraVex(), VoltRamirez(), SolaBrightwing(), Grizz(),
            PixelPip(), NyxFrost(), BlazeTorres(), JunoStrike(), OllieKickflip()
        };

        // ---- The ten -----------------------------------------------------------------------------

        static CharacterDefinition KaiTempest() => Fighter("KaiTempest", "kai_tempest", "Kai Tempest",
            "Wind monk. Fast hands, faster feet.",
            "Kai trained on the windiest peak of the Sunspire Isles, where the Tickworks vents roar day and night. He fights " +
            "the way the wind moves: never where you expect, and always a little faster than you'd like.",
            new Look { build = B.Slim, head = Hd.Band, extra = Ex.Scarf, skin = C(198, 140, 100), hair = C(30, 30, 40),
                       top = C(90, 170, 200), accent = C(240, 240, 255), pants = C(60, 70, 110), shoes = C(40, 40, 50) },
            0.9f, Move(8.2f, 3.3f, 0.36f), 5, 3, 0f, Kit.Dash, "Gale Step", "Cyclone Drop", () =>
            {
                var b = new Book("KaiTempest", "Kai");
                var fin = b.Strike("TyphoonKick", "Typhoon Kick", "Combo ender: a spinning kick that launches.", Fd(5, 4, 16),
                                   new Vector2(0.9f, 0.1f), new Vector2(1.3f, 1f), H(3, 8f, 40f), 3f);
                var palm = b.Shot("StormPalm", "Storm Palm", "Combo finisher (SPECIAL): a point-blank blast of wind.", Fd(6, 1, 16),
                                  H(3, 9f, 25f), 13f, 0.25f, 0.5f, C(200, 240, 255));
                var knee = b.Strike("RisingKnee", "Rising Knee", "Second hit. Attack again for Typhoon Kick, or SPECIAL for Storm Palm.",
                                    Fd(4, 3, 12, 8), new Vector2(0.75f, 0.3f), new Vector2(0.9f, 0.9f), L(1, 4f, 30f), 2.5f);
                var jab = b.Strike("PalmJab", "Palm Jab", "A quick palm strike. Keep pressing Attack for a combo.", Fd(3, 3, 10, 8),
                                   new Vector2(0.7f, 0.05f), new Vector2(0.85f, 0.6f), L(1, 3.5f, 15f), 2.5f);
                Link(jab, knee); Link(knee, fin, palm);
                var up = b.Strike("SkyPalm", "Sky Palm", "Up + Attack: an upward palm that pops foes into the air.", Fd(4, 4, 12),
                                  new Vector2(0.2f, 1.1f), new Vector2(1.1f, 1f), H(2, 7.5f, 82f), 0f);
                var air = b.Air("WhirlKick", "Whirl Kick", "Air Attack: a spinning kick on both sides.", Fd(3, 7, 9), Vector2.zero, 1.0f, L(1, 4.5f, 35f));
                var blade = b.Shot("WindBlade", "Wind Blade", "Special: a fast crescent of wind.", Fd(8, 1, 16), L(1, 5f, 20f), 16f, 0.5f, 0.35f, C(200, 240, 255));
                var rush = b.Strike("GustRush", "Gust Rush", "Side + Special: dashes forward on a gust, fist first.", Fd(6, 6, 18),
                                    new Vector2(0.8f, 0f), new Vector2(1.2f, 0.9f), H(2, 7f, 30f), 11f);
                var eye = b.Ring("EyeOfTheStorm", "Eye of the Storm", "Down + Special: a whirlwind that lifts everyone nearby.", Fd(8, 6, 18),
                                 Vector2.zero, 1.5f, H(2, 7f, 88f));
                var imp = b.Impact("CycloneLanding", "Cyclone Landing", 1.5f, H(2, 7.5f, 60f));
                return Set("KaiTempest", "Kai", jab, up, air, blade, rush, eye, null, imp);
            }, k => { if (k is NovaKitDefinition n) n.airDashSpeed = 16f; });

        static CharacterDefinition MaraVex() => Fighter("MaraVex", "mara_vex", "Mara Vex",
            "Shadow runner. Strikes and vanishes.",
            "Nobody hires Mara Vex; she picks her own jobs. She moves along the rooftops of the Tickworks towns at night and " +
            "is gone before the lanterns swing back. She double jumps, and she never fights fair.",
            new Look { build = B.Slim, head = Hd.Hood, extra = Ex.Scarf, skin = C(230, 190, 160), hair = C(46, 40, 70),
                       top = C(50, 46, 80), accent = C(200, 40, 70), pants = C(40, 36, 60), shoes = C(20, 20, 30) },
            0.8f, Move(8.6f, 3.4f, 0.35f, airJumps: 1), 4, 3, 0f, Kit.Dash, "Shade Step", "Nightfall", () =>
            {
                var b = new Book("MaraVex", "Mara");
                var fin = b.Strike("ShadowRend", "Shadow Rend", "Combo ender: a cross slash that knocks foes away.", Fd(4, 3, 15),
                                   new Vector2(0.9f, 0.05f), new Vector2(1.3f, 0.9f), H(2, 8f, 30f), 3f);
                var vanish = b.Strike("VanishingStrike", "Vanishing Strike", "Combo finisher (SPECIAL): she blinks through the foe.",
                                      Fd(5, 5, 18), new Vector2(0.9f, 0f), new Vector2(1.4f, 0.9f), H(3, 9f, 35f), 13f);
                var twin = b.Strike("TwinSlash", "Twin Slash", "Second hit. Attack for Shadow Rend, SPECIAL for Vanishing Strike.",
                                    Fd(3, 3, 11, 8), new Vector2(0.75f, 0.1f), new Vector2(1f, 0.7f), L(1, 3.5f, 20f), 2.5f);
                var slash = b.Strike("QuickSlash", "Quick Slash", "The fastest slash in the game. Press Attack again.", Fd(2, 3, 9, 8),
                                     new Vector2(0.7f, 0.05f), new Vector2(0.9f, 0.6f), L(1, 3f, 15f), 3f);
                Link(slash, twin); Link(twin, fin, vanish);
                var up = b.Strike("CrescentFlip", "Crescent Flip", "Up + Attack: a backflip kick.", Fd(4, 4, 13),
                                  new Vector2(0.3f, 1f), new Vector2(1.2f, 1.1f), H(2, 7f, 80f), 0f);
                var air = b.Air("KunaiSpin", "Kunai Spin", "Air Attack: blades out, spinning.", Fd(3, 8, 9), Vector2.zero, 1.0f, L(1, 4f, 35f));
                var star = b.Shot("StarToss", "Star Toss", "Special: a throwing star. Fast but light.", Fd(5, 1, 12), L(1, 3.5f, 10f), 19f, 0.45f, 0.25f, C(220, 220, 235));
                var slide = b.Strike("ShadowSlide", "Shadow Slide", "Side + Special: slides low under attacks.", Fd(4, 8, 16),
                                     new Vector2(0.7f, -0.45f), new Vector2(1.3f, 0.5f), L(2, 6f, 70f, 14), 12f);
                var snare = b.Trap("SmokeSnare", "Smoke Snare", "Down + Special: leaves a snare that stuns whoever steps in.", L(1, 4f, 80f, 40), C(140, 120, 170));
                var imp = b.Impact("NightfallStrike", "Nightfall Strike", 1.3f, H(2, 7f, 55f));
                return Set("MaraVex", "Mara", slash, up, air, star, slide, snare, null, imp);
            }, k => { if (k is NovaKitDefinition n) n.airDashSpeed = 17f; });

        static CharacterDefinition VoltRamirez() => Fighter("VoltRamirez", "volt_ramirez", "Volt Ramirez",
            "Boxer with lightning in his gloves.",
            "Volt won the Skyforge belt three years running before a Tickworks storm charged his gloves for good. Now every " +
            "punch crackles. He keeps it simple: get close, keep swinging, finish with the big one.",
            new Look { build = B.Normal, head = Hd.Mohawk, extra = Ex.Gloves, skin = C(140, 90, 60), hair = C(250, 210, 40),
                       top = C(230, 60, 60), accent = C(255, 220, 60), pants = C(40, 60, 160), shoes = C(240, 240, 240) },
            1.05f, Move(7.6f, 3.0f, 0.37f), 5, 3, 0.05f, Kit.Hop, "Spring Hop", null, () =>
            {
                var b = new Book("VoltRamirez", "Volt");
                var fin = b.Strike("VoltUppercut", "Volt Uppercut", "Combo ender: a charged uppercut.", Fd(5, 4, 15),
                                   new Vector2(0.6f, 0.6f), new Vector2(1f, 1.2f), H(3, 8.5f, 78f), 2f);
                var hay = b.Strike("HaymakerOverload", "Haymaker Overload", "Combo finisher (SPECIAL): a huge electric haymaker.",
                                   Fd(9, 4, 20), new Vector2(0.9f, 0.1f), new Vector2(1.3f, 1f), H(4, 10f, 35f, 18, 8), 4f,
                                   m => { m.shakeAmplitude = 0.3f; m.haptic = HapticStrength.Heavy; });
                var cross = b.Strike("Cross", "Cross", "Second punch. Attack for Volt Uppercut, SPECIAL for Haymaker Overload.",
                                     Fd(3, 3, 11, 8), new Vector2(0.8f, 0.15f), new Vector2(0.95f, 0.6f), L(1, 3.5f, 20f), 2.5f);
                var jab = b.Strike("Jab", "Jab", "A boxer's jab. Keep pressing Attack.", Fd(2, 3, 9, 8),
                                   new Vector2(0.75f, 0.15f), new Vector2(0.85f, 0.55f), L(1, 3f, 15f), 2f);
                Link(jab, cross); Link(cross, fin, hay);
                var up = b.Strike("RisingSpark", "Rising Spark", "Up + Attack: an overhead spark.", Fd(4, 4, 12),
                                  new Vector2(0.2f, 1.1f), new Vector2(1.1f, 0.9f), H(2, 7.5f, 85f), 0f);
                var air = b.Strike("HammerFist", "Hammer Fist", "Air Attack: both fists down.", Fd(5, 4, 12),
                                   new Vector2(0.4f, -0.5f), new Vector2(1.1f, 0.9f), H(2, 7f, 15f), 0f,
                                   m => { m.rootedOnGround = false; m.endsOnLanding = true; });
                var hook = b.Strike("ThunderHook", "Thunder Hook", "Special: a slow, heavy electric hook.", Fd(10, 4, 18),
                                    new Vector2(0.85f, 0.1f), new Vector2(1.2f, 0.9f), H(3, 9.5f, 30f), 3f);
                var charge = b.Strike("ChargeUppercut", "Charge Uppercut", "Side + Special: an armored rush into an uppercut.",
                                      Fd(8, 6, 20), new Vector2(0.8f, 0.4f), new Vector2(1.2f, 1.2f), H(3, 8.5f, 70f), 9f,
                                      m => { m.superArmor = true; m.armorStartFrame = 2; });
                var pound = b.Ring("GroundPound", "Ground Pound", "Down + Special: punches the floor; the shock pops foes up.",
                                   Fd(9, 3, 18), new Vector2(0f, -0.4f), 1.7f, H(2, 7f, 85f));
                var sky = b.Strike("SkyHook", "Sky Hook", "Air Special: a diving punch.", Fd(6, 6, 14),
                                   new Vector2(0.7f, -0.2f), new Vector2(1.1f, 1f), H(2, 8f, 30f), 0f,
                                   m => { m.rootedOnGround = false; m.endsOnLanding = true; });
                return Set("VoltRamirez", "Volt", jab, up, air, hook, charge, pound, sky, null);
            }, k => { if (k is LumaKitDefinition l) { l.hopUpSpeed = 13f; l.hopForwardSpeed = 5f; } });

        static CharacterDefinition SolaBrightwing() => Fighter("SolaBrightwing", "sola_brightwing", "Sola Brightwing",
            "Sun knight with a long reach.",
            "Sola keeps the dawn watch on the highest tower of the Isles. Her spear catches the first light of the morning, and " +
            "she can throw that light like a lance. She keeps her distance and makes you come to her.",
            new Look { build = B.Normal, head = Hd.Helmet, extra = Ex.Staff, skin = C(230, 180, 140), hair = C(240, 200, 80),
                       top = C(250, 250, 240), accent = C(255, 150, 40), pants = C(200, 160, 60), shoes = C(120, 80, 40) },
            1.0f, Move(7.4f, 3.1f, 0.37f), 5, 3, 0.05f, Kit.Dash, "Sun Glide", "Solar Dive", () =>
            {
                var b = new Book("SolaBrightwing", "Sola");
                var fin = b.Strike("SunburstSweep", "Sunburst Sweep", "Combo ender: a wide sweep of the spear.", Fd(6, 4, 16),
                                   new Vector2(1.1f, 0f), new Vector2(1.9f, 0.9f), H(3, 8f, 35f), 2f);
                var halo = b.Ring("HaloStrike", "Halo Strike", "Combo finisher (SPECIAL): a ring of sunlight.", Fd(8, 4, 18),
                                  new Vector2(0.3f, 0.2f), 1.7f, H(3, 8.5f, 60f));
                var twin = b.Strike("TwinThrust", "Twin Thrust", "Second thrust. Attack for Sunburst Sweep, SPECIAL for Halo Strike.",
                                    Fd(4, 3, 12, 8), new Vector2(1.1f, 0.1f), new Vector2(1.6f, 0.5f), L(1, 4f, 15f), 2f);
                var poke = b.Strike("SpearPoke", "Spear Poke", "A long jab with the spear. Keep pressing Attack.", Fd(4, 3, 10, 8),
                                    new Vector2(1.1f, 0.1f), new Vector2(1.6f, 0.45f), L(1, 3.5f, 10f), 1.5f);
                Link(poke, twin); Link(twin, fin, halo);
                var up = b.Strike("SpearArc", "Spear Arc", "Up + Attack: the spear sweeps overhead.", Fd(5, 5, 13),
                                  new Vector2(0.2f, 1.2f), new Vector2(1.8f, 0.9f), H(2, 7.5f, 80f), 0f);
                var air = b.Air("LanceSpiral", "Lance Spiral", "Air Attack: a spinning spear.", Fd(4, 8, 10), Vector2.zero, 1.25f, L(1, 4.5f, 35f));
                var lance = b.Shot("SunLance", "Sun Lance", "Special: throws a slow lance of light.", Fd(10, 1, 18), H(2, 7f, 20f), 11f, 0.7f, 0.45f, C(255, 220, 120));
                var charge = b.Strike("LanceCharge", "Lance Charge", "Side + Special: an armored charge, spear first.", Fd(8, 8, 20),
                                      new Vector2(1.1f, 0f), new Vector2(1.6f, 0.8f), H(3, 8f, 30f), 10f,
                                      m => { m.superArmor = true; m.armorStartFrame = 3; });
                var flare = b.Ring("FlareBurst", "Flare Burst", "Down + Special: a burst of light all around her.", Fd(9, 4, 20),
                                   Vector2.zero, 1.7f, H(2, 7f, 70f));
                var imp = b.Impact("SolarCrash", "Solar Crash", 1.6f, H(2, 7.5f, 60f));
                return Set("SolaBrightwing", "Sola", poke, up, air, lance, charge, flare, null, imp);
            });

        static CharacterDefinition Grizz() => Fighter("Grizz", "grizz", "Grizz",
            "A bear of a brawler. Literally.",
            "Grizz runs the lumber mill at the edge of Sunspire Meadows and settles disputes the old-fashioned way. He is slow, " +
            "he is huge, and once he starts a Maul Rush there is not much you can do about it.",
            new Look { build = B.Bulky, head = Hd.Ears, extra = Ex.Tail, skin = C(150, 100, 60), hair = C(120, 80, 45),
                       top = C(110, 70, 40), accent = C(220, 180, 120), pants = C(90, 60, 35), shoes = C(60, 40, 25) },
            1.6f, Move(6.2f, 2.9f, 0.4f, airJumps: 1), 7, 4, 0.15f, Kit.Guard, "Bark Shell", "Timber Drop", () =>
            {
                var b = new Book("Grizz", "Grizz");
                var fin = b.Strike("MaulSlam", "Maul Slam", "Combo ender: both paws down.", Fd(7, 4, 18),
                                   new Vector2(0.9f, 0f), new Vector2(1.4f, 1.1f), H(4, 9f, 45f, 17, 8), 2f);
                var hug = b.Strike("BearHugToss", "Bear Hug Toss", "Combo finisher (SPECIAL): grabs and hurls the foe behind him.",
                                   Fd(8, 3, 22), new Vector2(0.7f, 0.1f), new Vector2(1.1f, 1f), H(4, 10f, 140f, 18, 8), 2f);
                var dbl = b.Strike("DoubleSwipe", "Double Swipe", "Second swipe. Attack for Maul Slam, SPECIAL for Bear Hug Toss.",
                                   Fd(5, 4, 13, 8), new Vector2(0.85f, 0.1f), new Vector2(1.1f, 0.9f), L(2, 4.5f, 25f), 2f);
                var paw = b.Strike("PawSwipe", "Paw Swipe", "A heavy paw. Keep pressing Attack.", Fd(4, 4, 12, 8),
                                   new Vector2(0.85f, 0.1f), new Vector2(1.1f, 0.8f), L(2, 4f, 20f), 1.5f);
                Link(paw, dbl); Link(dbl, fin, hug);
                var up = b.Strike("RoarUppercut", "Roar Uppercut", "Up + Attack: a roaring uppercut.", Fd(6, 4, 16),
                                  new Vector2(0.4f, 1f), new Vector2(1.3f, 1.2f), H(3, 8f, 80f), 0f);
                var air = b.Air("BellyFlop", "Belly Flop", "Air Attack: all of Grizz, all at once.", Fd(5, 6, 12), new Vector2(0f, -0.3f), 1.2f, H(2, 6.5f, 40f));
                var log = b.Shot("LogToss", "Log Toss", "Special: throws a log. Slow and painful.", Fd(12, 1, 20), H(3, 8.5f, 30f), 9f, 0.8f, 0.55f, C(140, 98, 60));
                var rush = b.Strike("MaulRush", "Maul Rush", "Side + Special: an armored charge that bowls foes over.", Fd(9, 10, 22),
                                    new Vector2(0.9f, 0f), new Vector2(1.4f, 1.1f), H(3, 8.5f, 35f), 9f,
                                    m => { m.superArmor = true; m.armorStartFrame = 0; });
                var quake = b.Ring("EarthquakeStomp", "Earthquake Stomp", "Down + Special: a stomp felt across the stage.", Fd(12, 3, 22),
                                   new Vector2(0f, -0.4f), 2.2f, H(2, 7f, 80f));
                var imp = b.Impact("TimberCrash", "Timber Crash", 2.1f, H(3, 9f, 55f));
                return Set("Grizz", "Grizz", paw, up, air, log, rush, quake, null, imp);
            });

        static CharacterDefinition PixelPip() => Fighter("PixelPip", "pixel_pip", "Pixel Pip",
            "Tiny robot. Big ideas.",
            "Pip was built to sweep the gear floors of the Tickworks and decided it had better things to do. It is small, light " +
            "and very hard to hit, and it carries more gadgets than anyone has ever counted.",
            new Look { build = B.Small, head = Hd.Bald, extra = Ex.Antenna, skin = C(180, 200, 210), hair = C(90, 110, 130),
                       top = C(100, 200, 220), accent = C(255, 90, 90), pants = C(70, 80, 100), shoes = C(40, 40, 50), eye = C(255, 240, 80) },
            0.7f, Move(8.0f, 3.2f, 0.34f, airJumps: 1), 4, 3, 0f, Kit.Hop, "Rocket Hop", null, () =>
            {
                var b = new Book("PixelPip", "Pip");
                var fin = b.Strike("OverclockPunch", "Overclock Punch", "Combo ender: a spring-loaded punch.", Fd(5, 3, 15),
                                   new Vector2(0.8f, 0f), new Vector2(1.1f, 0.7f), H(2, 8f, 35f), 3f);
                var burst = b.Shot("LaserBurst", "Laser Burst", "Combo finisher (SPECIAL): a close-range laser blast.", Fd(6, 1, 16),
                                   H(3, 8.5f, 25f), 18f, 0.3f, 0.45f, C(255, 90, 90));
                var whirl = b.Strike("WrenchWhirl", "Wrench Whirl", "Second hit. Attack for Overclock Punch, SPECIAL for Laser Burst.",
                                     Fd(3, 4, 11, 8), new Vector2(0.65f, 0f), new Vector2(0.9f, 0.7f), L(1, 3.5f, 25f), 2f);
                var tap = b.Strike("BoltTap", "Bolt Tap", "A quick tap. Keep pressing Attack.", Fd(2, 3, 9, 8),
                                   new Vector2(0.6f, -0.1f), new Vector2(0.8f, 0.55f), L(1, 3f, 15f), 2f);
                Link(tap, whirl); Link(whirl, fin, burst);
                var up = b.Strike("AntennaZap", "Antenna Zap", "Up + Attack: a zap from the antenna.", Fd(4, 4, 12),
                                  new Vector2(0f, 0.9f), new Vector2(1f, 0.9f), H(2, 7f, 85f), 0f);
                var air = b.Air("PropellerSpin", "Propeller Spin", "Air Attack: spins like a top.", Fd(3, 9, 8), Vector2.zero, 0.9f, L(1, 4f, 40f));
                var ping = b.Shot("LaserPing", "Laser Ping", "Special: a fast little laser.", Fd(5, 1, 12), L(1, 3.5f, 10f), 20f, 0.45f, 0.22f, C(255, 90, 90));
                var drill = b.Strike("DrillDash", "Drill Dash", "Side + Special: drills forward through foes.", Fd(6, 10, 16),
                                     new Vector2(0.7f, 0f), new Vector2(1f, 0.7f), L(2, 6f, 30f, 14), 12f);
                var mine = b.Trap("MineDrop", "Mine Drop", "Down + Special: drops a mine. One at a time.", H(2, 7f, 75f), C(255, 90, 90));
                var rocket = b.Strike("RocketDrop", "Rocket Drop", "Air Special: fires its feet rockets downward.", Fd(6, 6, 14),
                                      new Vector2(0.2f, -0.7f), new Vector2(1f, 0.8f), H(2, 7f, 60f), 0f,
                                      m => { m.rootedOnGround = false; m.endsOnLanding = true; });
                return Set("PixelPip", "Pip", tap, up, air, ping, drill, mine, rocket, null);
            }, k => { if (k is LumaKitDefinition l) { l.hopUpSpeed = 13.5f; l.hopForwardSpeed = 7f; } });

        static CharacterDefinition NyxFrost() => Fighter("NyxFrost", "nyx_frost", "Nyx Frost",
            "Ice sorceress. Slows you down, then finishes.",
            "Nyx keeps the cold stores deep under the Tickworks, where the old gears are packed in ice. She learned to shape the " +
            "frost into shards and rings, and her hits leave foes stunned for just long enough.",
            new Look { build = B.Slim, head = Hd.Hair, extra = Ex.Cape, skin = C(230, 235, 250), hair = C(200, 230, 255),
                       top = C(90, 120, 220), accent = C(170, 220, 255), pants = C(60, 70, 150), shoes = C(40, 50, 100) },
            0.85f, Move(7.2f, 3.2f, 0.37f), 5, 3, 0f, Kit.Dash, "Frost Blink", "Icicle Plunge", () =>
            {
                var b = new Book("NyxFrost", "Nyx");
                var fin = b.Strike("GlacierSpike", "Glacier Spike", "Combo ender: an ice spike from below.", Fd(6, 4, 16),
                                   new Vector2(0.9f, 0.3f), new Vector2(1.2f, 1.2f), H(3, 8f, 75f), 1.5f);
                var ring = b.Ring("BlizzardRing", "Blizzard Ring", "Combo finisher (SPECIAL): a ring of frost that holds foes.",
                                  Fd(7, 4, 18), new Vector2(0.4f, 0.1f), 1.6f, H(2, 6f, 60f, 26));
                var edge = b.Strike("IceEdge", "Ice Edge", "Second hit. Attack for Glacier Spike, SPECIAL for Blizzard Ring.",
                                    Fd(4, 3, 12, 8), new Vector2(0.8f, 0.1f), new Vector2(1.1f, 0.7f), L(1, 3.5f, 20f, 14), 2f);
                var tap = b.Strike("FrostTap", "Frost Tap", "A chilly tap. Keep pressing Attack.", Fd(3, 3, 10, 8),
                                   new Vector2(0.7f, 0.05f), new Vector2(0.9f, 0.6f), L(1, 3f, 15f, 12), 2f);
                Link(tap, edge); Link(edge, fin, ring);
                var up = b.Strike("IcicleCrown", "Icicle Crown", "Up + Attack: icicles form overhead.", Fd(5, 5, 13),
                                  new Vector2(0.1f, 1.1f), new Vector2(1.4f, 1f), H(2, 7f, 85f), 0f);
                var air = b.Air("SnowSwirl", "Snow Swirl", "Air Attack: a swirl of snow.", Fd(3, 8, 10), Vector2.zero, 1.05f, L(1, 4f, 35f, 13));
                var shard = b.Shot("IceShard", "Ice Shard", "Special: a slow shard that stuns on hit.", Fd(9, 1, 18), L(2, 4f, 15f, 30), 10f, 0.8f, 0.45f, C(170, 220, 255));
                var slide = b.Strike("GlacierSlide", "Glacier Slide", "Side + Special: skates forward on ice.", Fd(5, 8, 18),
                                     new Vector2(0.8f, -0.3f), new Vector2(1.2f, 0.7f), L(2, 5f, 45f, 22), 11f);
                var frost = b.Ring("FrostRing", "Frost Ring", "Down + Special: freezes the ground around her.", Fd(10, 4, 20),
                                   new Vector2(0f, -0.3f), 1.8f, L(2, 3f, 80f, 34));
                var imp = b.Impact("IcicleShatter", "Icicle Shatter", 1.5f, H(2, 7f, 60f, 20));
                return Set("NyxFrost", "Nyx", tap, up, air, shard, slide, frost, null, imp);
            }, k => { if (k is NovaKitDefinition n) n.airDashSpeed = 18f; });

        static CharacterDefinition BlazeTorres() => Fighter("BlazeTorres", "blaze_torres", "Blaze Torres",
            "Firefighter. Runs toward trouble.",
            "When a Tickworks boiler blows, Blaze is the first one up the ladder. She swings a rescue axe like it weighs nothing, " +
            "and her water cannon can push anyone off their feet. Heavy, armored and hard to stop.",
            new Look { build = B.Bulky, head = Hd.Helmet, extra = Ex.Gloves, skin = C(180, 120, 90), hair = C(220, 50, 40),
                       top = C(230, 190, 60), accent = C(255, 230, 120), pants = C(60, 60, 70), shoes = C(30, 30, 30) },
            1.4f, Move(6.8f, 3.0f, 0.39f), 6, 4, 0.1f, Kit.Guard, "Hose Hover", "Ladder Slam", () =>
            {
                var b = new Book("BlazeTorres", "Blaze");
                var fin = b.Strike("RescueSmash", "Rescue Smash", "Combo ender: the axe comes down.", Fd(7, 4, 17),
                                   new Vector2(0.9f, 0.2f), new Vector2(1.3f, 1.2f), H(4, 9f, 50f, 17, 8), 2f);
                var back = b.Ring("Backdraft", "Backdraft", "Combo finisher (SPECIAL): a burst of flame from her coat.",
                                  Fd(9, 4, 20), new Vector2(0.3f, 0.1f), 1.8f, H(3, 9f, 50f));
                var swing = b.Strike("AxeSwing", "Axe Swing", "Second swing. Attack for Rescue Smash, SPECIAL for Backdraft.",
                                     Fd(5, 4, 13, 8), new Vector2(0.9f, 0.1f), new Vector2(1.2f, 0.9f), L(2, 4.5f, 25f), 2f);
                var chop = b.Strike("AxeChop", "Axe Chop", "A short chop. Keep pressing Attack.", Fd(4, 3, 11, 8),
                                    new Vector2(0.85f, 0.1f), new Vector2(1f, 0.8f), L(1, 4f, 20f), 1.5f);
                Link(chop, swing); Link(swing, fin, back);
                var up = b.Strike("LadderLift", "Ladder Lift", "Up + Attack: swings the ladder upward.", Fd(6, 5, 15),
                                  new Vector2(0.3f, 1.2f), new Vector2(1.2f, 1.3f), H(3, 8f, 82f), 0f);
                var air = b.Strike("AxeDrop", "Axe Drop", "Air Attack: the axe straight down.", Fd(6, 4, 12),
                                   new Vector2(0.5f, -0.5f), new Vector2(1.1f, 1f), H(3, 7.5f, 20f), 0f,
                                   m => { m.rootedOnGround = false; m.endsOnLanding = true; });
                var cannon = b.Shot("WaterCannon", "Water Cannon", "Special: a blast of water that pushes far.", Fd(8, 1, 18), L(1, 9f, 15f, 12), 14f, 0.5f, 0.5f, C(120, 190, 255));
                var shoulder = b.Strike("ShoulderCharge", "Shoulder Charge", "Side + Special: an armored shoulder charge.", Fd(8, 8, 20),
                                        new Vector2(0.8f, 0.1f), new Vector2(1.2f, 1.1f), H(3, 8.5f, 35f), 10f,
                                        m => { m.superArmor = true; m.armorStartFrame = 0; });
                var wall = b.Ring("FireWall", "Fire Wall", "Down + Special: flames rise around her.", Fd(10, 5, 20),
                                  new Vector2(0f, 0.2f), 1.8f, H(2, 7.5f, 75f));
                var imp = b.Impact("LadderCrash", "Ladder Crash", 2.0f, H(3, 8.5f, 55f));
                return Set("BlazeTorres", "Blaze", chop, up, air, cannon, shoulder, wall, null, imp);
            });

        static CharacterDefinition JunoStrike() => Fighter("JunoStrike", "juno_strike", "Juno Strike",
            "Striker. Never stops moving.",
            "Juno captains the Meadow Rovers, the best football side in the Isles, and she plays every fight like a cup final. " +
            "She builds up speed, and the faster she runs the harder her kicks land.",
            new Look { build = B.Normal, head = Hd.Band, extra = Ex.Ball, skin = C(120, 80, 55), hair = C(20, 20, 25),
                       top = C(40, 150, 90), accent = C(255, 255, 255), pants = C(240, 240, 240), shoes = C(240, 90, 40) },
            0.95f, Move(8.4f, 3.2f, 0.36f, coast: 10f, skid: 42f), 5, 3, 0f, Kit.Boost, "Step Over", "Diving Header", () =>
            {
                var b = new Book("JunoStrike", "Juno");
                var fin = b.Strike("Volley", "Volley", "Combo ender: a full volley. Harder at speed.", Fd(5, 4, 15),
                                   new Vector2(0.9f, 0.2f), new Vector2(1.2f, 0.9f), H(3, 8f, 40f), 3f, m => m.speedBonus = 0.6f);
                var bike = b.Strike("BicycleKick", "Bicycle Kick", "Combo finisher (SPECIAL): an overhead bicycle kick.",
                                    Fd(7, 5, 18), new Vector2(0.4f, 1f), new Vector2(1.4f, 1.2f), H(3, 9f, 65f), 1f);
                var knee = b.Strike("KneeJuggle", "Knee Juggle", "Second touch. Attack for Volley, SPECIAL for Bicycle Kick.",
                                    Fd(3, 3, 11, 8), new Vector2(0.6f, 0.3f), new Vector2(0.9f, 0.9f), L(1, 3.5f, 60f), 2f);
                var poke = b.Strike("ToePoke", "Toe Poke", "A quick poke. Keep pressing Attack.", Fd(3, 3, 9, 8),
                                    new Vector2(0.75f, -0.3f), new Vector2(0.9f, 0.5f), L(1, 3f, 15f), 2.5f);
                Link(poke, knee); Link(knee, fin, bike);
                var up = b.Strike("Header", "Header", "Up + Attack: heads it away.", Fd(4, 4, 12),
                                  new Vector2(0.4f, 1f), new Vector2(1f, 0.9f), H(2, 7.5f, 75f), 0f);
                var air = b.Air("ScissorKick", "Scissor Kick", "Air Attack: legs everywhere.", Fd(3, 7, 9), new Vector2(0.2f, 0f), 1.0f, L(1, 4.5f, 35f));
                var shot = b.Shot("PowerShot", "Power Shot", "Special: strikes the ball at the foe.", Fd(8, 1, 16), H(2, 7.5f, 20f), 17f, 0.55f, 0.35f, C(245, 245, 245));
                var tackle = b.Strike("SlideTackle", "Slide Tackle", "Side + Special: a sliding tackle. Harder at speed.", Fd(4, 9, 18),
                                      new Vector2(0.8f, -0.45f), new Vector2(1.4f, 0.5f), L(2, 6f, 75f, 15), 12f, m => m.speedBonus = 0.8f);
                var flick = b.Ring("RainbowFlick", "Rainbow Flick", "Down + Special: flicks the ball over her head. Pops foes up.",
                                   Fd(6, 4, 16), new Vector2(0.2f, 0.4f), 1.3f, H(2, 7.5f, 95f));
                var imp = b.Impact("HeaderCrash", "Header Crash", 1.3f, H(2, 7.5f, 50f));
                return Set("JunoStrike", "Juno", poke, up, air, shot, tackle, flick, null, imp);
            });

        static CharacterDefinition OllieKickflip() => Fighter("OllieKickflip", "ollie_kickflip", "Ollie Kickflip",
            "Skater. Rides walls, grinds everything.",
            "Ollie learned to skate on the brass rails of the Skyforge, and the guards gave up chasing him years ago. He rides up " +
            "walls, grinds into foes and never, ever lands a trick the same way twice.",
            new Look { build = B.Slim, head = Hd.Cap, extra = Ex.Board, skin = C(200, 150, 110), hair = C(60, 40, 30),
                       top = C(250, 120, 40), accent = C(60, 60, 70), pants = C(50, 70, 120), shoes = C(240, 240, 240) },
            0.9f, Move(9.0f, 3.3f, 0.36f, coast: 7f, skid: 32f, wallRide: 0.4f), 5, 3, 0f, Kit.Boost, "Board Boost", "Kickflip Slam", () =>
            {
                var b = new Book("OllieKickflip", "Ollie");
                var fin = b.Strike("NollieSmash", "Nollie Smash", "Combo ender: the board's nose, hard. Harder at speed.", Fd(5, 4, 15),
                                   new Vector2(0.9f, 0.1f), new Vector2(1.2f, 0.9f), H(3, 8f, 40f), 3f, m => m.speedBonus = 0.5f);
                var toss = b.Shot("BoardToss", "Board Toss", "Combo finisher (SPECIAL): throws the board, spinning.", Fd(7, 1, 18),
                                  H(3, 8f, 30f), 15f, 0.5f, 0.45f, C(60, 60, 70));
                var whip = b.Strike("TailWhip", "Tail Whip", "Second hit. Attack for Nollie Smash, SPECIAL for Board Toss.",
                                    Fd(3, 4, 11, 8), new Vector2(0.75f, -0.1f), new Vector2(1.1f, 0.6f), L(1, 3.5f, 25f), 2.5f);
                var jab = b.Strike("BoardJab", "Board Jab", "Pokes with the board. Keep pressing Attack.", Fd(3, 3, 9, 8),
                                   new Vector2(0.75f, 0f), new Vector2(0.9f, 0.6f), L(1, 3f, 15f), 2.5f);
                Link(jab, whip); Link(whip, fin, toss);
                var up = b.Strike("OlliePopKick", "Ollie Pop Kick", "Up + Attack: pops the board up into the foe.", Fd(4, 4, 12),
                                  new Vector2(0.3f, 1f), new Vector2(1.1f, 1f), H(2, 7.5f, 80f), 0f);
                var air = b.Air("KickflipSpin", "Kickflip Spin", "Air Attack: a kickflip with the board.", Fd(3, 8, 9), new Vector2(0f, -0.3f), 1.0f, L(1, 4.5f, 40f));
                var wheel = b.Shot("WheelShot", "Wheel Shot", "Special: flicks a spare wheel.", Fd(5, 1, 12), L(1, 4f, 15f), 18f, 0.5f, 0.25f, C(240, 240, 240));
                var grind = b.Strike("GrindRush", "Grind Rush", "Side + Special: grinds forward. Harder at speed.", Fd(4, 9, 16),
                                     new Vector2(0.8f, -0.2f), new Vector2(1.3f, 0.6f), L(2, 6f, 35f, 14), 12f, m => m.speedBonus = 0.8f);
                var manual = b.Ring("ManualSlam", "Manual Slam", "Down + Special: slams the board flat. Pops foes up.", Fd(8, 3, 18),
                                    new Vector2(0f, -0.4f), 1.5f, H(2, 7f, 85f));
                var imp = b.Impact("KickflipCrash", "Kickflip Crash", 1.3f, H(2, 7.5f, 50f));
                return Set("OllieKickflip", "Ollie", jab, up, air, wheel, grind, manual, null, imp);
            });
    }
}
