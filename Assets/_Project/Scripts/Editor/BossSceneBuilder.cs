using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Boss Test (SQ_ClockworkWarden_Test): the Tickworks engine hall where the Clockwork Warden waits.
    /// A walled arena 24 units wide with a one-way ledge on each side (health crystals on them), the hero on the left,
    /// and the Warden's parts built at runtime by ClockworkWarden. Reached from the Sunspire Meadows results NEXT button.
    /// </summary>
    public static class BossSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Story/" + SceneIds.BossTest + ".unity";
        const float Left = -12f, Right = 12f, Floor = 0f, Ceiling = 10f;

        [MenuItem("PKR/Build Boss Test", priority = 24)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        public const string SovereignScenePath = "Assets/_Project/Scenes/Story/" + SceneIds.FourfoldSovereign + ".unity";

        public const string OrreryScenePath = "Assets/_Project/Scenes/Story/" + SceneIds.OrrerySentinel + ".unity";

        public static void BuildNoPrompt()
        {
            BuildArena(ScenePath, false);
            BuildArena(SovereignScenePath, true);
            BuildArena(OrreryScenePath, true, orrery: true);
        }

        /// <summary>
        /// One boss arena. sovereign = the Shattered Realms finale: the same fight engine as the Warden but faster
        /// (tempo 1.25), tougher (36 HP), two extra cogs per volley, and its body changes realm color each phase.
        /// </summary>
        static void BuildArena(string scenePath, bool sovereign, bool orrery = false)
        {
            EditorUtil.EnsureLayers();
            // New scene FIRST: opening a scene unloads assets loaded before it, and Unity 6.6 then throws
            // "has been destroyed" when a builder still holds them (seen in the first batch build).
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var roster = DataAssets.GetOrCreateRoster();
            var nova = DataAssets.GetOrCreateNova();
            var level = orrery ? DataAssets.GetOrCreateOrreryLevel()
                      : sovereign ? DataAssets.GetOrCreateSovereignLevel() : DataAssets.GetOrCreateWardenLevel();
            if (!sovereign) DataAssets.GetOrCreateSunspireTestLevel(); // makes sure Sunspire's NEXT points here
            var mat = EditorUtil.UnlitSpriteMaterial();
            var stone = PlaceholderArt.StoneBlock();
            var ground = PlaceholderArt.Ground();
            var oneWay = PlaceholderArt.OneWay();
            var plate = PlaceholderArt.PlatformPlate();
            var gear = PlaceholderArt.DecorGear();


            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = level.skyColor;
            camGo.transform.position = new Vector3(0f, 4.5f, -10f);
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.bounds = new Rect(Left - 1f, Floor - 2f, Right - Left + 2f, Ceiling + 6f);

            string theme = orrery ? "astral" : sovereign ? "shattered" : "foundry";
            PixelArt.Backdrop(theme, level.skyColor, Floor, mat, new Vector2(0f, 4.5f));
            PixelArt.PostFX(cam);

            var root = new GameObject("Arena").transform;
            if (PixelArt.HasTiles(theme))
            {
                int l = Mathf.RoundToInt(Left), r = Mathf.RoundToInt(Right), f = Mathf.RoundToInt(Floor), c = Mathf.RoundToInt(Ceiling);
                PixelArt.Ground(root, "Floor", theme, mat, l - 1, f - 2, r - l + 2, 2);
                PixelArt.Ground(root, "Wall_Left", theme, mat, l - 1, f - 2, 1, c + 4, null, stone: true);
                PixelArt.Ground(root, "Wall_Right", theme, mat, r, f - 2, 1, c + 4, null, stone: true);
                PixelArt.Ground(root, "Ceiling", theme, mat, l - 1, c + 2, r - l + 2, 1, null, stone: true);
                oneWay = PixelArt.Tile(theme, "plank") ?? oneWay;
            }
            else
            {
                var floorGo = SandboxBuilder.Block(root, "Floor", ground, mat, new Vector2(0f, Floor - 1f), new Vector2(Right - Left + 2f, 2f));
                if (sovereign) floorGo.GetComponent<SpriteRenderer>().color = AsciiLevelBuilder.TintFor("Shattered Crossing");
                SandboxBuilder.Block(root, "Wall_Left", stone, mat, new Vector2(Left - 0.5f, Ceiling * 0.5f), new Vector2(1f, Ceiling + 4f));
                SandboxBuilder.Block(root, "Wall_Right", stone, mat, new Vector2(Right + 0.5f, Ceiling * 0.5f), new Vector2(1f, Ceiling + 4f));
                SandboxBuilder.Block(root, "Ceiling", stone, mat, new Vector2(0f, Ceiling + 2.5f), new Vector2(Right - Left + 2f, 1f));
            }
            SandboxBuilder.OneWay(root, "Ledge_Left", oneWay, mat, new Vector2(Left + 2.5f, 2.5f), 3.5f);
            SandboxBuilder.OneWay(root, "Ledge_Right", oneWay, mat, new Vector2(Right - 2.5f, 2.5f), 3.5f);
            foreach (var x in new[] { Left + 2.5f, Right - 2.5f })
            {
                var spot = new GameObject("Health").AddComponent<PickupSpot>();
                spot.transform.SetParent(root, false);
                spot.transform.position = new Vector2(x, 3.6f);
                spot.kind = PickupKind.Health;
                spot.value = 1;
            }

            var start = new GameObject("StartPoint").transform;
            start.position = new Vector2(Left + 2f, 1.2f);

            var bossGo = new GameObject(orrery ? "OrrerySentinel" : sovereign ? "FourfoldSovereign" : "ClockworkWarden");
            bossGo.AddComponent<Damageable>();
            var warden = bossGo.AddComponent<ClockworkWarden>();
            EditorUtil.SetFloat(warden, "arenaLeft", Left);
            EditorUtil.SetFloat(warden, "arenaRight", Right);
            EditorUtil.SetFloat(warden, "floorY", Floor);
            EditorUtil.SetFloat(warden, "ceilingY", Ceiling);
            EditorUtil.SetField(warden, "spriteMaterial", mat);
            EditorUtil.SetField(warden, "bodySprite", PlaceholderArt.WardenBody());
            PixelArt.ApplyBossFrames(warden, "bodyFrames", "warden");
            EditorUtil.SetField(warden, "gearSprite", gear);
            EditorUtil.SetField(warden, "pistonSprite", plate);
            if (orrery)
            {
                // World 4 finale: the same engine again, faster and tougher still, in starlight colours.
                var so = new SerializedObject(warden);
                so.FindProperty("bossName").stringValue = "THE ORRERY SENTINEL";
                so.FindProperty("tempo").floatValue = 1.4f;
                so.FindProperty("extraCogs").intValue = 3;
                so.FindProperty("maxHealth").intValue = 44;
                var tints = so.FindProperty("phaseTints");
                tints.arraySize = 3;
                tints.GetArrayElementAtIndex(0).colorValue = new Color32(150, 180, 255, 255);
                tints.GetArrayElementAtIndex(1).colorValue = new Color32(200, 150, 255, 255);
                tints.GetArrayElementAtIndex(2).colorValue = new Color32(255, 220, 130, 255);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (sovereign)
            {
                var so = new SerializedObject(warden);
                so.FindProperty("bossName").stringValue = "THE FOURFOLD SOVEREIGN";
                so.FindProperty("tempo").floatValue = 1.25f;
                so.FindProperty("extraCogs").intValue = 2;
                so.FindProperty("maxHealth").intValue = 36;
                var tints = so.FindProperty("phaseTints");
                var realm = DataAssets.ShatteredRealmSections;
                tints.arraySize = 3;
                tints.GetArrayElementAtIndex(0).colorValue = realm[0].color; // ember
                tints.GetArrayElementAtIndex(1).colorValue = realm[1].color; // sky
                tints.GetArrayElementAtIndex(2).colorValue = realm[3].color; // toy (the last stand)
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var hero = SandboxBuilder.BuildHero(nova, mat, start.position, roster);
            follow.target = hero.transform;

            var flow = new GameObject("LevelFlow").AddComponent<LevelFlowController>();
            EditorUtil.SetField(flow, "level", level);
            EditorUtil.SetField(flow, "roster", roster);
            EditorUtil.SetField(flow, "player", hero);
            EditorUtil.SetField(flow, "startPoint", start);
            EditorUtil.SetFloat(flow, "killY", -10f);
            EditorUtil.SetField(flow, "spriteMaterial", mat);
            EditorUtil.SetField(flow, "shardSprite", PlaceholderArt.StarShard());
            EditorUtil.SetField(flow, "healthSprite", PlaceholderArt.HealthCrystal());

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var touch = new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            EditorUtil.SetField(hero.GetComponent<PlayerInputRouter>(), "touchControls", touch);
            new GameObject("HUD").AddComponent<StoryHUD>();
            var bossHud = new GameObject("BossHUD").AddComponent<BossHUD>();
            EditorUtil.SetField(bossHud, "boss", warden);
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
            new GameObject("LevelCompleteScreen").AddComponent<LevelCompleteScreen>();

            level.totalShards = 0;
            level.totalSecrets = 0;
            EditorUtility.SetDirty(level);

            EditorUtil.EnsureFolder(Path.GetDirectoryName(scenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtil.AddSceneToBuild(scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] Boss arena built at {scenePath}.");
        }
    }
}
