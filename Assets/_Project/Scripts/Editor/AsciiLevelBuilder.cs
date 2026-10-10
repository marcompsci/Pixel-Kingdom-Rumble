using System.IO;
using PKR.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Map Levels: turns the text maps in StoryLayouts into Story Quest scenes (see AsciiLevel for the
    /// legend). One map cell is one world unit; cell (x, y) covers [x, x+1] x [y, y+1], and things that stand on the
    /// ground have their feet at y. Two jump-and-run levels (Sunspire Heights, Gearfall Caverns) and two stealth
    /// levels (Rooftop Run, Night Market Heist). Part of PKR > Build All Scenes.
    /// </summary>
    public static class AsciiLevelBuilder
    {
        public const string Folder = "Assets/_Project/Scenes/Story";
        public const string ContractFolder = "Assets/_Project/Scenes/Contracts";

        static Material _mat;
        static Transform _root;
        static int _shards, _crates;
        static Sprite _ground, _stone, _oneWay, _plate, _spikes, _flag, _gate, _gear, _crate, _crateEmpty,
                      _vine, _hay, _water, _spring, _ledger, _shard, _scroll;
        static Sprite[] _relics;
        static int _relicIndex;

        /// <summary>Shadow Contracts extras for one map: ids, what to steal, and where to save scores.</summary>
        public class ContractOptions
        {
            public string contractId, clueId, objectiveName = "the relic", objectiveDisplay = "Ancient Relic";
            public Sprite objectiveSprite;
        }

        static ContractOptions _contract;
        static readonly string[] RelicNames = { "Sun Idol", "Jade Mask", "Moon Amulet", "Gilded Gear", "Storm Coin", "Star Chalice" };

        [MenuItem("PKR/Build Map Levels", priority = 23)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        public static void BuildNoPrompt()
        {
            BuildLevel(SceneIds.SunspireHeights, StoryLayouts.SunspireHeights, () => DataAssets.GetOrCreateSunspireHeights());
            BuildLevel(SceneIds.GearfallCaverns, StoryLayouts.GearfallCaverns, () => DataAssets.GetOrCreateGearfallCaverns());
            BuildLevel(SceneIds.RooftopRun, StoryLayouts.RooftopRun, () => DataAssets.GetOrCreateRooftopRun());
            BuildLevel(SceneIds.NightMarketHeist, StoryLayouts.NightMarketHeist, () => DataAssets.GetOrCreateNightMarketHeist());
            DataAssets.GetOrCreateStoryWorld(); // keep the level select route in order
            AssetDatabase.SaveAssets();
        }

        /// <summary>Builds the six Shadow Contracts scenes (MS_*) from ContractLayouts.</summary>
        public static void BuildContracts()
        {
            foreach (var spec in DataAssets.ContractSpecs)
            {
                var s = spec;
                BuildLevel(s.sceneName, s.rows, () => DataAssets.GetOrCreateContractLevel(s), new ContractOptions
                {
                    contractId = s.id, clueId = s.containsClue, objectiveName = s.objectiveName, objectiveDisplay = s.objectiveDisplay
                });
            }
            DataAssets.GetOrCreateMissionBoard();
            AssetDatabase.SaveAssets();
        }

        static void BuildLevel(string sceneName, string[] rows, System.Func<LevelDefinition> levelAsset, ContractOptions contract = null)
        {
            _contract = contract;
            _relicIndex = 0;
            var map = new AsciiLevel(rows);
            var errors = map.Validate();
            if (errors.Count > 0)
            {
                Debug.LogError($"[PKR] Map {sceneName} is invalid: {string.Join("; ", errors)}");
                return;
            }

            EditorUtil.EnsureLayers();
            // New scene FIRST: opening a scene unloads assets loaded before it (Unity 6.6 "has been destroyed").
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var level = levelAsset();
            var roster = DataAssets.GetOrCreateRoster();
            var nova = DataAssets.GetOrCreateNova();
            LoadArt();
            _shards = 0;
            _crates = 0;
            bool stealth = map.Count('g') > 0 || map.Count('V') > 0;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = level.skyColor;
            camGo.transform.position = new Vector3(4f, 4f, -10f);
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.bounds = new Rect(0f, -3f, map.Width, map.Height + 3f);

            _root = new GameObject("Level").transform;

            // --- Terrain ---------------------------------------------------------------------------------
            foreach (var r in map.MergedRects('#')) Block("Ground", _ground, r);
            foreach (var r in map.MergedRects('X')) Block("Stone", _stone, r);
            foreach (var r in map.MergedRects('~')) Water(r);
            foreach (var r in map.MergedRects('L')) Vine(map, r);
            foreach (var run in map.Runs)
            {
                switch (run.tile)
                {
                    case '=':
                        SandboxBuilder.OneWay(_root, "Plank", _oneWay, _mat, new Vector2(run.x + run.length * 0.5f, run.y + 0.5f), run.length);
                        break;
                    case '^':
                        Spikes(map, run);
                        break;
                    case 'H':
                        Hay(run);
                        break;
                }
            }

            // --- Things ----------------------------------------------------------------------------------
            Vector2 start = Vector2.zero;
            int checkpoint = 0;
            foreach (var p in map.Points)
            {
                float cx = p.x + 0.5f;
                switch (p.tile)
                {
                    case 'P': start = new Vector2(cx, p.y + 0.75f); break;
                    case 'G': Goal(new Vector2(cx, p.y)); break;
                    case 'C': Checkpoint(new Vector2(cx, p.y), checkpoint++); break;
                    case '*': Spot(PickupKind.StarShard, new Vector2(cx, p.y + 0.5f)); break;
                    case '+': Spot(PickupKind.Health, new Vector2(cx, p.y + 0.5f)); break;
                    case '?': Crate(new Vector2(cx, p.y + 0.5f)); break;
                    case 'J': Spring(new Vector2(cx, p.y + 0.5f)); break;
                    case 'O': Objective(new Vector2(cx, p.y + 0.5f)); break;
                    case 'm':
                    {
                        int travel = map.MoverTravel(p.x, p.y);
                        Mover("MovingPlatform", new Vector2(cx, p.y + 0.25f), 3f, new Vector2(travel, 0f), Mathf.Max(1.5f, travel / 3f));
                        break;
                    }
                    case 'u': Mover("Lift", new Vector2(cx, p.y + 0.25f), 3f, new Vector2(0f, 7f), 3f); break;
                    case 'B': Enemy(DataAssets.GetOrCreateCogBeetle(), cx, p.y); break;
                    case 'T': Enemy(DataAssets.GetOrCreateSpringTick(), cx, p.y); break;
                    case 'M': Enemy(DataAssets.GetOrCreateGyroMoth(), cx, p.y + 0.5f); break;
                    case 'K': Enemy(DataAssets.GetOrCreateBoltKnight(), cx, p.y); break;
                    case 'g': Enemy(DataAssets.GetOrCreateGearwatchSentry(), cx, p.y); break;
                    case 'V': Enemy(DataAssets.GetOrCreateGearwatchCaptain(), cx, p.y); break;
                    case 'R': Relic(new Vector2(cx, p.y + 0.5f)); break;
                    case 'Z': Scroll(new Vector2(cx, p.y + 0.5f)); break;
                }
            }
            if (map.HasSecret)
            {
                var min = new Vector2(map.SecretMinX, map.SecretMinY);
                var size = new Vector2(map.SecretMaxX - map.SecretMinX + 1, map.SecretMaxY - map.SecretMinY + 1);
                SecretRoom($"secret_{level.id}", min + size * 0.5f, size);
            }
            for (float x = 12f; x < map.Width - 6; x += 26f) Decor(new Vector2(x, map.Height - 5f), stealth ? 1.6f : 2.6f);

            // --- Hero, flow, UI --------------------------------------------------------------------------
            var startT = new GameObject("StartPoint").transform;
            startT.position = start;
            var hero = SandboxBuilder.BuildHero(nova, _mat, start, roster);
            follow.target = hero.transform;

            var flow = new GameObject("LevelFlow").AddComponent<LevelFlowController>();
            EditorUtil.SetField(flow, "level", level);
            EditorUtil.SetField(flow, "roster", roster);
            EditorUtil.SetField(flow, "player", hero);
            EditorUtil.SetField(flow, "startPoint", startT);
            EditorUtil.SetFloat(flow, "killY", -4f);
            EditorUtil.SetField(flow, "spriteMaterial", _mat);
            EditorUtil.SetField(flow, "shardSprite", _shard);
            EditorUtil.SetField(flow, "healthSprite", PlaceholderArt.HealthCrystal());
            if (stealth)
            {
                var tracker = new GameObject("StealthTracker").AddComponent<StealthTracker>();
                if (_contract != null) tracker.objectiveName = _contract.objectiveName;
            }
            if (_contract != null)
            {
                var mt = new GameObject("MissionTracker").AddComponent<MissionTracker>();
                mt.contractId = _contract.contractId;
                mt.clueId = _contract.clueId ?? "";
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var touch = new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            EditorUtil.SetField(hero.GetComponent<PlayerInputRouter>(), "touchControls", touch);
            new GameObject("HUD").AddComponent<StoryHUD>();
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
            new GameObject("LevelCompleteScreen").AddComponent<LevelCompleteScreen>();

            level.totalShards = _shards + _crates;
            level.totalSecrets = map.HasSecret ? 1 : 0;
            EditorUtility.SetDirty(level);

            string folder = _contract != null ? ContractFolder : Folder;
            string path = $"{folder}/{sceneName}.unity";
            EditorUtil.EnsureFolder(folder);
            EditorSceneManager.SaveScene(scene, path);
            EditorUtil.AddSceneToBuild(path);
            Debug.Log($"[PKR] Built {path}: {map.Width}x{map.Height}, {level.totalShards} shards, {map.Count('g')} guards, {map.Count('R')} relics.");
        }

        static void LoadArt()
        {
            _mat = EditorUtil.UnlitSpriteMaterial();
            _ground = PlaceholderArt.Ground();
            _stone = PlaceholderArt.StoneBlock();
            _oneWay = PlaceholderArt.OneWay();
            _plate = PlaceholderArt.PlatformPlate();
            _spikes = PlaceholderArt.Spikes();
            _flag = PlaceholderArt.CheckpointFlag();
            _gate = PlaceholderArt.GoalGate();
            _gear = PlaceholderArt.DecorGear();
            _crate = PlaceholderArt.ShardCrate();
            _crateEmpty = PlaceholderArt.ShardCrateEmpty();
            _vine = PlaceholderArt.Vine();
            _hay = PlaceholderArt.HayBale();
            _water = PlaceholderArt.WaterTile();
            _spring = PlaceholderArt.SpringPad();
            _ledger = PlaceholderArt.Ledger();
            _shard = PlaceholderArt.StarShard();
            _scroll = PlaceholderArt.CipherScroll();
            _relics = new[] { PlaceholderArt.RelicIdol(), PlaceholderArt.RelicMask(), PlaceholderArt.RelicAmulet() };
        }

        // ---- Pieces ----------------------------------------------------------------------------------

        static GameObject Block(string name, Sprite sprite, AsciiLevel.Rect r) =>
            SandboxBuilder.Block(_root, name, sprite, _mat, new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f), new Vector2(r.w, r.h));

        static SpriteRenderer Art(string name, Sprite sprite, Vector2 center, Vector2 size, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : _root, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = order;
            return sr;
        }

        static void Water(AsciiLevel.Rect r) =>
            Art("Water", _water, new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f - 1f), new Vector2(r.w, r.h + 2f), 12);

        static void Vine(AsciiLevel level, AsciiLevel.Rect r)
        {
            var center = new Vector2(r.x + r.w * 0.5f, r.y + r.h * 0.5f);
            var sr = Art("Vine", _vine, center, new Vector2(r.w, r.h), -2);
            var zone = sr.gameObject.AddComponent<ClimbZone>();
            zone.size = new Vector2(r.w, r.h);
            int topY = r.y + r.h - 1;
            zone.hopDirection = level.IsSolid(r.x + r.w, topY) ? 1 : level.IsSolid(r.x - 1, topY) ? -1 : 1;
        }

        static void Hay(AsciiLevel.Run run)
        {
            var center = new Vector2(run.x + run.length * 0.5f, run.y + 0.65f);
            var sr = Art("HayBale", _hay, center, new Vector2(run.length, 1.3f), 20);
            sr.color = new Color(1f, 1f, 1f, 0.92f); // draws over the hero hiding inside
            var spot = sr.gameObject.AddComponent<HideSpot>();
            spot.size = new Vector2(run.length, 1.6f);
            sr.gameObject.transform.position = center; // HideSpot measures from here
        }

        static void Spikes(AsciiLevel level, AsciiLevel.Run run)
        {
            var go = new GameObject("Spikes") { layer = PKRLayers.Hazard };
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(run.x + run.length * 0.5f, run.y + 0.25f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _spikes;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(run.length, 0.5f);
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 5;
            var cd = go.AddComponent<ContactDamage>();
            cd.team = TeamIds.Enemy;
            cd.size = new Vector2(run.length - 0.1f, 0.4f);
            cd.launchUpward = true;
            cd.inactiveWhileStunned = false;
            cd.hit = new HitData
            {
                damage = 1, pipDamage = 1, baseKnockback = 11f, exposedMultiplier = 1f,
                angleDegrees = 80f, baseHitstunFrames = 16, hitstopFrames = 4, isHeavy = false
            };
            // Spikes need something under them, or a launched hero could fall through the strip.
            if (!level.IsSolid(run.x, run.y - 1))
                SandboxBuilder.Block(_root, "SpikeBed", _stone, _mat, new Vector2(run.x + run.length * 0.5f, run.y - 0.5f), new Vector2(run.length, 1f));
        }

        static void Spot(PickupKind kind, Vector2 pos)
        {
            var go = new GameObject(kind == PickupKind.StarShard ? "Shard" : "Health");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var s = go.AddComponent<PickupSpot>();
            s.kind = kind;
            s.value = 1;
            if (kind == PickupKind.StarShard) _shards++;
        }

        static void Crate(Vector2 center)
        {
            var go = SandboxBuilder.Block(_root, "ShardCrate", _crate, _mat, center, Vector2.one);
            var crate = go.AddComponent<ShardCrate>();
            crate.art = go.GetComponent<SpriteRenderer>();
            crate.emptySprite = _crateEmpty;
            crate.shardSprite = _shard;
            _crates++;
        }

        static void Spring(Vector2 top)
        {
            var go = new GameObject("SpringPad");
            go.transform.SetParent(_root, false);
            go.transform.position = top;
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            art.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = _spring;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 6;
            var pad = go.AddComponent<SpringPad>();
            pad.art = art.transform;
        }

        static void Objective(Vector2 pos)
        {
            var go = new GameObject("Objective_Ledger") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _contract != null ? PlaceholderArt.ContractPrize() : _ledger;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 15;
            go.AddComponent<StealthObjective>().displayName = _contract != null ? _contract.objectiveDisplay : "Gearwright's Ledger";
            var glow = Decor(pos, 0.7f); // a glow behind it so it reads at a distance (goes away with it)
            glow.color = new Color(1f, 0.9f, 0.5f, 0.5f);
            glow.transform.SetParent(go.transform, true);
        }

        static void Relic(Vector2 pos)
        {
            var go = new GameObject("Relic") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = _relics[_relicIndex % _relics.Length];
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 15;
            go.AddComponent<RelicPickup>().relicName = RelicNames[_relicIndex % RelicNames.Length];
            _relicIndex++;
        }

        static void Scroll(Vector2 pos)
        {
            var go = new GameObject("CipherScroll") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _scroll;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 15;
            go.AddComponent<ClueScroll>();
        }

        static void Enemy(EnemyDefinition def, float x, float feetY)
        {
            var go = new GameObject("Spawn_" + def.displayName);
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector2(x, feetY + def.bodySize.y * 0.5f + 0.05f);
            var sp = go.AddComponent<EnemySpawnPoint>();
            sp.enemy = def;
            sp.startDirection = -1;
        }

        static void Mover(string name, Vector2 center, float width, Vector2 travel, float leg)
        {
            var go = new GameObject(name) { layer = PKRLayers.Ground };
            go.transform.SetParent(_root, false);
            go.transform.position = center;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _plate;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, 0.5f);
            if (_mat != null) sr.sharedMaterial = _mat;
            var mp = go.AddComponent<MovingPlatform>();
            mp.travel = travel;
            mp.legDuration = leg;
            mp.pauseAtEnds = 0.6f;
        }

        static void Checkpoint(Vector2 groundPos, int index)
        {
            var go = new GameObject($"Checkpoint_{index + 1}") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = groundPos;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 3f);
            box.offset = new Vector2(0f, 1.5f);
            var flag = new GameObject("Flag");
            flag.transform.SetParent(go.transform, false);
            var sr = flag.AddComponent<SpriteRenderer>();
            sr.sprite = _flag;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 4;
            var cp = go.AddComponent<Checkpoint>();
            cp.index = index;
            cp.flag = sr;
        }

        static void Goal(Vector2 groundPos)
        {
            var go = new GameObject("GoalGate") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = groundPos;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 3.5f);
            box.offset = new Vector2(0f, 1.75f);
            var art = new GameObject("Arch");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = _gate;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 3;
            go.AddComponent<GoalGate>();
        }

        static void SecretRoom(string id, Vector2 center, Vector2 size)
        {
            var go = new GameObject("SecretArea") { layer = PKRLayers.Pickup };
            go.transform.SetParent(_root, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size - new Vector2(0.4f, 0.2f);
            var area = go.AddComponent<SecretArea>();
            area.secretId = id;
            var cover = Art("FakeWall", _stone, center, size, 40, go.transform);
            area.covers.Add(cover);
        }

        static SpriteRenderer Decor(Vector2 pos, float scale)
        {
            var go = new GameObject("Decor_Gear");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _gear;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = -10;
            return sr;
        }
    }
}
