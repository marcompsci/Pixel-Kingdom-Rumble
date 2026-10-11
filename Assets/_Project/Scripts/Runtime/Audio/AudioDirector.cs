using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PKR
{
    /// <summary>
    /// Lives on the bootstrap object. Picks the music for each scene (story, stealth contracts, versus/arena) and
    /// plays the sounds for game-wide events (checkpoints, secrets, stealth alarms, level clear).
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EventBus<CheckpointActivated>.Subscribe(OnCheckpoint);
            EventBus<SecretFound>.Subscribe(OnSecret);
            EventBus<StealthNotice>.Subscribe(OnStealth);
            EventBus<LevelCompleted>.Subscribe(OnCompleted);
            EventBus<ArenaKO>.Subscribe(OnArenaKO);
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventBus<CheckpointActivated>.Unsubscribe(OnCheckpoint);
            EventBus<SecretFound>.Unsubscribe(OnSecret);
            EventBus<StealthNotice>.Unsubscribe(OnStealth);
            EventBus<LevelCompleted>.Unsubscribe(OnCompleted);
            EventBus<ArenaKO>.Unsubscribe(OnArenaKO);
        }

        void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        /// <summary>Which music loop a scene gets (null keeps whatever is playing).</summary>
        public static string MusicFor(string scene)
        {
            if (string.IsNullOrEmpty(scene) || scene == SceneIds.Boot) return null;
            if (scene.StartsWith(SceneIds.ContractPrefix) || scene == SceneIds.MissionBoard ||
                scene == SceneIds.RooftopRun || scene == SceneIds.NightMarketHeist) return Sfx.MusicStealth;
            if (scene.StartsWith("VS_") || scene.StartsWith("AC_") || scene == SceneIds.VersusSelect) return Sfx.MusicVersus;
            return Sfx.MusicStory;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || Services.Audio == null) return;
            var music = MusicFor(scene.name);
            if (music != null) Services.Audio.PlayMusic(Sfx.Clip(music));
        }

        void OnCheckpoint(CheckpointActivated e) => Sfx.Play(Sfx.Checkpoint, 0.8f, 0f);
        void OnSecret(SecretFound e) => Sfx.Play(Sfx.Secret, 0.9f, 0f);
        void OnCompleted(LevelCompleted e) => Sfx.Play(Sfx.Victory, 0.9f, 0f);
        void OnArenaKO(ArenaKO e) => Sfx.Play(Sfx.KO, 0.9f, 0.03f);

        void OnStealth(StealthNotice e)
        {
            if (string.IsNullOrEmpty(e.text)) return;
            if (e.text.StartsWith("SPOTTED")) Sfx.Play(Sfx.Spotted, 0.9f, 0f);
            else if (e.text.StartsWith("SILENT TAKEDOWN") || e.text.StartsWith("TARGET ELIMINATED")) Sfx.Play(Sfx.Takedown);
            else if (e.text.Contains("STOLEN") || e.text.StartsWith("CIPHER SCROLL")) Sfx.Play(Sfx.Secret, 0.9f, 0f);
        }
    }
}
