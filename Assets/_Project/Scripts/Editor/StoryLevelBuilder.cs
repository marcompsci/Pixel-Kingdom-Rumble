using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Story Test Level: generates "Sunspire Meadows (Test)", an original layout with five sections:
    ///  A  Start meadow: shards, a Cog Beetle, a raised step.
    ///  B  Floating islands over a pit, a sideways moving platform, a Spring Tick.
    ///  C  Checkpoint 1, spike valley under one-way planks, a health crystal, a Cog Beetle.
    ///  D  Lift shaft: a rising platform beside a tall wall; a hidden room behind a fake wall (the secret).
    ///  E  Checkpoint 2, final run with three enemies, the goal gate.
    /// Re-running rebuilds the scene; tuning lives in the data assets.
    /// </summary>
    public static class StoryLevelBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Story/" + SceneIds.StoryTest + ".unity";

        static Material _mat;
        static Sprite _ground, _stone, _oneWay, _plate, _spikes, _flag, _gate, _gear;
        static Transform _root;

        [MenuItem("PKR/Build Story Test Level", priority = 21)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        /// <summary>Build without the save prompt (used by PKR > Build All Scenes).</summary>
        public static void BuildNoPrompt()
        {

            _shardCount = 0;
            EditorUtil.EnsureLayers();
            var nova = DataAssets.GetOrCreateNova();
            var beetle = DataAssets.GetOrCreateCogBeetle();
            var tick = DataAssets.GetOrCreateSpringTick();
            var level = DataAssets.GetOrCreateSunspireTestLevel();
            _mat = EditorUtil.UnlitSpriteMaterial();
            _ground = PlaceholderArt.Ground();
            _stone = PlaceholderArt.StoneBlock();
            _oneWay = PlaceholderArt.OneWay();
            _plate = PlaceholderArt.PlatformPlate();
            _spikes = PlaceholderArt.Spikes();
            _flag = PlaceholderArt.CheckpointFlag();
            _gate = PlaceholderArt.GoalGate();
            _gear = PlaceholderArt.DecorGear();
            var shard = PlaceholderArt.StarShard();
            var health = PlaceholderArt.HealthCrystal();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Camera ------------------------------------------------------------------------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = level.skyColor;
            camGo.transform.position = new Vector3(0f, 2f, -10f);
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.bounds = new Rect(-7f, -6f, 147f, 22f);

            _root = new GameObject("Level").transform;
            var start = new GameObject("StartPoint").transform;
            start.position = new Vector2(-3f, 1.2f);

            // --- A: Start meadow ---------------------------------------------------------------
            Block("A_Floor", _ground, new Vector2(12f, -1f), new Vector2(36f, 2f));
            Block("A_WallLeft", _stone, new Vector2(-6.5f, 6f), new Vector2(1f, 16f));
            Block("A_Step", _stone, new Vector2(18f, 1f), new Vector2(4f, 2f));
            for (int i = 0; i < 5; i++) Spot(PickupKind.StarShard, new Vector2(2f + i, 1f));
            Spot(PickupKind.StarShard, new Vector2(17f, 3f));
            Spot(PickupKind.StarShard, new Vector2(19f, 3f));
            Enemy(beetle, new Vector2(12f, 0.5f), -1);
            Decor(new Vector2(8f, 4.5f), 2.5f);
            Decor(new Vector2(25f, 6f), 1.6f);

            // --- B: Floating islands over the pit ------------------------------------------------
            Block("B_Island1", _ground, new Vector2(34f, -0.5f), new Vector2(4f, 1f));
            Mover("B_MovingPlatform", new Vector2(39.5f, -0.25f), 3f, new Vector2(8f, 0f), 2.6f);
            Block("B_Island2", _ground, new Vector2(51.5f, -0.5f), new Vector2(5f, 1f));
            Spot(PickupKind.StarShard, new Vector2(41f, 2f));
            Spot(PickupKind.StarShard, new Vector2(43f, 2f));
            Spot(PickupKind.StarShard, new Vector2(45f, 2f));
            Enemy(tick, new Vector2(52f, 0.6f), -1);
            Decor(new Vector2(43f, 7f), 3f);

            // --- C: Checkpoint 1 + spike valley --------------------------------------------------
            Block("C_Floor", _ground, new Vector2(71f, -1f), new Vector2(30f, 2f));
            Checkpoint(new Vector2(58f, 0f), 0);
            Spikes(new Vector2(66f, 0.25f), 6f);
            SandboxBuilder.OneWay(_root, "C_Plank1", _oneWay, _mat, new Vector2(64f, 2.5f), 3f);
            SandboxBuilder.OneWay(_root, "C_Plank2", _oneWay, _mat, new Vector2(68f, 2.5f), 3f);
            Spot(PickupKind.Health, new Vector2(68f, 3.6f));
            for (int i = 0; i < 3; i++) Spot(PickupKind.StarShard, new Vector2(72f + i, 1f));
            Enemy(beetle, new Vector2(78f, 0.5f), -1);

            // --- D: Lift shaft + secret room -----------------------------------------------------
            Block("D_Floor", _ground, new Vector2(93f, -1f), new Vector2(14f, 2f));
            Block("D_TallWall", _stone, new Vector2(100.5f, 4f), new Vector2(1f, 10f));      // top at y 9
            // Lift starts flush with the floor (nothing can get squashed under it) and rises to a top of 7.5.
            Mover("D_Lift", new Vector2(97.5f, 0.25f), 3f, new Vector2(0f, 7f), 3f);
            // Secret room: x 86..92, y 3.5..7, entered from the right; a fake wall hides the inside.
            Block("D_Secret_Floor", _stone, new Vector2(89f, 3f), new Vector2(6f, 1f));
            Block("D_Secret_Ceiling", _stone, new Vector2(89f, 7.5f), new Vector2(8f, 1f));
            Block("D_Secret_WallLeft", _stone, new Vector2(85.5f, 5.25f), new Vector2(1f, 3.5f));
            for (int i = 0; i < 5; i++) Spot(PickupKind.StarShard, new Vector2(87f + i, 4.2f));
            Spot(PickupKind.StarShard, new Vector2(93.5f, 4.6f)); // breadcrumb pointing at the secret
            SecretRoom("secret_lift_room", new Vector2(89f, 5.25f), new Vector2(6f, 3.5f));
            Decor(new Vector2(93f, 10f), 2.2f);

            // --- E: Checkpoint 2 + final run -----------------------------------------------------
            Block("E_Floor", _ground, new Vector2(120f, -1f), new Vector2(38f, 2f));
            Block("E_WallRight", _stone, new Vector2(139.5f, 6f), new Vector2(1f, 16f));
            Checkpoint(new Vector2(104f, 0f), 1);
            Block("E_Block", _stone, new Vector2(116.5f, 1f), new Vector2(3f, 2f));
            Spot(PickupKind.StarShard, new Vector2(110f, 1f));
            Spot(PickupKind.StarShard, new Vector2(116f, 3f));
            Spot(PickupKind.StarShard, new Vector2(117f, 3f));
            Spot(PickupKind.StarShard, new Vector2(124f, 1f));
            Spot(PickupKind.StarShard, new Vector2(125f, 1f));
            Spot(PickupKind.Health, new Vector2(120f, 1f));
            Enemy(beetle, new Vector2(112f, 0.5f), -1);
            Enemy(beetle, new Vector2(122f, 0.5f), 1);
            Enemy(tick, new Vector2(129f, 0.6f), -1);
            Goal(new Vector2(135f, 0f));
            Decor(new Vector2(131f, 7f), 3f);

            // --- Player ----------------------------------------------------------------------------
            var hero = SandboxBuilder.BuildNova(nova, _mat, start.position);
            follow.target = hero.transform;

            // --- Flow, input, HUD -------------------------------------------------------------------
            var flowGo = new GameObject("LevelFlow");
            var flow = flowGo.AddComponent<LevelFlowController>();
            EditorUtil.SetField(flow, "level", level);
            EditorUtil.SetField(flow, "player", hero);
            EditorUtil.SetField(flow, "startPoint", start);
            EditorUtil.SetFloat(flow, "killY", -10f);
            EditorUtil.SetField(flow, "spriteMaterial", _mat);
            EditorUtil.SetField(flow, "shardSprite", shard);
            EditorUtil.SetField(flow, "healthSprite", health);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var touch = new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            EditorUtil.SetField(hero.GetComponent<PlayerInputRouter>(), "touchControls", touch);
            new GameObject("HUD").AddComponent<StoryHUD>();
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
            new GameObject("LevelCompleteScreen").AddComponent<LevelCompleteScreen>();

            // Record totals on the level asset for menus.
            level.totalShards = CountShards();
            level.totalSecrets = 1;
            EditorUtility.SetDirty(level);

            EditorUtil.EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorUtil.AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] Story test level built at {ScenePath} ({level.totalShards} shards, 1 secret). Press Play.");
        }

        // ---- Helpers ---------------------------------------------------------------------------------

        static void Block(string name, Sprite sprite, Vector2 center, Vector2 size) =>
            SandboxBuilder.Block(_root, name, sprite, _mat, center, size);

        static int _shardCount;

        static int CountShards()
        {
            int n = _shardCount;
            _shardCount = 0;
            return n;
        }

        static void Spot(PickupKind kind, Vector2 pos)
        {
            var go = new GameObject(kind == PickupKind.StarShard ? "Shard" : "Health");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var s = go.AddComponent<PickupSpot>();
            s.kind = kind;
            s.value = 1;
            if (kind == PickupKind.StarShard) _shardCount++;
        }

        static void Enemy(EnemyDefinition def, Vector2 pos, int dir)
        {
            var go = new GameObject("Spawn_" + def.displayName);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var sp = go.AddComponent<EnemySpawnPoint>();
            sp.enemy = def;
            sp.startDirection = dir;
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
            mp.pauseAtEnds = 0.5f;
        }

        static void Spikes(Vector2 center, float width)
        {
            var go = new GameObject("Spikes") { layer = PKRLayers.Hazard };
            go.transform.SetParent(_root, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _spikes;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, 0.5f);
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 5;
            var cd = go.AddComponent<ContactDamage>();
            cd.team = TeamIds.Enemy;
            cd.size = new Vector2(width - 0.1f, 0.4f);
            cd.launchUpward = true;
            cd.inactiveWhileStunned = false;
            cd.hit = new PKR.Core.HitData
            {
                damage = 1, pipDamage = 1, baseKnockback = 9f, exposedMultiplier = 1f,
                angleDegrees = 80f, baseHitstunFrames = 16, hitstopFrames = 4, isHeavy = false
            };
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

            // Fake wall: looks solid, has no collider, draws above the player.
            var cover = new GameObject("FakeWall");
            cover.transform.SetParent(go.transform, false);
            var sr = cover.AddComponent<SpriteRenderer>();
            sr.sprite = _stone;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = 40;
            area.covers.Add(sr);
        }

        static void Decor(Vector2 pos, float scale)
        {
            var go = new GameObject("Decor_Gear");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _gear;
            if (_mat != null) sr.sharedMaterial = _mat;
            sr.sortingOrder = -10;
        }
    }
}
