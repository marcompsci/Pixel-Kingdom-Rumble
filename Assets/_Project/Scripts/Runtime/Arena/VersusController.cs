using System;
using System.Collections;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Raised by VersusController for the HUD: "ROUND 1", "FIGHT!", "K.O.", "TIME", "YOU WIN".</summary>
    public struct VersusAnnounce { public string text; public float seconds; }

    public struct VersusMatchEnded
    {
        public int winner;           // 0 = player, 1 = CPU, -1 = draw
        public string playerName, opponentName;
        public int playerRounds, opponentRounds;
        public int starShardReward;
    }

    /// <summary>
    /// Runs a 1-on-1 Versus match in the dojo: spawns your fighter and the CPU's (chosen on the Versus select
    /// screen), plays round intros, K.O.s and time-ups through Core VersusMatch, and reports the result.
    /// Health uses the Story model (no blinking invulnerability, so combos connect) with VersusRules health.
    /// </summary>
    public class VersusController : MonoBehaviour, IBotArena
    {
        public class Fighter
        {
            public string name;
            public CharacterDefinition def;
            public GameObject go;
            public Damageable health;
            public PlatformerMotor2D motor;
            public Vector2 spawn;
        }

        [SerializeField] CharacterRoster roster;
        [SerializeField] CharacterDefinition fallbackHero;
        [SerializeField] Material spriteMaterial;
        [SerializeField] Vector2 playerSpawn = new Vector2(-4f, 1f);
        [SerializeField] Vector2 opponentSpawn = new Vector2(4f, 1f);
        [Tooltip("Floor: x range = walls, yMax = floor height. Used by the CPU.")]
        [SerializeField] Rect stageRect = new Rect(-13f, -1f, 26f, 1f); // wider than the walls so the CPU never sees an edge
        [SerializeField] CameraFollow2D arenaCamera;
        [SerializeField] float roundIntroSeconds = 1.6f;
        [SerializeField] float roundOutroSeconds = 2.2f;
        [Tooltip("Off in tests so they don't add Star Shards to the real save.")]
        public bool grantRewards = true;
        [Tooltip("Off in tests: start the match yourself with StartMatch.")]
        public bool autoStart = true;

        public static readonly Color PlayerColor = new Color32(46, 220, 190, 255);
        public static readonly Color OpponentColor = new Color32(255, 100, 100, 255);
        static readonly Color OpponentTint = new Color(1f, 0.82f, 0.82f);

        public VersusMatch Match { get; private set; }
        public Fighter Player { get; private set; }
        public Fighter Opponent { get; private set; }
        public bool IsLive => Match != null && Match.Phase == VersusPhase.Fighting && !_ending && !_resolving;
        public Rect StageRect => stageRect;
        public bool PerfectRoundWon { get; private set; }

        public event Action RoundStarted;

        bool _ending, _resolving;
        Coroutine _flow;

        void Start()
        {
            if (autoStart) StartMatch(GameSession.SelectedCharacterId, GameSession.VersusOpponentId, GameSession.VersusBotLevel);
        }

        void OnDestroy() => Clear();

        // ---- Match ---------------------------------------------------------------------------------

        public void StartMatch(string playerId, string opponentId, BotLevel level)
        {
            Clear();
            Match = new VersusMatch();
            PerfectRoundWon = false;
            var playerDef = Pick(playerId, null);
            var opponentDef = Pick(opponentId, playerDef);
            Player = Spawn(playerDef, playerSpawn, 0, human: true, level);
            Opponent = Spawn(opponentDef, opponentSpawn, 1, human: false, level);
            _flow = StartCoroutine(RoundIntro());
        }

        CharacterDefinition Pick(string id, CharacterDefinition avoid)
        {
            var def = roster != null && !string.IsNullOrEmpty(id) ? roster.Find(id) : null;
            if (def != null && def.playableInThisBuild) return def;
            if (roster != null)
            {
                // Random opponent: any selectable fighter, preferring someone other than you.
                var pool = new System.Collections.Generic.List<CharacterDefinition>();
                foreach (var h in roster.heroes) if (roster.IsSelectable(h) && h != avoid) pool.Add(h);
                if (pool.Count == 0 && avoid != null) pool.Add(avoid);
                if (pool.Count > 0) return pool[UnityEngine.Random.Range(0, pool.Count)];
            }
            return fallbackHero;
        }

        Fighter Spawn(CharacterDefinition def, Vector2 at, int slot, bool human, BotLevel level)
        {
            Color tint = human ? (roster != null ? roster.TintFor(def.id) : Color.white) : OpponentTint;
            string name = def.displayName.ToUpperInvariant();
            var go = FighterFactory.CreateHero(def, spriteMaterial, at, slot, DamageModel.StoryHealth, tint,
                                              human ? "P1 " + name : "CPU " + name);
            var dmg = go.GetComponent<Damageable>();
            dmg.Configure(DamageModel.StoryHealth, slot, VersusRules.HealthFor(def.weight), def.weight, def.armorPercent, 0f);
            dmg.Died += () => OnFighterDied(slot);
            var motor = go.GetComponent<PlatformerMotor2D>();
            motor.SetFacing(at.x <= 0f ? 1 : -1);
            if (human)
            {
                go.AddComponent<PlayerInputRouter>();
                go.AddComponent<PlayerMarker>();
            }
            else
            {
                go.AddComponent<BotController>().Init(this, BotDifficulty.For(level), 7919 + RuntimeIds.Next(), isPassive: false);
            }
            if (arenaCamera != null) arenaCamera.group.Add(go.transform);
            return new Fighter { name = name, def = def, go = go, health = dmg, motor = motor, spawn = at };
        }

        void Clear()
        {
            if (_flow != null) StopCoroutine(_flow);
            _flow = null;
            _ending = _resolving = false;
            SparkTrap.DespawnAll();
            Projectile.DespawnAll();
            if (arenaCamera != null) arenaCamera.group.Clear();
            if (Player != null && Player.go != null) Destroy(Player.go);
            if (Opponent != null && Opponent.go != null) Destroy(Opponent.go);
            Player = Opponent = null;
        }

        void Update()
        {
            if (!IsLive) return;
            var state = Services.State;
            if (state != null && state.State != GameState.Playing) return; // paused
            if (Match.Tick(Time.deltaTime))
            {
                Match.EndRoundOnTime(Share(Player), Share(Opponent));
                _flow = StartCoroutine(RoundOutro(timeUp: true));
            }
        }

        static float Share(Fighter f) => f.health.MaxHealth > 0 ? (float)f.health.Health / f.health.MaxHealth : 0f;

        void OnFighterDied(int slot)
        {
            if (!IsLive) return;
            // Resolve after the physics step so a trade where both fall on the same frame is a double K.O.
            _resolving = true;
            _flow = StartCoroutine(ResolveKO());
        }

        IEnumerator ResolveKO()
        {
            yield return new WaitForFixedUpdate();
            _resolving = false;
            if (Match.Phase != VersusPhase.Fighting) yield break;
            bool p = Player.health.IsDead, o = Opponent.health.IsDead;
            if (p && o) Match.ReportDoubleKO();
            else Match.ReportKO(p ? 0 : 1);
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.4f, 0.35f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
            _flow = StartCoroutine(RoundOutro(timeUp: false));
        }

        IEnumerator RoundIntro()
        {
            ResetFighters();
            SetControl(false);
            if (Services.State != null) Services.State.SetState(GameState.Playing);
            int next = Match.Round + 1;
            bool finalRound = Match.Wins(0) == Match.RoundsToWin - 1 && Match.Wins(1) == Match.RoundsToWin - 1;
            Announce(finalRound ? "FINAL ROUND" : $"ROUND {next}", roundIntroSeconds * 0.6f);
            yield return new WaitForSeconds(roundIntroSeconds * 0.6f);
            Announce("FIGHT!", roundIntroSeconds * 0.4f);
            yield return new WaitForSeconds(roundIntroSeconds * 0.4f);
            Match.StartRound();
            SetControl(true);
            RoundStarted?.Invoke();
            _flow = null;
        }

        IEnumerator RoundOutro(bool timeUp)
        {
            _ending = true;
            SetControl(false);
            Freeze();
            int w = Match.LastRoundWinner;
            bool perfect = w == 0 && Player.health.Health >= Player.health.MaxHealth;
            if (perfect) PerfectRoundWon = true;
            string headline = timeUp ? "TIME" : (w < 0 ? "DOUBLE K.O." : "K.O.");
            Announce(headline, roundOutroSeconds * 0.45f);
            yield return new WaitForSeconds(roundOutroSeconds * 0.45f);
            string sub = w < 0 ? "DRAW" : w == 0 ? $"{Player.name} WINS" : $"{Opponent.name} WINS";
            if (perfect) sub = "PERFECT!";
            Announce(sub, roundOutroSeconds * 0.55f);
            yield return new WaitForSeconds(roundOutroSeconds * 0.55f);
            _ending = false;
            if (Match.Phase == VersusPhase.MatchOver) Finish();
            else _flow = StartCoroutine(RoundIntro());
        }

        void Finish()
        {
            _flow = null;
            bool won = Match.Winner == 0;
            int reward = VersusRules.Reward(won, PerfectRoundWon);
            if (!grantRewards) reward = 0;
            else if (Services.Save != null)
            {
                Services.Save.AddShards(reward);
                Services.Save.SaveNow();
            }
            if (Services.State != null) Services.State.SetState(GameState.Results);
            if (Services.Haptics != null) Services.Haptics.Play(won ? HapticStrength.Heavy : HapticStrength.Medium);
            EventBus<VersusMatchEnded>.Raise(new VersusMatchEnded
            {
                winner = Match.Winner, playerName = Player.name, opponentName = Opponent.name,
                playerRounds = Match.Wins(0), opponentRounds = Match.Wins(1), starShardReward = reward
            });
        }

        void ResetFighters()
        {
            foreach (var f in new[] { Player, Opponent })
            {
                if (f == null || f.go == null) continue;
                if (!f.go.activeSelf) f.go.SetActive(true);
                if (f.go.TryGetComponent(out HeroAbilities a)) a.Cancel();
                if (f.go.TryGetComponent(out AttackRunner atk)) atk.Cancel();
                f.motor.Intent.ClearAll();
                SetDownPose(f, false);
                if (f.go.TryGetComponent(out Invulnerability inv)) inv.Clear();
                f.motor.Teleport(f.spawn);
                f.motor.SetFacing(f.spawn.x <= 0f ? 1 : -1);
                f.health.ResetState();
            }
            SparkTrap.DespawnAll();
            Projectile.DespawnAll();
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.SnapToTarget();
        }

        /// <summary>Round over: stop every move, clear projectiles and traps, nobody can be hurt; K.O.'d fighters fall.</summary>
        void Freeze()
        {
            foreach (var f in new[] { Player, Opponent })
            {
                if (f == null || f.go == null) continue;
                if (f.go.TryGetComponent(out HeroAbilities a)) a.Cancel();
                if (f.go.TryGetComponent(out AttackRunner atk)) atk.Cancel();
                f.motor.Intent.ClearAll();
                if (f.go.TryGetComponent(out Invulnerability inv)) inv.Grant(roundOutroSeconds + 1f);
                if (f.health.IsDead) SetDownPose(f, true);
            }
            SparkTrap.DespawnAll();
            Projectile.DespawnAll();
        }

        /// <summary>Placeholder K.O. reaction: the body sprite tips over onto its back.</summary>
        static void SetDownPose(Fighter f, bool down)
        {
            if (f.go.transform.childCount == 0) return;
            var body = f.go.transform.GetChild(0);
            body.localRotation = down ? Quaternion.Euler(0f, 0f, f.motor.Facing > 0 ? 90f : -90f) : Quaternion.identity;
            body.localPosition = new Vector3(0f, down ? -0.45f : -0.7f, 0f);
        }

        void SetControl(bool on)
        {
            if (Player != null && Player.go != null && Player.go.TryGetComponent(out PlayerInputRouter router))
            {
                router.GameplayLocked = !on; // pause (Esc/P/Start) keeps working between rounds
                if (!on) Player.motor.Intent.ClearAll();
            }
            if (!on && Opponent != null && Opponent.motor != null) Opponent.motor.Intent.ClearAll();
        }

        static void Announce(string text, float seconds) =>
            EventBus<VersusAnnounce>.Raise(new VersusAnnounce { text = text, seconds = seconds });

        // ---- IBotArena -----------------------------------------------------------------------------

        public bool GapOpen(out float left, out float right) { left = right = 0f; return false; }

        public GameObject NearestOpponent(GameObject self, Vector2 from)
        {
            if (Player == null || Opponent == null) return null;
            var other = self == Player.go ? Opponent.go : Player.go;
            return other != null && other.activeSelf ? other : null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireCube(stageRect.center, stageRect.size);
        }
    }
}
