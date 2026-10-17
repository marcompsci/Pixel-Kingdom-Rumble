using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>What a daily order asks for.</summary>
    public enum DailyKind { Kills, ReachRound, Revives, BoxSpins, Perks, Doors, Missions, BossKills }

    /// <summary>One daily order: a kind, a target, an optional map ("" = any Last Stand map) and its reward.</summary>
    public sealed class DailyOrder
    {
        public DailyKind Kind;
        public int Target;
        public string MapId = "";
        public int RewardShards, RewardXp;

        public string Text
        {
            get
            {
                string where = string.IsNullOrEmpty(MapId) ? "in Last Stand" : "on " + LastStandMap.Get(MapId).Title;
                switch (Kind)
                {
                    case DailyKind.Kills: return $"Kill {Target} zombies {where}";
                    case DailyKind.ReachRound: return $"Reach round {Target} {where}";
                    case DailyKind.Revives: return $"Revive teammates {Target} times";
                    case DailyKind.BoxSpins: return $"Open the mystery box {Target} times";
                    case DailyKind.Perks: return $"Buy {Target} perks";
                    case DailyKind.Doors: return $"Clear {Target} debris doors";
                    case DailyKind.Missions: return $"Finish {Target} Zombie Land missions";
                    default: return $"Defeat {Target} bosses";
                }
            }
        }

        /// <summary>Rounds take the best value reached; everything else adds up.</summary>
        public bool IsMax => Kind == DailyKind.ReachRound;
    }

    /// <summary>
    /// DAILY ORDERS: three small goals a day, the same for everyone, new at 00:00 UTC. Finishing all three keeps a
    /// streak going (a bigger bonus each day, up to a week). Players wanted reasons to come back each day without
    /// timers or energy, and without punishing a missed day hard: a missed day only restarts the streak, nothing is lost.
    /// Nothing here is sold.
    /// </summary>
    public static class ZombieDaily
    {
        /// <summary>Day 0 is 1 January 2026 (UTC).</summary>
        public static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public const int Orders = 3, MaxStreakBonusDays = 7, StreakBonusPerDay = 50;

        public static int DayOf(DateTime utc) => (int)Math.Floor((utc.ToUniversalTime().Date - Epoch).TotalDays);

        public static TimeSpan TimeLeft(DateTime utc) => Epoch.AddDays(DayOf(utc) + 1) - utc.ToUniversalTime();

        public static string LeftText(DateTime utc)
        {
            var t = TimeLeft(utc);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m left" : $"{Math.Max(1, (int)t.TotalMinutes)}m left";
        }

        static uint Hash(int day, uint salt)
        {
            uint x = (uint)day * 2654435761u ^ salt * 2246822519u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; x *= 3266489917u; x ^= x >> 16;
            return x;
        }

        // an easy, a medium and a hard order each day (kinds never repeat within a day)
        static readonly DailyKind[] Easy = { DailyKind.Kills, DailyKind.BoxSpins, DailyKind.Doors, DailyKind.Perks };
        static readonly DailyKind[] Medium = { DailyKind.ReachRound, DailyKind.Kills, DailyKind.Missions, DailyKind.Revives };
        static readonly DailyKind[] Hard = { DailyKind.ReachRound, DailyKind.BossKills, DailyKind.Kills };

        public static DailyOrder[] For(int day)
        {
            var list = new List<DailyOrder>();
            var used = new HashSet<DailyKind>();
            var tiers = new[] { Easy, Medium, Hard };
            for (int tier = 0; tier < 3; tier++)
            {
                var pool = tiers[tier];
                int start = (int)(Hash(day, (uint)(10 + tier)) % (uint)pool.Length);
                DailyKind kind = pool[start];
                for (int k = 0; k < pool.Length && used.Contains(kind); k++) kind = pool[(start + k + 1) % pool.Length];
                if (used.Contains(kind)) kind = DailyKind.Doors;
                used.Add(kind);
                var o = new DailyOrder { Kind = kind, RewardShards = 40 + 30 * tier, RewardXp = 100 + 100 * tier };
                uint h = Hash(day, (uint)(20 + tier));
                switch (kind)
                {
                    case DailyKind.Kills: o.Target = new[] { 60, 150, 300 }[tier]; break;
                    case DailyKind.ReachRound: o.Target = tier == 1 ? 8 : 12; break;
                    case DailyKind.Revives: o.Target = 3; break;
                    case DailyKind.BoxSpins: o.Target = 3; break;
                    case DailyKind.Perks: o.Target = 4; break;
                    case DailyKind.Doors: o.Target = 4; break;
                    case DailyKind.Missions: o.Target = 2; break;
                    default: o.Target = 1; break;
                }
                // round goals (and the hard kill goal) name a map, so each map gets its days
                if (kind == DailyKind.ReachRound || kind == DailyKind.Kills && tier == 2)
                    o.MapId = LastStandMap.All[(int)(h % (uint)LastStandMap.All.Length)].Id;
                list.Add(o);
            }
            return list.ToArray();
        }

        public static DailyOrder[] Today(DateTime utc) => For(DayOf(utc));

        /// <summary>Moves the career's daily record onto this day (yesterday's progress is dropped).</summary>
        public static void Roll(ZombieCareer c, int day)
        {
            c.Sanitize();
            if (c.dailyDay == day) return;
            c.dailyDay = day;
            c.dailyProgress.Clear();
            for (int i = 0; i < Orders; i++) c.dailyProgress.Add(0);
            c.dailyPaid = 0;
            // a missed day ends the streak (it shows 0 until today's three are done)
            if (c.streakDay < day - 1) c.streak = 0;
        }

        /// <summary>
        /// Something happened that orders might count. Returns the indexes of orders this finished (pay each once);
        /// allDone is true when this finished the day's last order (the streak grows; pay StreakBonus).
        /// </summary>
        public static List<int> Report(ZombieCareer c, int day, DailyKind kind, int amount, string mapId, out bool allDone)
        {
            allDone = false;
            var done = new List<int>();
            if (c == null || amount <= 0) return done;
            Roll(c, day);
            var orders = For(day);
            for (int i = 0; i < orders.Length; i++)
            {
                var o = orders[i];
                if (o.Kind != kind || (c.dailyPaid & (1 << i)) != 0) continue;
                if (!string.IsNullOrEmpty(o.MapId) && o.MapId != mapId) continue;
                c.dailyProgress[i] = o.IsMax ? Math.Max(c.dailyProgress[i], amount) : c.dailyProgress[i] + amount;
                if (c.dailyProgress[i] < o.Target) continue;
                c.dailyProgress[i] = o.Target;
                c.dailyPaid |= 1 << i;
                done.Add(i);
            }
            if (done.Count > 0 && c.dailyPaid == (1 << Orders) - 1)
            {
                allDone = true;
                c.streak = c.streakDay == day - 1 ? c.streak + 1 : 1;
                c.streakDay = day;
                if (c.streak > c.bestStreak) c.bestStreak = c.streak;
                c.dailyDays++;
            }
            return done;
        }

        public static int StreakBonus(int streak) => StreakBonusPerDay * Math.Max(1, Math.Min(streak, MaxStreakBonusDays));

        public static int DoneCount(ZombieCareer c, int day)
        {
            if (c == null || c.dailyDay != day) return 0;
            int n = 0;
            for (int i = 0; i < Orders; i++) if ((c.dailyPaid & (1 << i)) != 0) n++;
            return n;
        }

        public static int ProgressOf(ZombieCareer c, int day, int index) =>
            c != null && c.dailyDay == day && c.dailyProgress != null && index < c.dailyProgress.Count ? c.dailyProgress[index] : 0;

        /// <summary>The streak as it stands today (0 if yesterday was missed and today isn't done yet).</summary>
        public static int StreakNow(ZombieCareer c, int day) => c == null ? 0 : c.streakDay >= day - 1 ? c.streak : 0;
    }
}
