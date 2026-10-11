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
        public static string StartSceneFromEnv()
        {
            string s = System.Environment.GetEnvironmentVariable("PKR_SCENE");
            return !string.IsNullOrEmpty(s) && Application.CanStreamedLevelBeLoaded(s) ? s : null;
        }

        public static string StartSceneFromArgs(string[] args)
        {
            if (args == null) return null;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-pkrScene" && !string.IsNullOrEmpty(args[i + 1]) && Application.CanStreamedLevelBeLoaded(args[i + 1]))
                    return args[i + 1];
            return null;
        }
    }
}
