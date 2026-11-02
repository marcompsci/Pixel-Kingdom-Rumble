using System.Collections.Generic;
using System.Runtime.InteropServices;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Live player counts for the Kingdom Plaza (see PKRPresence.mm). Announce the place you're in, watch the places
    /// you want counts for, and read Count (null = unknown: offline, not signed in to Game Center, or not iOS).
    /// Never invents a number.
    /// </summary>
    public class LivePresence : MonoBehaviour
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern bool PKR_Presence_Ready();
        [DllImport("__Internal")] static extern void PKR_Presence_Query(int group);
        [DllImport("__Internal")] static extern int PKR_Presence_Count(int group);
        [DllImport("__Internal")] static extern void PKR_Presence_Announce(int group);
#endif
        static LivePresence _runner;
        static readonly HashSet<int> Watched = new HashSet<int>();
        static int _here = -1;
        float _nextQuery, _nextAnnounce;

        static LivePresence Runner
        {
            get
            {
                if (_runner != null) return _runner;
                var go = new GameObject("[LivePresence]");
                DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideInHierarchy;
                _runner = go.AddComponent<LivePresence>();
                return _runner;
            }
        }

        /// <summary>True when counts can be fetched at all (iOS, signed in to Game Center).</summary>
        public static bool Available =>
#if UNITY_IOS && !UNITY_EDITOR
            PKR_Presence_Ready();
#else
            false;
#endif

        /// <summary>The player is now in this place (a player group); -1 when they leave the plaza and its quests.</summary>
        public static void Enter(int group)
        {
            _here = group;
            Runner._nextAnnounce = 0f;
        }

        public static void Leave() => _here = -1;

        /// <summary>Keep this place's count fresh (every LiveCount.RefreshSeconds).</summary>
        public static void Watch(int group)
        {
            if (Watched.Add(group)) Runner._nextQuery = 0f;
        }

        public static void UnwatchAll() => Watched.Clear();

        public static int? Count(int group)
        {
#if UNITY_IOS && !UNITY_EDITOR
            int n = PKR_Presence_Count(group);
            return n >= 0 ? n : (int?)null;
#else
            return null;
#endif
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (_here >= 0 && now >= _nextAnnounce)
            {
                _nextAnnounce = now + LiveCount.AnnounceSeconds;
#if UNITY_IOS && !UNITY_EDITOR
                PKR_Presence_Announce(_here);
#endif
            }
            if (Watched.Count > 0 && now >= _nextQuery)
            {
                _nextQuery = now + LiveCount.RefreshSeconds;
#if UNITY_IOS && !UNITY_EDITOR
                foreach (int g in Watched) PKR_Presence_Query(g);
#endif
            }
        }
    }
}
