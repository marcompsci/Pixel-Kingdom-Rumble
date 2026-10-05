using System.Runtime.InteropServices;
using UnityEngine;

namespace PKR
{
    public enum HapticStrength { Light = 0, Medium = 1, Heavy = 2 }

    /// <summary>
    /// Haptic taps for hits, landings and UI. Respects the haptics toggle.
    /// iOS uses UIImpactFeedbackGenerator via Assets/Plugins/iOS/PKRHaptics.mm.
    /// Other platforms / Editor: no-op (logs once in Editor so you can see it firing).
    /// Throttled so rapid multi-hits don't buzz continuously.
    /// </summary>
    public class HapticsService : MonoBehaviour
    {
        const float MinInterval = 0.05f;
        float _last = -1f;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PKR_HapticImpact(int style);
#endif

        public void Play(HapticStrength strength)
        {
            var settings = Services.Settings?.Data;
            if (settings != null && !settings.hapticsEnabled) return;
            if (Time.unscaledTime - _last < MinInterval) return;
            _last = Time.unscaledTime;

#if UNITY_IOS && !UNITY_EDITOR
            PKR_HapticImpact((int)strength);
#elif UNITY_EDITOR
            if (_logEditor) { Debug.Log($"[Haptics] {strength} (no-op in Editor)"); _logEditor = false; }
#endif
        }

#if UNITY_EDITOR
        bool _logEditor = true;
#endif
    }
}
