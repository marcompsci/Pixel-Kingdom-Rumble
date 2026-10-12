using PKR.Core;

namespace PKR
{
    public struct CheckpointActivated { public int index; }
    public struct PlayerRespawned { public bool afterDeath; }
    public struct SecretFound { public string id; public int found; public int total; }
    /// <summary>A Codex page unlocked during play (first time a foe is defeated or a place is reached).</summary>
    public struct CodexEntryUnlocked { public CodexCategory category; public string id; public string displayName; }

    /// <summary>Raised once when the goal is reached. Everything the level-complete screen needs.</summary>
    public struct LevelCompleted
    {
        public string levelId;
        public string levelName;
        public float timeSeconds;
        public float parTimeSeconds;
        public bool newBestTime;
        public int shards;
        public int totalShards;
        public int secrets;
        public int totalSecrets;
        public int deaths;
        public int starShardReward;
        /// <summary>Part of starShardReward from the Lucky Shards upgrade.</summary>
        public int luckyBonus;
        /// <summary>Story world whose every level this clear completed for the first time ("" if none), and its bonus.</summary>
        public string worldCompleted;
        public int worldBonus;
        /// <summary>Display names of heroes this clear unlocked (empty if none).</summary>
        public string[] unlockedHeroes;
        /// <summary>Stealth levels only: the grade (GHOST/SHADOW/AGENT/BRAWLER), times spotted and takedowns.</summary>
        public string stealthRank;
        public int timesSpotted;
        public int takedowns;
        /// <summary>Shadow Contracts only (isContract): score, relics and whether this run revealed a secret contract.</summary>
        public bool isContract;
        public int contractScore;
        public bool newBestScore;
        public int relics;
        public int totalRelics;
        public bool clueFound;
    }
}
