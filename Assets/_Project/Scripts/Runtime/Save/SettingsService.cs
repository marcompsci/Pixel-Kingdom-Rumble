using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Player settings (audio, haptics, high contrast, control layout), stored separately from progress
    /// so a "reset progress" never wipes accessibility choices.
    /// </summary>
    public class SettingsService : MonoBehaviour
    {
        public const string FileName = "pkr_settings.json";

        public SettingsData Data { get; private set; } = new SettingsData();

        public void Load()
        {
            Data = JsonFileStore.TryRead<SettingsData>(FileName) ?? new SettingsData();
            Data.Sanitize();
            if (!Data.reducedMotionChecked)
            {
                // follow iOS Reduce Motion the first time; the in-game toggle decides after that
                Data.reducedMotion = DeviceIdiom.SystemReduceMotion;
                Data.reducedMotionChecked = true;
                _dirty = true;
            }
        }

        const float SaveDelay = 0.5f;
        bool _dirty;
        float _lastChange;

        /// <summary>
        /// Call after changing any field on Data. Applies and notifies immediately; the file write is batched
        /// (0.5 s after the last change, or on pause/quit) so dragging a slider doesn't write every frame.
        /// </summary>
        public void Commit()
        {
            Data.Sanitize();
            _dirty = true;
            _lastChange = Time.unscaledTime;
            Services.Audio?.ApplySettings(Data);
            EventBus<SettingsChanged>.Raise(new SettingsChanged { settings = Data });
        }

        public void SaveNow()
        {
            JsonFileStore.TryWrite(FileName, Data);
            _dirty = false;
        }

        void Update()
        {
            if (_dirty && Time.unscaledTime - _lastChange >= SaveDelay) SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _dirty) SaveNow();
        }

        void OnApplicationQuit()
        {
            if (_dirty) SaveNow();
        }

        public void SetMusicVolume(float v) { Data.musicVolume = v; Commit(); }
        public void SetSfxVolume(float v) { Data.sfxVolume = v; Commit(); }
        public void SetHaptics(bool on) { Data.hapticsEnabled = on; Commit(); }
        public void SetHighContrast(bool on) { Data.highContrastUI = on; Commit(); }
        public void SetAssistMode(bool on) { Data.assistMode = on; Commit(); }
        public void SetStrongAimAssist(bool on) { Data.strongAimAssist = on; Commit(); }
        public void SetHudScale(float v) { Data.hudScale = Mathf.Clamp(v, SettingsData.MinHudScale, SettingsData.MaxHudScale); Commit(); }
        public void SetScreenShake(bool on) { Data.screenShakeEnabled = on; Commit(); }
        public void SetGraphicsMode(GraphicsMode m) { Data.graphicsMode = (int)m; Commit(); }
        public void SetShowFps(bool on) { Data.showFps = on; Commit(); }
        public void SetColorAssist(ColorAssist m) { Data.colorAssist = (int)m; Commit(); }
        public void SetReducedMotion(bool on) { Data.reducedMotion = on; Data.reducedMotionChecked = true; Commit(); }
        public void SetLeftHanded(bool on) { Data.leftHanded = on; Commit(); }
        public void SetBigTouchTargets(bool on) { Data.bigTouchTargets = on; Commit(); }
        public void SetSoundCues(bool on) { Data.soundCues = on; Commit(); }

        public ColorAssist ColorMode => (ColorAssist)Data.colorAssist;

        /// <summary>A colour for something that carries meaning, in the player's palette.</summary>
        public static Color RoleColor(ColorRole role, float alpha = 1f)
        {
            var mode = Services.Settings != null ? (ColorAssist)Services.Settings.Data.colorAssist : ColorAssist.Off;
            var c = Accessibility.Color(role, mode);
            return new Color(c.r / 255f, c.g / 255f, c.b / 255f, alpha);
        }

        public static bool ReducedMotion => Services.Settings != null && Services.Settings.Data.reducedMotion;
        public static float ShakeScale => Services.Settings == null ? 1f : Accessibility.ShakeScale(Services.Settings.Data.screenShakeEnabled, Services.Settings.Data.reducedMotion);
        public void ResetControlLayout() { Data.ResetLayout(); Commit(); }
        public void SetFloatingJoystick(bool on) { Data.floatingJoystick = on; Commit(); }
        public void SetDpadMode(bool on) { Data.dpadMode = on; Commit(); }
        public void SetMuted(bool on) { Data.muted = on; Commit(); }
        public void SetControlOpacity(float v) { Data.controlOpacity = v; Commit(); }

        /// <summary>Scale every on-screen control (joystick and buttons) at once.</summary>
        public void SetControlScale(float scale)
        {
            if (Data.controlLayout != null)
                foreach (var c in Data.controlLayout) if (c != null) c.scale = scale;
            Commit();
        }

        /// <summary>Average scale of the on-screen controls (for the settings slider).</summary>
        public float ControlScale
        {
            get
            {
                if (Data.controlLayout == null || Data.controlLayout.Count == 0) return 1f;
                float sum = 0f; int n = 0;
                foreach (var c in Data.controlLayout) if (c != null) { sum += c.scale; n++; }
                return n > 0 ? sum / n : 1f;
            }
        }
    }
}
