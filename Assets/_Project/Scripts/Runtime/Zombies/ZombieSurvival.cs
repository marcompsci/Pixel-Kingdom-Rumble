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

        public SurvivalRun Run { get; } = new SurvivalRun();
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
            _bossPending = ZombieRules.IsBossRound(round) && pumpkinKing != null;
            _spawnTimer = 1f;
            Intermission = false;
            Sfx.Play(Sfx.RoundStart, 0.9f, 0f);
            Banner?.Invoke(_bossPending ? $"ROUND {round}\nTHE PUMPKIN KING RISES!" : $"ROUND {round}");
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
                Banner?.Invoke($"ROUND {Run.Round} SURVIVED!  +{bonus}");
                StartCoroutine(NextRound(ZombieRules.Intermission));
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
            _spawnTimer = ZombieRules.SpawnInterval(Run.Round);
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
            health.Configure(DamageModel.StoryHealth, TeamIds.Enemy, ZombieRules.Health(kind, Run.Round), def.weight, 0f, 0f);
            if (kind != ZombieKind.Bat) ai.SpeedOverride = ZombieRules.Speed(kind, Run.Round);
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
            Vector2 pos = ai.transform.position;
            RemoveZombie(ai);
            PuffEffect.Play(pos, new Color32(120, 200, 90, 255), spriteMaterial, 0.24f, 4f, 0.45f);
            if (kind == ZombieKind.PumpkinKing) Banner?.Invoke("THE PUMPKIN KING FALLS!");
            if (!_nuking && ZombieRules.Drops(kind, Run.DropsThisRound, Random.value))
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
                case StationKind.MysteryCrate: return $"USE: MYSTERY CRATE  {ZombieRules.CrateCost}";
                case StationKind.Perk:
                    var p = s.Perk;
                    return Run.HasPerk(p) ? $"{ZombieRules.PerkName(p)} (OWNED)"
                        : $"USE: {ZombieRules.PerkName(p)}  {ZombieRules.PerkCost(p)}\n{ZombieRules.PerkInfo(p)}";
                case StationKind.Forge:
                    return Gunner.Current != null && Gunner.Current.spec.upgraded ? "THIS GUN IS ALREADY CURSED"
                        : $"USE: UPGRADE FORGE  {ZombieRules.ForgeCost}";
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
                    if (Pay(ZombieRules.CrateCost)) StartCoroutine(SpinCrate(s));
                    break;
                case StationKind.Perk:
                {
                    var p = s.Perk;
                    if (Run.HasPerk(p)) return;
                    if (!Run.CanAfford(ZombieRules.PerkCost(p))) { Deny(); return; }
                    Run.BuyPerk(p);
                    ApplyPerk(p);
                    Sfx.Play(Sfx.Purchase, 0.9f, 0f);
                    Banner?.Invoke(ZombieRules.PerkName(p) + "!\n" + ZombieRules.PerkInfo(p));
                    break;
                }
                case StationKind.Forge:
                    if (Gunner.Current == null || Gunner.Current.spec.upgraded) return;
                    if (Pay(ZombieRules.ForgeCost)) StartCoroutine(Forge(s));
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
            ZombieWeapon prize = ZombieArmory.RollCrate(Random.value, holding);
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
            // The secret: every lantern lit. A free cursed Moonbeam Ray and a pile of points.
            Run.Earn(ZombieRules.LanternReward);
            if (Gunner != null) Gunner.Give(ZombieArmory.Upgrade(ZombieArmory.MoonbeamRay));
            Banner?.Invoke("ALL THE LANTERNS BURN!\nCURSED MOONBEAM RAY!");
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
                PuffEffect.Play(player.transform.position, new Color32(120, 200, 90, 255), spriteMaterial, 0.3f, 4f, 0.6f);
                if (player.TryGetComponent(out PlayerInputRouter router)) router.enabled = false;
            }
            var save = Services.Save;
            if (save != null)
            {
                NewBest = save.Data.RecordSurvival(Run.Round, Run.TotalEarned, Run.Kills);
                ShardReward = ShardsFor(Run.Round, Run.Kills);
                Economy.Grant(save.Data, ShardReward);
                save.SaveNow();
            }
            if (Services.State != null) Services.State.SetState(GameState.Results);
            Sfx.Play(Sfx.KO, 0.9f, 0f);
            RunEnded?.Invoke();
        }

        public bool NewBest { get; private set; }
        public int ShardReward { get; private set; }

        /// <summary>Star Shards for a run: 5 a round survived plus 1 for every 4 kills.</summary>
        public static int ShardsFor(int round, int kills) => Mathf.Max(0, round - 1) * 5 + kills / 4;
    }
}
