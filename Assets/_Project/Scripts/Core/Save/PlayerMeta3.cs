using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Story Quest: how many times a level has been started (shown on level select and the clear screen).</summary>
    public static class StoryAttempts
    {
        public static void Begin(LevelRecord rec) { if (rec != null) rec.attempts = Math.Min(int.MaxValue - 1, Math.Max(0, rec.attempts) + 1); }

        /// <summary>Clear screen line: "Cleared on attempt 4!" the first time, "Attempt 7" after that.</summary>
        public static string ClearLine(int attempts, bool firstClear)
        {
            if (attempts <= 0) return "";
            if (firstClear) return attempts == 1 ? "Cleared on your first try!" : $"Cleared on attempt {attempts}!";
            return $"Attempt {attempts}";
        }

        /// <summary>Level select: "3 tries so far" for an uncleared level, "" when never tried.</summary>
        public static string TriesLine(int attempts) => attempts <= 0 ? "" : attempts == 1 ? "1 try so far" : $"{attempts} tries so far";
    }

    /// <summary>A world's completion: clears, shards and secrets, each counted equally where the world has them.</summary>
    public static class StoryCompletion
    {
        public static int Percent(int cleared, int levels, int shards, int totalShards, int secrets, int totalSecrets)
        {
            double sum = 0; int parts = 0;
            if (levels > 0) { sum += Math.Min(1.0, (double)cleared / levels); parts++; }
            if (totalShards > 0) { sum += Math.Min(1.0, (double)shards / totalShards); parts++; }
            if (totalSecrets > 0) { sum += Math.Min(1.0, (double)secrets / totalSecrets); parts++; }
            if (parts == 0) return 0;
            int p = (int)Math.Floor(sum / parts * 100.0 + 1e-9);
            return Math.Max(0, Math.Min(100, p));
        }
    }

    /// <summary>Speedrun total: every cleared level's best time added up.</summary>
    public static class StorySpeedrun
    {
        public static (float seconds, int levels) Total(SaveData s, IEnumerable<string> levelIds)
        {
            float t = 0f; int n = 0;
            if (s == null || levelIds == null) return (0f, 0);
            foreach (var id in levelIds)
            {
                var r = s.GetLevel(id);
                if (r == null || !r.completed || r.bestTimeSeconds <= 0f || float.IsNaN(r.bestTimeSeconds)) continue;
                t += r.bestTimeSeconds; n++;
            }
            return (t, n);
        }
    }

    /// <summary>Versus: your own record with each hero you play as.</summary>
    public static class HeroRecords
    {
        public static HeadToHeadRecord Get(SaveData s, string hero)
        {
            if (s == null || s.lifetime == null || string.IsNullOrEmpty(hero)) return null;
            foreach (var r in s.lifetime.heroRecords) if (r.id == hero) return r;
            return null;
        }

        public static void Record(SaveData s, string hero, bool won)
        {
            if (s == null || string.IsNullOrEmpty(hero)) return;
            var r = Get(s, hero);
            if (r == null) { r = new HeadToHeadRecord { id = hero }; s.lifetime.heroRecords.Add(r); }
            if (won) r.wins++; else r.losses++;
        }

        public static string Line(HeadToHeadRecord r) => r == null || r.wins + r.losses == 0 ? "no Versus matches yet" : $"YOUR RECORD {r.wins}W - {r.losses}L";

        /// <summary>Your top Versus hero: most wins (ties: fewer losses), or null when none has a win.</summary>
        public static HeadToHeadRecord Best(SaveData s)
        {
            HeadToHeadRecord best = null;
            if (s == null || s.lifetime == null) return null;
            foreach (var r in s.lifetime.heroRecords)
                if (r != null && r.wins > 0 && (best == null || r.wins > best.wins || (r.wins == best.wins && r.losses < best.losses))) best = r;
            return best;
        }
    }

    /// <summary>Last Stand: how a run compares with your average on that map.</summary>
    public static class StandPace
    {
        public static string VsAverage(int round, float average, int runsBefore)
        {
            if (runsBefore < 1) return "";
            float d = round - average;
            if (Math.Abs(d) < 0.5f) return "right on your average";
            string n = Math.Abs(d).ToString("0.#");
            return d > 0 ? $"{n} rounds above your average!" : $"{n} rounds below your average";
        }

        /// <summary>True on the round that first beats the best you came in with.</summary>
        public static bool IsNewBestRound(int round, int bestBefore) => bestBefore > 0 && round == bestBefore + 1;
    }

    /// <summary>A welcome-back gift for opening the game after a week or more away (once per return).</summary>
    public static class ComebackGift
    {
        public const int AwayDays = 7, Shards = 25;

        public static int DaysAway(int lastDayKey, DateTime today)
        {
            if (lastDayKey <= 0) return 0;
            int y = lastDayKey / 10000, m = lastDayKey / 100 % 100, d = lastDayKey % 100;
            if (y < 2000 || m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(y, m)) return 0;
            return Math.Max(0, (int)(today.Date - new DateTime(y, m, d)).TotalDays);
        }

        /// <summary>The gift for today given the last day the game was opened (0 when not due).</summary>
        public static int For(int lastDayKey, DateTime today) => DaysAway(lastDayKey, today) >= AwayDays ? Shards : 0;
    }
}
