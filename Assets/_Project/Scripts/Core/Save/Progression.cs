using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>What a daily challenge counts.</summary>
    public enum ChallengeKind { ClearLevels, CollectShards, WinVersus, DefeatEnemies, SurvivalRound, ClearMaze, ClearEvent, PlayMatches }

    /// <summary>One of today's challenges in the save.</summary>
    [Serializable]
    public class ChallengeRecord
    {
        public string id = "";
        public int progress;
        public bool done;
    }

    /// <summary>A challenge template from the pool.</summary>
    public struct ChallengeSpec
    {
        public string id, text;
        public ChallengeKind kind;
        public int target, shards, xp;
        /// <summary>SurvivalRound: the target is a round to reach in one run (best, not a running total).</summary>
        public bool best;
    }

    /// <summary>
    /// The player level: XP from everything (levels, matches, survival rounds, challenges). Each level pays Star Shards,
    /// and every 5th level pays a big bonus on top. Pure, so it is unit-tested.
    /// </summary>
    public static class PlayerLevel
    {
        public const int MaxLevel = 99;

        // XP sources
        public const int LevelClearXp = 60, FirstClearBonusXp = 40, VersusWinXp = 50, VersusLossXp = 20, ArenaXp = 30,
                         SurvivalRoundXp = 15, EventClearXp = 80;

        /// <summary>XP needed to go from level n to n+1: 120, 170, 220 ... (gentle, then steady).</summary>
        public static int XpToNext(int level) => 120 + 50 * (Math.Max(1, level) - 1);

        /// <summary>Total XP at which a level starts (level 1 = 0).</summary>
        public static int XpAtLevel(int level)
        {
            int total = 0;
            for (int l = 1; l < Math.Min(level, MaxLevel); l++) total += XpToNext(l);
            return total;
        }

        public static int LevelFor(int xp)
        {
            int level = 1;
            int need = XpToNext(level);
            while (level < MaxLevel && xp >= need)
            {
                xp -= need;
                level++;
                need = XpToNext(level);
            }
            return level;
        }

        /// <summary>0..1 through the current level.</summary>
        public static float Progress01(int xp)
        {
            int level = LevelFor(xp);
            if (level >= MaxLevel) return 1f;
            return (xp - XpAtLevel(level)) / (float)XpToNext(level);
        }

        /// <summary>Star Shards for reaching a level (milestone levels add MilestoneBonus).</summary>
        public static int ShardReward(int level) => 20 + 5 * level + (IsMilestone(level) ? MilestoneBonus : 0);

        public const int MilestoneBonus = 150;

        /// <summary>Every 5th level is a milestone with a bonus (enough for a palette in the shop).</summary>
        public static bool IsMilestone(int level) => level > 1 && level % 5 == 0;

        /// <summary>
        /// Adds XP and pays every level reached on the way. Returns the levels gained (empty if none).
        /// </summary>
        public static List<int> AddXp(SaveData save, int xp)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            var gained = new List<int>();
            if (xp <= 0) return gained;
            int before = LevelFor(save.playerXp);
            save.playerXp = (int)Math.Min(int.MaxValue / 2, (long)save.playerXp + xp);
            int after = LevelFor(save.playerXp);
            for (int l = before + 1; l <= after; l++)
            {
                gained.Add(l);
                Economy.Grant(save, ShardReward(l));
            }
            return gained;
        }
    }

    /// <summary>
    /// Three daily challenges, picked from the pool by the date (same three for everyone on a given day, all different
    /// kinds). Progress is reported from gameplay; a finished challenge pays its shards and XP at once.
    /// </summary>
    public static class DailyChallenges
    {
        public const int PerDay = 3;

        public static readonly ChallengeSpec[] Pool =
        {
            new ChallengeSpec { id = "clear_2", text = "Clear 2 levels", kind = ChallengeKind.ClearLevels, target = 2, shards = 40, xp = 60 },
            new ChallengeSpec { id = "clear_4", text = "Clear 4 levels", kind = ChallengeKind.ClearLevels, target = 4, shards = 70, xp = 100 },
            new ChallengeSpec { id = "shards_40", text = "Collect 40 shards in levels", kind = ChallengeKind.CollectShards, target = 40, shards = 40, xp = 60 },
            new ChallengeSpec { id = "shards_100", text = "Collect 100 shards in levels", kind = ChallengeKind.CollectShards, target = 100, shards = 80, xp = 110 },
            new ChallengeSpec { id = "win_2", text = "Win 2 Versus matches", kind = ChallengeKind.WinVersus, target = 2, shards = 50, xp = 70 },
            new ChallengeSpec { id = "win_4", text = "Win 4 Versus matches", kind = ChallengeKind.WinVersus, target = 4, shards = 80, xp = 110 },
            new ChallengeSpec { id = "foes_15", text = "Defeat 15 foes", kind = ChallengeKind.DefeatEnemies, target = 15, shards = 40, xp = 60 },
            new ChallengeSpec { id = "foes_40", text = "Defeat 40 foes", kind = ChallengeKind.DefeatEnemies, target = 40, shards = 70, xp = 100 },
            new ChallengeSpec { id = "round_5", text = "Reach round 5 in Zombie Survival", kind = ChallengeKind.SurvivalRound, target = 5, shards = 60, xp = 80, best = true },
            new ChallengeSpec { id = "round_10", text = "Reach round 10 in Zombie Survival", kind = ChallengeKind.SurvivalRound, target = 10, shards = 120, xp = 160, best = true },
            new ChallengeSpec { id = "maze_1", text = "Clear a maze", kind = ChallengeKind.ClearMaze, target = 1, shards = 50, xp = 70 },
            new ChallengeSpec { id = "event_1", text = "Clear a Holiday or Halloween run", kind = ChallengeKind.ClearEvent, target = 1, shards = 50, xp = 70 },
            new ChallengeSpec { id = "matches_3", text = "Play 3 Versus or Arena matches", kind = ChallengeKind.PlayMatches, target = 3, shards = 40, xp = 60 },
        };

        public static ChallengeSpec? Find(string id)
        {
            foreach (var c in Pool) if (c.id == id) return c;
            return null;
        }

        /// <summary>A stable number for a date (yyyymmdd).</summary>
        public static int DayKey(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

        /// <summary>Today's three picks: deterministic for the day, three different kinds.</summary>
        public static List<ChallengeSpec> PickFor(int dayKey)
        {
            var picks = new List<ChallengeSpec>();
            var kinds = new HashSet<ChallengeKind>();
            uint s = (uint)dayKey * 2654435761u + 12345u;
            int guard = 0;
            while (picks.Count < PerDay && guard++ < 200)
            {
                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                var c = Pool[(int)(s % (uint)Pool.Length)];
                if (kinds.Add(c.kind)) picks.Add(c);
            }
            return picks;
        }

        /// <summary>Rolls the save over to today's challenges if the day changed. True when they were replaced.</summary>
        public static bool EnsureToday(SaveData save, int dayKey)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (save.dailyChallenges == null) save.dailyChallenges = new List<ChallengeRecord>();
            if (save.dailyDayKey == dayKey && save.dailyChallenges.Count == PerDay) return false;
            save.dailyDayKey = dayKey;
            save.dailyChallenges.Clear();
            foreach (var c in PickFor(dayKey)) save.dailyChallenges.Add(new ChallengeRecord { id = c.id });
            return true;
        }

        /// <summary>
        /// Counts progress toward today's challenges. Finished ones pay out (shards now; XP is returned so the caller
        /// can add it and show any level-ups). Returns the challenges completed by this report.
        /// </summary>
        public static List<ChallengeSpec> Report(SaveData save, ChallengeKind kind, int amount, out int xpEarned)
        {
            xpEarned = 0;
            var done = new List<ChallengeSpec>();
            if (save == null || save.dailyChallenges == null || amount <= 0) return done;
            foreach (var rec in save.dailyChallenges)
            {
                if (rec == null || rec.done) continue;
                var spec = Find(rec.id);
                if (!spec.HasValue || spec.Value.kind != kind) continue;
                var c = spec.Value;
                rec.progress = c.best ? Math.Max(rec.progress, amount) : Math.Min(c.target, rec.progress + amount);
                if (rec.progress >= c.target)
                {
                    rec.progress = c.target;
                    rec.done = true;
                    Economy.Grant(save, c.shards);
                    xpEarned += c.xp;
                    done.Add(c);
                }
            }
            return done;
        }
    }
}
