using System.Collections;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Brief global freeze on impact ("hit stop") so hits feel heavy. Uses real time, never overrides a
    /// player pause, and overlapping requests extend rather than stack.
    /// </summary>
    public static class HitStop
    {
        static float _until;
        static float _resumeScale = 1f;
        static Runner _runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _until = 0f;
            _resumeScale = 1f;
            _runner = null;
        }

        public static bool IsActive => Time.realtimeSinceStartup < _until;

        public static void Request(int frames)
        {
            if (frames <= 0) return;
            var state = Services.State;
            if (state != null && state.IsPaused) return;

            float end = Time.realtimeSinceStartup + frames / 60f;
            if (end <= _until) return;
            bool alreadyRunning = IsActive;
            _until = end;
            if (alreadyRunning) return;

            _resumeScale = Time.timeScale > 0f ? Time.timeScale : 1f; // keep any slow-motion in effect
            Time.timeScale = 0f;
            if (_runner == null)
            {
                var go = new GameObject("[PKR HitStop]") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
            _runner.StartCoroutine(Wait());
        }

        static IEnumerator Wait()
        {
            while (Time.realtimeSinceStartup < _until) yield return null;
            var state = Services.State;
            Time.timeScale = state != null && state.IsPaused ? 0f : _resumeScale;
        }

        sealed class Runner : MonoBehaviour { }
    }
}
