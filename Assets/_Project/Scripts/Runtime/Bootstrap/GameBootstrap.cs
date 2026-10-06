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

            ConfigurePhysicsLayers();

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

        /// <summary>
        /// Bodies that should pass through each other. Attack queries ignore this matrix, so hits still land.
        /// Hurtboxes are query-only triggers and never need physics contacts.
        /// </summary>
        public static void ConfigurePhysicsLayers()
        {
            Physics2D.IgnoreLayerCollision(PKRLayers.Player, PKRLayers.Enemy, true);
            Physics2D.IgnoreLayerCollision(PKRLayers.Player, PKRLayers.Player, true);
            Physics2D.IgnoreLayerCollision(PKRLayers.Enemy, PKRLayers.Enemy, true);
            for (int i = 0; i < 32; i++) Physics2D.IgnoreLayerCollision(PKRLayers.Hurtbox, i, true);
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
