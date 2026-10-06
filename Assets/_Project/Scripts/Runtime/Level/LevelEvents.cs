namespace PKR
{
    public struct CheckpointActivated { public int index; }
    public struct PlayerRespawned { public bool afterDeath; }
    public struct SecretFound { public string id; public int found; public int total; }

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
    }
}
