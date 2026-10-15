using System.Collections;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// 00_Boot: shows the title for a moment while services start, then opens the main menu.
    /// (Services are created before any scene loads, so any scene can still be played directly in the Editor.)
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        public float minimumSeconds = 0.8f;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "BootCanvas", 10, landscape: false, out _);
            var title = UIFactory.Label(safe, "PIXEL KINGDOM\nRUMBLE", 110, TextAnchor.MiddleCenter, 400f, title: true);
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 400f);
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(minimumSeconds);
            string start = StartSceneFromArgs(System.Environment.GetCommandLineArgs()) ?? StartSceneFromEnv();
            if (start != null && start.StartsWith(SceneIds.ContractPrefix))
            {
                GameSession.Mode = SessionMode.Contracts;
                GameSession.ContractScene = start;
            }
            if (Services.Scenes != null) Services.Scenes.Load(start ?? SceneIds.MainMenu);
            else Debug.LogError("[BootLoader] Services missing; GameBootstrap did not run.");
        }

        /// <summary>
        /// App Store screenshot runs launch the app with "-pkrScene SCENE" (e.g. xcrun simctl launch ... -pkrScene
        /// MS_FirstLight) to open straight into one scene. Only scenes in the build are accepted.
        /// </summary>
        /// <summary>Same as -pkrScene, from the PKR_SCENE environment variable (simctl launch passes SIMCTL_CHILD_PKR_SCENE).</summary>
        public static string StartSceneFromEnv() => Accept(System.Environment.GetEnvironmentVariable("PKR_SCENE"));

        /// <summary>
        /// Screenshot runs can also open a panel: "SCENE@panel" (e.g. 01_MainMenu@shop, SQ_SunspireHeights@pause).
        /// The menu (or pause menu) reads it once with TakeShotPanel.
        /// </summary>
        public static string ShotPanel { get; private set; }

        public static string TakeShotPanel(string panel)
        {
            if (string.IsNullOrEmpty(ShotPanel) || ShotPanel != panel) return null;
            ShotPanel = null;
            return panel;
        }

        static string Accept(string spec)
        {
            if (string.IsNullOrEmpty(spec)) return null;
            string scene = spec, panel = null;
            int at = spec.IndexOf('@');
            if (at > 0) { scene = spec.Substring(0, at); panel = spec.Substring(at + 1).ToLowerInvariant(); }
            if (!Application.CanStreamedLevelBeLoaded(scene)) return null;
            ShotPanel = panel;
            return scene;
        }

        public static string StartSceneFromArgs(string[] args)
        {
            if (args == null) return null;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-pkrScene" && Accept(args[i + 1]) is string scene)
                    return scene;
            return null;
        }
    }
}
