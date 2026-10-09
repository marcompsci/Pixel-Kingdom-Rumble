using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// PKR > Build Versus Scenes: 04_VersusSelect (the fighter grid, portrait) and VS_Dojo (a flat, walled 1-on-1
    /// stage with a lantern-lit backdrop, landscape). Fighters are spawned at runtime from the select screen.
    /// Part of PKR > Build All Scenes.
    /// </summary>
    public static class VersusSceneBuilder
    {
        public const string SelectPath = "Assets/_Project/Scenes/" + SceneIds.VersusSelect + ".unity";
        public const string StagePath = "Assets/_Project/Scenes/Versus/" + SceneIds.VersusStage + ".unity";
        static readonly Color DojoSky = new Color32(52, 34, 58, 255);
        static readonly Color MenuSky = new Color32(36, 40, 74, 255);

        [MenuItem("PKR/Build Versus Scenes", priority = 25)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        public static void BuildNoPrompt()
        {
            BuildSelect();
            BuildStage();
        }

        static void BuildSelect()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MenuSky;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var ui = new GameObject("VersusSelect").AddComponent<VersusSelectUI>();
            EditorUtil.SetField(ui, "roster", DataAssets.GetOrCreateRoster());
            EditorSceneManager.SaveScene(scene, SelectPath);
            EditorUtil.AddSceneToBuild(SelectPath);
        }

        static void BuildStage()
        {
            EditorUtil.EnsureLayers();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var roster = DataAssets.GetOrCreateRoster();
            var nova = DataAssets.GetOrCreateNova();
            var mat = EditorUtil.UnlitSpriteMaterial();
            var stone = PlaceholderArt.StoneBlock();
            var ground = PlaceholderArt.Ground();
            var gear = PlaceholderArt.DecorGear();
            var plate = PlaceholderArt.PlatformPlate();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = DojoSky;
            camGo.transform.position = new Vector3(0f, 3f, -10f);
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.bounds = new Rect(-13f, -2f, 26f, 16f);

            var stage = new GameObject("Stage").transform;
            // Floor x -12..12 with its top at y = 0, and walls you can't leave through.
            SandboxBuilder.Block(stage, "Floor", ground, mat, new Vector2(0f, -1f), new Vector2(26f, 2f));
            SandboxBuilder.Block(stage, "Wall_Left", stone, mat, new Vector2(-12.75f, 5f), new Vector2(1.5f, 12f));
            SandboxBuilder.Block(stage, "Wall_Right", stone, mat, new Vector2(12.75f, 5f), new Vector2(1.5f, 12f));
            SandboxBuilder.Block(stage, "Ceiling", stone, mat, new Vector2(0f, 11.5f), new Vector2(27f, 1f));

            // Backdrop: a big gear "sun" and brass beams, behind everything (decor only, no colliders).
            Decor(stage, gear, mat, new Vector2(0f, 6.5f), 5f, new Color(1f, 0.85f, 0.6f, 0.55f));
            Decor(stage, gear, mat, new Vector2(-8f, 7.5f), 2.2f, new Color(1f, 1f, 1f, 0.35f));
            Decor(stage, gear, mat, new Vector2(8f, 7.5f), 2.2f, new Color(1f, 1f, 1f, 0.35f));
            for (int i = -2; i <= 2; i++) Beam(stage, plate, mat, new Vector2(i * 5f, 3.5f));

            var matchGo = new GameObject("VersusMatch");
            var match = matchGo.AddComponent<VersusController>();
            EditorUtil.SetField(match, "roster", roster);
            EditorUtil.SetField(match, "fallbackHero", nova);
            EditorUtil.SetField(match, "spriteMaterial", mat);
            EditorUtil.SetField(match, "arenaCamera", follow);
            var so = new SerializedObject(match);
            so.FindProperty("playerSpawn").vector2Value = new Vector2(-4f, 1f);
            so.FindProperty("opponentSpawn").vector2Value = new Vector2(4f, 1f);
            so.FindProperty("stageRect").rectValue = new Rect(-11.5f, -1f, 23f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            var hud = new GameObject("VersusHUD").AddComponent<VersusHUD>();
            EditorUtil.SetField(hud, "match", match);
            new GameObject("PauseMenu").AddComponent<PauseMenu>();

            EditorUtil.EnsureFolder(Path.GetDirectoryName(StagePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, StagePath);
            EditorUtil.AddSceneToBuild(StagePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PKR] Versus built: {SelectPath} and {StagePath}.");
        }

        static void Decor(Transform parent, Sprite sprite, Material mat, Vector2 pos, float scale, Color tint)
        {
            var go = new GameObject("Decor");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = -20;
        }

        static void Beam(Transform parent, Sprite sprite, Material mat, Vector2 pos)
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(0.5f, 7f);
            sr.color = new Color(1f, 1f, 1f, 0.25f);
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingOrder = -15;
        }
    }
}
