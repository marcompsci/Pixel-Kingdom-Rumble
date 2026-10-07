using System;

namespace PKR.Core
{
    /// <summary>What the level select shows for one level.</summary>
    public struct LevelSlot
    {
        public string levelId;
        public bool unlocked;
        public bool completed;
        public float bestTimeSeconds;
        public int shards;
        public int secrets;
        /// <summary>Best rank, or null if never ranked.</summary>
        public ClearRank? rank;
    }

    /// <summary>
    /// Story Quest route rules: levels unlock in order (each needs the previous one cleared); a cleared level always
    /// stays open. Pure, so it is unit-tested.
    /// </summary>
    public static class LevelSelect
    {
        public static LevelSlot[] Build(string[] levelIds, SaveData save)
        {
            if (levelIds == null) return Array.Empty<LevelSlot>();
            var slots = new LevelSlot[levelIds.Length];
            bool previousCleared = true; // the first level is always open
            for (int i = 0; i < levelIds.Length; i++)
            {
                var rec = save != null ? save.GetLevel(levelIds[i]) : null;
                bool done = rec != null && rec.completed;
                slots[i] = new LevelSlot
                {
                    levelId = levelIds[i],
                    unlocked = previousCleared || done, // a level you've cleared is never shown locked
                    completed = done,
                    bestTimeSeconds = rec != null ? rec.bestTimeSeconds : 0f,
                    shards = rec != null ? rec.mostShardsCollected : 0,
                    secrets = rec != null ? rec.mostSecretsFound : 0,
                    rank = rec != null ? rec.BestRank : null
                };
                previousCleared = done;
            }
            return slots;
        }

        /// <summary>Level to highlight: the first unlocked level not yet cleared, else the last unlocked one, else 0.</summary>
        public static int Suggested(LevelSlot[] slots)
        {
            if (slots == null || slots.Length == 0) return 0;
            int lastUnlocked = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].unlocked) break;
                lastUnlocked = i;
                if (!slots[i].completed) return i;
            }
            return lastUnlocked;
        }

        /// <summary>Short rank label for cards ("S", "A", … or "-").</summary>
        public static string RankLabel(ClearRank? rank) => rank.HasValue ? rank.Value.ToString() : "-";
    }
}
