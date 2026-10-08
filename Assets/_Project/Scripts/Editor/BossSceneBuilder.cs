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

        public static void BuildNoPrompt()
        {
            EditorUtil.EnsureLayers();
            // New scene FIRST: opening a scene unloads assets loaded before it, and Unity 6.6 then throws
            // "has been destroyed" when a builder still holds them (seen in the first batch build).
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var roster = DataAssets.GetOrCreateRoster();
            var nova = DataAssets.GetOrCreateNova();
            var level = DataAssets.GetOrCreateWardenLevel();
            DataAssets.GetOrCreateSunspireTestLevel(); // makes sure Sunspire's NEXT points here
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

            var root = new GameObject("Arena").transform;
            SandboxBuilder.Block(root, "Floor", ground, mat, new Vector2(0f, Floor - 1f), new Vector2(Right - Left + 2f, 2f));
            SandboxBuilder.Block(root, "Wall_Left", stone, mat, new Vector2(Left - 0.5f, Ceiling * 0.5f), new Vector2(1f, Ceiling + 4f));
            SandboxBuilder.Block(root, "Wall_Right", stone, mat, new Vector2(Right + 0.5f, Ceiling * 0.5f), new Vector2(1f, Ceiling + 4f));
            SandboxBuilder.Block(root, "Ceiling", stone, mat, new Vector2(0f, Ceiling + 2.5f), new Vector2(Right - Left + 2f, 1f));
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

            var bossGo = new GameObject("ClockworkWarden");
            bossGo.AddComponent<Damageable>();
            var warden = bossGo.AddComponent<ClockworkWarden>();
            EditorUtil.SetFloat(warden, "arenaLeft", Left);
            EditorUtil.SetFloat(warden, "arenaRight", Right);
            EditorUtil.SetFloat(warden, "floorY", Floor);
            EditorUtil.SetFloat(warden, "ceilingY", Ceiling);
            EditorUtil.SetField(warden, "spriteMaterial", mat);
            EditorUtil.SetField(warden, "bodySprite", gear);
            EditorUtil.SetField(warden, "gearSprite", gear);
            EditorUtil.SetField(warden, "pistonSprite", plate);

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

            EditorUtil.EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorUtil.AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] Clockwork Warden arena built at {ScenePath}. Press Play, or reach it from Sunspire Meadows > NEXT.");
        }
    }
}
