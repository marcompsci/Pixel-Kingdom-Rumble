using System;
using System.Collections;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// ZOMBIE LAND: the open-city campaign (top-down). Builds New Hollow, the hero and the vehicles, runs the dead's
    /// ecosystem (roaming population per district, rot nests, screamers calling hordes, noise drawing the dead, blood
    /// hounds after five minutes out in the open), the fourteen missions, safehouse checkpoints, loot, the relic cases,
    /// the six lairs and the First Turned. Progress lives in SaveData.zombieLand and survives death.
    /// </summary>
    public enum ZLMode { Campaign, LastStand }

    [DefaultExecutionOrder(-30)]
    public class ZLGame : MonoBehaviour
    {
        public static ZLGame Current { get; private set; }

        [Header("Set by the scene builder")]
        public Material material;
        public CharacterRoster roster;
        public EnemyDefinition shambler, runner, bloater, spitter, brute;
        public Sprite[] survivorFrames = new Sprite[0];
        [Tooltip("Campaign = Zombie Land (city.json). LastStand = round-based survival on block.json.")]
        public ZLMode mode = ZLMode.Campaign;

        public bool IsLastStand => mode == ZLMode.LastStand;
        /// <summary>Online Last Stand: the link to the other players (null when playing alone or with AI).</summary>
        public ZLNetLink Net { get; set; }
        /// <summary>This phone is a guest in someone else's online game: the host runs the zombies.</summary>
        public bool IsGuest => Net != null && !Net.IsHost;
        /// <summary>Every hero on the field (Zombie Land: just you; Last Stand: the squad).</summary>
        public readonly List<ZLPlayer> Squad = new List<ZLPlayer>();
        /// <summary>The hero landing a melee hit right now (for points).</summary>
        public ZLPlayer MeleeAttacker { get; private set; }
        public ZLLastStand Stand { get; private set; }
        /// <summary>Power-ups running: insta-kill and double scrap/points (seconds left).</summary>
        public float InstaKillLeft { get; private set; }
        public float DoubleScrapLeft { get; private set; }
        public bool DoubleScrap => DoubleScrapLeft > 0f;
        /// <summary>The Survivor Rank XP (read once at start, for field kits).</summary>
        public int CareerXp { get; private set; }
        public IReadOnlyList<ZLLoot> Loot => _loot;
        readonly List<(GameObject go, ZLPowerUp kind, float until)> _powerUps = new List<(GameObject, ZLPowerUp, float)>();
        readonly Dictionary<GameObject, int> _powerUpIds = new Dictionary<GameObject, int>();
        int _pendingXp;
        bool _nuking;

        public ZLWorld World { get; private set; }
        public ZLCityData City => World != null ? World.City : null;
        public ZLFlowField Flow { get; private set; }
        public ZLPlayer Player { get; private set; }
        public ZombieLandProgress Progress { get; private set; }
        public ZLPuzzle Puzzle { get; private set; }
        public ZLMissionRun Active { get; private set; }
        public ZLBoss Boss { get; private set; }
        public float Roamed { get; private set; }
        public bool InSafehouse { get; private set; }
        public bool RespawnPending { get; private set; }
        public string Toast { get; private set; }
        public float ToastLeft { get; private set; }
        public bool Ready { get; private set; }

        public bool Paused
        {
            get
            {
                if (CutscenePlayer.IsPlaying || RespawnPending || ZLMapPanel.IsOpen || ZLShopPanel.IsOpen) return true;
                var s = Services.State;
                return s != null && s.State == GameState.Paused;
            }
        }

        /// <summary>The flow field the dead follow: toward every standing hero.</summary>
        void BuildFlow()
        {
            if (Squad.Count <= 1) { Flow.Build(TargetPos); return; }
            _targets.Clear();
            foreach (var h in Squad) if (h != null && h.Standing) _targets.Add(h.transform.position);
            if (_targets.Count == 0) _targets.Add(TargetPos);
            Flow.Build(_targets);
        }
        readonly List<Vector2> _targets = new List<Vector2>();

        /// <summary>The standing hero closest to a point (where a zombie goes), else the local hero.</summary>
        public ZLPlayer NearestHero(Vector2 at)
        {
            if (Squad.Count <= 1) return Player;
            ZLPlayer best = null;
            float bd = float.MaxValue;
            foreach (var h in Squad)
            {
                if (h == null || !h.Standing) continue;
                float d = ((Vector2)h.transform.position - at).sqrMagnitude;
                if (d < bd) { bd = d; best = h; }
            }
            return best != null ? best : Player;
        }

        /// <summary>Where a zombie at this point should head.</summary>
        public Vector2 TargetFor(Vector2 at)
        {
            if (Squad.Count <= 1) return TargetPos;
            var h = NearestHero(at);
            return h != null ? (Vector2)h.transform.position : TargetPos;
        }

        public Vector2 TargetPos => Player == null ? Vector2.zero
            : Player.Driving != null ? (Vector2)Player.Driving.transform.position : (Vector2)Player.transform.position;
        public bool PlayerDriving => Player != null && Player.Driving != null;
        public bool PlayerHidden => InSafehouse;

        readonly List<ZLZombie> _zombies = new List<ZLZombie>();
        readonly List<ZLNest> _nests = new List<ZLNest>();
        readonly Dictionary<ZLNest, int> _nestSpawns = new Dictionary<ZLNest, int>();
        readonly Dictionary<ZLZombie, ZLNest> _fromNest = new Dictionary<ZLZombie, ZLNest>();
        readonly List<ZLVehicle> _vehicles = new List<ZLVehicle>();
        readonly List<IZLUse> _uses = new List<IZLUse>();
        readonly List<ZLLoot> _loot = new List<ZLLoot>();
        readonly List<ZLSafehouse> _safehouses = new List<ZLSafehouse>();
        readonly List<ZLMissionMarker> _markers = new List<ZLMissionMarker>();
        readonly Dictionary<string, Sprite> _shots = new Dictionary<string, Sprite>();
        public IReadOnlyList<ZLZombie> Zombies => _zombies;
        public IReadOnlyList<ZLSafehouse> Safehouses => _safehouses;
        public IReadOnlyList<ZLMissionMarker> Markers => _markers;
        public IReadOnlyList<ZLVehicle> Vehicles => _vehicles;

        float _flowT, _spawnT, _houndT, _saveT, _discoverT, _groanT;
        int _houndPacks;
        bool _warned;

        // ---- setup -------------------------------------------------------------------------------------

        void Awake()
        {
            Current = this;
            ZLBullet.ClearPool();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (Progress != null && !IsLastStand) Save(true);
            FlushXp();
        }

        /// <summary>Survivor Rank XP earned in Zombie Land is banked a little at a time (kills add up).</summary>
        public void AddXp(int xp) => _pendingXp += xp;

        void FlushXp()
        {
            if (_pendingXp <= 0) return;
            int xp = _pendingXp;
            _pendingXp = 0;
            ProgressTracker.AddZombieXp(xp);
        }

        void Start()
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            CareerXp = save != null && save.zombieCareer != null ? save.zombieCareer.xp : 0;
            if (IsLastStand)
            {
                // A fresh, unsaved loadout for the run (Zombie Land's own progress is never touched).
                Progress = new ZombieLandProgress();
                ZLRules.NewGame(Progress, 1);
                var pistol = Progress.Gun(ZLCatalog.Pistol.id);
                pistol.reserve = LastStandRules.PistolRounds - pistol.mag;
                var starter = LastStandRules.StarterGun(SurvivorRank.StarterTier(CareerXp));
                if (starter != null && starter.id != ZLCatalog.Pistol.id)
                {
                    ZLRules.GiveGun(Progress, starter, starter.magazine * 4);
                    Progress.equipped = starter.id;
                }
                Progress.medkits = 0;
                Puzzle = ZLPuzzle.FromSeed(1);
                World = gameObject.AddComponent<ZLWorld>();
                World.material = material;
                // Screenshot / trailer runs: EV_LastStand@pierplay, @demo (an AI squad plays itself), @pierdemo
                string sp = BootLoader.ShotPanel ?? "";
                if (sp.StartsWith("pier")) { GameSession.Mode = SessionMode.Zombies; GameSession.ZombieMap = LastStandMap.SaltPier.Id; }
                if (sp.StartsWith("rail")) { GameSession.Mode = SessionMode.Zombies; GameSession.ZombieMap = LastStandMap.RailYard.Id; }
                if (sp.EndsWith("demo")) { GameSession.Mode = SessionMode.Zombies; GameSession.ZombieSquad = SquadKind.AISquad; }
                LsMap = LastStandMap.Get(GameSession.Mode == SessionMode.Zombies ? GameSession.ZombieMap : LastStandRules.MapId);
                if (!World.Build(LsMap.File)) return;
                Flow = new ZLFlowField(City, City.w + City.h);
                SpawnHero();
                Stand = gameObject.AddComponent<ZLLastStand>();
                Stand.Init(this);
                if (Services.State != null) Services.State.SetState(GameState.Playing);
                Ready = true;
                Coach = gameObject.AddComponent<ZLCoach>();
                Coach.Init(this);
                if (BootLoader.ShotPanel != null) Coach.enabled = false;
                Flow.Build(TargetPos);
                string lsShot = BootLoader.TakeShotPanel("play") ?? BootLoader.TakeShotPanel("box") ?? BootLoader.TakeShotPanel("over")
                                ?? BootLoader.TakeShotPanel("pierplay") ?? BootLoader.TakeShotPanel("demo") ?? BootLoader.TakeShotPanel("pierdemo")
                                ?? BootLoader.TakeShotPanel("raildemo");
                if (lsShot != null) StartCoroutine(Stand.ShotSetup(lsShot));
                return;
            }
            Progress = save != null ? save.zombieLand : new ZombieLandProgress();
            Progress.Sanitize();
            if (Progress.seed == 0) ZLRules.NewGame(Progress, Mathf.Abs(Environment.TickCount) % 100000 + 1);
            Puzzle = ZLPuzzle.FromSeed(Progress.seed);

            // Screenshot run EV_ZombieLand@labs: start in the chapter 3 sublevel.
            if (BootLoader.ShotPanel == "labs")
            {
                Progress.introSeen = Progress.bossDefeated = Progress.chapter2Begun = Progress.chapter2Done = true;
                ZLRules.BeginChapter3(Progress);
                Progress.zone = ZLCatalog.LabsMap;
                Progress.arriveAt = "";
            }
            else if (BootLoader.ShotPanel != null) { Progress.zone = ""; Progress.arriveAt = ""; }   // other shots are in the city
            // Chapter 3: the sublevel is its own map, reached by the freight lift.
            if (Progress.InLabs && !Progress.chapter3Begun) Progress.zone = "";
            World = gameObject.AddComponent<ZLWorld>();
            World.material = material;
            if (!World.Build(Progress.InLabs ? ZLCatalog.LabsMap : "city")) return;
            Flow = new ZLFlowField(City);
            // each map has its own safehouses: a checkpoint from the other map falls back to this one's first
            if (!City.safehouses.Exists(x => x.id == Progress.checkpoint))
                Progress.checkpoint = Progress.InLabs && City.safehouses.Count > 0 ? City.safehouses[0].id : Progress.InLabs ? Progress.checkpoint : "";

            SpawnHero();
            Progress.arriveAt = "";
            SpawnLift();
            SpawnVehicles();
            SpawnSafehouses();
            SpawnLoot();
            SpawnRelics();
            SpawnLairs();
            SpawnNests();
            SpawnMarkers();
            SetupIsleBridge();
            if (Services.State != null) Services.State.SetState(GameState.Playing);
            Ready = true;
            Coach = gameObject.AddComponent<ZLCoach>();
            Coach.Init(this);
            if (BootLoader.ShotPanel != null) Coach.enabled = false;   // no first-time tips over App Store shots
            Flow.Build(TargetPos);
            // Screenshot runs: EV_ZombieLand@play / @drive / @map / @boss skip the intro and set up a view.
            string shot = BootLoader.TakeShotPanel("play") ?? BootLoader.TakeShotPanel("drive") ?? BootLoader.TakeShotPanel("map")
                          ?? BootLoader.TakeShotPanel("boss") ?? BootLoader.TakeShotPanel("shop") ?? BootLoader.TakeShotPanel("isle")
                          ?? BootLoader.TakeShotPanel("labs");
            if (shot != null)
            {
                Progress.introSeen = true;
                StartCoroutine(ShotSetup(shot));
            }
            if (!Progress.introSeen)
            {
                Progress.introSeen = true;
                Story("", ZLCatalog.Intro, "zl_city", null);
            }
            else if (Progress.bossDefeated && !Progress.chapter2Begun && shot == null) StartChapter2();
            else if (Progress.chapter2Done && !Progress.chapter3Begun && shot == null) StartChapter3();
            else if (Progress.InLabs) ShowToast(Progress.InChapter3 ? $"{ZLCatalog.Chapter3Name}  ·  KEYCARDS {Progress.Keycards}/{ZLCatalog.KeycardsNeeded}."
                                                : "THE SUBLEVEL IS QUIET. THE LIFT GOES BACK UP.");
            else ShowToast(Progress.InChapter3 ? $"{ZLCatalog.Chapter3Name}: TAKE THE FREIGHT LIFT UNDER THE GRAYSTONE LAB."
                         : Progress.InChapter2 ? $"{ZLCatalog.Chapter2Name}  ·  SIGILS {Progress.Sigils}/{ZLCatalog.SigilsNeeded}. CROSS THE CAUSEWAY."
                         : Progress.bossDefeated ? "ZOMBIE LAND IS CLEAR. ROAM FREE." : $"SHARDS {Progress.Shards}/{ZLCatalog.ShardsNeeded}. KEEP GOING.");
        }

        // ---- chapter 2: Blackwater Isle --------------------------------------------------------------

        GameObject _isleGate;
        readonly HashSet<string> _nestIds = new HashSet<string>();

        /// <summary>The army's quarantine line on the causeway: sealed (barricades) until the First Turned falls.</summary>
        void SetupIsleBridge()
        {
            var d = City.doors.Find(x => x.id == ZLCatalog.IsleBridge);
            if (d == null) return;
            if (Progress.bossDefeated) { City.Open(d); return; }
            _isleGate = new GameObject("QuarantineLine");
            _isleGate.transform.position = new Vector2(d.x + d.w * 0.5f, d.y + d.h * 0.5f);
            for (int y = d.y; y < d.y + d.h; y++)
            {
                var t = new GameObject("Barricade");
                t.transform.SetParent(_isleGate.transform, false);
                t.transform.position = new Vector2(d.x + d.w * 0.5f, y + 0.5f);
                t.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                var sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite("Props/barricade");
                sr.sortingOrder = ZLSort.ByY(y + 0.5f);
                if (material != null) sr.sharedMaterial = material;
            }
        }

        /// <summary>Chapter 2 begins: Dr. Hale's call, the causeway opens, the first Blackwater lead.</summary>
        public void StartChapter2()
        {
            if (!ZLRules.BeginChapter2(Progress)) return;
            var d = City.doors.Find(x => x.id == ZLCatalog.IsleBridge);
            if (d != null) City.Open(d);
            if (_isleGate != null) { Destroy(_isleGate); _isleGate = null; }
            SpawnNests();
            Save(true);
            Story("DR. HALE", ZLCatalog.Chapter2Intro, "zl_city", () => ShowToast(ZLCatalog.Chapter2Name + ": CROSS THE CAUSEWAY EAST OF THE HARBOR."));
        }

        /// <summary>The tide gate under Blackwater Penitentiary: Mother Brine, once you carry every sigil.</summary>
        public void TryTideGate(Vector2 door)
        {
            if (Boss != null || !Progress.InChapter2) return;
            if (!ZLRules.TideGateOpen(Progress))
            {
                ShowToast($"THE TIDE GATE: SEALED. SEA-GLASS SIGILS {Progress.Sigils}/{ZLCatalog.SigilsNeeded}.");
                Sfx.Play(Sfx.Click, 0.7f, 0f);
                return;
            }
            Story(ZLCatalog.Chapter2BossName, ZLCatalog.Chapter2BossIntro, "zl_boss", () =>
            {
                Vector2 at = door + Vector2.down * 4f;
                if (!City.Walkable(at)) at = door + Vector2.right * 4f;
                if (!City.Walkable(at)) at = door;
                var go = new GameObject("MotherBrine");
                go.transform.position = at;
                Boss = go.AddComponent<ZLBoss>();
                Boss.Init(this, Frames("boss_idle", 2), Frames("boss_walk", 4), Frames("boss_attack", 2));
                Boss.MakeBrine();
                ShowToast(ZLCatalog.Chapter2BossName + "!");
            });
        }

        void Chapter2BossDefeated(ZLBoss boss)
        {
            Progress.chapter2Done = true;
            foreach (var z in _zombies.ToArray()) if (z != null) z.Vanish(true);
            _zombies.Clear();
            foreach (var n in _nests.ToArray()) if (n != null) Destroy(n.gameObject);
            _nests.Clear();
            Destroy(boss.gameObject, 0.1f);
            Boss = null;
            if (Services.Save != null) Services.Save.Data.starShards += ZLRules.Chapter2Reward;
            ProgressTracker.AddXp(600);
            AddXp(SurvivorRank.ZLBossXp);
            FlushXp();
            var won = ZombieDailyTracker.BossKilled();
            won.AddRange(ZombieDailyTracker.AfterProgress(_bossBefore));
            TellDaily(won);
            Sfx.Play(Sfx.Victory, 1f);
            Save(true);
            Story("", ZLCatalog.Chapter2Ending, "dawn", () =>
            {
                ShowToast($"+{ZLRules.Chapter2Reward} STAR SHARDS. BLACKWATER IS CLEAR.");
                StartChapter3();
            });
        }

        bool OnIsle => City != null && City.DistrictAt(TargetPos) == "isle";

        // ---- chapter 3: the Sublevel ---------------------------------------------------------------------

        void SpawnLift()
        {
            if (IsLastStand) return;
            bool city = !Progress.InLabs;
            string key = city ? ZLCatalog.LabLift : "lift";
            if (!City.sites.Exists(x => x.key == key)) return;
            var go = new GameObject("FreightLift");
            go.transform.position = City.SiteOf(key);
            var lift = go.AddComponent<ZLLift>();
            lift.Init(this, city);
            _uses.Add(lift);
        }

        /// <summary>Chapter 3 begins once Blackwater is clear: Dr. Hale finds the lab's freight lift.</summary>
        public void StartChapter3()
        {
            if (!ZLRules.BeginChapter3(Progress)) return;
            Save(true);
            Story("DR. HALE", ZLCatalog.Chapter3Intro, "zl_city", () => ShowToast(ZLCatalog.Chapter3Name + ": TAKE THE FREIGHT LIFT UNDER THE GRAYSTONE LAB."));
        }

        /// <summary>The freight lift: save, swap the map, reload Zombie Land on the other side.</summary>
        public void RideLift(bool down)
        {
            if (!ZLRules.LiftOpen(Progress) || Boss != null) return;
            Progress.zone = down ? ZLCatalog.LabsMap : "";
            Progress.arriveAt = down ? "lift" : ZLCatalog.LabLift;
            if (down) Progress.checkpoint = "safe_labs";
            else if (Progress.checkpoint == "safe_labs") Progress.checkpoint = "";
            Save(true);
            Ready = false;   // nothing else runs (a safehouse touch would move the checkpoint back) while the scene reloads
            Sfx.Play(Sfx.Click, 0.9f, 0f);
            LiftRides++;
            if (Services.Scenes != null) Services.Scenes.ReloadCurrent();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        /// <summary>How many times the lift was ridden this scene (tests).</summary>
        public int LiftRides { get; private set; }

        /// <summary>The Curator's vault at the bottom of the sublevel: three keycards open it.</summary>
        public void TryVault(Vector2 door)
        {
            if (Boss != null || !Progress.InChapter3) return;
            if (!ZLRules.VaultOpen(Progress))
            {
                ShowToast($"THE VAULT: SEALED. SECURITY KEYCARDS {Progress.Keycards}/{ZLCatalog.KeycardsNeeded}.");
                Sfx.Play(Sfx.Click, 0.7f, 0f);
                return;
            }
            Story(ZLCatalog.Chapter3BossName, ZLCatalog.Chapter3BossIntro, "zl_boss", () =>
            {
                Vector2 at = door + Vector2.down * 4f;
                if (!City.Walkable(at)) at = door + Vector2.left * 4f;
                if (!City.Walkable(at)) at = door;
                var go = new GameObject("TheCurator");
                go.transform.position = at;
                Boss = go.AddComponent<ZLBoss>();
                Boss.Init(this, Frames("boss_idle", 2), Frames("boss_walk", 4), Frames("boss_attack", 2));
                Boss.MakeCurator();
                ShowToast(ZLCatalog.Chapter3BossName + "!");
            });
        }

        void Chapter3BossDefeated(ZLBoss boss)
        {
            Progress.chapter3Done = true;
            foreach (var z in _zombies.ToArray()) if (z != null) z.Vanish(true);
            _zombies.Clear();
            foreach (var n in _nests.ToArray()) if (n != null) Destroy(n.gameObject);
            _nests.Clear();
            Destroy(boss.gameObject, 0.1f);
            Boss = null;
            if (Services.Save != null) Services.Save.Data.starShards += ZLRules.Chapter3Reward;
            ZLRules.GiveGun(Progress, ZLCatalog.Singularity, ZLCatalog.Singularity.magazine * 3);
            ProgressTracker.AddXp(800);
            AddXp(SurvivorRank.ZLBossXp);
            FlushXp();
            var won = ZombieDailyTracker.BossKilled();
            won.AddRange(ZombieDailyTracker.AfterProgress(_bossBefore));
            TellDaily(won);
            Sfx.Play(Sfx.Victory, 1f);
            Save(true);
            Story("", ZLCatalog.Chapter3Ending, "dawn", () => ShowToast($"+{ZLRules.Chapter3Reward} STAR SHARDS AND THE SINGULARITY. THE SUBLEVEL IS SEALED."));
        }

        public Sprite Sprite(string path) => World != null ? World.Sprite(path) : null;

        /// <summary>The minimap image for this map.</summary>
        public Sprite Minimap => Sprite(IsLastStand ? "minimap_" + LsMap.File : Progress != null && Progress.InLabs ? "minimap_" + ZLCatalog.LabsMap : "minimap");
        /// <summary>Last Stand: the map being played (from GameSession.ZombieMap).</summary>
        public LastStandMap LsMap { get; private set; } = LastStandMap.HollowBlock;

        public Sprite ShotSprite(string kind)
        {
            if (string.IsNullOrEmpty(kind)) kind = "ball";
            if (_shots.TryGetValue(kind, out var s)) return s;
            s = Resources.Load<Sprite>("PKR/Shots/shot_" + kind);
            _shots[kind] = s;
            return s;
        }

        void SpawnHero()
        {
            var def = roster != null ? roster.Find(GameSession.SelectedCharacterId) : null;
            if (def == null && roster != null && roster.heroes.Count > 0) def = roster.heroes[0];
            var go = new GameObject("Hero");
            Vector2 at = RespawnPoint();
            go.transform.position = at;
            Player = go.AddComponent<ZLPlayer>();
            Player.Init(this, def, material, roster != null && def != null ? roster.TintFor(def.id) : Color.white);
            Player.Loadout = Progress;
            Squad.Add(Player);
            var sd = Services.Save != null ? Services.Save.Data : null;
            if (sd != null)
            {
                var tr = ZombieCosmetics.Worn(sd.zombieCareer, CosmeticSlot.Tracer, CosmeticStats.From(sd.zombieCareer, sd.zombieLand));
                if (tr != null) Player.Tracer = new Color32(tr.R, tr.G, tr.B, 255);
            }
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollowTopDown>() : null;
            if (cam != null) { cam.target = go.transform; cam.Snap(); }
        }

        Vector2 RespawnPoint()
        {
            if (IsLastStand) return new Vector2(City.start.x, City.start.y);
            if (!string.IsNullOrEmpty(Progress.arriveAt) && City.sites.Exists(x => x.key == Progress.arriveAt))
                return City.SiteOf(Progress.arriveAt) + Vector2.up * 1.5f;
            foreach (var s in City.safehouses)
                if (s.id == Progress.checkpoint) return new Vector2(s.x + 1f, s.y);
            return new Vector2(City.start.x, City.start.y);
        }

        static readonly string[] CarSprites = { "car_red", "car_blue", "car_white", "car_yellow" };

        void SpawnVehicles()
        {
            int i = 0;
            foreach (var v in City.vehicles)
            {
                i++;
                var kind = (ZLVehicleKind)Enum.Parse(typeof(ZLVehicleKind), v.k);
                SpawnVehicle(kind, new Vector2(v.x, v.y), v.a, i);
            }
            if (Progress.rewards.Contains("motorcycles"))
                foreach (var s in City.safehouses)
                    if (Progress.safehouses.Contains(s.id)) SpawnVehicle(ZLVehicleKind.Motorcycle, new Vector2(s.x - 1.5f, s.y), 90f, 3);
        }

        public ZLVehicle SpawnVehicle(ZLVehicleKind kind, Vector2 pos, float angle, int variant, Color? tint = null)
        {
            string sprite;
            switch (kind)
            {
                case ZLVehicleKind.Truck: sprite = variant % 2 == 0 ? "truck_green" : "truck_gray"; break;
                case ZLVehicleKind.Bike: sprite = "bike"; break;
                case ZLVehicleKind.Motorcycle: sprite = variant % 2 == 0 ? "moto_black" : "moto_orange"; break;
                default: sprite = CarSprites[variant % CarSprites.Length]; break;
            }
            var go = new GameObject("Vehicle_" + kind);
            go.transform.position = pos;
            var veh = go.AddComponent<ZLVehicle>();
            veh.Init(this, kind, Sprite("Props/" + sprite), Sprite("Props/wreck_burnt"), angle);
            if (tint.HasValue) go.GetComponent<SpriteRenderer>().color = tint.Value;
            _vehicles.Add(veh);
            _uses.Add(veh);
            return veh;
        }

        void SpawnSafehouses()
        {
            foreach (var s in City.safehouses)
            {
                var go = new GameObject("Safehouse_" + s.id);
                go.transform.position = new Vector2(s.x, s.y);
                var sh = go.AddComponent<ZLSafehouse>();
                sh.Init(this, s);
                _safehouses.Add(sh);
                _uses.Add(sh);
            }
        }

        void SpawnLoot()
        {
            foreach (var p in City.pickups)
            {
                bool weapon = ZLCatalog.Gun(p.k) != null;
                if (weapon && Progress.taken.Contains(p.id)) continue;
                var go = new GameObject("Loot_" + p.k);
                go.transform.position = new Vector2(p.x, p.y);
                var l = go.AddComponent<ZLLoot>();
                l.Init(this, p.id, p.k, Sprite("Props/pk_" + (weapon ? "weapon" : p.k)));
                _loot.Add(l);
            }
        }

        void SpawnRelics()
        {
            foreach (var r in City.relics)
            {
                var relic = (ZLRelic)Enum.Parse(typeof(ZLRelic), r.relic);
                var go = new GameObject("Relic_" + r.relic);
                go.transform.position = new Vector2(r.x, r.y);
                var c = go.AddComponent<ZLRelicCase>();
                c.Init(this, relic);
                _uses.Add(c);
            }
        }

        void SpawnLairs()
        {
            foreach (var l in City.lairs)
            {
                var go = new GameObject("Lair_" + l.id);
                go.transform.position = new Vector2(l.x, l.y);
                var d = go.AddComponent<ZLLairDoor>();
                d.Init(this, l.id);
                _uses.Add(d);
            }
        }

        void SpawnNests()
        {
            if (Progress.chapter3Done) return;
            var frames = new[] { Sprite("Props/nest_0"), Sprite("Props/nest_1") };
            foreach (var n in City.nests)
            {
                if (Progress.nestsCleared.Contains(n.id)) continue;
                // the mainland's nests die with the First Turned; Blackwater's wake with chapter 2
                bool awake = n.district == ZLCatalog.LabsMap ? Progress.InChapter3
                           : n.district == "isle" ? Progress.bossDefeated && !Progress.chapter2Done
                           : !Progress.bossDefeated;
                if (!awake) continue;
                if (!_nestIds.Add(n.id)) continue;
                var go = new GameObject("Nest_" + n.id);
                go.transform.position = new Vector2(n.x, n.y);
                var nest = go.AddComponent<ZLNest>();
                int threat = ZLCatalog.District(n.district).threat;
                nest.Init(this, n.id, n.district, 160 + threat * 40, frames);
                _nests.Add(nest);
            }
        }

        public ZLNest SpawnMissionNest(Vector2 pos, string missionId, int threat)
        {
            var go = new GameObject("MissionNest");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 1.5f;
            var nest = go.AddComponent<ZLNest>();
            nest.Init(this, missionId, City.DistrictAt(pos), 320 + threat * 60, new[] { Sprite("Props/nest_0"), Sprite("Props/nest_1") }, missionId);
            _nests.Add(nest);
            return nest;
        }

        void SpawnMarkers()
        {
            foreach (var m in City.missions)
            {
                var spec = ZLCatalog.Mission(m.id);
                if (spec == null) continue;
                var go = new GameObject("Mission_" + m.id);
                go.transform.position = new Vector2(m.x, m.y);
                var mk = go.AddComponent<ZLMissionMarker>();
                mk.Init(this, spec, m);
                _markers.Add(mk);
                _uses.Add(mk);
            }
        }

        // ---- main loop -----------------------------------------------------------------------------------

        void Update()
        {
            if (!Ready || Player == null) return;
            float dt = Time.deltaTime;
            if (ToastLeft > 0f) ToastLeft -= Time.unscaledDeltaTime;
            if (CoachLeft > 0f) CoachLeft -= Time.unscaledDeltaTime;
            if (Paused) return;
            Progress.playSeconds += dt;

            _flowT -= dt;
            if (_flowT <= 0f) { _flowT = 0.35f; BuildFlow(); }
            if (InstaKillLeft > 0f) InstaKillLeft -= dt;
            if (DoubleScrapLeft > 0f) DoubleScrapLeft -= dt;
            UpdatePowerUps();
            if (IsLastStand)
            {
                Stand.Tick(dt);
                CollectLoot();
                return;
            }

            InSafehouse = false;
            foreach (var s in _safehouses)
                if (Vector2.Distance(s.transform.position, TargetPos) < ZLSafehouse.SafeRadius) { InSafehouse = true; s.Touch(); }

            _discoverT -= dt;
            if (_discoverT <= 0f) { _discoverT = 0.5f; Discover(); }

            if (!Progress.bossDefeated || Progress.InChapter2 && OnIsle || Progress.InChapter3 && Progress.InLabs)
            {
                _groanT -= dt;
                if (_groanT <= 0f)
                {
                    _groanT = UnityEngine.Random.Range(3f, 7f);
                    foreach (var z in _zombies)
                        if (z != null && Vector2.Distance(z.transform.position, TargetPos) < 9f) { Sfx.Play(Sfx.Groan, 0.35f, 0.15f); break; }
                }
                Ecosystem(dt);
                Hounds(dt);
            }
            if (Active != null) Active.Update(dt);
            CollectLoot();

            _saveT -= dt;
            if (_saveT <= 0f) { _saveT = 20f; Save(false); FlushXp(); }
        }

        void Discover()
        {
            foreach (var m in _markers)
            {
                if (m.Done || Progress.leads.Contains(m.Spec.id)) continue;
                if (Vector2.Distance(m.transform.position, TargetPos) < 14f)
                {
                    Progress.leads.Add(m.Spec.id);
                    ShowToast("NEW LEAD: " + m.Spec.name);
                    Sfx.Play(Sfx.Secret, 0.7f);
                }
            }
        }

        // ---- the ecosystem -------------------------------------------------------------------------------

        public int ThreatAt(Vector2 p) => ZLCatalog.District(City.DistrictAt(p)).threat;

        void Ecosystem(float dt)
        {
            _spawnT -= dt;
            if (_spawnT > 0f) return;
            _spawnT = 0.7f;
            Vector2 at = TargetPos;
            string district = City.DistrictAt(at);
            int threat = ZLCatalog.District(district).threat;
            int cleared = 0;
            foreach (var n in City.nests) if (n.district == district && Progress.nestsCleared.Contains(n.id)) cleared++;
            bool lights = district == "industrial" && Progress.Done("m_power");
            int want = Boss != null ? 0 : ZLRules.Population(threat, cleared, lights);

            int near = 0;
            for (int i = _zombies.Count - 1; i >= 0; i--)
            {
                var z = _zombies[i];
                if (z == null) { _zombies.RemoveAt(i); continue; }
                float d = Vector2.Distance(z.transform.position, at);
                if (!z.pinned && !z.hunting && d > 34f) { _zombies.RemoveAt(i); z.Vanish(false); continue; }
                if (!z.pinned && z.kind != ZLFoe.Hound && d < 30f) near++;
            }
            if (near < want && SpawnSpot(at, 15f, 22f, out var spot))
                SpawnZombie(ZLRules.Pick(threat, UnityEngine.Random.value), spot, threat, false);
        }

        /// <summary>A walkable spot out of sight of the camera, between rMin and rMax tiles away, not by a safehouse.</summary>
        public bool SpawnSpot(Vector2 around, float rMin, float rMax, out Vector2 spot)
        {
            var cam = Camera.main;
            float hh = cam != null ? cam.orthographicSize + 1.5f : 8f;
            float hw = cam != null ? hh * cam.aspect : 14f;
            Vector2 cp = cam != null ? (Vector2)cam.transform.position : around;
            for (int tries = 0; tries < 16; tries++)
            {
                float a = UnityEngine.Random.value * Mathf.PI * 2f;
                float r = UnityEngine.Random.Range(rMin, rMax);
                var p = around + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                p = new Vector2(Mathf.Floor(p.x) + 0.5f, Mathf.Floor(p.y) + 0.5f);
                if (!City.Walkable(p) || City.Tile(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y)) == 'b') continue;
                if (Mathf.Abs(p.x - cp.x) < hw && Mathf.Abs(p.y - cp.y) < hh) continue;
                bool safe = false;
                foreach (var s in _safehouses) if (Vector2.Distance(s.transform.position, p) < 9f) { safe = true; break; }
                if (safe) continue;
                spot = p;
                return true;
            }
            spot = around;
            return false;
        }

        public ZLZombie SpawnZombie(ZLFoe kind, Vector2 pos, int threat, bool hunting)
        {
            var go = new GameObject("Z_" + kind);
            go.transform.position = pos;
            var z = go.AddComponent<ZLZombie>();
            Sprite[] idle, move, hurt;
            Color tint = Color.white;
            float scale = 0.75f;
            EnemyDefinition def;
            switch (kind)
            {
                case ZLFoe.Runner: def = runner; break;
                case ZLFoe.Screamer: def = runner; tint = new Color(0.85f, 0.7f, 1f); break;
                case ZLFoe.Bloater: def = bloater; scale = 0.85f; break;
                case ZLFoe.Spitter: def = spitter; break;
                case ZLFoe.Brute: def = brute; scale = 1f; break;
                case ZLFoe.Herald: def = brute; scale = 1.35f; tint = new Color(0.75f, 0.7f, 0.95f); break;
                case ZLFoe.Riot: def = shambler; scale = 0.82f; tint = new Color(0.62f, 0.7f, 0.85f); break;
                case ZLFoe.Leaper: def = runner; scale = 0.66f; tint = new Color(0.75f, 0.95f, 0.65f); break;
                default: def = shambler; break;
            }
            if (kind == ZLFoe.Hound)
            {
                var run = new[] { Sprite("Props/hound_0"), Sprite("Props/hound_1"), Sprite("Props/hound_2"), Sprite("Props/hound_3") };
                idle = new[] { run[0] }; move = run; hurt = new[] { run[1] };
                scale = 1f;
            }
            else
            {
                var f = def != null ? def.frames : null;
                idle = f != null && f.idle.Length > 0 ? f.idle : new[] { def != null ? def.sprite : null };
                move = f != null && f.run.Length > 0 ? f.run : idle;
                hurt = f != null ? f.hurt : new Sprite[0];
            }
            z.Init(this, kind, threat, idle, move, hurt, tint, scale);
            if (kind == ZLFoe.Riot) z.AddShield(Sprite("Props/riot_shield"));
            if (!IsLastStand && Services.Settings != null && Services.Settings.Data.assistMode)
            {
                // Assist mode: slower, softer dead in Zombie Land (Last Stand scales its own spawns)
                z.maxHp = z.hp = Mathf.Max(1, Mathf.RoundToInt(z.hp * ZombieAssist.HealthScale(true, false)));
                z.speed *= ZombieAssist.SpeedScale(true, false);
            }
            if (kind == ZLFoe.Herald)
            {
                var crown = new GameObject("Crown").AddComponent<SpriteRenderer>();
                crown.transform.SetParent(z.transform, false);
                crown.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                crown.sprite = Sprite("Props/crown");
                crown.sortingOrder = ZLSort.Canopy;
                if (material != null) crown.sharedMaterial = material;
            }
            if (hunting) z.StartHunt();
            _zombies.Add(z);
            return z;
        }

        public int NestSpawnCount(ZLNest n) => _nestSpawns.TryGetValue(n, out var c) ? c : 0;

        public void SpawnFromNest(ZLNest n)
        {
            var p = (Vector2)n.transform.position + UnityEngine.Random.insideUnitCircle.normalized * 1.6f;
            if (!City.Walkable(p)) return;
            int threat = ZLCatalog.District(n.district).threat;
            var z = SpawnZombie(ZLRules.Pick(threat, UnityEngine.Random.value), p, threat, Vector2.Distance(p, TargetPos) < 12f);
            _nestSpawns[n] = NestSpawnCount(n) + 1;
            _fromNest[z] = n;
        }

        public void NestDestroyed(ZLNest n)
        {
            _nests.Remove(n);
            if (n.missionId == null && !Progress.nestsCleared.Contains(n.id))
            {
                Progress.nestsCleared.Add(n.id);
                int got = ZLShop.Earn(Progress, ZLShop.NestScrap, DoubleScrap);
                ShowToast("ROT NEST DESTROYED. " + ZLCatalog.District(n.district).name.ToUpperInvariant() + $" GETS QUIETER.  +{got} SCRAP");
                ProgressTracker.AddXp(40);
                AddXp(SurvivorRank.ZLNestXp);
            }
            Active?.NestDestroyed(n);
            Save(false);
        }

        public void ZombieDied(ZLZombie z)
        {
            _zombies.Remove(z);
            if (_fromNest.TryGetValue(z, out var n)) { _fromNest.Remove(z); if (n != null) _nestSpawns[n] = Mathf.Max(0, NestSpawnCount(n) - 1); }
            Progress.kills++;
            ProgressTracker.Report(ChallengeKind.DefeatEnemies, 1);
            if (_nuking) return;
            Progress.killsSinceDrop++;
            if (IsLastStand)
            {
                Stand.ZombieDied(z);
                if (!Stand.HoundRound && !Stand.Mut.NoDrops && ZLShop.DropsPowerUp(Progress.killsSinceDrop, UnityEngine.Random.value, 0.04f, 30)) SpawnPowerUp(ZLShop.PickPowerUp(UnityEngine.Random.value), z.transform.position);
                return;
            }
            Active?.ZombieDied(z);
            int scrap = ZLShop.Earn(Progress, ZLShop.ScrapForKill(z.kind), DoubleScrap);
            if (scrap >= 100) ZLFloater.Show($"+{scrap}", z.transform.position, new Color(1f, 0.85f, 0.35f), 0.9f);
            AddXp(SurvivorRank.ZLKillXp);
            if (Boss == null && ZLShop.DropsPowerUp(Progress.killsSinceDrop, UnityEngine.Random.value))
            {
                SpawnPowerUp(ZLShop.PickPowerUp(UnityEngine.Random.value), z.transform.position);
                return;
            }
            float roll = UnityEngine.Random.value;
            if (roll < 0.07f) DropLoot("ammo", z.transform.position);
            else if (roll < 0.09f) DropLoot("medkit", z.transform.position);
        }

        // ---- power-ups ------------------------------------------------------------------------------------

        public void SpawnPowerUp(ZLPowerUp kind, Vector2 at)
        {
            Progress.killsSinceDrop = 0;
            var go = new GameObject("PowerUp_" + kind);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite("Props/" + ZLShop.PowerUpArt(kind));
            sr.sortingOrder = ZLSort.Canopy - 1;
            if (material != null) sr.sharedMaterial = material;
            _powerUps.Add((go, kind, Time.time + ZLShop.PowerUpLifetime));
            if (_powerUpIds.Count > 32) { var live = new Dictionary<GameObject, int>(); foreach (var kv in _powerUpIds) if (kv.Key != null) live[kv.Key] = kv.Value; _powerUpIds.Clear(); foreach (var kv in live) _powerUpIds[kv.Key] = kv.Value; }
            _powerUpIds[go] = RuntimeIds.Next() & 0xffff;
            Sfx.Play(Sfx.Shard, 0.7f, 0f);
        }

        void UpdatePowerUps()
        {
            if (IsGuest) return;   // the host owns the power-ups
            for (int i = _powerUps.Count - 1; i >= 0; i--)
            {
                var p = _powerUps[i];
                if (p.go == null) { _powerUps.RemoveAt(i); continue; }
                float left = p.until - Time.time;
                p.go.transform.localScale = Vector3.one * (1.1f + Mathf.Sin(Time.time * 5f) * 0.1f);
                var sr = p.go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = left > 5f || Mathf.Repeat(Time.time, 0.3f) < 0.2f;
                if (left <= 0f) { Destroy(p.go); _powerUps.RemoveAt(i); continue; }
                ZLPlayer taker = null;
                foreach (var h in Squad)
                    if (h != null && h.Standing && Vector2.Distance(p.go.transform.position, h.transform.position) < 1.1f) { taker = h; break; }
                if (taker != null)
                {
                    ApplyPowerUp(p.kind, p.go.transform.position);
                    Destroy(p.go);
                    _powerUps.RemoveAt(i);
                }
            }
        }

        public void ApplyPowerUp(ZLPowerUp kind, Vector2 at)
        {
            Sfx.Play(Sfx.PowerUp, 0.9f, 0f);
            ZLFx.Spawn(GlowHalo.Sprite, at, new Color(1f, 0.9f, 0.5f, 0.8f), 0.5f, 1f, 4f, material);
            string label = ZLShop.PowerUpName(kind);
            switch (kind)
            {
                case ZLPowerUp.InstaKill: InstaKillLeft = ZLShop.PowerUpSeconds; break;
                case ZLPowerUp.DoubleScrap: DoubleScrapLeft = ZLShop.PowerUpSeconds; if (IsLastStand) label = "DOUBLE POINTS"; break;
                case ZLPowerUp.MaxAmmo:
                    foreach (var h in Squad) if (h != null && h.Loadout != null) { ZLShop.MaxAmmo(h.Loadout); h.CancelReload(); }
                    break;
                case ZLPowerUp.Nuke: Nuke(at); break;
            }
            ShowToast(label + "!", 2.5f);
            if (Net != null) Net.BroadcastToast(label + "!", 1);
        }

        /// <summary>Power-ups on the ground (for online snapshots).</summary>
        public IEnumerable<(int id, ZLPowerUp kind, Vector2 at)> PowerUpsOnGround()
        {
            foreach (var p in _powerUps)
                if (p.go != null) yield return (_powerUpIds.TryGetValue(p.go, out var id) ? id : 0, p.kind, p.go.transform.position);
        }

        /// <summary>Every ordinary zombie near the hero drops (not Heralds, bosses or nests). Pays a flat bonus.</summary>
        public void Nuke(Vector2 at)
        {
            _nuking = true;
            foreach (var z in _zombies.ToArray())
                if (z != null && z.Alive && z.kind != ZLFoe.Herald && (Squad.Count > 1 || Vector2.Distance(z.transform.position, TargetPos) < ZLShop.NukeRadius))
                    z.Hit(99999, Vector2.up, null);
            _nuking = false;
            _zombies.RemoveAll(z => z == null || !z.Alive);
            if (IsLastStand) Stand.Nuked();
            else ZLShop.Earn(Progress, ZLShop.NukeScrap, DoubleScrap);
            ZLFx.Spawn(GlowHalo.Sprite, TargetPos, new Color(1f, 1f, 0.9f, 0.9f), 0.6f, 2f, 30f, material);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.4f, 0.4f);
        }

        /// <summary>Guest: the host's power-up timers (for the HUD).</summary>
        public void MirrorPowerUps(bool instaKill, bool doublePoints)
        {
            InstaKillLeft = instaKill ? Mathf.Max(InstaKillLeft, 1f) : 0f;
            DoubleScrapLeft = doublePoints ? Mathf.Max(DoubleScrapLeft, 1f) : 0f;
        }

        /// <summary>Last Stand co-op: another hero on the field (AI, an online player, or a guest's view of one).</summary>
        public ZLPlayer SpawnSquadHero(string heroId, ZLControl control, Vector2 at, int slot, string name, Color ring)
        {
            var def = roster != null && !string.IsNullOrEmpty(heroId) ? roster.Find(heroId) : null;
            if (def == null && roster != null && roster.heroes.Count > 0) def = roster.heroes[(slot + 1) % roster.heroes.Count];
            var go = new GameObject("Hero_" + slot);
            go.transform.position = at;
            var h = go.AddComponent<ZLPlayer>();
            h.Init(this, def, material, roster != null && def != null ? roster.TintFor(def.id) : Color.white, control);
            h.Slot = slot;
            h.ShowSquadMarker(ring, name);
            Squad.Add(h);
            return h;
        }

        /// <summary>Insta-kill: one hit drops anything but a Herald or a boss.</summary>
        public bool InstaKills(ZLFoe kind) => InstaKillLeft > 0f && kind != ZLFoe.Herald && kind != ZLFoe.FirstTurned;

        void DropLoot(string kind, Vector2 at)
        {
            var go = new GameObject("Drop_" + kind);
            go.transform.position = at;
            var l = go.AddComponent<ZLLoot>();
            l.Init(this, "", kind, Sprite("Props/pk_" + kind));
            l.temporary = true;
            _loot.Add(l);
        }

        public void MakeNoise(Vector2 at, float noise)
        {
            if (noise <= 0f) return;
            float range = ZLRules.HearingRange(noise);
            foreach (var z in _zombies)
            {
                if (z == null || z.hunting) continue;
                float d = Vector2.Distance(z.transform.position, at);
                if (d < range * 0.45f) z.StartHunt();
                else if (d < range) z.Investigate(at);
            }
        }

        public void CallHorde(Vector2 at)
        {
            int threat = ThreatAt(at);
            int n = ZLRules.HordeSize(threat);
            ShowToast("A SCREAMER CALLED THE HORDE!");
            for (int i = 0; i < n; i++)
                if (SpawnSpot(TargetPos, 13f, 19f, out var p)) SpawnZombie(ZLRules.Pick(threat, UnityEngine.Random.value), p, threat, true);
        }

        public void SummonAround(Vector2 at, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var p = at + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(3f, 6f);
                if (City.Walkable(p)) SpawnZombie(ZLFoe.Shambler, p, 4, true);
            }
        }

        // ---- the blood hounds ------------------------------------------------------------------------

        void Hounds(float dt)
        {
            if (InSafehouse || Boss != null) return;
            Roamed += dt;
            if (!_warned && Roamed > ZLRules.HoundsAfter - 30f)
            {
                _warned = true;
                ShowToast("YOU FEEL WATCHED. FIND A SAFEHOUSE, OR THE HOUNDS WILL COME.");
            }
            if (!ZLRules.HoundsActive(Roamed)) return;
            _houndT -= dt;
            if (_houndT > 0f) return;
            _houndT = ZLRules.HoundInterval(Roamed);
            if (_houndPacks == 0) ShowToast("THE BLOOD HOUNDS ARE LOOSE!");
            SpawnHoundPack(ZLRules.HoundPack(_houndPacks++), TargetPos);
        }

        public void SpawnHoundPack(int n, Vector2 around)
        {
            Sfx.Play(Sfx.Groan, 1f, 0f);
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Heavy);
            for (int i = 0; i < n; i++)
                if (SpawnSpot(around, 14f, 20f, out var p)) SpawnZombie(ZLFoe.Hound, p, 3, true);
        }

        /// <summary>Seconds until the hounds come (negative once they're out).</summary>
        public float HoundsIn => ZLRules.HoundsAfter - Roamed;

        // ---- combat helpers -------------------------------------------------------------------------------

        public void Bite(ZLZombie z, int dmg)
        {
            var victim = z != null ? NearestHero(z.transform.position) : Player;
            if (victim == null || victim.Dead) return;
            if (victim.Driving != null)
            {
                victim.Driving.Damage(Mathf.Max(1, dmg / 2));
                return;
            }
            victim.Hurt(dmg, z != null ? (Vector2)z.transform.position : (Vector2)(Boss != null ? Boss.transform.position : victim.transform.position));
        }

        public void Explode(Vector2 at, float radius, int dmg, bool fromPlayer)
        {
            ZLFx.Spawn(GlowHalo.Sprite, at, fromPlayer ? new Color(1f, 0.6f, 0.2f, 0.9f) : new Color(0.5f, 0.85f, 0.3f, 0.8f), 0.5f, radius * 0.6f, radius * 2f, material);
            if (fromPlayer)
            {
                foreach (var h in Hostiles())
                {
                    var c = h as Component;
                    if (c == null) continue;
                    if (Vector2.Distance(c.transform.position, at) < radius) h.Hit(dmg, ((Vector2)c.transform.position - at).normalized, null);
                }
            }
            else
                foreach (var h in Squad)
                    if (h != null && h.Driving == null && Vector2.Distance(h.transform.position, at) < radius) h.Hurt(dmg, at);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.2f, 0.2f);
        }

        /// <summary>The Singularity Cannon: the dead nearby are dragged to the orb, then it bursts.</summary>
        public void Singularity(Vector2 at, int damage, ZLPlayer owner)
        {
            if (IsGuest) return;
            StartCoroutine(SingularityRoutine(at, damage, owner));
        }

        IEnumerator SingularityRoutine(Vector2 at, int damage, ZLPlayer owner)
        {
            ZLFx.Spawn(GlowHalo.Sprite, at, new Color(0.55f, 0.3f, 1f, 0.85f), ZLCatalog.PullSeconds + 0.1f, ZLCatalog.PullBurstRadius, -4f, material);
            float t = 0f;
            while (t < ZLCatalog.PullSeconds)
            {
                t += Time.deltaTime;
                foreach (var z in _zombies)
                {
                    if (z == null || !z.Alive || z.Body == null || z.kind == ZLFoe.Herald) continue;
                    Vector2 d = at - (Vector2)z.transform.position;
                    if (d.magnitude > ZLCatalog.Singularity.pull || d.magnitude < 0.2f) continue;
                    // move it directly (its own steering would cancel a velocity), never through a wall
                    Vector2 next = z.Body.position + d.normalized * Mathf.Min(d.magnitude, 7f * Time.deltaTime);
                    if (City.Walkable(next)) { z.Body.position = next; z.transform.position = next; }
                }
                yield return null;
            }
            var prevOwner = MeleeAttacker;
            MeleeAttacker = owner;   // points for the burst go to whoever fired
            Explode(at, ZLCatalog.PullBurstRadius, damage, true);
            MeleeAttacker = prevOwner;
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Shake(0.3f, 0.3f);
        }

        /// <summary>The Rime Cannon's frost burst: every zombie near the hit freezes.</summary>
        public void FreezeAround(Vector2 at, float seconds)
        {
            ZLFx.Spawn(GlowHalo.Sprite, at, new Color(0.6f, 0.9f, 1f, 0.75f), 0.4f, ZLCatalog.FreezeRadius * 0.5f, ZLCatalog.FreezeRadius * 2f, material);
            foreach (var z in _zombies)
                if (z != null && z.Alive && Vector2.Distance(z.transform.position, at) < ZLCatalog.FreezeRadius) z.Freeze(seconds);
        }

        IEnumerable<IZLHittable> Hostiles()
        {
            foreach (var z in _zombies.ToArray()) if (z != null && z.Alive) yield return z;
            foreach (var n in _nests.ToArray()) if (n != null && n.Alive) yield return n;
            if (Boss != null && Boss.Alive) yield return Boss;
        }

        public void Melee(Vector2 at, Vector2 aim) => Melee(at, aim, Player);

        public void Melee(Vector2 at, Vector2 aim, ZLPlayer attacker)
        {
            Sfx.Play(Sfx.Swing, attacker == Player ? 0.7f : 0.35f);
            if (IsGuest) return;   // the host lands the hits
            MeleeAttacker = attacker;
            ZLFx.Spawn(GlowHalo.Sprite, at + aim * 0.8f, new Color(1f, 1f, 1f, 0.5f), 0.15f, 0.6f, 2f, material);
            foreach (var h in Hostiles())
            {
                var c = (Component)h;
                Vector2 to = (Vector2)c.transform.position - at;
                if (to.magnitude < ZLRules.MeleeRange + (h is ZLNest ? 1f : 0f) && Vector2.Angle(aim, to) < 70f)
                    h.Hit(ZLRules.MeleeDamage, to.normalized, null);
            }
            MeleeAttacker = null;
        }

        public Vector2? AutoAim(Vector2 from, Vector2 facing, float range) => AutoAim(from, facing, range, 75f);

        /// <summary>Auto-aim within a cone (degrees either side of facing; anything within 3.5 tiles counts).</summary>
        public Vector2? AutoAim(Vector2 from, Vector2 facing, float range, float cone)
        {
            Vector2? best = null;
            float bestScore = float.MaxValue;
            foreach (var h in Hostiles())
            {
                var c = (Component)h;
                Vector2 p = c.transform.position;
                if (h is ZLBoss) p += Vector2.up * 0.8f;
                Vector2 to = p - from;
                float d = to.magnitude;
                if (d > range) continue;
                float ang = Vector2.Angle(facing, to);
                if (ang > cone && d > 3.5f) continue;
                if (!City.Sees(from, p)) continue;
                float score = d + ang * 0.03f;
                if (score < bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        // ---- interactables, loot -----------------------------------------------------------------------

        /// <summary>Adds something the hero can USE (Last Stand stations, doors, the box).</summary>
        public void RegisterUse(IZLUse u) { if (u != null && !_uses.Contains(u)) _uses.Add(u); }
        public void UnregisterUse(IZLUse u) => _uses.Remove(u);

        public IZLUse NearestUse(Vector2 at)
        {
            IZLUse best = null;
            float bd = float.MaxValue;
            foreach (var u in _uses)
            {
                if (u == null || (u as UnityEngine.Object) == null || !u.CanUse) continue;
                float d = Vector2.Distance(u.UsePos, at);
                if (d < u.UseRange && d < bd) { bd = d; best = u; }
            }
            if (Active != null)
            {
                var m = Active.NearestUse(at);
                if (m != null) best = m;
            }
            return best;
        }

        void CollectLoot()
        {
            if (Player == null || Player.Driving != null) return;
            Vector2 at = Player.transform.position;
            for (int i = _loot.Count - 1; i >= 0; i--)
            {
                var l = _loot[i];
                if (l == null) { _loot.RemoveAt(i); continue; }
                l.Tick(Time.deltaTime);
                if (!l.Available || Vector2.Distance(l.transform.position, at) > 0.9f) continue;
                if (Collect(l)) { if (l.temporary) { _loot.RemoveAt(i); Destroy(l.gameObject); } else l.Take(); }
            }
        }

        bool Collect(ZLLoot l)
        {
            var p = Progress;
            switch (l.kind)
            {
                case "ammo":
                    ZLRules.AmmoBox(p);
                    ZLFloater.Show("+AMMO", l.transform.position, new Color(1f, 0.9f, 0.5f));
                    Sfx.Play(Sfx.Reload, 0.8f);
                    return true;
                case "medkit":
                    if (p.medkits >= ZLRules.MaxMedkits) { if (Player.Health < Player.MaxHealth) { Player.Heal(ZLRules.MedkitHeal); return true; } return false; }
                    p.medkits++;
                    ZLFloater.Show("+MEDKIT", l.transform.position, new Color(1f, 0.6f, 0.6f));
                    Sfx.Play(Sfx.Heal, 0.7f);
                    return true;
                case "armor":
                    if (p.armor >= ZLRules.MaxArmor) return false;
                    p.armor = ZLRules.MaxArmor;
                    ZLFloater.Show("+ARMOR", l.transform.position, new Color(0.6f, 0.8f, 1f));
                    Sfx.Play(Sfx.PowerUp, 0.6f);
                    return true;
                default:
                    var gun = ZLCatalog.Gun(l.kind);
                    if (gun == null) return false;
                    ZLRules.GiveGun(p, gun, gun.magazine * 2);
                    p.equipped = gun.id;
                    Player.CancelReload();
                    if (!string.IsNullOrEmpty(l.id)) p.taken.Add(l.id);
                    ShowToast("NEW GUN: " + gun.name);
                    Sfx.Play(Sfx.Purchase, 0.8f);
                    Save(false);
                    return true;
            }
        }

        public void UseMedkit()
        {
            if (Player == null || Player.Dead || Progress.medkits <= 0 || Player.Health >= Player.MaxHealth) return;
            Progress.medkits--;
            Player.Heal(ZLRules.MedkitHeal);
        }

        public void SwapGun() => SwapGun(Player);

        public void SwapGun(ZLPlayer who)
        {
            var lo = who != null && who.Loadout != null ? who.Loadout : Progress;
            var guns = lo.guns;
            if (guns.Count < 2) return;
            int i = guns.FindIndex(g => g.id == lo.equipped);
            lo.equipped = guns[(i + 1) % guns.Count].id;
            who.CancelReload();
            if (who != Player) return;
            var g2 = ZLCatalog.Gun(lo.equipped);
            ZLFloater.Show(g2 != null ? g2.name : "", who.transform.position, Color.white, 0.8f);
            Sfx.Play(Sfx.Reload, 0.5f, 0f);
        }

        /// <summary>A hero fired (noise draws the dead; online, the others see the bullets).</summary>
        public void HeroShot(ZLPlayer who, Vector2 from, Vector2 aim, ZLGun gun)
        {
            MakeNoise(who.transform.position, gun.noise);
            if (Net != null) Net.BroadcastShot(who.Slot, from, aim, gun.id);
        }

        // ---- co-op: downs and revives -------------------------------------------------------------------

        /// <summary>A downed teammate within reach of this hero.</summary>
        public ZLPlayer DownedTeammateNear(ZLPlayer me)
        {
            if (Squad.Count <= 1) return null;
            foreach (var h in Squad)
                if (h != null && h != me && h.Downed && Vector2.Distance(h.transform.position, me.transform.position) < SquadRules.ReviveRange) return h;
            return null;
        }

        /// <summary>A hero's health hit zero.</summary>
        public void HeroFell(ZLPlayer who)
        {
            if (Squad.Count <= 1) { if (who == Player) PlayerDied(); return; }
            if (ZLShop.UseSecondChance(who.Loadout))
            {
                who.StandUp(1f);
                SecondChanceBlast(who);
                Announce($"{who.DisplayName}: SECOND CHANCE!");
                return;
            }
            who.GoDown();
            Announce(who == Player ? "YOU'RE DOWN! HANG ON FOR A REVIVE." : $"{who.DisplayName} IS DOWN! HOLD USE NEXT TO THEM.");
            CheckSquadWiped();
        }

        public void TickDowned(ZLPlayer who, float dt)
        {
            who.TickBleed(dt);
            if (who.BleedOut > 0f) return;
            who.BleedOutNow();
            Announce(who == Player ? "YOU BLED OUT. BACK NEXT ROUND." : $"{who.DisplayName} BLED OUT. BACK NEXT ROUND.");
            CheckSquadWiped();
        }

        public void Revived(ZLPlayer who, ZLPlayer by)
        {
            who.StandUp(SquadRules.ReviveHealth);
            Sfx.Play(Sfx.Heal, 0.8f);
            Announce($"{by.DisplayName} REVIVED {who.DisplayName}!");
            if (Stand != null) Stand.CountRevive(by);
        }

        void CheckSquadWiped()
        {
            foreach (var h in Squad) if (h != null && h.Standing) return;
            if (IsLastStand && Stand != null && !Stand.Over)
            {
                RespawnPending = true;
                Stand.GameOver();
            }
        }

        void SecondChanceBlast(ZLPlayer who)
        {
            _nuking = true;
            foreach (var z in _zombies.ToArray())
                if (z != null && z.Alive && z.kind != ZLFoe.Herald && Vector2.Distance(z.transform.position, who.transform.position) < 4f) z.Hit(99999, Vector2.up, null);
            _nuking = false;
            ZLFx.Spawn(GlowHalo.Sprite, who.transform.position, new Color(0.5f, 1f, 0.6f, 0.9f), 0.6f, 1f, 9f, material);
        }

        /// <summary>A toast here and, online, on everyone's phone.</summary>
        public void Announce(string text, float seconds = 3f)
        {
            ShowToast(text, seconds);
            if (Net != null && Net.IsHost) Net.BroadcastToast(text, 0);
        }

        // ---- vehicles ------------------------------------------------------------------------------------

        public void VehicleEntered(ZLVehicle v) => Active?.VehicleEntered(v);
        public void VehicleExited(ZLVehicle v) => Active?.VehicleExited(v);
        public void VehicleWrecked(ZLVehicle v)
        {
            _uses.Remove(v);
            Active?.VehicleWrecked(v);
        }

        public void RemoveVehicle(ZLVehicle v)
        {
            if (v == null) return;
            _vehicles.Remove(v);
            _uses.Remove(v);
            if (v.Occupied) v.Eject(true);
            Destroy(v.gameObject);
        }

        // ---- missions ------------------------------------------------------------------------------------

        public void StartMission(ZLMissionMarker marker)
        {
            if (Active != null)
            {
                ShowToast("FINISH " + Active.Spec.name + " FIRST (OR ABANDON IT ON THE MAP).");
                return;
            }
            if (!Progress.leads.Contains(marker.Spec.id)) Progress.leads.Add(marker.Spec.id);
            var run = new ZLMissionRun(this, marker.Spec, marker.Site);
            Story(marker.Spec.giver, new[] { marker.Spec.briefing }, "zl_city", () =>
            {
                Active = run;
                run.Begin();
                ShowToast(marker.Spec.name + ": " + run.Objective);
            });
        }

        public void MissionComplete(ZLMissionRun run)
        {
            if (Active != run) return;
            Active = null;
            var spec = run.Spec;
            ZLRules.Complete(Progress, spec.id);
            run.Cleanup();
            foreach (var m in _markers) if (m.Spec.id == spec.id) m.Refresh();
            ProgressTracker.AddXp(spec.required ? 120 : 80);
            AddXp(spec.required ? SurvivorRank.ZLStoryMissionXp : SurvivorRank.ZLMissionXp);
            int got = ZLShop.Earn(Progress, spec.required ? ZLShop.StoryMissionScrap : ZLShop.BonusMissionScrap, false);
            var lines = new List<string> { spec.done + $"  (+{got} scrap)" };
            TellDaily(ZombieDailyTracker.Report(DailyKind.Missions, 1));
            if (spec.required && spec.chapter == 3)
                lines.Add($"SECURITY KEYCARDS: {Progress.Keycards}/{ZLCatalog.KeycardsNeeded}" +
                          (Progress.Keycards >= ZLCatalog.KeycardsNeeded ? ". The Curator's vault will open now." : ""));
            else if (spec.required && spec.chapter == 2)
                lines.Add($"SEA-GLASS SIGILS: {Progress.Sigils}/{ZLCatalog.SigilsNeeded}" +
                          (Progress.Sigils >= ZLCatalog.SigilsNeeded ? ". The tide gate under the penitentiary will open now." : ""));
            else if (spec.required)
            {
                if (Puzzle.Clues.TryGetValue(spec.id, out var clue)) lines.Add("CLUE: " + clue.text);
                lines.Add($"GRAYSTONE SHARDS: {Progress.Shards}/{ZLCatalog.ShardsNeeded}" +
                          (Progress.Shards >= ZLCatalog.ShardsNeeded ? ". The seal on his lair will break now. Bring the right weapon." : ""));
            }
            Sfx.Play(Sfx.Victory, 0.8f);
            Save(true);
            Story(spec.giver, lines.ToArray(), "zl_city", null);
        }

        public void MissionFailed(ZLMissionRun run, string why)
        {
            if (Active != run) return;
            Active = null;
            run.Cleanup();
            ShowToast("MISSION FAILED: " + why + " Try again at the marker.");
            Sfx.Play(Sfx.Hurt, 0.8f);
        }

        public void AbandonMission()
        {
            if (Active == null) return;
            var run = Active;
            Active = null;
            run.Cleanup();
            ShowToast("MISSION ABANDONED.");
        }

        // ---- lairs and the First Turned ----------------------------------------------------------------

        public void TryLair(string lairId, Vector2 door)
        {
            if (lairId == ZLCatalog.TideGate) { TryTideGate(door); return; }
            if (lairId == ZLCatalog.Vault) { TryVault(door); return; }
            var lair = ZLCatalog.Lair(lairId);
            if (Boss != null || Progress.bossDefeated) return;
            if (Progress.Shards < ZLCatalog.ShardsNeeded)
            {
                ShowToast($"{lair.name.ToUpperInvariant()}: SEALED. YOUR SHARDS: {Progress.Shards}/{ZLCatalog.ShardsNeeded}.");
                Sfx.Play(Sfx.Click, 0.7f, 0f);
                return;
            }
            if (lairId != Puzzle.TrueLair)
            {
                if (!Progress.lairsSearched.Contains(lairId)) Progress.lairsSearched.Add(lairId);
                ShowToast("EMPTY. HE ISN'T HERE... BUT HIS HOUNDS ARE.");
                SpawnHoundPack(5, door);
                CallHorde(door);
                return;
            }
            Story(ZLCatalog.BossRealName.ToUpperInvariant(), ZLCatalog.BossIntro, "zl_boss", () =>
            {
                foreach (var z in _zombies.ToArray()) if (z != null && Vector2.Distance(z.transform.position, door) < 14f) z.Vanish(true);
                _zombies.RemoveAll(z => z == null);
                var go = new GameObject("TheFirstTurned");
                Vector2 at = door + Vector2.up * 4f;
                if (!City.Walkable(at)) at = door + Vector2.down * 4f;
                if (!City.Walkable(at)) at = door;
                go.transform.position = at;
                Boss = go.AddComponent<ZLBoss>();
                Boss.Init(this, Frames("boss_idle", 2), Frames("boss_walk", 4), Frames("boss_attack", 2));
                ShowToast(ZLCatalog.BossName + "!");
            });
        }

        Sprite[] Frames(string name, int n)
        {
            var a = new Sprite[n];
            for (int i = 0; i < n; i++) a[i] = Sprite($"Props/{name}_{i}");
            return a;
        }

        CosmeticStats _bossBefore;

        public void BossDefeated(ZLBoss boss)
        {
            _bossBefore = ZombieDailyTracker.Snapshot();
            if (boss != null && boss.Curator) { Chapter3BossDefeated(boss); return; }
            if (boss != null && boss.Brine) { Chapter2BossDefeated(boss); return; }
            Progress.bossDefeated = true;
            foreach (var z in _zombies.ToArray()) if (z != null) z.Vanish(true);
            _zombies.Clear();
            foreach (var n in _nests.ToArray()) if (n != null) Destroy(n.gameObject);
            _nests.Clear();
            Destroy(boss.gameObject, 0.1f);
            Boss = null;
            if (Services.Save != null) Services.Save.Data.starShards += ZLRules.ClearReward;
            ProgressTracker.AddXp(600);
            AddXp(SurvivorRank.ZLBossXp);
            FlushXp();
            var won = ZombieDailyTracker.BossKilled();
            won.AddRange(ZombieDailyTracker.AfterProgress(_bossBefore));
            TellDaily(won);
            Sfx.Play(Sfx.Victory, 1f);
            Save(true);
            Story("", ZLCatalog.Ending, "dawn", () =>
            {
                ShowToast($"+{ZLRules.ClearReward} STAR SHARDS. ZOMBIE LAND IS CLEAR.");
                StartChapter2();
            });
        }

        // ---- death and checkpoints ---------------------------------------------------------------------

        public void LightSafehouse(ZLSafehouse s)
        {
            if (Progress.safehouses.Contains(s.Data.id)) { Progress.checkpoint = s.Data.id; return; }
            Progress.safehouses.Add(s.Data.id);
            Progress.checkpoint = s.Data.id;
            ShowToast("CHECKPOINT: " + s.Data.name);
            Sfx.Play(Sfx.Checkpoint, 0.9f);
            if (Progress.rewards.Contains("motorcycles")) SpawnVehicle(ZLVehicleKind.Motorcycle, (Vector2)s.transform.position + new Vector2(-1.5f, 0f), 90f, 3);
            Save(true);
        }

        public void PlayerDied()
        {
            if (RespawnPending) return;
            if (ZLShop.UseSecondChance(Player.Loadout ?? Progress))
            {
                Player.Revive(Player.transform.position);
                ShowToast("SECOND CHANCE!", 2.5f);
                SecondChanceBlast(Player);
                return;
            }
            if (IsLastStand)
            {
                RespawnPending = true;
                Stand.GameOver();
                return;
            }
            RespawnPending = true;
            if (Active != null) { var run = Active; Active = null; run.Cleanup(); }
            StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            Sfx.Play(Sfx.KO, 0.9f, 0f);
            yield return new WaitForSecondsRealtime(2.6f);
            ZLRules.Respawn(Progress, SurvivorRank.Has(CareerXp, "zl_medic") ? 2 : 1, SurvivorRank.Has(CareerXp, "zl_armor") ? ZLRules.MaxArmor / 2 : 0);
            Roamed = 0f;
            _houndPacks = 0;
            _houndT = 0f;
            _warned = false;
            if (Boss != null) { Destroy(Boss.gameObject); Boss = null; }
            Vector2 at = RespawnPoint();
            foreach (var z in _zombies.ToArray()) if (z != null && (Vector2.Distance(z.transform.position, at) < 26f || z.kind == ZLFoe.Hound)) z.Vanish(false);
            _zombies.RemoveAll(z => z == null || !z.Alive);
            Player.Revive(at);
            if (CameraFollowTopDown.Main != null) CameraFollowTopDown.Main.Snap();
            RespawnPending = false;
            Save(true);
            ShowToast("YOU WOKE UP AT THE SAFEHOUSE. MISSIONS AND SHARDS ARE STILL YOURS.");
        }

        // ---- UI helpers --------------------------------------------------------------------------------

        /// <summary>First-time coaching tip (ZLCoach) shown in its own banner, apart from toasts.</summary>
        public string CoachTip { get; private set; }
        public float CoachLeft { get; private set; }
        public ZLCoach Coach { get; private set; }

        public void ShowCoach(string text)
        {
            CoachTip = text;
            CoachLeft = ZombieTips.Show;
        }

        /// <summary>Daily orders / achievements / LOCKER lines as one toast.</summary>
        public void TellDaily(List<string> lines)
        {
            if (lines == null || lines.Count == 0) return;
            ShowToast(string.Join("\n", lines), 4.5f);
        }

        public void ShowToast(string text, float seconds = 4f)
        {
            Toast = text;
            ToastLeft = seconds;
        }

        public void Story(string speaker, string[] lines, string art, Action done)
        {
            // Batch runs (tests, the balance lab) have nobody to tap through a story: carry straight on.
            if (!CutscenePlayer.Allowed) { done?.Invoke(); return; }
            var panels = new BeatPanel[lines.Length];
            for (int i = 0; i < lines.Length; i++) panels[i] = new BeatPanel(speaker, lines[i], art);
            CutscenePlayer.Play(new StoryBeat { id = "zombieland", panels = panels }, done);
        }

        public void Save(bool now)
        {
            if (Services.Save == null || IsLastStand) return;
            Services.Save.MarkDirty();
            if (now) Services.Save.SaveNow();
        }

        // ---- test hooks (PlayMode tests) ---------------------------------------------------------------

        /// <summary>Starts a mission without the briefing popup.</summary>
        public ZLMissionRun DebugStartMission(string id)
        {
            foreach (var m in _markers)
                if (m.Spec.id == id)
                {
                    Active = new ZLMissionRun(this, m.Spec, m.Site);
                    Active.Begin();
                    return Active;
                }
            return null;
        }

        public ZLBoss DebugSpawnBoss(Vector2 at)
        {
            var go = new GameObject("TheFirstTurned");
            go.transform.position = at;
            Boss = go.AddComponent<ZLBoss>();
            Boss.Init(this, Frames("boss_idle", 2), Frames("boss_walk", 4), Frames("boss_attack", 2));
            return Boss;
        }

        public void DebugSetRoamed(float seconds) => Roamed = seconds;

        IEnumerator ShotSetup(string shot)
        {
            yield return new WaitForSeconds(1f);
            if (shot == "drive")
            {
                ZLVehicle best = null;
                foreach (var v in _vehicles)
                    if (v.spec.kind == ZLVehicleKind.Car && (best == null || Vector2.Distance(v.transform.position, TargetPos) < Vector2.Distance(best.transform.position, TargetPos))) best = v;
                if (best != null) best.Use(Player);
                for (int i = 0; i < 90 && best != null; i++) { best.Drive(best.transform.right); yield return null; }
            }
            else if (shot == "map") { var hud = FindAnyObjectByType<ZLMapPanel>(); if (hud != null) hud.Open(); }
            else if (shot == "shop")
            {
                Progress.scrap = Mathf.Max(Progress.scrap, 6000);
                var panel = FindAnyObjectByType<ZLShopPanel>();
                if (panel == null) panel = gameObject.AddComponent<ZLShopPanel>();
                panel.Open(_safehouses.Count > 0 ? _safehouses[0] : null);
            }
            else if (shot == "boss") DebugSpawnBoss(TargetPos + Vector2.up * 5f);
            else if (shot == "labs")
            {
                // Chapter 3: the sublevel's power wing, the AI hero with the Singularity against riot guards and leapers
                Vector2 at = City.SiteOf("power") + new Vector2(2f, 3f);
                if (!City.Walkable(at)) at = City.SiteOf("power");
                Player.transform.position = at;
                Player.Body.position = at;
                Player.DebugInvulnerable = true;
                Player.Loadout = Progress;
                Player.BecomeAI(new ZLAIBrain(this, 0));
                var touch = FindAnyObjectByType<TouchControlsUI>();
                if (touch != null) touch.gameObject.SetActive(false);
                ZLRules.GiveGun(Progress, ZLCatalog.Singularity, ZLCatalog.Singularity.magazine + ZLCatalog.Singularity.maxReserve);
                Progress.equipped = ZLCatalog.Singularity.id;
                // a crowd closing in from all sides (tough enough to still be there when the shot is taken)
                var kinds = new[] { ZLFoe.Riot, ZLFoe.Leaper, ZLFoe.Shambler, ZLFoe.Runner, ZLFoe.Leaper, ZLFoe.Shambler, ZLFoe.Riot, ZLFoe.Shambler };
                for (int i = 0; i < 14; i++)
                {
                    float a = i * Mathf.PI * 2f / 14f;
                    Vector2 zp = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (7f + (i % 3));
                    if (!City.Walkable(zp)) continue;
                    var z = SpawnZombie(kinds[i % kinds.Length], zp, 5, true);
                    if (z != null) { z.maxHp *= 8; z.hp = z.maxHp; z.speed *= 0.35f; }
                }
                ShowToast(ZLCatalog.Chapter3Name + "  ·  KEYCARDS 0/" + ZLCatalog.KeycardsNeeded, 10f);
            }
            else if (shot == "isle")
            {
                // Chapter 2 on a fresh screenshot install: across the causeway, in the Blackwater village street
                Progress.bossDefeated = true;
                Progress.chapter2Begun = true;
                var d = City.doors.Find(x => x.id == ZLCatalog.IsleBridge);
                if (d != null) City.Open(d);
                if (_isleGate != null) Destroy(_isleGate);
                SpawnNests();
                Vector2 at = new Vector2(222.5f, 126.5f);
                Player.transform.position = at;
                Player.Body.position = at;
                Player.DebugInvulnerable = true;
                Player.Loadout = Progress;
                Player.BecomeAI(new ZLAIBrain(this, 0));   // it plays itself for the trailer
                var touch = FindAnyObjectByType<TouchControlsUI>();
                if (touch != null) touch.gameObject.SetActive(false);
                ZLRules.GiveGun(Progress, ZLCatalog.RimeCannon, ZLCatalog.RimeCannon.magazine + ZLCatalog.RimeCannon.maxReserve);
                Progress.equipped = ZLCatalog.RimeCannon.id;
                var kinds = new[] { ZLFoe.Riot, ZLFoe.Leaper, ZLFoe.Shambler, ZLFoe.Riot, ZLFoe.Runner, ZLFoe.Leaper };
                for (int i = 0; i < kinds.Length; i++) SpawnZombie(kinds[i], at + new Vector2(5f + i % 3, -3f + i), 5, true);
                ShowToast(ZLCatalog.Chapter2Name + "  ·  SIGILS 0/" + ZLCatalog.SigilsNeeded, 10f);
            }
            else
            {
                // walk up the street a little so some of the dead are about
                Player.transform.position = TargetPos + Vector2.up * 6f;
                if (!City.Walkable(Player.transform.position)) Player.transform.position = TargetPos + Vector2.right * 6f;
            }
        }

        /// <summary>Starts Zombie Land over (new mystery, new seed). Called from the map panel.</summary>
        public void NewGame()
        {
            ZLRules.NewGame(Progress, Mathf.Abs(Environment.TickCount) % 100000 + 1);
            Save(true);
            if (Services.Scenes != null) Services.Scenes.ReloadCurrent();
        }
    }
}
