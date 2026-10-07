using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Best result for one Story Quest level.</summary>
    [Serializable]
    public class LevelRecord
    {
        public string levelId = "";
        public bool completed;
        /// <summary>Best clear time in seconds; 0 = no clear yet.</summary>
        public float bestTimeSeconds;
        public int mostShardsCollected;
        public int mostSecretsFound;
        /// <summary>Best clear rank + 1 (1 = C … 4 = S); 0 = no rank yet. Stored +1 so old saves (missing field = 0) read as "none".</summary>
        public int bestRankPlusOne;

        /// <summary>Best rank, or null if never ranked.</summary>
        public ClearRank? BestRank => bestRankPlusOne >= 1 && bestRankPlusOne <= 4 ? (ClearRank)(bestRankPlusOne - 1) : (ClearRank?)null;
    }

    /// <summary>
    /// Player progress. Uses public fields + Lists only so UnityEngine.JsonUtility can serialize it.
    /// Bump CurrentVersion and extend Migrate() whenever the shape changes.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;
        public const string DefaultCharacterId = "nova";

        public int version = CurrentVersion;
        public int starShards;
        public List<string> unlockedCharacters = new List<string> { DefaultCharacterId };
        public List<string> ownedCosmetics = new List<string>();
        public List<string> equippedCosmetics = new List<string>();
        public List<string> unlockedCodexEntries = new List<string>();
        public List<string> achievements = new List<string>();
        public List<LevelRecord> levels = new List<LevelRecord>();
        public string lastSelectedCharacter = DefaultCharacterId;

        public static SaveData CreateNew() => new SaveData();

        public bool IsCharacterUnlocked(string id) => !string.IsNullOrEmpty(id) && unlockedCharacters.Contains(id);

        public bool UnlockCharacter(string id)
        {
            if (string.IsNullOrEmpty(id) || unlockedCharacters.Contains(id)) return false;
            unlockedCharacters.Add(id);
            return true;
        }

        public LevelRecord GetLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            for (int i = 0; i < levels.Count; i++)
                if (levels[i] != null && levels[i].levelId == levelId) return levels[i];
            return null;
        }

        /// <summary>
        /// Merge a finished run into the record. Each stat keeps its own best.
        /// Returns true if any best improved.
        /// </summary>
        public bool RecordLevelResult(string levelId, float timeSeconds, int shards, int secrets)
        {
            if (string.IsNullOrEmpty(levelId)) throw new ArgumentException("levelId required", nameof(levelId));
            var rec = GetLevel(levelId);
            if (rec == null)
            {
                rec = new LevelRecord { levelId = levelId };
                levels.Add(rec);
            }
            bool improved = !rec.completed;
            rec.completed = true;
            if (timeSeconds > 0f && (rec.bestTimeSeconds <= 0f || timeSeconds < rec.bestTimeSeconds))
            {
                rec.bestTimeSeconds = timeSeconds;
                improved = true;
            }
            if (shards > rec.mostShardsCollected) { rec.mostShardsCollected = shards; improved = true; }
            if (secrets > rec.mostSecretsFound) { rec.mostSecretsFound = secrets; improved = true; }
            return improved;
        }

        /// <summary>Keep the better of the stored and new rank. Returns true if it improved. The level must have a record.</summary>
        public bool RecordRank(string levelId, ClearRank rank)
        {
            var rec = GetLevel(levelId);
            if (rec == null) return false;
            int value = (int)rank + 1;
            if (value <= rec.bestRankPlusOne) return false;
            rec.bestRankPlusOne = value;
            return true;
        }

        /// <summary>Repairs nulls/negatives and upgrades old versions. Safe to call on any loaded data.</summary>
        public void SanitizeAndMigrate()
        {
            if (version < 1) version = 1;
            // Future: if (version < 2) { ...; version = 2; }
            version = CurrentVersion;

            if (starShards < 0) starShards = 0;
            if (unlockedCharacters == null) unlockedCharacters = new List<string>();
            if (!unlockedCharacters.Contains(DefaultCharacterId)) unlockedCharacters.Insert(0, DefaultCharacterId);
            if (ownedCosmetics == null) ownedCosmetics = new List<string>();
            if (equippedCosmetics == null) equippedCosmetics = new List<string>();
            if (unlockedCodexEntries == null) unlockedCodexEntries = new List<string>();
            if (achievements == null) achievements = new List<string>();
            if (levels == null) levels = new List<LevelRecord>();
            levels.RemoveAll(l => l == null || string.IsNullOrEmpty(l.levelId));
            foreach (var l in levels)
            {
                if (l.bestTimeSeconds < 0f) l.bestTimeSeconds = 0f;
                if (l.mostShardsCollected < 0) l.mostShardsCollected = 0;
                if (l.mostSecretsFound < 0) l.mostSecretsFound = 0;
                if (l.bestRankPlusOne < 0 || l.bestRankPlusOne > 4) l.bestRankPlusOne = 0;
            }
            // Can't equip what you don't own.
            equippedCosmetics.RemoveAll(c => !ownedCosmetics.Contains(c));
            if (string.IsNullOrEmpty(lastSelectedCharacter) || !unlockedCharacters.Contains(lastSelectedCharacter))
                lastSelectedCharacter = DefaultCharacterId;
        }
    }
}
