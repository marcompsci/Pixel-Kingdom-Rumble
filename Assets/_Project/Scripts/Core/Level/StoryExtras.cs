using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>A Story Quest time-trial ghost: the best clear of one level (GhostTrack data).</summary>
    [Serializable]
    public class StoryGhost
    {
        public string levelId = "";
        public float seconds;
        public string data = "";
    }

    /// <summary>Story Quest time-trial ghosts in the save (one per level, the fastest clear).</summary>
    public static class StoryGhosts
    {
        /// <summary>Keeps the save small: more than this many and the oldest-added go.</summary>
        public const int MaxLevels = 60;

        public static StoryGhost Find(SaveData s, string levelId)
        {
            if (s == null || s.storyGhosts == null || string.IsNullOrEmpty(levelId)) return null;
            foreach (var g in s.storyGhosts) if (g != null && g.levelId == levelId) return g;
            return null;
        }

        /// <summary>Offers a finished clear. Kept when it's the first or a faster one. Returns true when stored.</summary>
        public static bool Offer(SaveData s, string levelId, float seconds, GhostTrack run)
        {
            if (s == null || string.IsNullOrEmpty(levelId) || run == null || run.Count < 2 || !(seconds > 0f)) return false;
            if (run.Count >= GhostTrack.MaxSamples) return false; // too long to have been recorded whole
            if (s.storyGhosts == null) s.storyGhosts = new List<StoryGhost>();
            var g = Find(s, levelId);
            if (g != null && g.seconds > 0f && g.seconds <= seconds) return false;
            if (g == null)
            {
                g = new StoryGhost { levelId = levelId };
                s.storyGhosts.Add(g);
                while (s.storyGhosts.Count > MaxLevels) s.storyGhosts.RemoveAt(0);
            }
            g.seconds = seconds;
            g.data = run.Encode();
            return true;
        }

        public static void Sanitize(SaveData s)
        {
            if (s.storyGhosts == null) s.storyGhosts = new List<StoryGhost>();
            s.storyGhosts.RemoveAll(g => g == null || string.IsNullOrEmpty(g.levelId) || string.IsNullOrEmpty(g.data) || !(g.seconds > 0f));
            while (s.storyGhosts.Count > MaxLevels) s.storyGhosts.RemoveAt(0);
        }
    }

    /// <summary>FLAWLESS: clear a Story Quest level without taking a hit. Pays once per level.</summary>
    public static class StoryFlawless
    {
        public const int Shards = 20;

        /// <summary>Records a clear; returns the Star Shards earned (only the first flawless clear of a level pays).</summary>
        public static int Record(LevelRecord rec, bool tookHit)
        {
            if (rec == null || tookHit || rec.flawless) return 0;
            rec.flawless = true;
            return Shards;
        }

        public static int Count(SaveData s)
        {
            int n = 0;
            if (s != null && s.levels != null) foreach (var l in s.levels) if (l != null && l.flawless) n++;
            return n;
        }
    }

    /// <summary>Accessibility: Story Quest game speed (Settings > ACCESSIBILITY).</summary>
    public static class GameSpeed
    {
        public static readonly int[] Steps = { 100, 90, 80, 70 };

        public static int Clamp(int percent)
        {
            foreach (int s in Steps) if (s == percent) return percent;
            return 100;
        }

        public static int Next(int percent)
        {
            int i = Array.IndexOf(Steps, Clamp(percent));
            return Steps[(i + 1) % Steps.Length];
        }

        public static float Scale(int percent) => Clamp(percent) / 100f;

        /// <summary>Slower speeds are an assist: they don't set best times or time medals (the clear still counts).</summary>
        public static bool CountsForRecords(int percent) => Clamp(percent) == 100;
    }
}
