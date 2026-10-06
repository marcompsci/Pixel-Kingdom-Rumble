using System;

namespace PKR.Core
{
    /// <summary>Ground patrol: walk until a wall or a ledge, then turn around.</summary>
    public static class PatrolLogic
    {
        /// <summary>
        /// Next walking direction (+1/-1). Turns at walls, and at ledges while grounded
        /// (so knocked-back enemies don't flip mid-air).
        /// </summary>
        public static int NextDirection(int dir, bool wallAhead, bool groundAhead, bool grounded)
        {
            int d = dir >= 0 ? 1 : -1;
            if (wallAhead) return -d;
            if (grounded && !groundAhead) return -d;
            return d;
        }
    }

    /// <summary>
    /// Hopper: waits on the ground, then hops toward the target when it's in range.
    /// Pure state machine; the runtime feeds it time, grounded state and target offset.
    /// </summary>
    public class HopperLogic
    {
        public float Cooldown { get; }
        public float Range { get; }
        float _timer;

        public HopperLogic(float cooldown = 1.4f, float range = 7f, float initialDelay = 0.5f)
        {
            if (cooldown <= 0f) throw new ArgumentOutOfRangeException(nameof(cooldown));
            if (range <= 0f) throw new ArgumentOutOfRangeException(nameof(range));
            Cooldown = cooldown;
            Range = range;
            _timer = Math.Max(0f, initialDelay);
        }

        /// <summary>
        /// Advance. Returns true on the step a hop should start; dir is the hop direction (+1/-1).
        /// The timer only runs while grounded, so hops are spaced from landing to takeoff.
        /// </summary>
        public bool Tick(float dt, bool grounded, float targetDx, float targetDy, out int dir)
        {
            dir = targetDx >= 0f ? 1 : -1;
            if (!grounded) return false;
            if (_timer > 0f) { _timer -= dt; return false; }
            bool inRange = Math.Abs(targetDx) <= Range && Math.Abs(targetDy) <= Range * 0.75f;
            if (!inRange) return false;
            _timer = Cooldown;
            return true;
        }
    }

    /// <summary>
    /// One attempt at a Story Quest level: time, collectibles, secrets, deaths and checkpoint progress.
    /// </summary>
    public class LevelRun
    {
        public string LevelId { get; }
        public float ElapsedSeconds { get; private set; }
        public int Shards { get; private set; }
        public int Deaths { get; private set; }
        public int CheckpointIndex { get; private set; } = -1;
        public bool IsComplete { get; private set; }
        public int SecretsFound => _secrets.Count;

        readonly System.Collections.Generic.HashSet<string> _secrets = new System.Collections.Generic.HashSet<string>();

        public LevelRun(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) throw new ArgumentException("levelId required", nameof(levelId));
            LevelId = levelId;
        }

        public void Tick(float dt)
        {
            if (!IsComplete && dt > 0f) ElapsedSeconds += dt;
        }

        public void CollectShards(int amount)
        {
            if (!IsComplete && amount > 0) Shards += amount;
        }

        /// <summary>Returns true the first time a given secret is found.</summary>
        public bool FindSecret(string secretId) => !IsComplete && !string.IsNullOrEmpty(secretId) && _secrets.Add(secretId);

        /// <summary>Checkpoints only move forward. Returns true if this became the active checkpoint.</summary>
        public bool ReachCheckpoint(int index)
        {
            if (IsComplete || index <= CheckpointIndex) return false;
            CheckpointIndex = index;
            return true;
        }

        public void RecordDeath()
        {
            if (!IsComplete) Deaths++;
        }

        /// <summary>Finish the run once and write it into the save. Returns shards awarded (0 if already complete).</summary>
        public int Complete(SaveData save)
        {
            if (IsComplete) return 0;
            IsComplete = true;
            if (save == null) return 0;
            bool firstClear = save.GetLevel(LevelId) == null || !save.GetLevel(LevelId).completed;
            save.RecordLevelResult(LevelId, ElapsedSeconds, Shards, SecretsFound);
            int reward = Economy.LevelReward(Shards, SecretsFound, firstClear);
            Economy.Grant(save, reward);
            return reward;
        }
    }
}
