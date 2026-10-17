using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>The zombie career, shared by Zombie Land, Zombie Survival and Last Stand (saved in SaveData.zombieCareer).</summary>
    [Serializable]
    public class ZombieCareer
    {
        /// <summary>Survivor Rank XP.</summary>
        public int xp;
        /// <summary>Highest rank already paid out (Star Shards are paid once per rank).</summary>
        public int rankPaid = 1;
        /// <summary>The zombie mode and map picked last on the ZOMBIES menu (so PLAY repeats it).</summary>
        public int lastMode;
        /// <summary>Best rounds per map and mode: "manor_Endless" -> 23.</summary>
        public List<string> bestKeys = new List<string>();
        public List<int> bestRounds = new List<int>();
        public int raidWins, hardcoreRuns, totalKills, coopRuns;
        /// <summary>Last Stand runs finished (the first few get the rookie warm-up).</summary>
        public int lastStandRuns;
        /// <summary>Hidden bosses beaten: "lantern_wraith" (Manor, Hardcore), "hollow_king" (Last Stand).</summary>
        public List<string> secrets = new List<string>();
        /// <summary>Weekly challenge: the weeks finished ("W41"), this week's key and best round, weeks won.</summary>
        public List<string> weeklyDone = new List<string>();
        public string weeklyKey = "";
        public int weeklyBest, weeklyWins;
        /// <summary>Daily orders (ZombieDaily): the day they're for, progress on each, which are paid (bits), the streak.</summary>
        public int dailyDay = -1;
        public List<int> dailyProgress = new List<int>();
        public int dailyPaid, streak, streakDay = -10, bestStreak, dailyDays;
        /// <summary>Teammates revived and bosses beaten (all zombie modes).</summary>
        public int revives, bossKills;
        /// <summary>Equipped cosmetics (ZombieCosmetics ids; "" = none).</summary>
        public string tracer = "", title = "";
        /// <summary>Prestige level (0-10), lifetime XP when it last went up, the highest rank paid since then.</summary>
        public int prestige, prestigeBase, seasonPaid = 1;
        /// <summary>Weapon mastery: kills per gun id (parallel lists; JsonUtility has no dictionaries).</summary>
        public List<string> gunKillIds = new List<string>();
        public List<int> gunKills = new List<int>();

        public int Best(string map, SurvivalMode mode)
        {
            int i = bestKeys.IndexOf(map + "_" + mode);
            return i >= 0 && i < bestRounds.Count ? bestRounds[i] : 0;
        }

        /// <summary>Records a round reached; true if it's a new best for that map and mode.</summary>
        public bool RecordRound(string map, SurvivalMode mode, int round)
        {
            string key = map + "_" + mode;
            int i = bestKeys.IndexOf(key);
            if (i < 0) { bestKeys.Add(key); bestRounds.Add(round); return round > 0; }
            while (bestRounds.Count <= i) bestRounds.Add(0);
            if (round <= bestRounds[i]) return false;
            bestRounds[i] = round;
            return true;
        }

        public bool Has(string secret) => secrets.Contains(secret);

        public void Sanitize()
        {
            if (bestKeys == null) bestKeys = new List<string>();
            if (bestRounds == null) bestRounds = new List<int>();
            if (secrets == null) secrets = new List<string>();
            if (weeklyDone == null) weeklyDone = new List<string>();
            if (weeklyKey == null) weeklyKey = "";
            if (dailyProgress == null) dailyProgress = new List<int>();
            while (dailyProgress.Count < ZombieDaily.Orders) dailyProgress.Add(0);
            if (tracer == null) tracer = "";
            if (title == null) title = "";
            while (bestRounds.Count < bestKeys.Count) bestRounds.Add(0);
            if (xp < 0) xp = 0;
            if (rankPaid < 1) rankPaid = 1;
            if (gunKillIds == null) gunKillIds = new List<string>();
            if (gunKills == null) gunKills = new List<int>();
            while (gunKills.Count < gunKillIds.Count) gunKills.Add(0);
            while (gunKills.Count > gunKillIds.Count) gunKills.RemoveAt(gunKills.Count - 1);
            prestige = Math.Max(0, Math.Min(ZombiePrestige.Max, prestige));
            if (prestige == 0) prestigeBase = 0;
            prestigeBase = Math.Max(0, Math.Min(xp, prestigeBase));
            if (seasonPaid < 1) seasonPaid = 1;
        }
    }

    /// <summary>Something a Survivor Rank unlocks.</summary>
    public struct RankUnlock
    {
        public int rank;
        public string id, name, info;
        public RankUnlock(int rank, string id, string name, string info) { this.rank = rank; this.id = id; this.name = name; this.info = info; }
    }

    /// <summary>
    /// SURVIVOR RANK: one career across every zombie mode. Players asked for progression that gives a reason to keep
    /// playing; they hated slow grinds for tiny boosts, energy timers and paywalls. So ranks come quickly at first,
    /// every rank pays Star Shards, the unlocks are things you feel (starting guns, field kits), and nothing is sold.
    /// </summary>
    public static class SurvivorRank
    {
        public const int MaxRank = 30;

        /// <summary>Total XP needed to reach a rank: 0, 150, 350, 600, 900 ... (each rank 50 more than the last).</summary>
        public static int XpAt(int rank)
        {
            int r = Math.Max(1, Math.Min(MaxRank, rank)) - 1;
            return 150 * r + 25 * r * (r - 1);
        }

        public static int RankFor(int xp)
        {
            int r = 1;
            while (r < MaxRank && xp >= XpAt(r + 1)) r++;
            return r;
        }

        public static float Progress01(int xp)
        {
            int r = RankFor(xp);
            if (r >= MaxRank) return 1f;
            int a = XpAt(r), b = XpAt(r + 1);
            return (xp - a) / (float)Math.Max(1, b - a);
        }

        public static string Title(int rank)
        {
            if (rank >= 30) return "UNDYING";
            if (rank >= 25) return "LEGEND";
            if (rank >= 20) return "WARDEN";
            if (rank >= 15) return "SLAYER";
            if (rank >= 10) return "VETERAN";
            if (rank >= 6) return "SURVIVOR";
            if (rank >= 3) return "SCAVENGER";
            return "FRESH MEAT";
        }

        /// <summary>Star Shards paid on reaching a rank.</summary>
        public static int ShardsFor(int rank) => 40 + 10 * rank;

        public static readonly RankUnlock[] Unlocks =
        {
            new RankUnlock(2, "kit_points", "POCKET CHANGE", "Survival & Last Stand: start with +250 points"),
            new RankUnlock(3, "starter_1", "STARTER GUN I", "Survival & Last Stand: start with a better gun"),
            new RankUnlock(4, "zl_medic", "FIELD MEDIC", "Zombie Land: wake up at the safehouse with 2 medkits"),
            new RankUnlock(5, "kit_points2", "DEEP POCKETS", "Survival & Last Stand: start with +500 points"),
            new RankUnlock(6, "starter_2", "STARTER GUN II", "Survival & Last Stand: start with a shotgun"),
            new RankUnlock(8, "zl_scout", "SCOUT'S EYE", "Zombie Land: gun and ammo caches show on the minimap"),
            new RankUnlock(10, "zl_discount", "REGULAR", "Zombie Land: safehouse supplies cost 15% less"),
            new RankUnlock(12, "starter_3", "STARTER GUN III", "Survival & Last Stand: start with an automatic"),
            new RankUnlock(15, "kit_spin", "LUCKY FIRST SPIN", "Survival & Last Stand: your first mystery box spin is free"),
            new RankUnlock(20, "zl_armor", "PADDED", "Zombie Land: wake up with half armor"),
            new RankUnlock(25, "kit_forge", "FORGE FRIEND", "Survival & Last Stand: the upgrade forge costs 40% less"),
            new RankUnlock(30, "undying", "UNDYING", "A gold title and the respect of the dead"),
        };

        public static bool Has(int xp, string unlockId)
        {
            int rank = RankFor(xp);
            foreach (var u in Unlocks) if (u.id == unlockId) return rank >= u.rank;
            return false;
        }

        /// <summary>The next unlock still ahead of this rank (rank 0 if none).</summary>
        public static RankUnlock Next(int rank)
        {
            foreach (var u in Unlocks) if (u.rank > rank) return u;
            return default;
        }

        /// <summary>Starting gun tier from the Starter Gun unlocks (0 = the basic gun).</summary>
        public static int StarterTier(int xp) => Has(xp, "starter_3") ? 3 : Has(xp, "starter_2") ? 2 : Has(xp, "starter_1") ? 1 : 0;

        /// <summary>Bonus starting points from the Pocket Change / Deep Pockets kits.</summary>
        public static int BonusPoints(int xp) => Has(xp, "kit_points2") ? 500 : Has(xp, "kit_points") ? 250 : 0;

        // ---- XP -----------------------------------------------------------------------------------------------

        /// <summary>
        /// XP for a Survival or Last Stand run: 20 a round survived and 1 per 2 kills; a Raid win +250; Hardcore half again.
        /// </summary>
        public static int RunXp(SurvivalMode mode, int round, int kills, bool won)
        {
            int survived = Math.Max(0, won ? round : round - 1);
            int xp = survived * 20 + kills / 2;
            if (won) xp += 250;
            if (mode == SurvivalMode.Hardcore) xp = (int)Math.Round(xp * 1.5f);
            return xp;
        }

        public const int ZLKillXp = 1, ZLNestXp = 40, ZLMissionXp = 100, ZLStoryMissionXp = 150, ZLBossXp = 800, HiddenBossXp = 400;

        /// <summary>Adds XP; returns the ranks newly reached (each to be paid once).</summary>
        public static List<int> Grant(ZombieCareer c, int xp)
        {
            var reached = new List<int>();
            if (c == null || xp <= 0) return reached;
            c.xp += xp;
            int now = RankFor(c.xp);
            while (c.rankPaid < now)
            {
                c.rankPaid++;
                reached.Add(c.rankPaid);
            }
            reached.AddRange(ZombiePrestige.SeasonRanksReached(c));   // after a prestige, the climb pays again
            return reached;
        }
    }
}
