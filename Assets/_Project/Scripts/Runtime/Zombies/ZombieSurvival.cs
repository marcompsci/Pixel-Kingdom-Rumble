using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Halloween Zombie Survival: endless rounds of zombies in the manor yard. Each round sends more (and faster,
    /// tougher) zombies through the window barricades and, later, off the roof; every 5th round the Pumpkin King leads
    /// them. Points from hits and kills buy wall guns, the mystery crate, perks, the upgrade forge and the gallery door.
    /// Power-ups drop now and then. When the hero goes down (and has no Second Wind) the run ends; the best round,
    /// points and kills are saved and the run pays Star Shards.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class ZombieSurvival : MonoBehaviour
    {
        public static ZombieSurvival Current { get; private set; }

        [Header("Scene")]
        public GameObject player;
        public Transform startPoint;
        public Material spriteMaterial;
        public Transform[] windowSpawns;
        public Transform[] roofSpawns;
        public Barricade[] barricades;
        public ZombieStation[] stations;
        public float killY = -4f;

        [Header("Cast")]
        public EnemyDefinition shambler, runner, bloater, spitter, brute, bat, pumpkinKing;

        [Header("Art")]
        public Sprite[] powerUpSprites = new Sprite[5]; // PowerUpKind order
        public Sprite healthSprite;

        public SurvivalRun Run { get; private set; } = new SurvivalRun();
        public SurvivalMode Mode => Run.Mode;
        /// <summary>Supply caches earned this run (one every 3 rounds survived).</summary>
        public int CachesEarned { get; private set; }
        /// <summary>Hardcore secret: the Lantern Wraith is out (or beaten).</summary>
        public bool WraithSummoned { get; private set; }
        public bool WraithBeaten { get; private set; }
        public List<int> RanksReached { get; private set; } = new List<int>();
        public int XpEarned { get; private set; }
        bool _wraithPending, _freeSpin;
        EnemyAI _wraith;
        /// <summary>Map id for records (Survivor Rank bests).</summary>
        public const string MapId = "manor";
        public bool GameOver { get; private set; }
        public bool Intermission { get; private set; } = true;
        public float IntermissionLeft { get; private set; }
        public int ZombiesLeft => _toSpawn + _alive.Count;
        public float InstaKillLeft { get; private set; }
        public float DoublePointsLeft { get; private set; }
        /// <summary>What USE would do right now (shown by the HUD), e.g. "HEX SCATTERGUN  1500".</summary>
        public string Prompt { get; private set; } = "";
        public Damageable PlayerHealth { get; private set; }
        public ZombieGunner Gunner { get; private set; }
        public int LanternsLit { get; private set; }
        public int LanternsTotal { get; private set; }

        readonly Dictionary<EnemyAI, ZombieKind> _alive = new Dictionary<EnemyAI, ZombieKind>();
        readonly Dictionary<Damageable, System.Action<HitResult>> _hitHandlers = new Dictionary<Damageable, System.Action<HitResult>>();
        readonly List<EnemyAI> _pendingKill = new List<EnemyAI>();
        readonly List<(GameObject go, PowerUpKind kind, float until)> _drops = new List<(GameObject, PowerUpKind, float)>();
        int _toSpawn;
        bool _bossPending;
        float _spawnTimer;
        bool _nuking;
        PlatformerMotor2D _motor;

        public event System.Action<string> Banner;
        public event System.Action RunEnded;

        void Awake()
        {
            Current = this;
            int xp = Services.Save != null && Services.Save.Data.zombieCareer != null ? Services.Save.Data.zombieCareer.xp : 0;
            var mode = GameSession.Mode == SessionMode.Zombies ? GameSession.ZombieMode : SurvivalMode.Endless;
            Run = new SurvivalRun(mode, SurvivorRank.BonusPoints(xp));
            _freeSpin = SurvivorRank.Has(xp, "kit_spin");
            if (GetComponent<JuiceDirector>() == null) gameObject.AddComponent<JuiceDirector>();
            if (player != null)
            {
                PlayerHealth = player.GetComponent<Damageable>();
                _motor = player.GetComponent<PlatformerMotor2D>();
                Gunner = player.GetComponent<ZombieGunner>();
                if (Gunner == null) Gunner = player.AddComponent<ZombieGunner>();
                Gunner.material = spriteMaterial;
            }
            foreach (var s in stations) if (s != null && s.kind == StationKind.Lantern) LanternsTotal++;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            if (PlayerHealth != null)
            {
                PlayerHealth.bonusMaxHealth = Upgrades.BonusHealth(Services.Save != null ? Services.Save.Data : null);
                PlayerHealth.ResetState();
                PlayerHealth.Died += OnPlayerDied;
                PlayerHealth.Hit += OnPlayerHit;
            }
            foreach (var b in barricades) if (b != null) b.Repaired += OnRepaired;
            if (Gunner != null)
            {
                int xp = Services.Save != null && Services.Save.Data.zombieCareer != null ? Services.Save.Data.zombieCareer.xp : 0;
                int tier = SurvivorRank.StarterTier(xp);
                if (tier > 0)
                {
                    if (Gunner.Slots.Count == 0) Gunner.Give(ZombieArmory.PeaPopper);
                    Gunner.Give(ZombieArmory.Wall[Mathf.Clamp(tier, 1, ZombieArmory.Wall.Length - 1)]);
                }
            }
            if (Services.State != null) Services.State.SetState(GameState.Playing);
            StartCoroutine(NextRound(3f));
        }

        // ---- rounds -----------------------------------------------------------------------------------------------

        IEnumerator NextRound(float wait)
        {
            Intermission = true;
            IntermissionLeft = wait;
            while (IntermissionLeft > 0f)
            {
                if (!GameOver && (Services.State == null || Services.State.State == GameState.Playing)) IntermissionLeft -= Time.deltaTime;
                yield return null;
            }
            if (GameOver) yield break;
            int round = Run.NextRound();
            _toSpawn = ZombieRules.ZombiesInRound(round);
            _bossPending = SurvivalModes.IsBossRound(Mode, round) && pumpkinKing != null;
            _spawnTimer = 1f;
            Intermission = false;
            Sfx.Play(Sfx.RoundStart, 0.9f, 0f);
            string head = Mode == SurvivalMode.Raid ? $"ROUND {round}/{SurvivalModes.RaidRounds}" : $"ROUND {round}";
            if (SurvivalModes.IsFinalRound(Mode, round)) head += "\nFINAL ROUND!";
            Banner?.Invoke(_bossPending ? head + "\nTHE PUMPKIN KING RISES!" : head);
            if (_wraithPending)
            {
                _wraithPending = false;
                SpawnWraith();
            }
        }

        void Update()
        {
            if (GameOver) return;
            bool playing = Services.State == null || Services.State.State == GameState.Playing;
            if (!playing) return;
            float dt = Time.deltaTime;

            if (InstaKillLeft > 0f) InstaKillLeft -= dt;
            if (DoublePointsLeft > 0f && (DoublePointsLeft -= dt) <= 0f) Run.DoublePoints = false;

            if (player != null && player.transform.position.y < killY && _motor != null)
            {
                _motor.Teleport(startPoint != null ? (Vector2)startPoint.position : Vector2.zero);
                if (PlayerHealth != null) PlayerHealth.TakeDirectDamage(1);
            }

            // Spook Strike kills are applied here, after the hit that triggered them has finished.
            if (_pendingKill.Count > 0)
            {
                var list = new List<EnemyAI>(_pendingKill);
                _pendingKill.Clear();
                foreach (var e in list)
                    if (e != null && _alive.ContainsKey(e) && e.TryGetComponent(out Damageable d) && !d.IsDead) d.Kill();
            }

            if (!Intermission) SpawnTick(dt);
            UpdateDrops();
            UpdateStations();

            if (!Intermission && _toSpawn <= 0 && !_bossPending && _alive.Count == 0)
            {
                int bonus = Run.Earn(ZombieRules.RoundBonus(Run.Round));
                if (SurvivalModes.IsFinalRound(Mode, Run.Round))
                {
                    Run.Won = true;
                    Banner?.Invoke("RAID COMPLETE!");
                    EndRun();
                    return;
                }
                Banner?.Invoke($"ROUND {Run.Round} SURVIVED!  +{bonus}");
                if (SurvivalModes.CacheAfter(Run.Round)) StartCoroutine(SupplyCache());
                StartCoroutine(NextRound(SurvivalModes.Intermission(Mode)));
                Intermission = true;
            }
        }

        void SpawnTick(float dt)
        {
            _spawnTimer -= dt;
            if (_spawnTimer > 0f) return;
            if (_alive.Count >= ZombieRules.MaxAlive(Run.Round)) { _spawnTimer = 0.4f; return; }
            if (_bossPending)
            {
                _bossPending = false;
                Spawn(ZombieKind.PumpkinKing);
                _spawnTimer = 2f;
                return;
            }
            if (_toSpawn <= 0) return;
            _toSpawn--;
            Spawn(ZombieRules.Pick(Run.Round, Random.value));
            _spawnTimer = ZombieRules.SpawnInterval(Run.Round) * SurvivalModes.SpawnScale(Mode);
        }

        EnemyDefinition DefFor(ZombieKind k)
        {
            switch (k)
            {
                case ZombieKind.Runner: return runner != null ? runner : shambler;
                case ZombieKind.Bloater: return bloater != null ? bloater : shambler;
                case ZombieKind.Spitter: return spitter != null ? spitter : shambler;
                case ZombieKind.Brute: return brute != null ? brute : shambler;
                case ZombieKind.Bat: return bat != null ? bat : shambler;
                case ZombieKind.PumpkinKing: return pumpkinKing != null ? pumpkinKing : brute;
                default: return shambler;
            }
        }

        Vector2 SpawnPoint(ZombieKind k)
        {
            bool roofOk = Run.Round >= ZombieRules.RoofSpawnsFromRound && roofSpawns != null && roofSpawns.Length > 0;
            if ((k == ZombieKind.Bat || (roofOk && Random.value < 0.25f)) && roofSpawns != null && roofSpawns.Length > 0)
                return roofSpawns[Random.Range(0, roofSpawns.Length)].position;
            if (windowSpawns == null || windowSpawns.Length == 0) return startPoint != null ? (Vector2)startPoint.position + Vector2.up * 6f : Vector2.zero;
            return windowSpawns[Random.Range(0, windowSpawns.Length)].position;
        }

        void Spawn(ZombieKind kind)
        {
            var def = DefFor(kind);
            if (def == null) return;
            Vector2 pos = SpawnPoint(kind);
            int dir = player != null && player.transform.position.x < pos.x ? -1 : 1;
            var ai = EnemyFactory.Spawn(def, pos + Vector2.up * (def.bodySize.y * 0.5f + 0.05f), dir, spriteMaterial);
            var health = ai.GetComponent<Damageable>();
            int hp = ZombieRules.Health(kind, Run.Round);
            hp = kind == ZombieKind.PumpkinKing ? SurvivalModes.BossHealth(Mode, Run.Round, hp) : Mathf.RoundToInt(hp * SurvivalModes.HealthScale(Mode));
            health.Configure(DamageModel.StoryHealth, TeamIds.Enemy, Mathf.Max(1, hp), def.weight, 0f, 0f);
            if (kind != ZombieKind.Bat) ai.SpeedOverride = ZombieRules.Speed(kind, Run.Round) * SurvivalModes.SpeedScale(Mode);
            ai.ChaseRangeOverride = 200f; // in the arena every zombie knows where you are
            ai.Defeated -= OnZombieDefeated;
            ai.Defeated += OnZombieDefeated;
            System.Action<HitResult> onHit = r => OnZombieHit(ai);
            health.Hit += onHit;
            _hitHandlers[health] = onHit;
            _alive[ai] = kind;
            if (kind == ZombieKind.PumpkinKing && CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.25f, 0.5f);
            if (Random.value < 0.3f) Sfx.Play(Sfx.Groan, 0.45f, 0.2f);
        }

        void OnZombieHit(EnemyAI ai)
        {
            if (!_alive.ContainsKey(ai)) return;
            Run.Earn(ZombieRules.HitPoints);
            if (InstaKillLeft > 0f && !_pendingKill.Contains(ai)) _pendingKill.Add(ai);
        }

        void OnZombieDefeated(EnemyAI ai)
        {
            if (!_alive.TryGetValue(ai, out var kind)) return;
            bool melee = !Projectile.Resolving && !_nuking;
            if (!_nuking) Run.Kill(kind, melee);
            ProgressTracker.Report(ChallengeKind.DefeatEnemies, 1);
            Vector2 pos = ai.transform.position;
            RemoveZombie(ai);
            PuffEffect.Play(pos, new Color32(120, 200, 90, 255), spriteMaterial, 0.24f, 4f, 0.45f);
            if (ai == _wraith)
            {
                _wraith = null;
                WraithDefeated();
            }
            else if (kind == ZombieKind.PumpkinKing) Banner?.Invoke("THE PUMPKIN KING FALLS!");
            if (!_nuking && ZombieRules.Drops(kind, Run.DropsThisRound, Run.KillsSinceDrop, Random.value, Mode, Run.HasPerk(PerkKind.Scavenger)))
            {
                Run.CountDrop();
                SpawnDrop(ZombieRules.PickPowerUp(Random.value), pos + Vector2.up * 0.4f);
            }
        }

        void RemoveZombie(EnemyAI ai)
        {
            _alive.Remove(ai);
            ai.Defeated -= OnZombieDefeated;
            if (ai.TryGetComponent(out Damageable d) && _hitHandlers.TryGetValue(d, out var h))
            {
                d.Hit -= h;
                _hitHandlers.Remove(d);
            }
            EnemyFactory.Release(ai);
        }

        // ---- power-ups --------------------------------------------------------------------------------------------

        void SpawnDrop(PowerUpKind kind, Vector2 pos)
        {
            var go = new GameObject("PowerUp_" + kind);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            int i = (int)kind;
            sr.sprite = powerUpSprites != null && i < powerUpSprites.Length ? powerUpSprites[i] : null;
            if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
            sr.sortingOrder = 30;
            _drops.Add((go, kind, Time.time + ZombieRules.DropLifetime));
            Sfx.Play(Sfx.Shard, 0.6f, 0f);
        }

        void UpdateDrops()
        {
            Vector2 hero = player != null ? (Vector2)player.transform.position : new Vector2(9999f, 9999f);
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var d = _drops[i];
                if (d.go == null) { _drops.RemoveAt(i); continue; }
                float left = d.until - Time.time;
                var sr = d.go.GetComponent<SpriteRenderer>();
                d.go.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 5f) * 0.08f);
                if (sr != null) sr.enabled = left > 5f || Mathf.Repeat(Time.time, 0.3f) < 0.2f; // blink before it goes
                if (left <= 0f) { Destroy(d.go); _drops.RemoveAt(i); continue; }
                if (((Vector2)d.go.transform.position - hero).sqrMagnitude < 1.1f * 1.1f)
                {
                    Apply(d.kind, d.go.transform.position);
                    Destroy(d.go);
                    _drops.RemoveAt(i);
                }
            }
        }

        void Apply(PowerUpKind kind, Vector2 at)
        {
            Sfx.Play(Sfx.PowerUp, 0.9f, 0f);
            PuffEffect.Play(at, new Color32(255, 230, 120, 255), spriteMaterial, 0.3f, 5f, 0.5f);
            Banner?.Invoke(ZombieRules.PowerUpName(kind) + "!");
            switch (kind)
            {
                case PowerUpKind.AmmoPinata:
                    if (Gunner != null) Gunner.RefillAll();
                    break;
                case PowerUpKind.SpookyStrike:
                    InstaKillLeft = ZombieRules.PowerUpSeconds;
                    break;
                case PowerUpKind.TwinTreats:
                    DoublePointsLeft = ZombieRules.PowerUpSeconds;
                    Run.DoublePoints = true;
                    break;
                case PowerUpKind.Moonburst:
                    _nuking = true;
                    foreach (var e in new List<EnemyAI>(_alive.Keys))
                        if (e != null && _alive.TryGetValue(e, out var k) && k != ZombieKind.PumpkinKing && e.TryGetComponent(out Damageable d)) d.Kill();
                    _nuking = false;
                    Run.Earn(ZombieRules.MoonburstPoints);
                    if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.3f, 0.4f);
                    break;
                case PowerUpKind.BoardUp:
                    foreach (var b in barricades) if (b != null) b.SetBoards(ZombieRules.BarricadeBoards);
                    Run.Earn(ZombieRules.BoardUpPoints);
                    break;
            }
        }

        void OnRepaired() => Run.Earn(ZombieRules.RepairPoints);

        // ---- stations ---------------------------------------------------------------------------------------------

        void UpdateStations()
        {
            Prompt = "";
            if (player == null || Gunner == null) return;
            Vector2 hero = player.transform.position;
            ZombieStation near = null;
            foreach (var s in stations)
            {
                if (s == null) continue;
                if (s.kind == StationKind.Lantern)
                {
                    if (!s.Used && s.InReach(hero) && player.TryGetComponent(out AttackRunner atk) && atk.IsAttacking) LightLantern(s);
                    continue;
                }
                if (s.kind == StationKind.Door && s.Used) continue;
                if (s.InReach(hero)) { near = s; break; }
            }
            if (near == null)
            {
                bool nearWindow = false;
                foreach (var b in barricades)
                    if (b != null && b.Boards < ZombieRules.BarricadeBoards && Mathf.Abs(b.transform.position.x - hero.x) < b.repairReach &&
                        Mathf.Abs(b.transform.position.y - hero.y) < 2f) nearWindow = true;
                if (nearWindow) Prompt = "HOLD USE: REPAIR THE WINDOW  +10";
                else if (Gunner.Slots.Count > 1 && Gunner.UsePressed) Gunner.Swap();
                return;
            }
            Prompt = PromptFor(near);
            if (Gunner.UsePressed) Use(near);
        }

        string PromptFor(ZombieStation s)
        {
            if (Time.time < s.BusyUntil) return s.kind == StationKind.Forge ? "THE FORGE IS WORKING..." : "THE CRATE IS SPINNING...";
            switch (s.kind)
            {
                case StationKind.WallWeapon:
                    var w = s.Weapon;
                    return Gunner.Holds(w.id) ? $"USE: {w.name} AMMO  {w.AmmoCost}" : $"USE: BUY {w.name}  {w.cost}";
                case StationKind.MysteryCrate: return _freeSpin ? "USE: MYSTERY CRATE  FREE!" : $"USE: MYSTERY CRATE  {ZombieRules.CrateCost}";
                case StationKind.Perk:
                    var p = s.Perk;
                    return Run.HasPerk(p) ? $"{ZombieRules.PerkName(p)} (OWNED)"
                        : !Run.PerkSlotFree ? $"{ZombieRules.PerkName(p)}  (PERK LIMIT {Run.PerkLimit})"
                        : $"USE: {ZombieRules.PerkName(p)}  {ZombieRules.PerkCost(p)}\n{ZombieRules.PerkInfo(p)}";
                case StationKind.Forge:
                    return Gunner.Current != null && Gunner.Current.spec.upgraded ? "THIS GUN IS ALREADY CURSED"
                        : $"USE: UPGRADE FORGE  {ForgeCost}";
                case StationKind.Door: return $"USE: OPEN THE GALLERY  {ZombieRules.DoorCost}";
                default: return "";
            }
        }

        void Use(ZombieStation s)
        {
            if (Time.time < s.BusyUntil) return;
            switch (s.kind)
            {
                case StationKind.WallWeapon:
                {
                    var w = s.Weapon;
                    if (Gunner.Holds(w.id))
                    {
                        if (Pay(w.AmmoCost)) { Gunner.RefillAmmo(w.id); Sfx.Play(Sfx.Reload, 0.7f, 0f); }
                    }
                    else if (Pay(w.cost)) { Gunner.Give(w); Banner?.Invoke(w.name); }
                    break;
                }
                case StationKind.MysteryCrate:
                    if (_freeSpin)
                    {
                        _freeSpin = false;
                        Banner?.Invoke("LUCKY FIRST SPIN!");
                        StartCoroutine(SpinCrate(s));
                    }
                    else if (Pay(ZombieRules.CrateCost)) StartCoroutine(SpinCrate(s));
                    break;
                case StationKind.Perk:
                {
                    var p = s.Perk;
                    if (Run.HasPerk(p)) return;
                    if (!Run.PerkSlotFree)
                    {
                        Sfx.Play(Sfx.Click, 0.6f, 0f);
                        Banner?.Invoke($"PERK LIMIT ({Run.PerkLimit})" + (Run.ExtraPerkSlot ? "" : "\nLIGHT EVERY LANTERN FOR ONE MORE"));
                        return;
                    }
                    if (!Run.CanAfford(ZombieRules.PerkCost(p))) { Deny(); return; }
                    Run.BuyPerk(p);
                    ApplyPerk(p);
                    Sfx.Play(Sfx.Purchase, 0.9f, 0f);
                    Banner?.Invoke(ZombieRules.PerkName(p) + "!\n" + ZombieRules.PerkInfo(p));
                    break;
                }
                case StationKind.Forge:
                    if (Gunner.Current == null || Gunner.Current.spec.upgraded) return;
                    if (Pay(ForgeCost)) StartCoroutine(Forge(s));
                    break;
                case StationKind.Door:
                    if (Pay(ZombieRules.DoorCost))
                    {
                        s.OpenDoor();
                        Sfx.Play(Sfx.Secret, 0.8f, 0f);
                        Banner?.Invoke("THE GALLERY IS OPEN!");
                    }
                    break;
            }
        }

        int ForgeCost
        {
            get
            {
                int xp = Services.Save != null && Services.Save.Data.zombieCareer != null ? Services.Save.Data.zombieCareer.xp : 0;
                return SurvivorRank.Has(xp, "kit_forge") ? Mathf.RoundToInt(ZombieRules.ForgeCost * 0.6f) : ZombieRules.ForgeCost;
            }
        }

        bool Pay(int cost)
        {
            if (!Run.Spend(cost)) { Deny(); return false; }
            Sfx.Play(Sfx.Purchase, 0.8f, 0f);
            return true;
        }

        void Deny()
        {
            Sfx.Play(Sfx.Click, 0.6f, 0f);
            Banner?.Invoke("NOT ENOUGH POINTS");
        }

        IEnumerator SpinCrate(ZombieStation s)
        {
            s.BusyUntil = Time.time + 2.4f;
            string holding = Gunner.Current != null ? Gunner.Current.spec.id : "";
            ZombieWeapon prize = ZombieArmory.RollCrate(Random.value, holding, Run.SpinsSinceWonder);
            Run.CountSpin(ZombieArmory.IsWonder(prize.id));
            float t = 0f;
            int i = 0;
            while (t < 2.2f)
            {
                // Flick through the armory, slowing down, then land on the prize.
                var show = ZombieArmory.Crate[i++ % ZombieArmory.Crate.Length];
                Banner?.Invoke("? " + show.name + " ?");
                Sfx.Play(Sfx.Click, 0.4f, 0.2f);
                float wait = Mathf.Lerp(0.08f, 0.35f, t / 2.2f);
                t += wait;
                yield return new WaitForSeconds(wait);
            }
            Gunner.Give(prize);
            Sfx.Play(Sfx.Relic, 0.9f, 0f);
            Banner?.Invoke(prize.name + "!");
            PuffEffect.Play(s.transform.position + Vector3.up, new Color32(200, 150, 255, 255), spriteMaterial, 0.3f, 5f, 0.5f);
        }

        IEnumerator Forge(ZombieStation s)
        {
            s.BusyUntil = Time.time + 2.5f;
            Banner?.Invoke("THE FORGE GLOWS...");
            for (int k = 0; k < 5; k++)
            {
                PuffEffect.Play(s.transform.position + Vector3.up * 0.8f, new Color32(170, 120, 255, 255), spriteMaterial, 0.22f, 3f, 0.4f);
                Sfx.Play(Sfx.Board, 0.5f, 0.2f);
                yield return new WaitForSeconds(0.45f);
            }
            if (Gunner.UpgradeCurrent())
            {
                Sfx.Play(Sfx.Special, 0.9f, 0f);
                Banner?.Invoke(Gunner.Current.spec.name + "!");
            }
        }

        void ApplyPerk(PerkKind p)
        {
            switch (p)
            {
                case PerkKind.IronHeart:
                    if (PlayerHealth != null)
                    {
                        PlayerHealth.bonusMaxHealth += ZombieRules.IronHeartBonusHealth;
                        PlayerHealth.ResetState(); // refill to the new maximum
                    }
                    break;
                case PerkKind.QuickDraw:
                    if (Gunner != null) Gunner.FireRateScale = ZombieRules.QuickDrawRate;
                    break;
                case PerkKind.FleetFeet:
                    if (_motor != null) _motor.SpeedScale = ZombieRules.FleetFeetSpeed;
                    break;
                case PerkKind.Deadeye:
                    if (Gunner != null) Gunner.DamageBonus = ZombieRules.DeadeyeBonus;
                    break;
            }
        }

        void LightLantern(ZombieStation s)
        {
            s.Light();
            LanternsLit++;
            Sfx.Play(Sfx.Secret, 0.9f, 0f);
            PuffEffect.Play(s.transform.position + Vector3.up * 0.5f, new Color32(255, 200, 80, 255), spriteMaterial, 0.3f, 4f, 0.6f);
            if (LanternsLit < LanternsTotal)
            {
                Banner?.Invoke($"A LANTERN FLICKERS ON... ({LanternsLit}/{LanternsTotal})");
                return;
            }
            // The secret: every lantern lit. A free cursed Moonbeam Ray, a pile of points and a fifth perk slot.
            // In Hardcore it also wakes the Lantern Wraith, who comes with the next round.
            Run.Earn(ZombieRules.LanternReward);
            Run.ExtraPerkSlot = true;
            if (Gunner != null) Gunner.Give(ZombieArmory.Upgrade(ZombieArmory.MoonbeamRay));
            Banner?.Invoke("ALL THE LANTERNS BURN!\nCURSED MOONBEAM RAY · 5TH PERK SLOT" +
                           (Mode == SurvivalMode.Hardcore ? "\nSOMETHING WAKES IN THE DARK..." : ""));
            if (Mode == SurvivalMode.Hardcore && !WraithSummoned) { WraithSummoned = true; _wraithPending = true; }
            var save = Services.Save;
            if (save != null && !save.Data.zombieLanternsLit)
            {
                save.Data.zombieLanternsLit = true;
                save.MarkDirty();
            }
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.2f, 0.5f);
        }

        // ---- the hero ---------------------------------------------------------------------------------------------

        void OnPlayerHit(HitResult r)
        {
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
        }

        void OnPlayerDied()
        {
            if (GameOver) return;
            if (Run.UseSecondWind())
            {
                StartCoroutine(SecondWind());
                return;
            }
            EndRun();
        }

        IEnumerator SecondWind()
        {
            Banner?.Invoke("SECOND WIND!");
            yield return new WaitForSeconds(0.6f);
            if (PlayerHealth != null) PlayerHealth.ResetState();
            if (player != null && player.TryGetComponent(out Invulnerability inv)) inv.Grant(3f, blink: true);
            // The blast that comes with it clears the zombies right next to the hero.
            Vector2 at = player != null ? (Vector2)player.transform.position : Vector2.zero;
            _nuking = true;
            foreach (var e in new List<EnemyAI>(_alive.Keys))
                if (e != null && ((Vector2)e.transform.position - at).sqrMagnitude < 9f && e.TryGetComponent(out Damageable d)) d.Kill();
            _nuking = false;
            PuffEffect.Play(at, new Color32(120, 255, 170, 255), spriteMaterial, 0.4f, 7f, 0.6f);
        }

        void EndRun()
        {
            GameOver = true;
            StopAllCoroutines();
            if (player != null)
            {
                if (!Run.Won) PuffEffect.Play(player.transform.position, new Color32(120, 200, 90, 255), spriteMaterial, 0.3f, 4f, 0.6f);
                if (player.TryGetComponent(out PlayerInputRouter router)) router.enabled = false;
            }
            var save = Services.Save;
            int survived = Run.Won ? Run.Round : Mathf.Max(0, Run.Round - 1);
            CachesEarned = SurvivalModes.Caches(survived);
            if (save != null)
            {
                var c = save.Data.zombieCareer ?? (save.Data.zombieCareer = new ZombieCareer());
                NewBest = c.RecordRound(MapId, Mode, Run.Round);
                if (Mode == SurvivalMode.Endless) NewBest |= save.Data.RecordSurvival(Run.Round, Run.TotalEarned, Run.Kills);
                c.totalKills += Run.Kills;
                if (Run.Won) c.raidWins++;
                if (Mode == SurvivalMode.Hardcore) c.hardcoreRuns++;
                ShardReward = SurvivalModes.Shards(Mode, Run.Round, Run.Kills, Run.Won);
                Economy.Grant(save.Data, ShardReward);
                save.SaveNow();
            }
            ProgressTracker.Report(ChallengeKind.SurvivalRound, Run.Round);
            ProgressTracker.AddXp(PlayerLevel.SurvivalRoundXp * Mathf.Max(0, Run.Round - 1));
            XpEarned = SurvivorRank.RunXp(Mode, Run.Round, Run.Kills, Run.Won);
            RanksReached = ProgressTracker.AddZombieXp(XpEarned);
            ZombieBoards.Submit(MapId, Mode, Run.Round);
            if (Services.Save != null) Services.Save.SaveNow();
            if (Services.State != null) Services.State.SetState(GameState.Results);
            Sfx.Play(Run.Won ? Sfx.Victory : Sfx.KO, 0.9f, 0f);
            RunEnded?.Invoke();
        }

        // ---- supply caches and the Lantern Wraith ---------------------------------------------------------------

        IEnumerator SupplyCache()
        {
            yield return new WaitForSeconds(1.2f);
            if (GameOver) yield break;
            Vector2 at = startPoint != null ? (Vector2)startPoint.position + Vector2.up * 0.6f : (player != null ? (Vector2)player.transform.position + Vector2.up : Vector2.zero);
            var kind = Run.Round % 2 == 0 ? PowerUpKind.AmmoPinata : ZombieRules.PickPowerUp(Random.value);
            SpawnDrop(kind, at);
            Banner?.Invoke($"SUPPLY CACHE!  ({Run.Round / SurvivalModes.CacheEvery})\n+{SurvivalModes.ShardsPerCache} SHARDS AT THE END");
        }

        void SpawnWraith()
        {
            var def = pumpkinKing != null ? pumpkinKing : brute;
            if (def == null) return;
            Vector2 pos = SpawnPoint(ZombieKind.PumpkinKing);
            int dir = player != null && player.transform.position.x < pos.x ? -1 : 1;
            var ai = EnemyFactory.Spawn(def, pos + Vector2.up * (def.bodySize.y * 0.5f + 0.05f), dir, spriteMaterial);
            var health = ai.GetComponent<Damageable>();
            int hp = Mathf.RoundToInt((ZombieRules.Health(ZombieKind.PumpkinKing, Run.Round) * 2.5f + 60) * SurvivalModes.HealthScale(Mode));
            health.Configure(DamageModel.StoryHealth, TeamIds.Enemy, hp, def.weight, 0f, 0f);
            ai.SpeedOverride = 0.55f;
            ai.ChaseRangeOverride = 200f;
            ai.transform.localScale = ai.transform.localScale * 1.3f;
            foreach (var sr in ai.GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(0.65f, 0.85f, 1f, 0.9f);
            ai.Defeated -= OnZombieDefeated;
            ai.Defeated += OnZombieDefeated;
            System.Action<HitResult> onHit = r => OnZombieHit(ai);
            health.Hit += onHit;
            _hitHandlers[health] = onHit;
            _alive[ai] = ZombieKind.PumpkinKing;
            _wraith = ai;
            Banner?.Invoke("THE LANTERN WRAITH!");
            if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.35f, 0.7f);
            Sfx.Play(Sfx.Spotted, 1f, 0f);
        }

        void WraithDefeated()
        {
            WraithBeaten = true;
            Run.Earn(2000);
            Banner?.Invoke($"THE LANTERN WRAITH IS GONE!\n+{SurvivalModes.HiddenBossShards} STAR SHARDS");
            var save = Services.Save;
            if (save != null)
            {
                var c = save.Data.zombieCareer ?? (save.Data.zombieCareer = new ZombieCareer());
                if (!c.Has("lantern_wraith")) c.secrets.Add("lantern_wraith");
                Economy.Grant(save.Data, SurvivalModes.HiddenBossShards);
                save.SaveNow();
            }
            ProgressTracker.AddZombieXp(SurvivorRank.HiddenBossXp);
            Sfx.Play(Sfx.Victory, 0.9f, 0f);
        }

        /// <summary>Test hook: jump to a round (the next one starts after a short wait).</summary>
        public void DebugSkipToRound(int round)
        {
            StopAllCoroutines();
            foreach (var e in new List<EnemyAI>(_alive.Keys)) if (e != null) RemoveZombie(e);
            _alive.Clear();
            _toSpawn = 0;
            _bossPending = false;
            while (Run.Round < round - 1) Run.NextRound();
            Intermission = true;
            StartCoroutine(NextRound(0.1f));
        }

        /// <summary>Test hook: every zombie on the field and still to come is gone (the round ends).</summary>
        public void DebugClearRound()
        {
            _toSpawn = 0;
            _bossPending = false;
            foreach (var e in new List<EnemyAI>(_alive.Keys))
                if (e != null && e.TryGetComponent(out Damageable d)) d.Kill();
        }

        public bool NewBest { get; private set; }
        public int ShardReward { get; private set; }

        /// <summary>Star Shards for an Endless run: 5 a round survived, 1 for every 4 kills, plus the supply caches.</summary>
        public static int ShardsFor(int round, int kills) => SurvivalModes.Shards(SurvivalMode.Endless, round, kills, false);
    }
}
