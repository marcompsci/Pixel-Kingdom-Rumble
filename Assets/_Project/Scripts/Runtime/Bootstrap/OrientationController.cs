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
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        public static void ApplyForScene(string sceneName) =>
            Apply(SceneIds.IsGameplay(sceneName) ? Mode.Landscape : Mode.Portrait);
    }
}
