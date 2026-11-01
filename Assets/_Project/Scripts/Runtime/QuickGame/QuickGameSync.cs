using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Always-on: whenever the game opens or comes back to the front, reads the Quick Game widget's stats, pays Star
    /// Shards for clears made in the widget since last time (QuickGame.Sync), and tells the widget which hero to draw.
    /// </summary>
    public class QuickGameSync : MonoBehaviour
    {
        static QuickGameSync _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("QuickGameSync");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<QuickGameSync>();
        }

        void Start() => SyncNow();

        void OnApplicationFocus(bool focus)
        {
            if (focus) SyncNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SendProfile(); // the hero picked this session shows up in the widget
        }

        public static void SyncNow()
        {
            SendProfile();
            var save = Services.Save;
            if (save == null) return;
            var stats = QuickGameBridge.ReadStats();
            if (stats == null) return;
            int before = save.Data.quickGame != null ? save.Data.quickGame.paidClears : 0;
            int paid = QuickGame.Sync(save.Data, stats);
            save.SaveNow();
            if (paid > 0)
            {
                int clears = stats.clears - before;
                ProgressTracker.Toast($"+{paid} STAR SHARDS", $"Quick Game widget: {Plural.Of(clears, "stage")} cleared", new Color32(255, 214, 90, 255));
            }
        }

        static void SendProfile()
        {
            var save = Services.Save;
            string hero = save != null ? save.Data.lastSelectedCharacter : SaveData.DefaultCharacterId;
            bool seasons = Services.Settings == null || Services.Settings.Data.seasonalMode != SeasonCalendar.ModeOff;
            QuickGameBridge.WriteProfile(hero, seasons);
        }
    }
}
