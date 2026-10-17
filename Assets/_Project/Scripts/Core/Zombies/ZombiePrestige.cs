using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// PRESTIGE: at Survivor Rank 30 a player can start the ranks again (up to ten times) for a prestige badge, a big
    /// Star Shard payout and prestige-only looks. Nothing is lost: every rank unlock stays (they follow lifetime XP),
    /// the XP leaderboard keeps counting, and each rank climbed again pays its Star Shards again.
    /// </summary>
    public static class ZombiePrestige
    {
        public const int Max = 10;

        /// <summary>The rank shown now: this prestige's climb (lifetime rank before the first prestige).</summary>
        public static int Rank(ZombieCareer c) => c == null ? 1 : SurvivorRank.RankFor(SeasonXp(c));

        /// <summary>XP earned since the last prestige.</summary>
        public static int SeasonXp(ZombieCareer c) => c == null ? 0 : Math.Max(0, c.xp - (c.prestige > 0 ? c.prestigeBase : 0));

        public static float Progress01(ZombieCareer c) => SurvivorRank.Progress01(SeasonXp(c));

        public static bool CanPrestige(ZombieCareer c) => c != null && c.prestige < Max && Rank(c) >= SurvivorRank.MaxRank;

        /// <summary>Star Shards for reaching a prestige level.</summary>
        public static int Reward(int level) => 500 + 250 * Math.Max(1, level);

        /// <summary>Starts the ranks again. Returns the Star Shards to grant (0 if not allowed).</summary>
        public static int Prestige(ZombieCareer c)
        {
            if (!CanPrestige(c)) return 0;
            c.prestige++;
            c.prestigeBase = c.xp;
            c.seasonPaid = 1;
            return Reward(c.prestige);
        }

        /// <summary>Ranks newly reached this prestige (each pays once), after XP was added.</summary>
        public static List<int> SeasonRanksReached(ZombieCareer c)
        {
            var reached = new List<int>();
            if (c == null || c.prestige <= 0) return reached;
            int now = Rank(c);
            while (c.seasonPaid < now) { c.seasonPaid++; reached.Add(c.seasonPaid); }
            return reached;
        }

        public static string Badge(int level) => level <= 0 ? "" : "P" + level;

        /// <summary>"PRESTIGE 2  ·  SURVIVOR RANK 7  ·  SURVIVOR" (no prestige part before the first).</summary>
        public static string Label(ZombieCareer c)
        {
            int r = Rank(c);
            string p = c != null && c.prestige > 0 ? $"PRESTIGE {c.prestige}  ·  " : "";
            return $"{p}SURVIVOR RANK {r}  ·  {SurvivorRank.Title(r)}";
        }

        public static string Info => "Start Survivor Rank again from 1. You keep every unlock, gun kit and look; " +
                                     "every rank pays its Star Shards again, and you get a prestige badge.";
    }

    /// <summary>Weapon finishes: a gun's kill count earns bronze, silver and gold; gold on six guns makes obsidian.</summary>
    public enum WeaponFinish { None = 0, Bronze = 1, Silver = 2, Gold = 3, Obsidian = 4 }

    /// <summary>
    /// WEAPON MASTERY: kills with each gun, across every zombie mode. Finishes are looks only (the gun's name colour and
    /// its bullets when no LOCKER tracer is worn), so mastery never makes one player stronger than another online.
    /// </summary>
    public static class WeaponMastery
    {
        public static readonly int[] Steps = { 50, 150, 400 };
        public const int ObsidianGolds = 6;

        /// <summary>Guns that count (everything but relics).</summary>
        public static IEnumerable<ZLGun> Guns()
        {
            foreach (var g in ZLCatalog.Guns) if (!g.isRelic) yield return g;
        }

        public static int Kills(ZombieCareer c, string gunId)
        {
            if (c == null || string.IsNullOrEmpty(gunId) || c.gunKillIds == null || c.gunKills == null) return 0;
            int i = c.gunKillIds.IndexOf(gunId);
            return i >= 0 && i < c.gunKills.Count ? c.gunKills[i] : 0;
        }

        /// <summary>Counts kills with a gun. Returns the finish it reached if this made it go up, else None.</summary>
        public static WeaponFinish AddKills(ZombieCareer c, string gunId, int n = 1)
        {
            if (c == null || n <= 0 || string.IsNullOrEmpty(gunId)) return WeaponFinish.None;
            var g = ZLCatalog.Gun(gunId);
            if (g == null || g.isRelic) return WeaponFinish.None;
            c.Sanitize();
            var before = FinishFor(c, gunId);
            int i = c.gunKillIds.IndexOf(gunId);
            if (i < 0) { c.gunKillIds.Add(gunId); c.gunKills.Add(0); i = c.gunKillIds.Count - 1; }
            c.gunKills[i] = (int)Math.Min(int.MaxValue, (long)c.gunKills[i] + n);
            var after = FinishFor(c, gunId);
            return after > before ? after : WeaponFinish.None;
        }

        public static WeaponFinish FinishByKills(int kills) =>
            kills >= Steps[2] ? WeaponFinish.Gold : kills >= Steps[1] ? WeaponFinish.Silver : kills >= Steps[0] ? WeaponFinish.Bronze : WeaponFinish.None;

        public static int Golds(ZombieCareer c)
        {
            int n = 0;
            foreach (var g in Guns()) if (Kills(c, g.id) >= Steps[2]) n++;
            return n;
        }

        public static WeaponFinish FinishFor(ZombieCareer c, string gunId)
        {
            var f = FinishByKills(Kills(c, gunId));
            return f == WeaponFinish.Gold && Golds(c) >= ObsidianGolds ? WeaponFinish.Obsidian : f;
        }

        /// <summary>Kills needed for the next finish (0 at gold).</summary>
        public static int NextGoal(int kills)
        {
            foreach (int s in Steps) if (kills < s) return s;
            return 0;
        }

        public static string Name(WeaponFinish f) => f == WeaponFinish.None ? "" : f.ToString().ToUpperInvariant();

        /// <summary>Stars before a gun's name on the HUD (one per finish).</summary>
        public static string Stars(WeaponFinish f) => f == WeaponFinish.None ? "" : new string('*', Math.Min(4, (int)f)) + " ";

        public static (byte r, byte g, byte b) Color(WeaponFinish f)
        {
            switch (f)
            {
                case WeaponFinish.Bronze: return (205, 127, 50);
                case WeaponFinish.Silver: return (200, 210, 225);
                case WeaponFinish.Gold: return (255, 205, 60);
                case WeaponFinish.Obsidian: return (150, 110, 255);
                default: return (255, 255, 255);
            }
        }

        /// <summary>Gold and obsidian also colour the gun's bullets (unless a LOCKER tracer is worn).</summary>
        public static bool TintsBullets(WeaponFinish f) => f >= WeaponFinish.Gold;
    }
}
