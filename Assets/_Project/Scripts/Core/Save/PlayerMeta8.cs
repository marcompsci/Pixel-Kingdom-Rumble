using System;

namespace PKR.Core
{
    /// <summary>A title that grows with your badge count, shown on PROFILE and in the menu greeting.</summary>
    public static class RankTitle
    {
        public static readonly int[] At = { 0, 5, 10, 18, 26, 34 };
        public static readonly string[] Names = { "SQUIRE", "ADVENTURER", "KNIGHT", "HERO", "LEGEND", "MYTH" };

        public static string For(int badges)
        {
            int i = 0;
            for (int k = 0; k < At.Length; k++) if (badges >= At[k]) i = k;
            return Names[i];
        }

        /// <summary>"KNIGHT in 3 badges" for the next title, "" at the top.</summary>
        public static string Next(int badges)
        {
            for (int k = 0; k < At.Length; k++) if (badges < At[k]) return $"{Names[k]} in {Plural.Of(At[k] - badges, "badge")}";
            return "";
        }
    }

    /// <summary>Time played today (resets at local midnight).</summary>
    public static class TodayPlay
    {
        public static void Add(SaveData s, int dayKey, float seconds)
        {
            if (s == null || seconds <= 0f || float.IsNaN(seconds)) return;
            var l = s.lifetime;
            if (l.todayKey != dayKey) { l.todayKey = dayKey; l.todaySeconds = 0f; }
            l.todaySeconds += seconds;
        }

        public static float Seconds(SaveData s, int dayKey) => s != null && s.lifetime.todayKey == dayKey ? s.lifetime.todaySeconds : 0f;

        /// <summary>"You've played 25m today." once there's at least a minute.</summary>
        public static string Line(SaveData s, int dayKey)
        {
            float t = Seconds(s, dayKey);
            return t >= 60f ? $"You've played {PlayTime.Format(t)} today." : "";
        }
    }

    /// <summary>NIGHT OWL (midnight to 4) and EARLY BIRD (5 to 8) badges, marked when the game is opened then.</summary>
    public static class TimeBadges
    {
        public static void Mark(SaveData s, int hour)
        {
            if (s == null) return;
            if (hour >= 0 && hour < 4) s.lifetime.nightOwl = true;
            if (hour >= 5 && hour < 8) s.lifetime.earlyBird = true;
        }
    }

    /// <summary>Shadow Contracts: how many have a score, and the clear-screen score change.</summary>
    public static class ContractMeta
    {
        public static int Cleared(SaveData s)
        {
            int n = 0;
            if (s != null && s.missions != null) foreach (var m in s.missions) if (m != null && m.bestScore > 0) n++;
            return n;
        }

        /// <summary>"+350 over your best" / "120 short of your best" / "" when there was no earlier score.</summary>
        public static string VsBest(int score, int previousBest)
        {
            if (previousBest <= 0) return "";
            int d = score - previousBest;
            return d > 0 ? $"+{d:N0} over your best" : d == 0 ? "matched your best" : $"{-d:N0} short of your best";
        }
    }

    /// <summary>Last Stand: the longest run (seconds survived).</summary>
    public static class StandTime
    {
        public static bool Record(SaveData s, float seconds)
        {
            if (s == null || seconds <= s.lifetime.longestStand || float.IsNaN(seconds)) return false;
            bool had = s.lifetime.longestStand > 0f;
            s.lifetime.longestStand = seconds;
            return had;
        }
    }

    /// <summary>Training lab best combo, kept across sessions.</summary>
    public static class LabRecord
    {
        public static bool Offer(SaveData s, int combo)
        {
            if (s == null || combo <= s.lifetime.labBestCombo) return false;
            s.lifetime.labBestCombo = combo;
            return true;
        }
    }
}
