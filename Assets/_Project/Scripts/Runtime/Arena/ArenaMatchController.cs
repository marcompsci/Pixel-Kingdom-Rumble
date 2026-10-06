using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    public struct ArenaMatchStarted { public MatchMode mode; public int fighters; }
    public struct ArenaKO { public int victimSlot; public int attackerSlot; public bool eliminated; }

    public struct ArenaMatchEnded
    {
        public MatchMode mode;
        public int[] placements;     // per slot, 1-based
        public string[] names;
        public int[] knockouts;
        public int[] falls;
        public int[] scores;
        public int playerPlacement;
        public int starShardReward;
    }

    /// <summary>
    /// Runs an Arena Clash match in the Skyforge Arena: spawns the player and CPU fighters, frames them with the
    /// camera, detects blast-zone KOs (crediting whoever hit the victim last), respawns with blinking
    /// invulnerability, runs the clock and the bridge event, and reports results (with a Star Shard reward).
    /// The match only starts when ArenaSetupUI calls StartMatch.
    /// </summary>
    public class ArenaMatchController : MonoBehaviour
    {
        public class Fighter
        {
            public int slot;
            public string name;
            public bool human;
            public Color color;
            public GameObject go;
            public Damageable health;
            public PlatformerMotor2D motor;
        }

        [SerializeField] CharacterRoster roster;
        [SerializeField] CharacterDefinition fallbackHero;
        [SerializeField] Material spriteMaterial;
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] Transform respawnPoint;
        [SerializeField] Rect blastZone = new Rect(-22f, -12f, 44f, 32f);
        [Tooltip("Main stage area: x range = edges, yMax = floor height. Used by CPU fighters.")]
        [SerializeField] Rect stageRect = new Rect(-9f, -1f, 18f, 1f);
        [SerializeField] SkyforgeBridge bridge;
        [SerializeField] CameraFollow2D arenaCamera;
        [SerializeField] float respawnDelay = 1f;
        [SerializeField] float respawnInvulnerability = 2f;
        [Tooltip("A hit counts toward a KO if it landed within this many seconds of the fall.")]
        [SerializeField] float creditWindow = 5f;
        [Tooltip("Off in tests so they don't add Star Shards to the real save.")]
        public bool grantRewards = true;

        public static readonly Color[] SlotColors =
        {
            new Color32(46, 220, 190, 255), new Color32(255, 100, 100, 255),
            new Color32(120, 230, 110, 255), new Color32(255, 214, 80, 255)
        };
        static readonly Color[] SlotTints =
        {
            Color.white, new Color(1f, 0.6f, 0.6f), new Color(0.6f, 1f, 0.65f), new Color(1f, 0.92f, 0.5f)
        };

        public bool IsLive { get; private set; }
        public MatchState Match { get; private set; }
        public ArenaMatchConfig Config { get; private set; } = new ArenaMatchConfig();
        public IReadOnlyList<Fighter> Fighters => _fighters;
        public Rect StageRect => stageRect;
        public Rect BlastZone => blastZone;

        readonly List<Fighter> _fighters = new List<Fighter>();
        readonly HashSet<int> _respawning = new HashSet<int>();

        /// <summary>Runtime setup (tests, or building an arena in code). The scene builder fills the serialized fields instead.</summary>
        public void Configure(CharacterDefinition hero, Transform[] spawns, Rect blast, Rect stage, Material material = null)
        {
            fallbackHero = hero;
            spawnPoints = spawns;
            blastZone = blast;
            stageRect = stage;
            if (material != null) spriteMaterial = material;
        }

        void Update()
        {
            var state = Services.State;
            // Hold the game in the menu state (no pause button / touch controls) until the match starts.
            if (!IsLive && state != null && state.State == GameState.Playing) state.SetState(GameState.Menu);

            if (!IsLive || Match == null) return;
            if (state == null || state.State == GameState.Playing) Match.Tick(Time.deltaTime);
            if (Match.IsOver) EndMatch();
        }

        void FixedUpdate()
        {
            if (!IsLive) return;
            foreach (var f in _fighters)
            {
                if (Match.IsOver) break; // a second fall in the same step must not be reported as an elimination
                if (f.go == null || !f.go.activeSelf || _respawning.Contains(f.slot)) continue;
                if (!blastZone.Contains(f.motor.Body.position)) KnockOut(f);
            }
        }

        // ---- Match lifecycle ---------------------------------------------------------------------

        public void StartMatch(ArenaMatchConfig config)
        {
            ClearFighters();
            Config = config;
            Config.Clamp();
            Match = Config.CreateMatch();

            var playerDef = roster != null ? roster.Find(GameSession.SelectedCharacterId) : null;
            if (playerDef == null || !playerDef.playableInThisBuild) playerDef = fallbackHero;
            // CPU fighters pick random selectable heroes (seeded per match so a rematch can differ).
            var cpuRng = new System.Random(unchecked(System.Environment.TickCount * 31 + Config.FighterCount));

            if (arenaCamera != null) arenaCamera.group.Clear();
            for (int slot = 0; slot < Config.FighterCount; slot++)
            {
                bool human = slot == 0;
                var def = human ? playerDef : PickCpuHero(cpuRng);
                Vector2 pos = spawnPoints != null && spawnPoints.Length > 0
                    ? (Vector2)spawnPoints[slot % spawnPoints.Length].position
                    : new Vector2(-6f + slot * 4f, 2f);
                string name = human ? $"P1 {def.displayName.ToUpperInvariant()}" : $"CPU {slot + 1}";
                var go = FighterFactory.CreateHero(def, spriteMaterial, pos, slot, DamageModel.ArenaPips, SlotTints[slot], name);
                var motor = go.GetComponent<PlatformerMotor2D>();
                motor.SetFacing(pos.x <= 0f ? 1 : -1);

                if (human)
                {
                    go.AddComponent<PlayerInputRouter>();
                    go.AddComponent<PlayerMarker>();
                }
                else
                {
                    var bot = go.AddComponent<BotController>();
                    bot.Init(this, BotDifficulty.For(Config.botLevel), 7919 * (slot + 1), Config.mode == MatchMode.Training);
                }

                _fighters.Add(new Fighter
                {
                    slot = slot, name = name, human = human, color = SlotColors[slot], go = go,
                    health = go.GetComponent<Damageable>(), motor = motor
                });
                if (arenaCamera != null) arenaCamera.group.Add(go.transform);
            }

            if (bridge != null) { bridge.ResetCycle(); bridge.Running = Config.mode != MatchMode.Training; }
            IsLive = true;
            if (Services.State != null) Services.State.SetState(GameState.Playing);
            EventBus<ArenaMatchStarted>.Raise(new ArenaMatchStarted { mode = Config.mode, fighters = Config.FighterCount });
        }

        CharacterDefinition PickCpuHero(System.Random rng)
        {
            if (roster == null || roster.heroes.Count == 0) return fallbackHero;
            var selectable = new bool[roster.heroes.Count];
            for (int i = 0; i < selectable.Length; i++)
                selectable[i] = roster.IsSelectable(roster.heroes[i]); // CPUs only use heroes you've unlocked
            int k = RosterSelection.PickRandom(selectable, rng);
            return k >= 0 ? roster.heroes[k] : fallbackHero;
        }

        void ClearFighters()
        {
            StopAllCoroutines();
            SparkTrap.DespawnAll();
            _respawning.Clear();
            foreach (var f in _fighters) if (f.go != null) Destroy(f.go);
            _fighters.Clear();
            if (arenaCamera != null) arenaCamera.group.Clear();
        }

        /// <summary>End the match now (used by Training's "end" path and tests).</summary>
        public void EndMatch()
        {
            if (!IsLive) return;
            IsLive = false;
            if (Match != null && !Match.IsOver) Match.End();
            if (bridge != null) bridge.Running = false;

            foreach (var f in _fighters)
            {
                if (f.go == null) continue;
                if (f.go.TryGetComponent(out PlayerInputRouter router)) router.enabled = false;
                f.motor.Intent.ClearAll();
            }

            var placements = Match.Placements();
            int n = _fighters.Count;
            var result = new ArenaMatchEnded
            {
                mode = Config.mode, placements = placements, names = new string[n], knockouts = new int[n],
                falls = new int[n], scores = new int[n], playerPlacement = placements.Length > 0 ? placements[0] : 0
            };
            for (int i = 0; i < n; i++)
            {
                var rec = Match.Get(i);
                result.names[i] = _fighters[i].name;
                result.knockouts[i] = rec.knockouts;
                result.falls[i] = rec.falls;
                result.scores[i] = rec.score;
            }
            if (Config.mode != MatchMode.Training && grantRewards && Services.Save != null)
            {
                result.starShardReward = Economy.ArenaReward(result.playerPlacement);
                Services.Save.AddShards(result.starShardReward);
                Services.Save.SaveNow();
            }

            if (Services.State != null) Services.State.SetState(GameState.Results);
            EventBus<ArenaMatchEnded>.Raise(result);
        }

        // ---- KOs -------------------------------------------------------------------------------

        void KnockOut(Fighter f)
        {
            var d = f.health;
            int attacker = -1;
            if (d != null && d.LastAttackerTeam >= 0 && d.LastAttackerTeam != f.slot && Time.time - d.LastHitTime <= creditWindow)
                attacker = d.LastAttackerTeam;

            Vector2 at = f.motor.Body.position;
            Vector2 fx = new Vector2(Mathf.Clamp(at.x, blastZone.xMin + 1f, blastZone.xMax - 1f),
                                     Mathf.Clamp(at.y, blastZone.yMin + 1f, blastZone.yMax - 1f));
            PuffEffect.Play(fx, f.color, spriteMaterial, 0.4f, 7f, 0.6f);
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.35f, 0.3f);
            if (f.human && Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);

            bool respawn = Match.ReportFall(f.slot, attacker);
            EventBus<ArenaKO>.Raise(new ArenaKO { victimSlot = f.slot, attackerSlot = attacker, eliminated = !respawn });

            if (f.go.TryGetComponent(out HeroAbilities abilities)) abilities.Cancel();
            if (f.go.TryGetComponent(out AttackRunner atk)) atk.Cancel();
            f.motor.Intent.ClearAll();

            if (respawn) StartCoroutine(Respawn(f));
            else f.go.SetActive(false);
        }

        IEnumerator Respawn(Fighter f)
        {
            _respawning.Add(f.slot);
            f.go.SetActive(false);
            yield return new WaitForSeconds(respawnDelay);
            if (!IsLive) { _respawning.Remove(f.slot); yield break; }
            f.go.SetActive(true);
            Vector2 p = respawnPoint != null ? (Vector2)respawnPoint.position : new Vector2(0f, 8f);
            // Drop in over a main platform (never over the bridge gap), alternating sides.
            p.x = (f.slot % 2 == 0 ? -1f : 1f) * Mathf.Abs(stageRect.xMax) * 0.5f;
            f.motor.Teleport(p);
            f.health.ResetState();
            if (f.go.TryGetComponent(out Invulnerability inv)) inv.Grant(respawnInvulnerability, blink: true);
            _respawning.Remove(f.slot);
        }

        /// <summary>True (with its x range) while the Skyforge bridge is retracted or about to be.</summary>
        public bool GapOpen(out float left, out float right)
        {
            left = right = 0f;
            if (bridge == null || bridge.Phase == BridgePhase.Solid) return false;
            // Use the art's bounds: a disabled collider (bridge retracted) reports empty bounds.
            var b = bridge.art != null ? bridge.art.bounds : bridge.GetComponent<Collider2D>().bounds;
            left = b.min.x;
            right = b.max.x;
            return true;
        }

        void OnDestroy() => ClearFighters();

        /// <summary>Closest active opponent (for CPU targeting).</summary>
        public GameObject NearestOpponent(GameObject self, Vector2 from)
        {
            GameObject best = null;
            float bestD = float.MaxValue;
            foreach (var f in _fighters)
            {
                if (f.go == null || f.go == self || !f.go.activeSelf || _respawning.Contains(f.slot)) continue;
                float d = ((Vector2)f.go.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = f.go; }
            }
            return best;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(blastZone.center, blastZone.size);
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireCube(stageRect.center, stageRect.size);
        }
    }
}
