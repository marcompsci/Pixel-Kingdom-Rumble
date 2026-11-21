using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Story Quest: PERFECT clears (S rank, FLAWLESS, every shard and secret) and the clear-screen callouts.</summary>
    public static class StoryPerfect
    {
        public const int Shards = 15;

        public static bool Is(ClearRank rank, bool flawless, int shards, int totalShards, int secrets, int totalSecrets) =>
            rank == ClearRank.S && flawless && shards >= totalShards && secrets >= totalSecrets;

        /// <summary>Marks the level PERFECT; returns the one-off shards (0 when it already was, or the run wasn't).</summary>
        public static int Record(LevelRecord rec, bool perfect)
        {
            if (rec == null || !perfect || rec.perfect) return 0;
            rec.perfect = true;
            return Shards;
        }

        public static int Count(SaveData s)
        {
            int n = 0;
            if (s != null) foreach (var l in s.levels) if (l != null && l.perfect) n++;
            return n;
        }

        /// <summary>"MEDAL UP: GOLD!" when a clear earns a better time medal than your old best did ("" otherwise).</summary>
        public static string MedalUp(float previousBest, float time, float par)
        {
            if (previousBest <= 0f || par <= 0f) return "";
            var before = StoryMedals.For(previousBest, par);
            var now = StoryMedals.For(time, par);
            return now > before ? $"MEDAL UP: {StoryMedals.Name(now)}!" : "";
        }

        /// <summary>How much faster the next medal needs ("GOLD: 3.2s faster"), "" at GOLD or without a par.</summary>
        public static string NextMedalGap(float bestTime, float par)
        {
            if (bestTime <= 0f || par <= 0f) return "";
            var have = StoryMedals.For(bestTime, par);
            if (have >= TimeMedal.Gold) return "";
            var next = StoryMedals.Next(have);
            float gap = bestTime - StoryMedals.Target(next, par);
            return gap > 0f ? $"{StoryMedals.Name(next)}: {gap:0.0}s faster" : "";
        }

        /// <summary>"UP FROM A" when the clear beats the level's old best rank.</summary>
        public static string RankUp(ClearRank? before, ClearRank now) =>
            before.HasValue && now > before.Value ? $"UP FROM {before.Value}!" : "";
    }

    /// <summary>A world at 100% is celebrated once.</summary>
    public static class WorldFull
    {
        public static bool Claim(SaveData s, string world)
        {
            if (s == null || string.IsNullOrEmpty(world) || s.lifetime.worlds100.Contains(world)) return false;
            s.lifetime.worlds100.Add(world);
            return true;
        }
    }

    /// <summary>Badge milestones: every 5 badges pays Star Shards once.</summary>
    public static class BadgeRewards
    {
        public const int Every = 5, Shards = 25;

        /// <summary>Shards owed for milestones reached since the last payout (records them).</summary>
        public static int Pay(SaveData s)
        {
            if (s == null) return 0;
            int reached = s.lifetime.badges.Count / Every;
            int owed = Math.Max(0, reached - s.lifetime.badgeRewards);
            s.lifetime.badgeRewards = Math.Max(s.lifetime.badgeRewards, reached);
            return owed * Shards;
        }

        public static int ToNext(SaveData s) => s == null ? Every : Every - s.lifetime.badges.Count % Every;
    }

    /// <summary>Last Stand accuracy records (only runs with enough shots count).</summary>
    public static class StandAccuracy
    {
        public const int MinShots = 50;

        /// <summary>Records a run's accuracy; true when it's a new best.</summary>
        public static bool Record(SaveData s, int shots, int hits)
        {
            if (s == null || shots < MinShots) return false;
            int acc = StandHistory.Accuracy(shots, hits);
            if (acc <= s.lifetime.bestAccuracy) return false;
            s.lifetime.bestAccuracy = acc;
            return true;
        }
    }

    /// <summary>Lifetime zombie-kill milestones (every 1,000).</summary>
    public static class KillMilestone
    {
        public const int Step = 1000;
        public static int Crossed(int before, int after) => after / Step > before / Step ? after / Step * Step : 0;
    }

    /// <summary>"today", "yesterday", "3 days ago" and "Playing since Nov 2026".</summary>
    public static class PlayDates
    {
        static bool TryDate(int key, out DateTime d)
        {
            d = default;
            int y = key / 10000, m = key / 100 % 100, day = key % 100;
            if (y < 2000 || m < 1 || m > 12 || day < 1 || day > DateTime.DaysInMonth(y, m)) return false;
            d = new DateTime(y, m, day);
            return true;
        }

        public static string Ago(int dayKey, DateTime today)
        {
            if (!TryDate(dayKey, out var d)) return "";
            int n = (int)(today.Date - d).TotalDays;
            return n <= 0 ? "today" : n == 1 ? "yesterday" : $"{n} days ago";
        }

        static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public static string Since(int dayKey) => TryDate(dayKey, out var d) ? $"Playing since {Months[d.Month - 1]} {d.Year}" : "";
    }
}
