using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Counts time played per mode for PROFILE (always on, survives scene loads). Gameplay time goes to the mode in
    /// GameSession; menus, pause and results count as MENUS. Flushed to the save every minute and when the app is
    /// backgrounded. Nothing leaves the device.
    /// </summary>
    public class PlayTimeTracker : MonoBehaviour
    {
        static PlayTimeTracker _instance;
        float _pending, _flushAt, _session;
        int _breaksShown;
        int _pendingMode = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null || Application.isBatchMode) return;
            var go = new GameObject("PlayTimeTracker");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            _instance = go.AddComponent<PlayTimeTracker>();
        }

        public static int ModeIndex(SessionMode m, bool playing)
        {
            if (!playing) return PlayTime.Menus;
            switch (m)
            {
                case SessionMode.StoryQuest: return 0;
                case SessionMode.ArenaClash: return 1;
                case SessionMode.Versus: return 2;
                case SessionMode.Contracts: return 3;
                case SessionMode.Holiday:
                case SessionMode.Halloween: return 4;
                case SessionMode.Zombies: return 5;
                default: return PlayTime.Menus;
            }
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f); // a long hitch (or a suspend) isn't play time
            bool playing = Services.State != null && Services.State.State == GameState.Playing;
            int mode = ModeIndex(GameSession.Mode, playing);
            if (mode != _pendingMode) Flush();
            _pendingMode = mode;
            _pending += dt;
            _session += dt;
            if (Time.unscaledTime >= _flushAt) Flush();
            bool on = Services.Settings != null && Services.Settings.Data.breakReminder;
            if (BreakReminder.Due(on, _session, _breaksShown))
            {
                _breaksShown++;
                EventBus<ProgressToast>.Raise(new ProgressToast
                {
                    title = _breaksShown == 1 ? "AN HOUR OF PLAY!" : $"{_breaksShown} HOURS OF PLAY!",
                    detail = "Good time for a stretch and some water. Your progress is saved.",
                    color = new Color32(140, 220, 160, 255)
                });
            }
        }

        void Flush()
        {
            _flushAt = Time.unscaledTime + 60f;
            if (_pending <= 0f || _pendingMode < 0 || Services.Save == null) { _pending = 0f; return; }
            PlayTime.Add(Services.Save.Data, _pendingMode, _pending);
            _pending = 0f;
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            Flush();
            if (Services.Save != null) Services.Save.SaveNow();
        }
    }
}
