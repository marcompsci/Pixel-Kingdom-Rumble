using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Arena Test: the Skyforge Arena (AC_Skyforge_Test). An original, symmetrical stage:
    /// two main stone platforms joined by a retracting brass bridge (the stage event), two side planks and a
    /// top plank you can jump through, floating over an open sky with blast zones on every side.
    /// Fighters are spawned at runtime from the setup screen.
    /// </summary>
    [InitializeOnLoad]
    public static class ArenaSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Arena/" + SceneIds.ArenaTest + ".unity";
        static readonly Color ForgeSky = new Color32(64, 48, 96, 255);

        // Join PKR > Build All Scenes.
        static ArenaSceneBuilder() => MenuSceneBuilder.ArenaHook = BuildNoPrompt;

        [MenuItem("PKR/Build Arena Test", priority = 23)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        public static void BuildNoPrompt()
        {
            EditorUtil.EnsureLayers();
            var roster = DataAssets.GetOrCreateRoster();
            var nova = DataAssets.GetOrCreateNova();
            var mat = EditorUtil.UnlitSpriteMaterial();
            var stone = PlaceholderArt.StoneBlock();
            var oneWay = PlaceholderArt.OneWay();
            var plate = PlaceholderArt.PlatformPlate();
            var gear = PlaceholderArt.DecorGear();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ForgeSky;
            camGo.transform.position = new Vector3(0f, 2.5f, -10f);
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.bounds = new Rect(-24f, -10f, 48f, 30f);

            var stage = new GameObject("Stage").transform;
            // Main platforms: x -9..-2 and 2..9, top at y = 0.
            SandboxBuilder.Block(stage, "Main_Left", stone, mat, new Vector2(-5.5f, -0.75f), new Vector2(7f, 1.5f));
            SandboxBuilder.Block(stage, "Main_Right", stone, mat, new Vector2(5.5f, -0.75f), new Vector2(7f, 1.5f));
            // Underside pylons for silhouette (decor only).
            Decor(stage, gear, mat, new Vector2(-5.5f, -3f), 2.4f);
            Decor(stage, gear, mat, new Vector2(5.5f, -3f), 2.4f);
            Decor(stage, gear, mat, new Vector2(0f, 9f), 4f);

            // The bridge: x -2..2, flush with the main platforms.
            var bridgeGo = new GameObject("Skyforge_Bridge") { layer = PKRLayers.Ground };
            bridgeGo.transform.SetParent(stage, false);
            bridgeGo.transform.position = new Vector2(0f, -0.25f);
            var bridgeBox = bridgeGo.AddComponent<BoxCollider2D>();
            bridgeBox.size = new Vector2(4f, 0.5f);
            var bridgeArt = bridgeGo.AddComponent<SpriteRenderer>();
            bridgeArt.sprite = plate;
            bridgeArt.drawMode = SpriteDrawMode.Tiled;
            bridgeArt.size = new Vector2(4f, 0.5f);
            if (mat != null) bridgeArt.sharedMaterial = mat;
            var bridge = bridgeGo.AddComponent<SkyforgeBridge>();
            bridge.art = bridgeArt;

            SandboxBuilder.OneWay(stage, "Plank_Left", oneWay, mat, new Vector2(-5.5f, 2.75f), 4f);
            SandboxBuilder.OneWay(stage, "Plank_Right", oneWay, mat, new Vector2(5.5f, 2.75f), 4f);
            SandboxBuilder.OneWay(stage, "Plank_Top", oneWay, mat, new Vector2(0f, 5.5f), 4f);

            var spawns = new Transform[4];
            Vector2[] spawnPos = { new Vector2(-6.5f, 1.2f), new Vector2(6.5f, 1.2f), new Vector2(-5.5f, 4.2f), new Vector2(5.5f, 4.2f) };
            for (int i = 0; i < 4; i++)
            {
                spawns[i] = new GameObject($"Spawn_{i + 1}").transform;
                spawns[i].SetParent(stage, false);
                spawns[i].position = spawnPos[i];
            }
            var respawn = new GameObject("Respawn").transform;
            respawn.SetParent(stage, false);
            respawn.position = new Vector2(0f, 8f);

            var matchGo = new GameObject("ArenaMatch");
            var match = matchGo.AddComponent<ArenaMatchController>();
            EditorUtil.SetField(match, "roster", roster);
            EditorUtil.SetField(match, "fallbackHero", nova);
            EditorUtil.SetField(match, "spriteMaterial", mat);
            EditorUtil.SetField(match, "respawnPoint", respawn);
            EditorUtil.SetField(match, "bridge", bridge);
            EditorUtil.SetField(match, "arenaCamera", follow);
            var so = new SerializedObject(match);
            var sp = so.FindProperty("spawnPoints");
            sp.arraySize = spawns.Length;
            for (int i = 0; i < spawns.Length; i++) sp.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            so.FindProperty("blastZone").rectValue = new Rect(-22f, -12f, 44f, 32f);
            so.FindProperty("stageRect").rectValue = new Rect(-9f, -1f, 18f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            var hud = new GameObject("ArenaHUD").AddComponent<ArenaHUD>();
            EditorUtil.SetField(hud, "match", match);
            var setup = new GameObject("ArenaSetup").AddComponent<ArenaSetupUI>();
            EditorUtil.SetField(setup, "match", match);
            var results = new GameObject("ArenaResults").AddComponent<ArenaResultsScreen>();
            EditorUtil.SetField(results, "match", match);
            EditorUtil.SetField(results, "setup", setup);
            new GameObject("PauseMenu").AddComponent<PauseMenu>();

            EditorUtil.EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorUtil.AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] Skyforge Arena built at {ScenePath}. Play it from 00_Boot > Arena Clash, or press Play here.");
        }

        static void Decor(Transform parent, Sprite sprite, Material mat, Vector2 pos, float scale)
        {
            var go = new GameObject("Decor_Gear");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = -10;
        }
    }
}
