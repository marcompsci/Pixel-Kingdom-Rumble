using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Diagnostic scenes (DBG_*) that isolate one piece of the art pipeline each, for simulator screenshots.
    /// Built only when Logs/debug_scenes exists; otherwise any DBG_ scenes are removed from Build Settings so
    /// they never ship.
    /// </summary>
    public static class DebugScenes
    {
        const string Folder = "Assets/_Project/Scenes/Debug";
        const string Flag = "Logs/debug_scenes";

        public static void BuildOrClean()
        {
            if (File.Exists(Flag)) Build();
            else Clean();
        }

        static void Clean()
        {
            var keep = EditorBuildSettings.scenes.Where(s => !s.path.Contains("/Debug/DBG_")).ToArray();
            if (keep.Length != EditorBuildSettings.scenes.Length) EditorBuildSettings.scenes = keep;
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        static void Build()
        {
            EditorUtil.EnsureFolder(Folder);
            var sky = new Color(0.59f, 0.78f, 1f);
            Make("DBG_A_PostFXOnly", cam => { PixelArt.PostFX(cam); Ground(); });
            Make("DBG_B_MeadowAll", cam => { PixelArt.PostFX(cam); PixelArt.Backdrop("meadow", sky, 0f, Mat, Vector2.zero); Ground(); });
            Make("DBG_C_MeadowNoFX", cam => { PixelArt.Backdrop("meadow", sky, 0f, Mat, Vector2.zero); Ground(); });
            Make("DBG_D_CityAll", cam => { PixelArt.PostFX(cam); PixelArt.Backdrop("city", sky, 0f, Mat, Vector2.zero); Ground(); });
            foreach (var layer in new[] { "far", "mid", "near", "clouds" })
                Make("DBG_E_Meadow_" + layer, cam => { Layer("meadow", layer); Ground(); });
            Make("DBG_F_SkyOnly", cam => { new GameObject("Sky").AddComponent<SkyGradient>().material = Mat; Ground(); });
            Make("DBG_G_GroundOnly", cam => Ground());
        }

        static Material Mat => EditorUtil.UnlitSpriteMaterial();

        static void Layer(string theme, string key)
        {
            var go = new GameObject("Layer_" + key);
            var l = go.AddComponent<ParallaxLayer>();
            l.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PixelArt.Root}/Backgrounds/{theme}_{key}.png");
            l.material = Mat;
            l.factorX = 0.5f;
            l.factorY = 0.5f;
            l.sortingOrder = -80;
        }

        static void Ground()
        {
            var root = new GameObject("Level").transform;
            PixelArt.Ground(root, "Ground", "meadow", Mat, -12, -4, 24, 2);
        }

        static void Make(string name, System.Action<Camera> fill)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.9f, 0.2f, 0.6f); // magenta: obvious when nothing else draws
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            fill(cam);
            string path = $"{Folder}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorUtil.AddSceneToBuild(path);
        }
    }
}
