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
        /// <summary>Codex entries unlocked by play ("enemy:enemy_cog_beetle", "place:sq_sunspire_test"). See Codex.</summary>
        public List<string> unlockedCodexEntries = new List<string>();
        /// <summary>Codex entries the player has opened (no NEW badge).</summary>
        public List<string> seenCodexEntries = new List<string>();
        public List<EnemyTally> enemyDefeats = new List<EnemyTally>();
        /// <summary>Game Center achievement ids earned on this device (re-sent after sign-in, so none are lost offline).</summary>
        public List<string> achievements = new List<string>();
        public List<LevelRecord> levels = new List<LevelRecord>();
        /// <summary>Shadow Contracts: the one-time unlock ($3.99), clues found, and best results per contract.</summary>
        public bool contractsUnlocked;
        public List<string> foundClues = new List<string>();
        public List<MissionRecord> missions = new List<MissionRecord>();
        /// <summary>Shadow Contracts worlds whose door-opening scene has been shown (by world index).</summary>
        public List<int> openedWorldDoors = new List<int>();
        /// <summary>First-time hints already shown (by hint id), so each one only appears once.</summary>
        public List<string> seenHints = new List<string>();
        /// <summary>Rumble Rating (ranked points, tier, auto CPU skill, Daily Rumble streak).</summary>
        public RumbleProfile rumble = new RumbleProfile();
        /// <summary>Permanent Story upgrades bought with Star Shards (see Upgrades).</summary>
        public List<UpgradeRecord> upgrades = new List<UpgradeRecord>();
        /// <summary>Story comic scenes already shown (StoryBeats ids).</summary>
        public List<string> seenCutscenes = new List<string>();
        /// <summary>Story worlds whose completion bonus was paid (StoryWorlds index).</summary>
        public List<int> claimedWorldRewards = new List<int>();
        public string lastSelectedCharacter = DefaultCharacterId;
        /// <summary>Player level XP (PlayerLevel turns it into a level).</summary>
        public int playerXp;
        /// <summary>Today's daily challenges (DailyChallenges) and the day they belong to (yyyymmdd).</summary>
        public int dailyDayKey;
        public List<ChallengeRecord> dailyChallenges = new List<ChallengeRecord>();
        /// <summary>Halloween Zombie Survival bests.</summary>
        public int zombieBestRound, zombieBestPoints, zombieBestKills;
        /// <summary>Zombie Land: the open city campaign against the First Turned.</summary>
        public ZombieLandProgress zombieLand = new ZombieLandProgress();
        /// <summary>Found the Zombie Survival lantern secret at least once.</summary>
        public bool zombieLanternsLit;

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

        public MissionRecord GetMission(string contractId)
        {
            if (string.IsNullOrEmpty(contractId) || missions == null) return null;
            foreach (var m in missions) if (m != null && m.contractId == contractId) return m;
            return null;
        }

        /// <summary>Records a finished contract. Returns true if the score is a new best.</summary>
        public bool RecordMission(string contractId, int score, int relics)
        {
            if (string.IsNullOrEmpty(contractId)) return false;
            if (missions == null) missions = new List<MissionRecord>();
            var m = GetMission(contractId);
            if (m == null) { m = new MissionRecord { contractId = contractId }; missions.Add(m); }
            bool best = !m.completed || score > m.bestScore;
            m.completed = true;
            if (score > m.bestScore) m.bestScore = score;
            if (relics > m.bestRelics) m.bestRelics = relics;
            return best;
        }

        public bool HasClue(string clueId) => !string.IsNullOrEmpty(clueId) && foundClues != null && foundClues.Contains(clueId);

        /// <summary>Returns true the first time a clue is found.</summary>
        public bool FindClue(string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return false;
            if (foundClues == null) foundClues = new List<string>();
            if (foundClues.Contains(clueId)) return false;
            foundClues.Add(clueId);
            return true;
        }

        /// <summary>Records a Zombie Survival run. Returns true if the round reached is a new best (score ties broken by points).</summary>
        public bool RecordSurvival(int round, int points, int kills)
        {
            round = Math.Max(0, round); points = Math.Max(0, points); kills = Math.Max(0, kills);
            bool best = round > zombieBestRound || (round == zombieBestRound && points > zombieBestPoints && round > 0);
            if (best) { zombieBestRound = round; zombieBestPoints = points; }
            if (kills > zombieBestKills) zombieBestKills = kills;
            return best;
        }

        /// <summary>Records an achievement id; true only the first time.</summary>
        public bool EarnAchievement(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (achievements == null) achievements = new List<string>();
            if (achievements.Contains(id)) return false;
            achievements.Add(id);
            return true;
        }

        /// <summary>Records that a world's door-opening scene was shown; true only the first time.</summary>
        public bool MarkWorldDoorOpened(int world)
        {
            if (openedWorldDoors == null) openedWorldDoors = new List<int>();
            if (openedWorldDoors.Contains(world)) return false;
            openedWorldDoors.Add(world);
            return true;
        }

        /// <summary>Marks a first-time hint as seen; true only the first time (so the caller shows it once).</summary>
        public bool MarkHintSeen(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (seenHints == null) seenHints = new List<string>();
            if (seenHints.Contains(id)) return false;
            seenHints.Add(id);
            return true;
        }

        /// <summary>Repairs nulls/negatives and upgrades old versions. Safe to call on any loaded data.</summary>
        public void SanitizeAndMigrate()
        {
            if (version < 1) version = 1;
            // Future: if (version < 2) { ...; version = 2; }
            version = CurrentVersion;

            if (starShards < 0) starShards = 0;
            if (zombieLand == null) zombieLand = new ZombieLandProgress();
            if (zombieBestRound < 0) zombieBestRound = 0;
            if (playerXp < 0) playerXp = 0;
            if (dailyChallenges == null) dailyChallenges = new List<ChallengeRecord>();
            dailyChallenges.RemoveAll(c => c == null || !DailyChallenges.Find(c.id).HasValue);
            if (zombieBestPoints < 0) zombieBestPoints = 0;
            if (zombieBestKills < 0) zombieBestKills = 0;
            if (unlockedCharacters == null) unlockedCharacters = new List<string>();
            if (!unlockedCharacters.Contains(DefaultCharacterId)) unlockedCharacters.Insert(0, DefaultCharacterId);
            if (ownedCosmetics == null) ownedCosmetics = new List<string>();
            if (equippedCosmetics == null) equippedCosmetics = new List<string>();
            if (unlockedCodexEntries == null) unlockedCodexEntries = new List<string>();
            if (seenCodexEntries == null) seenCodexEntries = new List<string>();
            if (enemyDefeats == null) enemyDefeats = new List<EnemyTally>();
            enemyDefeats.RemoveAll(t => t == null || string.IsNullOrEmpty(t.enemyId));
            foreach (var t in enemyDefeats) if (t.defeated < 0) t.defeated = 0;
            if (achievements == null) achievements = new List<string>();
            if (levels == null) levels = new List<LevelRecord>();
            if (foundClues == null) foundClues = new List<string>();
            foundClues.RemoveAll(string.IsNullOrEmpty);
            achievements.RemoveAll(string.IsNullOrEmpty);
            if (openedWorldDoors == null) openedWorldDoors = new List<int>();
            if (seenHints == null) seenHints = new List<string>();
            seenHints.RemoveAll(string.IsNullOrEmpty);
            if (rumble == null) rumble = new RumbleProfile();
            rumble.Sanitize();
            if (upgrades == null) upgrades = new List<UpgradeRecord>();
            upgrades.RemoveAll(u => u == null || !Upgrades.Find(u.id).HasValue);
            foreach (var u in upgrades)
            {
                var spec = Upgrades.Find(u.id).Value;
                if (u.level < 0) u.level = 0;
                if (u.level > spec.MaxLevel) u.level = spec.MaxLevel;
            }
            if (seenCutscenes == null) seenCutscenes = new List<string>();
            seenCutscenes.RemoveAll(string.IsNullOrEmpty);
            if (claimedWorldRewards == null) claimedWorldRewards = new List<int>();
            if (missions == null) missions = new List<MissionRecord>();
            missions.RemoveAll(m => m == null || string.IsNullOrEmpty(m.contractId));
            foreach (var m in missions) { if (m.bestScore < 0) m.bestScore = 0; if (m.bestRelics < 0) m.bestRelics = 0; }
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
