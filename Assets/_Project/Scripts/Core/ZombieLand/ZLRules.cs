using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>A gun the hero owns, with its ammo.</summary>
    [Serializable]
    public class ZLOwnedGun
    {
        public string id;
        public int mag, reserve;
    }

    /// <summary>Saved Zombie Land progress. Dying keeps all of it (you restart at your last lit safehouse).</summary>
    [Serializable]
    public class ZombieLandProgress
    {
        /// <summary>Picks the true relic and lair; 0 = not started.</summary>
        public int seed;
        public List<string> completed = new List<string>();
        /// <summary>Mission leads the hero knows about (shown on the map).</summary>
        public List<string> leads = new List<string>();
        /// <summary>Lit safehouses (checkpoints) and the one to respawn at.</summary>
        public List<string> safehouses = new List<string>();
        public string checkpoint = "";
        public List<ZLOwnedGun> guns = new List<ZLOwnedGun>();
        public string equipped = "";
        /// <summary>Destroyed rot nests (the district gets quieter).</summary>
        public List<string> nestsCleared = new List<string>();
        /// <summary>Weapon/ammo/medkit pickups already taken (they don't come back).</summary>
        public List<string> taken = new List<string>();
        /// <summary>Lairs searched without finding him.</summary>
        public List<string> lairsSearched = new List<string>();
        public int medkits, armor;
        public int deaths, kills;
        public float playSeconds;
        public bool bossDefeated;
        public bool introSeen;
        /// <summary>Chapter 2 (Blackwater Isle): begun after the First Turned falls; done when Mother Brine falls.</summary>
        public bool chapter2Begun, chapter2Done;
        /// <summary>Chapter 3 (the Larkin sublevel): begun after Mother Brine; done when the Curator falls.</summary>
        public bool chapter3Begun, chapter3Done;
        /// <summary>Which map the hero is on: "" = the city, "labs" = the sublevel. arriveAt = a site to appear at next load.</summary>
        public string zone = "", arriveAt = "";
        /// <summary>Bonus rewards unlocked (motorcycles, medkits, assault_rifle, riot_shotgun).</summary>
        public List<string> rewards = new List<string>();
        /// <summary>SCRAP to spend at safehouse supply crates, and all scrap ever earned.</summary>
        public int scrap, scrapEarned;
        /// <summary>Perks bought (ZLPerk names; kept through death, Second Chance is used up).</summary>
        public List<string> perks = new List<string>();
        /// <summary>Guns hollow-forged at the upgrade forge.</summary>
        public List<string> forged = new List<string>();
        /// <summary>Mystery box spins since the wonder weapon, and kills since the last power-up (the pity timers).</summary>
        public int spinsSinceWonder, killsSinceDrop;

        /// <summary>Fills in lists missing from older saves.</summary>
        public void Sanitize()
        {
            if (completed == null) completed = new List<string>();
            if (leads == null) leads = new List<string>();
            if (safehouses == null) safehouses = new List<string>();
            if (guns == null) guns = new List<ZLOwnedGun>();
            if (nestsCleared == null) nestsCleared = new List<string>();
            if (taken == null) taken = new List<string>();
            if (lairsSearched == null) lairsSearched = new List<string>();
            if (rewards == null) rewards = new List<string>();
            if (perks == null) perks = new List<string>();
            if (forged == null) forged = new List<string>();
            if (checkpoint == null) checkpoint = "";
            if (equipped == null) equipped = "";
            if (zone == null) zone = "";
            if (arriveAt == null) arriveAt = "";
            if (zone != "" && zone != ZLCatalog.LabsMap) zone = "";
            if (scrap < 0) scrap = 0;
        }

        public bool Done(string missionId) => completed.Contains(missionId);

        public int Shards
        {
            get
            {
                int n = 0;
                foreach (var id in completed)
                {
                    var m = ZLCatalog.Mission(id);
                    if (m != null && m.required && m.chapter == 1) n++;
                }
                return n;
            }
        }

        /// <summary>Chapter 2: sea-glass sigils (one per required Blackwater mission).</summary>
        public int Sigils
        {
            get
            {
                int n = 0;
                foreach (var id in completed)
                {
                    var m = ZLCatalog.Mission(id);
                    if (m != null && m.required && m.chapter == 2) n++;
                }
                return n;
            }
        }

        /// <summary>Chapter 3: security keycards (one per required sublevel mission).</summary>
        public int Keycards
        {
            get
            {
                int n = 0;
                foreach (var id in completed)
                {
                    var m = ZLCatalog.Mission(id);
                    if (m != null && m.required && m.chapter == 3) n++;
                }
                return n;
            }
        }

        public bool InChapter3 => chapter2Done && chapter3Begun && !chapter3Done;
        public bool InLabs => zone == ZLCatalog.LabsMap;

        /// <summary>Chapter 2 is under way: the First Turned is dead and Mother Brine isn't yet.</summary>
        public bool InChapter2 => bossDefeated && chapter2Begun && !chapter2Done;

        public ZLOwnedGun Gun(string id)
        {
            foreach (var g in guns) if (g.id == id) return g;
            return null;
        }
    }

    /// <summary>Zombie Land numbers (pure, unit-tested).</summary>
    public static class ZLRules
    {
        // ---- the hero ---------------------------------------------------------------------------------
        public const int HeroHealth = 100;
        public const int MaxArmor = 50;
        public const int MedkitHeal = 50;
        public const int MaxMedkits = 3;
        public const float WalkSpeed = 4.6f;
        public const float SprintSpeed = 6.4f;
        public const float RollSpeed = 11f, RollTime = 0.28f, RollCooldown = 0.8f;
        public const int MeleeDamage = 6;
        public const float MeleeRange = 1.3f;

        // ---- the blood hounds: after five minutes out in the open (per life), he sends his pack ---------
        public const float HoundsAfter = 300f;

        /// <summary>Seconds between hound packs, given seconds already spent roaming (shrinks the longer you stay out).</summary>
        public static float HoundInterval(float roamed)
        {
            float over = Math.Max(0f, roamed - HoundsAfter);
            return Math.Max(18f, 40f - over / 15f);
        }

        /// <summary>Hounds in a pack: 3, growing by one every two packs, up to 7.</summary>
        public static int HoundPack(int packIndex) => Math.Min(7, 3 + packIndex / 2);

        public static bool HoundsActive(float roamed) => roamed >= HoundsAfter;

        // ---- the city's dead ------------------------------------------------------------------------
        /// <summary>How many zombies roam near the hero in a district (threat 1..5), minus cleared nests.</summary>
        public static int Population(int threat, int nestsClearedInDistrict, bool lightsOn = false)
        {
            int n = 10 + threat * 5 - nestsClearedInDistrict * 4;
            if (lightsOn) n -= 4;
            return Math.Max(6, Math.Min(36, n));
        }

        public static int Health(ZLFoe f, int threat)
        {
            int baseHp;
            switch (f)
            {
                case ZLFoe.Shambler: baseHp = 12; break;
                case ZLFoe.Runner: baseHp = 9; break;
                case ZLFoe.Screamer: baseHp = 10; break;
                case ZLFoe.Bloater: baseHp = 22; break;
                case ZLFoe.Spitter: baseHp = 14; break;
                case ZLFoe.Brute: baseHp = 60; break;
                case ZLFoe.Hound: baseHp = 14; break;
                case ZLFoe.Herald: baseHp = 260; break;
                case ZLFoe.FirstTurned: return 900;
                case ZLFoe.Riot: baseHp = 20; break;
                case ZLFoe.Leaper: baseHp = 10; break;
                case ZLFoe.Splitter: baseHp = 18; break;
                case ZLFoe.SplitterHalf: baseHp = 6; break;
                case ZLFoe.Burrower: baseHp = 16; break;
                default: baseHp = 10; break;
            }
            return (int)Math.Round(baseHp * (1f + 0.15f * (Math.Max(1, threat) - 1)));
        }

        public static float Speed(ZLFoe f, int threat)
        {
            float s;
            switch (f)
            {
                case ZLFoe.Shambler: s = 1.5f; break;
                case ZLFoe.Runner: s = 4.2f; break;
                case ZLFoe.Screamer: s = 2.4f; break;
                case ZLFoe.Bloater: s = 1.2f; break;
                case ZLFoe.Spitter: s = 1.8f; break;
                case ZLFoe.Brute: s = 1.6f; break;
                case ZLFoe.Hound: s = 7.2f; break;
                case ZLFoe.Herald: s = 2.6f; break;
                case ZLFoe.FirstTurned: s = 3f; break;
                case ZLFoe.Riot: s = 1.7f; break;
                case ZLFoe.Leaper: s = 2.9f; break;
                case ZLFoe.Splitter: s = 1.6f; break;
                case ZLFoe.SplitterHalf: s = 3.6f; break;
                case ZLFoe.Burrower: s = 1.9f; break;
                default: s = 1.5f; break;
            }
            return s * (1f + 0.05f * (Math.Max(1, threat) - 1));
        }

        public static int Bite(ZLFoe f)
        {
            switch (f)
            {
                case ZLFoe.Runner: return 7;
                case ZLFoe.Hound: return 9;
                case ZLFoe.Brute: return 22;
                case ZLFoe.Herald: return 18;
                case ZLFoe.FirstTurned: return 25;
                case ZLFoe.Bloater: return 12;
                case ZLFoe.Riot: return 14;
                case ZLFoe.Leaper: return 12;
                case ZLFoe.Splitter: return 11;
                case ZLFoe.SplitterHalf: return 6;
                case ZLFoe.Burrower: return 14;
                default: return 10;
            }
        }

        /// <summary>Which kind spawns, from a roll in [0,1) and the district threat (deeper = nastier mix).</summary>
        public static ZLFoe Pick(int threat, double roll)
        {
            double brute = threat >= 3 ? 0.04 + 0.02 * (threat - 3) : 0;
            double screamer = threat >= 2 ? 0.05 : 0.02;
            double spitter = threat >= 2 ? 0.07 : 0;
            double bloater = 0.06 + 0.01 * threat;
            double runner = 0.12 + 0.04 * threat;
            double riot = threat >= 3 ? 0.03 + 0.02 * (threat - 3) : 0;
            double leaper = threat >= 4 ? 0.03 + 0.03 * (threat - 4) : 0;
            double splitter = threat >= 3 ? 0.03 + 0.01 * (threat - 3) : 0;
            double burrower = threat >= 4 ? 0.03 : 0;
            if ((roll -= riot) < 0) return ZLFoe.Riot;
            if ((roll -= leaper) < 0) return ZLFoe.Leaper;
            if ((roll -= splitter) < 0) return ZLFoe.Splitter;
            if ((roll -= burrower) < 0) return ZLFoe.Burrower;
            if ((roll -= brute) < 0) return ZLFoe.Brute;
            if ((roll -= screamer) < 0) return ZLFoe.Screamer;
            if ((roll -= spitter) < 0) return ZLFoe.Spitter;
            if ((roll -= bloater) < 0) return ZLFoe.Bloater;
            if ((roll -= runner) < 0) return ZLFoe.Runner;
            return ZLFoe.Shambler;
        }

        // ---- splitters and burrowers ----------------------------------------------------------------------
        /// <summary>A splitter falls apart into this many halves.</summary>
        public const int SplitterHalves = 2;

        /// <summary>Burrowers dig when the hero is this far (tiles) and in reach, travel this fast under ground,
        /// surface this close, show the mound's "!" this long first, and rest this long between dives.</summary>
        public const float BurrowMin = 4f, BurrowMax = 14f, BurrowSpeed = 5.5f, BurrowSurfaceAt = 1.6f, BurrowTell = 0.55f,
                           BurrowCooldown = 6f, BurrowMaxTime = 3.5f;

        /// <summary>Should a burrower dive now? (hunting, out of bite range, not too far, rested)</summary>
        public static bool BurrowerDives(bool hunting, float dist, float cooldownLeft) =>
            hunting && cooldownLeft <= 0f && dist >= BurrowMin && dist <= BurrowMax;

        /// <summary>Under ground, time to come up: close enough, or out of breath.</summary>
        public static bool BurrowerSurfaces(float dist, float underFor) => dist <= BurrowSurfaceAt || underFor >= BurrowMaxTime;

        /// <summary>How far (tiles) the dead hear a noise of this loudness (gunshots, engines).</summary>
        public static float HearingRange(float noise) => 4f + noise * 14f;

        /// <summary>A scream calls a horde of this many.</summary>
        public static int HordeSize(int threat) => 6 + threat * 2;

        // ---- the First Turned ---------------------------------------------------------------------------
        /// <summary>Only the true relic hurts him; everything else does nothing.</summary>
        public static int BossDamage(ZLGun gun, int damage, ZLRelic trueRelic) =>
            gun != null && gun.isRelic && gun.relic == trueRelic ? damage : 0;

        /// <summary>Whether the lair opens: all ten shards, and it has to be the real lair.</summary>
        public static bool LairOpens(ZombieLandProgress p, string lairId, ZLPuzzle puzzle) =>
            p.Shards >= ZLCatalog.ShardsNeeded && puzzle.TrueLair == lairId;

        /// <summary>Boss phase from health fraction: 1 above 60%, 2 above 25%, 3 below.</summary>
        public static int BossPhase(float health01) => health01 > 0.6f ? 1 : health01 > 0.25f ? 2 : 3;

        // ---- new game, death, rewards ------------------------------------------------------------------
        public static void NewGame(ZombieLandProgress p, int seed)
        {
            p.seed = seed == 0 ? 1 : seed;
            p.completed.Clear(); p.leads.Clear(); p.safehouses.Clear(); p.guns.Clear(); p.nestsCleared.Clear();
            p.taken.Clear(); p.lairsSearched.Clear(); p.rewards.Clear();
            p.perks.Clear(); p.forged.Clear();
            p.scrap = 0; p.scrapEarned = 0; p.spinsSinceWonder = 0; p.killsSinceDrop = 0;
            p.leads.AddRange(ZLCatalog.StartingLeads);
            p.checkpoint = "";
            p.guns.Add(new ZLOwnedGun { id = ZLCatalog.Pistol.id, mag = ZLCatalog.Pistol.magazine, reserve = ZLCatalog.StartingRounds - ZLCatalog.Pistol.magazine });
            p.equipped = ZLCatalog.Pistol.id;
            p.medkits = 1; p.armor = 0; p.deaths = 0; p.kills = 0; p.playSeconds = 0f;
            p.bossDefeated = false; p.introSeen = false;
            p.chapter2Begun = false; p.chapter2Done = false;
            p.chapter3Begun = false; p.chapter3Done = false;
            p.zone = ""; p.arriveAt = "";
        }

        // ---- chapter 2 ---------------------------------------------------------------------------------

        /// <summary>Star Shards for beating Mother Brine.</summary>
        public const int Chapter2Reward = 2000;
        /// <summary>Mother Brine's health (every weapon hurts her, unlike the First Turned).</summary>
        public const int Chapter2BossHealth = 2400;

        /// <summary>Opens chapter 2 (only once the First Turned is dead): the causeway opens and the first lead is known.</summary>
        public static bool BeginChapter2(ZombieLandProgress p)
        {
            if (p == null || !p.bossDefeated || p.chapter2Begun) return false;
            p.chapter2Begun = true;
            foreach (var id in ZLCatalog.Chapter2StartingLeads) if (!p.leads.Contains(id)) p.leads.Add(id);
            return true;
        }

        public const int Chapter3Reward = 2500;
        /// <summary>The Curator's health (every weapon hurts it).</summary>
        public const int Chapter3BossHealth = 3200;

        /// <summary>Opens chapter 3 once Mother Brine is dead: the freight lift works and the first lead is known.</summary>
        public static bool BeginChapter3(ZombieLandProgress p)
        {
            if (p == null || !p.chapter2Done || p.chapter3Begun) return false;
            p.chapter3Begun = true;
            foreach (var id in ZLCatalog.Chapter3StartingLeads) if (!p.leads.Contains(id)) p.leads.Add(id);
            return true;
        }

        /// <summary>The freight lift runs once chapter 3 has begun.</summary>
        public static bool LiftOpen(ZombieLandProgress p) => p != null && p.chapter3Begun;

        /// <summary>The director's vault opens for the Curator when you carry every keycard.</summary>
        public static bool VaultOpen(ZombieLandProgress p) => p != null && p.InChapter3 && p.Keycards >= ZLCatalog.KeycardsNeeded;

        /// <summary>The tide gate opens for Mother Brine when you carry every sigil.</summary>
        public static bool TideGateOpen(ZombieLandProgress p) => p != null && p.InChapter2 && p.Sigils >= ZLCatalog.SigilsNeeded;

        // ---- the new dead ---------------------------------------------------------------------------------

        /// <summary>A riot zombie's shield covers this many degrees either side of where it faces.</summary>
        public const float ShieldHalfAngle = 60f;
        /// <summary>Bullets that hit the shield do this share of their damage.</summary>
        public const float ShieldScale = 0.2f;

        /// <summary>
        /// Damage a riot zombie takes from a shot travelling along (dx, dy) while it faces (fx, fy): reduced when the
        /// shot comes at the front of the shield. Explosions and melee pass shotX = shotY = 0 (full damage).
        /// </summary>
        public static int RiotDamage(int damage, float fx, float fy, float dx, float dy)
        {
            float fl = (float)Math.Sqrt(fx * fx + fy * fy), dl = (float)Math.Sqrt(dx * dx + dy * dy);
            if (fl < 1e-4f || dl < 1e-4f) return damage;
            // the shot comes from the front when it travels against the facing
            float cos = -(fx * dx + fy * dy) / (fl * dl);
            bool front = cos >= Math.Cos(ShieldHalfAngle * Math.PI / 180.0);
            return front ? Math.Max(1, (int)Math.Round(damage * ShieldScale)) : damage;
        }

        public static bool ShieldBlocks(float fx, float fy, float dx, float dy) => RiotDamage(100, fx, fy, dx, dy) < 100;

        /// <summary>Leapers: spring from this range, after this long crouched, for this long, this much faster.</summary>
        public const float LeapMin = 2.5f, LeapMax = 6.5f, LeapWindup = 0.5f, LeapTime = 0.38f, LeapSpeed = 13f, LeapCooldown = 2.4f;

        /// <summary>Damage to a frozen zombie (the Rime Cannon's shatter).</summary>
        public static int FrozenDamage(int damage) => (int)Math.Round(damage * ZLCatalog.FrozenDamageScale);

        /// <summary>
        /// After dying: progress (missions, shards, clues, guns found, lit safehouses) is kept. The pistol comes back
        /// with its starting 25 rounds if it had less; other guns keep what they had. The hound timer restarts.
        /// </summary>
        public static void Respawn(ZombieLandProgress p) => Respawn(p, 1, 0);

        /// <summary>Respawn with Survivor Rank field kits: at least this many medkits and this much armor.</summary>
        public static void Respawn(ZombieLandProgress p, int minMedkits, int minArmor)
        {
            p.deaths++;
            var pistol = p.Gun(ZLCatalog.Pistol.id);
            if (pistol == null)
            {
                pistol = new ZLOwnedGun { id = ZLCatalog.Pistol.id };
                p.guns.Insert(0, pistol);
            }
            int have = pistol.mag + pistol.reserve;
            if (have < ZLCatalog.StartingRounds)
            {
                pistol.mag = ZLCatalog.Pistol.magazine;
                pistol.reserve = ZLCatalog.StartingRounds - ZLCatalog.Pistol.magazine;
            }
            if (p.medkits < minMedkits) p.medkits = Math.Min(MaxMedkits, minMedkits);
            if (p.armor < minArmor) p.armor = Math.Min(MaxArmor, minArmor);
        }

        /// <summary>Marks a mission done, adds its follow-up leads and applies a bonus reward. Returns false if it was already done.</summary>
        public static bool Complete(ZombieLandProgress p, string missionId)
        {
            var m = ZLCatalog.Mission(missionId);
            if (m == null || p.completed.Contains(missionId)) return false;
            p.completed.Add(missionId);
            foreach (var r in m.reveals) if (!p.leads.Contains(r)) p.leads.Add(r);
            if (!string.IsNullOrEmpty(m.reward) && !p.rewards.Contains(m.reward))
            {
                p.rewards.Add(m.reward);
                var gun = ZLCatalog.Gun(m.reward);
                if (gun != null) GiveGun(p, gun, gun.magazine * 3);
                if (m.reward == "riot_shotgun") p.armor = MaxArmor;
                if (m.reward == "medkits") p.medkits = MaxMedkits;
            }
            return true;
        }

        public static void GiveGun(ZombieLandProgress p, ZLGun gun, int rounds)
        {
            var g = p.Gun(gun.id);
            if (g == null)
            {
                g = new ZLOwnedGun { id = gun.id };
                if (gun.isRelic) p.guns.RemoveAll(x => { var s = ZLCatalog.Gun(x.id); return s != null && s.isRelic; }); // one relic at a time
                p.guns.Add(g);
            }
            var e = ZLShop.Effective(p, gun);
            int total = g.mag + g.reserve + rounds;
            g.mag = Math.Min(e.magazine, total);
            g.reserve = Math.Min(e.maxReserve, total - g.mag);
        }

        /// <summary>An ammo box: tops up every gun by about two magazines (relics too, a little).</summary>
        public static void AmmoBox(ZombieLandProgress p)
        {
            foreach (var g in p.guns)
            {
                var spec = ZLShop.Effective(p, ZLCatalog.Gun(g.id));
                if (spec == null) continue;
                int add = spec.isRelic ? spec.magazine / 2 : Math.Max(spec.magazine * 2, 10);
                g.reserve = Math.Min(spec.maxReserve, g.reserve + add);
            }
        }

        /// <summary>Star Shards paid for clearing Zombie Land.</summary>
        public const int ClearReward = 3000;
    }
}
