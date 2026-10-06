using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Runs one Story Quest level: spawns pooled pickups and enemies, tracks the LevelRun (time, shards,
    /// secrets, deaths, checkpoints), handles pits and player death/respawn, and completes the level at the goal.
    /// Raises CheckpointActivated, PlayerRespawned, SecretFound and LevelCompleted on the EventBus.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class LevelFlowController : MonoBehaviour
    {
        public static LevelFlowController Current { get; private set; }

        [SerializeField] LevelDefinition level;
        [SerializeField] GameObject player;
        [SerializeField] Transform startPoint;
        [Tooltip("Falling below this height counts as a pit.")]
        [SerializeField] float killY = -10f;
        [SerializeField] float respawnDelay = 1f;
        [SerializeField] float respawnInvulnerability = 1.5f;

        [Header("Runtime-spawned art")]
        [SerializeField] Material spriteMaterial;
        [SerializeField] Sprite shardSprite;
        [SerializeField] Sprite healthSprite;

        public LevelDefinition Level => level;
        public LevelRun Run { get; private set; }
        public Damageable PlayerHealth { get; private set; }
        public bool IsComplete => Run != null && Run.IsComplete;
        public int TotalShards { get; private set; }
        public int TotalSecrets { get; private set; }

        PlatformerMotor2D _playerMotor;
        readonly List<EnemyAI> _enemies = new List<EnemyAI>();
        EnemySpawnPoint[] _enemySpawns;
        Checkpoint _activeCheckpoint;
        bool _respawning;

        void Awake()
        {
            Current = this;
            if (player != null)
            {
                PlayerHealth = player.GetComponent<Damageable>();
                _playerMotor = player.GetComponent<PlatformerMotor2D>();
            }
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            Run = new LevelRun(level != null ? level.id : "unknown_level");

            var spots = FindObjectsByType<PickupSpot>(FindObjectsSortMode.None);
            foreach (var s in spots)
            {
                if (s.kind == PickupKind.StarShard) TotalShards += s.value;
                Pickup.Spawn(s.kind, s.value, s.transform.position, SpriteFor(s.kind), spriteMaterial);
            }
            TotalSecrets = FindObjectsByType<SecretArea>(FindObjectsSortMode.None).Length;

            _enemySpawns = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
            SpawnEnemies();

            if (PlayerHealth != null) PlayerHealth.Died += OnPlayerDied;
            if (Services.State != null) Services.State.SetState(GameState.Playing);
        }

        Sprite SpriteFor(PickupKind kind) => kind == PickupKind.Health ? healthSprite : shardSprite;

        void Update()
        {
            if (Run == null || IsComplete) return;
            if (Services.State == null || Services.State.State == GameState.Playing) Run.Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (Run == null || IsComplete) return;

            if (!_respawning && player != null && player.transform.position.y < killY) OnPlayerFellInPit();

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null || !e.isActiveAndEnabled) { _enemies.RemoveAt(i); continue; }
                if (e.transform.position.y < killY) RemoveEnemy(e, drop: false);
            }
        }

        // ---- Enemies -------------------------------------------------------------------------------

        void SpawnEnemies()
        {
            foreach (var sp in _enemySpawns)
            {
                if (sp == null || sp.enemy == null) continue;
                var ai = EnemyFactory.Spawn(sp.enemy, sp.transform.position, sp.startDirection, spriteMaterial);
                ai.Defeated -= OnEnemyDefeated; // pooled objects may still be subscribed
                ai.Defeated += OnEnemyDefeated;
                _enemies.Add(ai);
            }
        }

        void ResetEnemies()
        {
            for (int i = _enemies.Count - 1; i >= 0; i--) RemoveEnemy(_enemies[i], drop: false);
            _enemies.Clear();
            SpawnEnemies();
        }

        void OnEnemyDefeated(EnemyAI e) => RemoveEnemy(e, drop: true);

        void RemoveEnemy(EnemyAI e, bool drop)
        {
            if (e == null) return;
            _enemies.Remove(e);
            Vector2 pos = e.transform.position;
            if (drop)
            {
                PuffEffect.Play(pos, new Color32(236, 186, 74, 255), spriteMaterial, 0.22f, 4f, 0.4f);
                int shards = e.Definition != null ? e.Definition.shardDrop : 0;
                for (int i = 0; i < shards; i++)
                {
                    var pop = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(5f, 7f));
                    Pickup.Spawn(PickupKind.StarShard, 1, pos + Vector2.up * 0.3f, shardSprite, spriteMaterial, pop);
                }
            }
            EnemyFactory.Release(e);
        }

        // ---- Player --------------------------------------------------------------------------------

        Vector2 RespawnPosition =>
            _activeCheckpoint != null ? _activeCheckpoint.RespawnPoint
            : startPoint != null ? (Vector2)startPoint.position
            : Vector2.zero;

        void OnPlayerFellInPit()
        {
            // Pits cost 1 HP and send you back to the checkpoint; the last HP means a full death.
            if (PlayerHealth == null) { StartCoroutine(RespawnRoutine(false)); return; }
            bool died = PlayerHealth.TakeDirectDamage(1);
            if (!died) StartCoroutine(RespawnRoutine(false));
            // If it killed the player, OnPlayerDied handles the respawn.
        }

        void OnPlayerDied()
        {
            if (IsComplete) return;
            Run.RecordDeath();
            if (!_respawning) StartCoroutine(RespawnRoutine(true));
        }

        IEnumerator RespawnRoutine(bool afterDeath)
        {
            _respawning = true;
            if (afterDeath)
            {
                if (player != null) PuffEffect.Play(player.transform.position, new Color32(46, 168, 178, 255), spriteMaterial, 0.25f, 4f, 0.5f);
                player.SetActive(false);
                yield return new WaitForSeconds(respawnDelay);
                player.SetActive(true);
            }
            else
            {
                yield return new WaitForSeconds(0.25f);
            }

            if (player.TryGetComponent(out HeroAbilities abilities)) abilities.Cancel();
            if (player.TryGetComponent(out AttackRunner attacks)) attacks.Cancel();
            _playerMotor.Teleport(RespawnPosition);
            if (afterDeath || (PlayerHealth != null && PlayerHealth.IsDead))
            {
                PlayerHealth.ResetState();
                ResetEnemies();
            }
            if (player.TryGetComponent(out Invulnerability inv)) inv.Grant(respawnInvulnerability, blink: true);
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.SnapToTarget();
            EventBus<PlayerRespawned>.Raise(new PlayerRespawned { afterDeath = afterDeath });
            _respawning = false;
        }

        // ---- Progress ------------------------------------------------------------------------------

        public void ReachCheckpoint(Checkpoint cp)
        {
            if (Run == null || IsComplete || cp == null) return;
            if (!Run.ReachCheckpoint(cp.index)) return;
            _activeCheckpoint = cp;
            cp.Activate();
            PuffEffect.Play(cp.transform.position + Vector3.up, cp.activeColor, spriteMaterial, 0.2f, 4f, 0.45f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            EventBus<CheckpointActivated>.Raise(new CheckpointActivated { index = cp.index });
        }

        public void OnPickupCollected(PickupKind kind, int value)
        {
            if (Run == null) return;
            if (kind == PickupKind.StarShard) Run.CollectShards(value);
        }

        public void FindSecret(string id)
        {
            if (Run == null || !Run.FindSecret(id)) return;
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            EventBus<SecretFound>.Raise(new SecretFound { id = id, found = Run.SecretsFound, total = TotalSecrets });
        }

        public void CompleteLevel()
        {
            if (Run == null || IsComplete) return;
            var save = Services.Save;
            var data = save != null ? save.Data : null;
            float previousBest = data != null && data.GetLevel(Run.LevelId) != null ? data.GetLevel(Run.LevelId).bestTimeSeconds : 0f;
            int reward = Run.Complete(data);
            if (save != null) save.SaveNow();

            // Freeze the hero in a victory pose; LevelCompleteScreen takes over.
            if (player.TryGetComponent(out PlayerInputRouter router)) router.enabled = false;
            _playerMotor.Intent.ClearAll();
            if (player.TryGetComponent(out Invulnerability inv)) inv.Grant(9999f); // nothing can hurt you after the goal
            if (Services.State != null) Services.State.SetState(GameState.Results);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);

            EventBus<LevelCompleted>.Raise(new LevelCompleted
            {
                levelId = Run.LevelId,
                levelName = level != null ? level.displayName : Run.LevelId,
                timeSeconds = Run.ElapsedSeconds,
                parTimeSeconds = level != null ? level.parTimeSeconds : 0f,
                newBestTime = previousBest <= 0f || Run.ElapsedSeconds < previousBest,
                shards = Run.Shards,
                totalShards = TotalShards,
                secrets = Run.SecretsFound,
                totalSecrets = TotalSecrets,
                deaths = Run.Deaths,
                starShardReward = reward
            });
        }
    }
}
