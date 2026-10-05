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
        }

        /// <summary>Call after changing any field on Data. Sanitizes, saves, and notifies listeners.</summary>
        public void Commit()
        {
            Data.Sanitize();
            JsonFileStore.TryWrite(FileName, Data);
            Services.Audio?.ApplySettings(Data);
            EventBus<SettingsChanged>.Raise(new SettingsChanged { settings = Data });
        }

        public void SetMusicVolume(float v) { Data.musicVolume = v; Commit(); }
        public void SetSfxVolume(float v) { Data.sfxVolume = v; Commit(); }
        public void SetHaptics(bool on) { Data.hapticsEnabled = on; Commit(); }
        public void SetHighContrast(bool on) { Data.highContrastUI = on; Commit(); }
        public void SetScreenShake(bool on) { Data.screenShakeEnabled = on; Commit(); }
        public void ResetControlLayout() { Data.ResetLayout(); Commit(); }
    }
}
