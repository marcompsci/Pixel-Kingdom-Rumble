using System.Runtime.InteropServices;
using System.Text;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Talks to the Quick Game widget through the shared App Group (iOS only; elsewhere there is no widget and these
    /// do nothing): reads the widget's stats and writes the profile it draws with (hero, live seasons on/off).
    /// </summary>
    public static class QuickGameBridge
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int PKR_Widget_ReadStats(byte[] buf, int cap);
        [DllImport("__Internal")] static extern void PKR_Widget_WriteProfile(string json);
#endif

        /// <summary>True on an iPhone/iPad build, where the widget can exist.</summary>
        public static bool Supported =>
#if UNITY_IOS && !UNITY_EDITOR
            true;
#else
            false;
#endif

        /// <summary>The widget's stats, or null if it was never played (or this isn't iOS).</summary>
        public static QuickGameStats ReadStats()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var buf = new byte[8192];
            int n = PKR_Widget_ReadStats(buf, buf.Length);
            if (n < 0)
            {
                buf = new byte[-n + 16];
                n = PKR_Widget_ReadStats(buf, buf.Length);
            }
            if (n <= 0) return null;
            return TinyJson.QuickGameStatsFrom(Encoding.UTF8.GetString(buf, 0, n));
#else
            return null;
#endif
        }

        public static void WriteProfile(string heroId, bool seasons)
        {
            string hero = string.IsNullOrEmpty(heroId) ? SaveData.DefaultCharacterId : heroId;
            string json = "{\"hero\":\"" + Escape(hero) + "\",\"seasons\":" + (seasons ? "true" : "false") + "}";
#if UNITY_IOS && !UNITY_EDITOR
            PKR_Widget_WriteProfile(json);
#else
            _lastProfile = json;
#endif
        }

        /// <summary>Editor/tests: the last profile that would have been sent.</summary>
        public static string LastProfile => _lastProfile;
        static string _lastProfile;

        static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
