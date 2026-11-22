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
        /// <summary>1, or 2 for couch play.</summary>
        public int humans;
        /// <summary>Rumble Rating result for P1 (null when the match wasn't rated).</summary>
        public RumbleOutcome rumble;
        /// <summary>A Kingdom Plaza party quest (None for Arena Clash).</summary>
        public PartyMode party;
        /// <summary>Each fighter's best combo (hits landed while the target was still reeling).</summary>
        public int[] bestCombo;
        /// <summary>Arena Clash: P1 set a new career best for KOs in a match / combo.</summary>
        public bool newBestKOs, newBestCombo;
    }

    /// <summary>
    /// Runs an Arena Clash match in the Skyforge Arena: spawns the player and CPU fighters, frames them with the
    /// camera, detects blast-zone KOs (crediting whoever hit the victim last), respawns with blinking
    /// invulnerability, runs the clock and the bridge event, and reports results (with a Star Shard reward).
    /// The match only starts when ArenaSetupUI calls StartMatch.
    /// </summary>
    public class ArenaMatchController : MonoBehaviour, IBotArena
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
        /// <summary>Ring-Out Brawl shrinks it during the match.</summary>
        public Rect BlastZone { get => blastZone; set => blastZone = value; }
        public Material SpriteMaterial => spriteMaterial;
        public bool IsRespawning(int slot) => _respawning.Contains(slot);
        PartyArena _party;
        TrainingLab _lab;
        ComboCounter[] _combos = new ComboCounter[0];
        public CharacterRoster Roster => roster;

        readonly List<Fighter> _fighters = new List<Fighter>();
        readonly HashSet<int> _respawning = new HashSet<int>();
        RumbleTracker _tracker;
        TouchControlsUI _p1Touch, _p2Touch;

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
            ArenaMods.Apply(Config.mutators);
            _tracker = new RumbleTracker(0);
            bool couch = Config.humans >= 2;
            var couchPlan = couch ? CouchInput.Make() : default;
            if (couch && couchPlan.splitTouch) SetupSplitTouch();

            var playerDef = roster != null ? roster.Find(GameSession.SelectedCharacterId) : null;
            if (playerDef == null || !playerDef.playableInThisBuild) playerDef = fallbackHero;
            CharacterDefinition p2Def = null;
            if (couch)
            {
                p2Def = roster != null && !string.IsNullOrEmpty(Config.p2CharacterId) ? roster.Find(Config.p2CharacterId) : null;
                if (p2Def == null || !p2Def.playableInThisBuild) p2Def = null;
            }
            // CPU fighters pick random selectable heroes (seeded per match so a rematch can differ).
            var cpuRng = new System.Random(unchecked(System.Environment.TickCount * 31 + Config.FighterCount));

            if (arenaCamera != null) arenaCamera.group.Clear();
            for (int slot = 0; slot < Config.FighterCount; slot++)
            {
                bool human = slot < Config.humans;
                var def = slot == 0 ? playerDef : (human ? (p2Def ?? PickCpuHero(cpuRng)) : (ChosenCpuHero() ?? PickCpuHero(cpuRng)));
                Vector2 pos = spawnPoints != null && spawnPoints.Length > 0
                    ? (Vector2)spawnPoints[slot % spawnPoints.Length].position
                    : new Vector2(-6f + slot * 4f, 2f);
                string name = human ? $"P{slot + 1} {def.displayName.ToUpperInvariant()}" : $"CPU {slot + 1}";
                // You wear your shop palette; CPUs (and couch P2) are tinted by slot so they're easy to tell apart.
                Color tint = slot == 0 && roster != null ? roster.TintFor(def.id) : SlotTints[slot];
                var go = FighterFactory.CreateHero(def, spriteMaterial, pos, slot, DamageModel.ArenaPips, tint, name);
                var motor = go.GetComponent<PlatformerMotor2D>();
                motor.SetFacing(pos.x <= 0f ? 1 : -1);
                if (ArenaMods.SizeScale != 1f) go.transform.localScale *= ArenaMods.SizeScale;

                if (human)
                {
                    var router = go.AddComponent<PlayerInputRouter>();
                    if (slot == 0) go.AddComponent<PlayerMarker>();
                    if (couch)
                    {
                        if (slot == 0) router.Bind(couchPlan.p1Devices, couchPlan.splitTouch ? _p1Touch : router.Touch, "P1");
                        else router.Bind(couchPlan.p2Devices, couchPlan.splitTouch ? _p2Touch : null, "P2");
                    }
                }
                else
                {
                    var bot = go.AddComponent<BotController>();
                    bool training = Config.mode == MatchMode.Training;
                    bot.Init(this, RumbleRecorder.BotTuning(Config.autoLevel, Config.botLevel), 7919 * (slot + 1),
                             training && Config.trainingAct != TrainingAct.Fight);
                    if (training) bot.trainingAct = Config.trainingAct;
                }

                _fighters.Add(new Fighter
                {
                    slot = slot, name = name, human = human, color = SlotColors[slot], go = go,
                    health = go.GetComponent<Damageable>(), motor = motor
                });
                if (arenaCamera != null) arenaCamera.group.Add(go.transform);
            }

            // Best combo per fighter for the results (credited to whoever landed the hit).
            _combos = new ComboCounter[_fighters.Count];
            for (int i = 0; i < _combos.Length; i++) _combos[i] = new ComboCounter();
            foreach (var f in _fighters)
            {
                var victim = f;
                if (victim.health != null) victim.health.Hit += r =>
                {
                    int by = victim.health.LastAttackerTeam;
                    if (!r.blocked && by >= 0 && by < _combos.Length && by != victim.slot) _combos[by].Register(Time.time, r.hitstunFrames / 60f, 0, r.isLaunch);
                };
            }
            if (Config.mode != MatchMode.Training)
                MetaTracker.Played(Config.party != PartyMode.None ? ContinueKind.Plaza : ContinueKind.ArenaClash);
            if (bridge != null) { bridge.ResetCycle(); bridge.Running = Config.mode != MatchMode.Training && Config.party == PartyMode.None; }
            if (Config.party != PartyMode.None || (Config.items && Config.mode != MatchMode.Training))
            {
                if (_party == null) _party = gameObject.AddComponent<PartyArena>();
                _party.Begin(this);
            }
            if (Config.mode == MatchMode.Training)
            {
                if (_lab == null) _lab = gameObject.AddComponent<TrainingLab>();
                _lab.Begin(this);
            }
            IsLive = true;
            if (Services.State != null) Services.State.SetState(GameState.Playing);
            EventBus<ArenaMatchStarted>.Raise(new ArenaMatchStarted { mode = Config.mode, fighters = Config.FighterCount });
        }

        /// <summary>Setup screen CPU HEROES: every CPU fights as this hero (if you've unlocked it).</summary>
        CharacterDefinition ChosenCpuHero()
        {
            if (roster == null || string.IsNullOrEmpty(Config.cpuCharacterId)) return null;
            var def = roster.Find(Config.cpuCharacterId);
            return def != null && def.playableInThisBuild && roster.IsSelectable(def) ? def : null;
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

        /// <summary>Couch play without controllers: shrink the normal touch controls to the left half, add P2's on the right.</summary>
        void SetupSplitTouch()
        {
            _p1Touch = FindAnyObjectByType<TouchControlsUI>();
            var go = new GameObject("P2TouchControls");
            go.transform.SetParent(transform, false);
            _p2Touch = go.AddComponent<TouchControlsUI>();
            if (_p1Touch != null)
            {
                _p2Touch.forceShow = _p1Touch.forceShow;
                _p1Touch.SetRegion(0f, 0.5f, 0.8f, "P1");
            }
            _p2Touch.SetRegion(0.5f, 1f, 0.8f, "P2");
        }

        void ClearSplitTouch()
        {
            if (_p2Touch != null) Destroy(_p2Touch.gameObject);
            _p2Touch = null;
            if (_p1Touch != null) _p1Touch.SetRegion(0f, 1f, 1f, null);
            _p1Touch = null;
        }

        void ClearFighters()
        {
            StopAllCoroutines();
            ClearSplitTouch();
            ArenaMods.Reset();
            SparkTrap.DespawnAll();
            if (_party != null) _party.Clear();
            if (_lab != null) _lab.Clear();
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
                falls = new int[n], scores = new int[n], playerPlacement = placements.Length > 0 ? placements[0] : 0,
                humans = Config.humans, party = Config.party, bestCombo = new int[n]
            };
            for (int i = 0; i < n && i < _combos.Length; i++) result.bestCombo[i] = _combos[i].Best;
            for (int i = 0; i < n; i++)
            {
                var rec = Match.Get(i);
                result.names[i] = _fighters[i].name;
                result.knockouts[i] = rec.knockouts;
                result.falls[i] = rec.falls;
                result.scores[i] = rec.score;
            }
            if (Config.party != PartyMode.None && grantRewards && Services.Save != null)
            {
                // Party quests pay their own table (by difficulty) and count toward ranks, the daily challenge, the cup
                // and STORY STATS.
                int score = Config.party == PartyMode.BalloonBattle ? Match.Get(0).stocks : Match.Get(0).score;
                var report = PartyFlow.Finish(new PartyOutcome
                {
                    mode = Config.party, placement = result.playerPlacement, score = score, placements = (int[])placements.Clone()
                });
                result.starShardReward = report.shards + report.dailyBonus + report.cupBonus;
            }
            else if (Config.mode != MatchMode.Training && grantRewards && Services.Save != null)
            {
                result.starShardReward = Economy.ArenaReward(result.playerPlacement);
                Services.Save.AddShards(result.starShardReward);
                Services.Save.SaveNow();
            }
            if (_lab != null) _lab.Clear();
            if (_party != null) _party.Clear();
            if (Config.Rated && grantRewards && Services.Save != null && n >= 2)
            {
                var me = Match.Get(0);
                var report = new RumbleMatchReport
                {
                    fighters = n, placement = placements[0], knockouts = me.knockouts, falls = me.falls,
                    selfDestructs = _tracker != null ? _tracker.SelfDestructs : 0,
                    firstBlood = _tracker != null && _tracker.FirstBlood,
                    revengeKOs = _tracker != null ? _tracker.RevengeKOs : 0,
                    comeback = _tracker != null && _tracker.WasLast,
                    clutch = Config.mode == MatchMode.Stock && Config.stocks > 1 && me.stocks == 1 && !me.IsEliminated,
                    difficulty = RumbleRecorder.Difficulty(Config.autoLevel, Config.botLevel),
                };
                result.rumble = RumbleRecorder.Record(report, Config.daily);
            }

            if (Config.mode != MatchMode.Training && grantRewards) ProgressTracker.MatchPlayed(result.playerPlacement == 1, versus: false);
            // Hero mastery for your fighter (party quests count too; Training doesn't).
            if (Config.mode != MatchMode.Training && grantRewards && _fighters.Count > 0 && _fighters[0].go != null)
            {
                var vis = _fighters[0].go.GetComponent<FighterVisual>();
                bool won = result.playerPlacement == 1;
                int xp = Config.party != PartyMode.None ? (won ? HeroMastery.PartyWinXp : HeroMastery.PartyMatchXp)
                                                        : (won ? HeroMastery.ArenaWinXp : HeroMastery.ArenaMatchXp);
                if (vis != null && vis.Character != null) MetaTracker.Mastery(vis.Character.id, xp, roster);
            }
            // Arena Clash career (Training and party quests don't count): bests, KO milestones and badges.
            if (Config.mode != MatchMode.Training && Config.party == PartyMode.None && grantRewards && Services.Save != null && n > 0)
            {
                var c = ArenaCareer.Record(Services.Save.Data, result.playerPlacement == 1, result.knockouts[0], result.bestCombo[0]);
                if (result.playerPlacement == 1 && Services.Save.Data.lifetime.arenaWins == 1)
                    EventBus<ProgressToast>.Raise(new ProgressToast { celebration = true, title = "FIRST ARENA CLASH WIN!", detail = "The first of many. Try a harder CPU next.", color = new Color32(255, 170, 60, 255) });
                result.newBestKOs = c.newBestKOs; result.newBestCombo = c.newBestCombo;
                if (c.koMilestone > 0)
                    EventBus<ProgressToast>.Raise(new ProgressToast { celebration = true, title = $"{c.koMilestone:N0} ARENA KOs!", detail = "A lifetime milestone in Arena Clash.", color = new Color32(255, 170, 60, 255) });
                MetaTracker.CheckBadges();
                Services.Save.MarkDirty();
            }
            else if (Config.party != PartyMode.None && grantRewards && Services.Save != null)
            {
                var pr = Services.Save.Data.party != null ? Services.Save.Data.party.Find(Config.party) : null;
                if (result.playerPlacement == 1 && pr != null && PartyMeta.WinMilestone(pr.wins))
                    EventBus<ProgressToast>.Raise(new ProgressToast { celebration = true, title = pr.wins == 1 ? $"FIRST {PartyModes.Info(Config.party).name} WIN!" : $"{PartyModes.Info(Config.party).name}: WIN #{pr.wins}", detail = "Party quest milestone.", color = new Color32(255, 200, 90, 255) });
                MetaTracker.CheckBadges();
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
            _tracker?.OnFall(f.slot, attacker, Match.Placements());
            if (_party != null) _party.OnFall(f.slot);
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

        /// <summary>
        /// Balloon Battle: a hard hit (or an item) popped one of this fighter's balloons. Counts like a fall (credit to
        /// the attacker); the last balloon knocks them out of the match.
        /// </summary>
        public void Pop(int victimSlot, int attackerSlot)
        {
            if (!IsLive || Match == null || Match.IsOver || victimSlot < 0 || victimSlot >= _fighters.Count) return;
            var f = _fighters[victimSlot];
            if (f.go == null || !f.go.activeSelf || _respawning.Contains(victimSlot)) return;
            bool stillIn = Match.ReportFall(victimSlot, attackerSlot);
            _tracker?.OnFall(victimSlot, attackerSlot, Match.Placements());
            EventBus<ArenaKO>.Raise(new ArenaKO { victimSlot = victimSlot, attackerSlot = attackerSlot, eliminated = !stillIn });
            PuffEffect.Play((Vector2)f.go.transform.position + Vector2.up * 1.2f, f.color, spriteMaterial, 0.3f, 6f, 0.5f);
            if (f.human && Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            if (!stillIn)
            {
                if (f.go.TryGetComponent(out HeroAbilities abilities)) abilities.Cancel();
                if (f.go.TryGetComponent(out AttackRunner atk)) atk.Cancel();
                f.motor.Intent.ClearAll();
                f.go.SetActive(false);
            }
            else if (f.go.TryGetComponent(out Invulnerability inv)) inv.Grant(1.2f, blink: true);
        }

        /// <summary>Closest active opponent (for CPU targeting). Party quests may point CPUs at coins, the crown zone or an item box instead.</summary>
        public GameObject NearestOpponent(GameObject self, Vector2 from)
        {
            if (_party != null && _party.Running)
            {
                var objective = _party.ObjectiveFor(self, from);
                if (objective != null) return objective;
            }
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
