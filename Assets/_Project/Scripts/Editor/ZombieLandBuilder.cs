using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PKR.EditorTools
{
    /// <summary>
    /// Builds EV_ZombieLand: a near-empty scene (camera, touch controls, HUD, pause menu and the ZLGame controller).
    /// The whole city is generated at runtime from Resources/PKR/ZombieLand/city.json.
    /// </summary>
    public static class ZombieLandBuilder
    {
        [MenuItem("PKR/Build Zombie Land", priority = 46)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildNoPrompt();
        }

        public static void BuildNoPrompt()
        {
            EditorUtil.EnsureLayers();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mat = EditorUtil.UnlitSpriteMaterial();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(20, 22, 30, 255);
            camGo.transform.position = new Vector3(20f, 20f, -10f);
            camGo.AddComponent<CameraFollowTopDown>();
            PixelArt.PostFX(cam);

            var game = new GameObject("ZombieLand").AddComponent<ZLGame>();
            game.material = mat;
            game.roster = DataAssets.GetOrCreateRoster();
            game.shambler = DataAssets.GetOrCreateZombieShambler();
            game.runner = DataAssets.GetOrCreateZombieRunner();
            game.bloater = DataAssets.GetOrCreateZombieBloater();
            game.spitter = DataAssets.GetOrCreateZombieSpitter();
            game.brute = DataAssets.GetOrCreateZombieBrute();
            var survivors = new List<Sprite>();
            foreach (var id in new[] { "rosie_crumb", "fitz_postwell", "lila_sprout", "benji_bolts", "mayor_opal" })
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArt.Root}/NPCs/{id}/{id}_idle_0.png");
                if (s != null) survivors.Add(s);
            }
            game.survivorFrames = survivors.ToArray();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            new GameObject("TouchControls").AddComponent<TouchControlsUI>();
            new GameObject("HUD").AddComponent<ZLHUD>();
            new GameObject("PauseMenu").AddComponent<PauseMenu>();

            string path = $"{AsciiLevelBuilder.EventFolder}/{SceneIds.ZombieLand}.unity";
            EditorUtil.EnsureFolder(AsciiLevelBuilder.EventFolder);
            EditorSceneManager.SaveScene(scene, path);
            EditorUtil.AddSceneToBuild(path);
            Debug.Log($"[PKR] Built {path}");
        }
    }
}
