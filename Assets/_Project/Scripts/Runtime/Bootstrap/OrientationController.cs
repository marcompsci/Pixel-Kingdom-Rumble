using System.Collections;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Portrait for menus, landscape for gameplay.
    /// Requires Player Settings > Resolution and Presentation > Default Orientation = Auto Rotation
    /// with Portrait, Landscape Left and Landscape Right allowed (PKR > Configure iOS Player Settings does this).
    /// Has no visible effect in the Editor Game view; test on device or Device Simulator.
    /// </summary>
    public static class OrientationController
    {
        public enum Mode { Portrait, Landscape }

        public static Mode Current { get; private set; } = Mode.Portrait;

        public static void Apply(Mode mode)
        {
            Current = mode;
            if (mode == Mode.Portrait)
            {
                // Force the rotation first, then lock auto-rotate to the allowed set.
                Screen.orientation = ScreenOrientation.Portrait;
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
            }
            else
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
            }
            // Re-enable auto-rotation a moment later: on iOS, overriding the forced rotation in the same frame
            // can cancel it.
            Runner.Instance.Restart(EnableAutoRotationSoon());
        }

        static IEnumerator EnableAutoRotationSoon()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(0.25f);
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        /// <summary>Hidden host for the delayed coroutine (OrientationController itself is static).</summary>
        sealed class Runner : MonoBehaviour
        {
            static Runner _instance;
            Coroutine _running;

            public static Runner Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("[PKR Orientation]") { hideFlags = HideFlags.HideInHierarchy };
                        DontDestroyOnLoad(go);
                        _instance = go.AddComponent<Runner>();
                    }
                    return _instance;
                }
            }

            public void Restart(IEnumerator routine)
            {
                if (_running != null) StopCoroutine(_running);
                _running = StartCoroutine(routine);
            }
        }

        public static void ApplyForScene(string sceneName) =>
            Apply(SceneIds.IsGameplay(sceneName) ? Mode.Landscape : Mode.Portrait);
    }
}
