using UnityEngine;
using UnityEngine.SceneManagement;

namespace PKR
{
    /// <summary>
    /// Creates the persistent service object before the first scene loads, so ANY scene can be
    /// played directly from the Editor (no need to start from 00_Boot).
    /// </summary>
    public static class GameBootstrap
    {
        public const int TargetFrameRate = 60;
        static GameObject _root;

        // Supports "Enter Play Mode Options" with domain reload disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _root = null;
            Services.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateServices()
        {
            if (_root != null) return;

            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.vSyncCount = 0; // targetFrameRate is ignored on desktop when vSync is on
            Time.fixedDeltaTime = 1f / TargetFrameRate; // physics in lockstep with rendering: smoother platforming

            _root = new GameObject("[PKR Services]");
            Object.DontDestroyOnLoad(_root);

            // Order matters: settings + save first, others read them on Awake/Start.
            Services.Settings = _root.AddComponent<SettingsService>();
            Services.Save = _root.AddComponent<SaveService>();
            Services.State = _root.AddComponent<GameStateManager>();
            Services.Scenes = _root.AddComponent<SceneLoader>();
            Services.Audio = _root.AddComponent<AudioService>();
            Services.Haptics = _root.AddComponent<HapticsService>();

            Services.Settings.Load();
            Services.Save.Load();
            Services.Audio.ApplySettings(Services.Settings.Data);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ConfigureFirstScene()
        {
            string scene = SceneManager.GetActiveScene().name;
            OrientationController.ApplyForScene(scene);
            Services.State.SetState(SceneIds.IsGameplay(scene) ? GameState.Playing : GameState.Menu);
        }
    }
}
