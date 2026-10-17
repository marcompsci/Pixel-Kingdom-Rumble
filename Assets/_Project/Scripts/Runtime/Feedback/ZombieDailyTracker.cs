using System;
using System.Collections.Generic;
using PKR.Core;

namespace PKR
{
    /// <summary>
    /// The game reports what happened (a kill, a revive, a round reached) and this pays daily orders, grows the streak,
    /// checks the zombie achievements and notices new LOCKER items. It returns lines for the caller to show as toasts.
    /// </summary>
    public static class ZombieDailyTracker
    {
        /// <summary>Tests can pin the clock.</summary>
        public static DateTime? NowOverride;
        public static DateTime Now => NowOverride ?? DateTime.UtcNow;
        public static int Today => ZombieDaily.DayOf(Now);

        static ZombieCareer Career
        {
            get
            {
                var save = Services.Save != null ? Services.Save.Data : null;
                if (save == null) return null;
                if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
                return save.zombieCareer;
            }
        }

        static CosmeticStats Stats()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            return CosmeticStats.From(save != null ? save.zombieCareer : null, save != null ? save.zombieLand : null);
        }

        /// <summary>Count something for today's orders. Returns what to tell the player (empty if nothing finished).</summary>
        public static List<string> Report(DailyKind kind, int amount, string mapId = "") => Report(kind, amount, mapId, null);

        static List<string> Report(DailyKind kind, int amount, string mapId, CosmeticStats? was)
        {
            var lines = new List<string>();
            var c = Career;
            if (c == null || amount <= 0) return lines;
            var before = was ?? Stats();
            int day = Today;
            var done = ZombieDaily.Report(c, day, kind, amount, mapId ?? "", out bool all);
            if (done.Count == 0) { if (was != null) lines.AddRange(AfterProgress(before)); return lines; }
            var orders = ZombieDaily.For(day);
            var save = Services.Save.Data;
            int xp = 0;
            foreach (int i in done)
            {
                var o = orders[i];
                Economy.Grant(save, o.RewardShards);
                xp += o.RewardXp;
                lines.Add($"DAILY ORDER DONE: {o.Text.ToUpperInvariant()}  +{o.RewardShards} SHARDS");
            }
            if (all)
            {
                int bonus = ZombieDaily.StreakBonus(c.streak);
                Economy.Grant(save, bonus);
                lines.Add($"ALL ORDERS DONE!  {c.streak}-DAY STREAK  +{bonus} SHARDS");
            }
            if (xp > 0) ProgressTracker.AddZombieXp(xp);
            lines.AddRange(AfterProgress(before));
            Services.Save.MarkDirty();
            return lines;
        }

        /// <summary>Something that may unlock achievements or LOCKER items happened (a boss, a record). Returns toasts.</summary>
        public static List<string> AfterProgress(CosmeticStats before)
        {
            var lines = new List<string>();
            var after = Stats();
            foreach (var item in ZombieCosmetics.NewlyUnlocked(before, after)) lines.Add("NEW IN THE LOCKER: " + item.Name);
            CheckAchievements();
            return lines;
        }

        public static CosmeticStats Snapshot() => Stats();

        /// <summary>Earns every zombie achievement the save qualifies for (owned ones are skipped).</summary>
        public static void CheckAchievements()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            if (save == null) return;
            foreach (var id in Achievements.ForZombies(save.zombieCareer, save.zombieLand))
            {
                if (GameCenterService.Instance != null) GameCenterService.Instance.Earn(id);
                else save.EarnAchievement(id);
            }
        }

        /// <summary>A boss died (any zombie mode): counts for orders and the career.</summary>
        public static List<string> BossKilled(string mapId = "")
        {
            var c = Career;
            var before = Stats();
            if (c != null) c.bossKills++;
            return Report(DailyKind.BossKills, 1, mapId, before);
        }

        public static List<string> Revived(string mapId = "")
        {
            var c = Career;
            var before = Stats();
            if (c != null) c.revives++;
            return Report(DailyKind.Revives, 1, mapId, before);
        }
    }
}
