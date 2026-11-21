using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Versus: your current and best run of wins in a row.</summary>
    public static class VersusStreak
    {
        /// <summary>Records a finished match; returns the streak after it (0 after a loss).</summary>
        public static int Record(SaveData s, bool won)
        {
            if (s == null) return 0;
            var l = s.lifetime;
            l.vsStreak = won ? Math.Max(0, l.vsStreak) + 1 : 0;
            l.vsBestStreak = Math.Max(l.vsBestStreak, l.vsStreak);
            return l.vsStreak;
        }

        /// <summary>Streaks worth a callout: 3, 5, then every 5.</summary>
        public static bool Milestone(int streak) => streak == 3 || (streak >= 5 && streak % 5 == 0);
    }

    /// <summary>Story Quest: clears in a row without a fall this session ("ON A ROLL").</summary>
    public static class CleanStreak
    {
        public static int Next(int streak, int deaths) => deaths > 0 ? 0 : Math.Max(0, streak) + 1;
        public static bool Milestone(int streak) => streak == 3 || (streak >= 5 && streak % 5 == 0);
    }

    /// <summary>Story Quest results lines.</summary>
    public static class StoryResults
    {
        /// <summary>Finished on your last heart (with more than one to start with).</summary>
        public static bool CloseCall(int health, int maxHealth) => maxHealth > 1 && health == 1;

        /// <summary>"1.2s faster than your old best!" / "0.8s off your best" / "" (no earlier best).</summary>
        public static string VsBest(float time, float previousBest)
        {
            if (previousBest <= 0f || time <= 0f || float.IsNaN(time)) return "";
            float d = previousBest - time;
            if (Math.Abs(d) < 0.05f) return "matched your best!";
            return d > 0f ? $"{d:0.0}s faster than your old best!" : $"{-d:0.0}s off your best";
        }
    }

    /// <summary>Story Quest totals for STORY STATS and PROFILE.</summary>
    public static class StoryRecords
    {
        public static int TotalTries(SaveData s)
        {
            int n = 0;
            if (s != null) foreach (var l in s.levels) if (l != null) n += Math.Max(0, l.attempts);
            return n;
        }

        /// <summary>The level you've started most (at least 3 tries), or null.</summary>
        public static LevelRecord Toughest(SaveData s)
        {
            LevelRecord best = null;
            if (s != null) foreach (var l in s.levels) if (l != null && l.attempts >= 3 && (best == null || l.attempts > best.attempts)) best = l;
            return best;
        }

        /// <summary>Your fastest cleared level (best time), or null.</summary>
        public static LevelRecord Fastest(SaveData s)
        {
            LevelRecord best = null;
            if (s != null) foreach (var l in s.levels)
                if (l != null && l.completed && l.bestTimeSeconds > 0f && (best == null || l.bestTimeSeconds < best.bestTimeSeconds)) best = l;
            return best;
        }

        public static int SRanks(SaveData s)
        {
            int n = 0;
            if (s != null) foreach (var l in s.levels) if (l != null && l.completed && l.BestRank == ClearRank.S) n++;
            return n;
        }
    }

    /// <summary>The main menu's hello for the time of day.</summary>
    public static class Greeting
    {
        public static string For(int hour)
        {
            hour = ((hour % 24) + 24) % 24;
            if (hour < 5) return "Up late, hero? The kingdom never sleeps.";
            if (hour < 12) return "Good morning, hero!";
            if (hour < 17) return "Good afternoon, hero!";
            if (hour < 21) return "Good evening, hero!";
            return "Evening, hero. One more round?";
        }
    }

    /// <summary>This session so far: what changed since the game opened (shown back on the main menu).</summary>
    public static class SessionSummary
    {
        public static List<string> Lines(int[] start, int[] now)
        {
            var o = new List<string>();
            if (start == null || now == null || start.Length != now.Length) return o;
            for (int i = 0; i < now.Length && i < WeeklyRecap.Labels.Length; i++)
            {
                if (i == 4) continue; // time played: not worth a toast
                int d = now[i] - start[i];
                if (d > 0) o.Add($"{WeeklyRecap.Labels[i]} +{d:N0}");
            }
            return o;
        }

        /// <summary>True when there's something new since the summary was last shown.</summary>
        public static bool Changed(int[] shown, int[] now)
        {
            if (now == null) return false;
            if (shown == null || shown.Length != now.Length) return true;
            for (int i = 0; i < now.Length; i++) if (i != 4 && now[i] != shown[i]) return true;
            return false;
        }
    }

    /// <summary>Zombies: the most recent Last Stand run.</summary>
    public static class StandLast
    {
        public static StandRunRecord Latest(SaveData s)
        {
            if (s == null || s.lifetime.standRuns.Count == 0) return null;
            return s.lifetime.standRuns[s.lifetime.standRuns.Count - 1];
        }

        public static string KillsPerRound(int kills, int round) => round <= 0 ? "" : $"{(float)kills / round:0.#} kills per round";
    }
}
