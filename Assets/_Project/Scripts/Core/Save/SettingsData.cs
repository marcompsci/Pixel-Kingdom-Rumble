using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// Where one on-screen control sits. Position is normalized (0..1) within its half of the screen
    /// so layouts survive different iPhone aspect ratios and safe areas.
    /// </summary>
    [Serializable]
    public class ControlPlacement
    {
        public string controlId = "";
        public float x;
        public float y;
        public float scale = 1f;

        public ControlPlacement() { }
        public ControlPlacement(string id, float x, float y, float scale = 1f)
        {
            controlId = id; this.x = x; this.y = y; this.scale = scale;
        }
    }

    public static class ControlIds
    {
        public const string Joystick = "joystick";
        public const string Jump = "jump";
        public const string Attack = "attack";
        public const string Special = "special";
        public const string Dodge = "dodge";
        public static readonly string[] All = { Joystick, Jump, Attack, Special, Dodge };
    }

    [Serializable]
    public class SettingsData
    {
        public const int CurrentVersion = 1;
        public const float MinControlScale = 0.6f;
        public const float MaxControlScale = 1.6f;

        public int version = CurrentVersion;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public bool hapticsEnabled = true;
        public bool highContrastUI;
        public bool screenShakeEnabled = true;
        /// <summary>0 = fixed joystick, 1 = floating joystick that recenters on touch.</summary>
        public bool floatingJoystick = true;
        public List<ControlPlacement> controlLayout = DefaultLayout();

        public static List<ControlPlacement> DefaultLayout() => new List<ControlPlacement>
        {
            // Joystick: left half. Buttons: right half, thumb-arc arrangement.
            new ControlPlacement(ControlIds.Joystick, 0.30f, 0.30f, 1f),
            new ControlPlacement(ControlIds.Jump,     0.80f, 0.22f, 1.15f),
            new ControlPlacement(ControlIds.Attack,   0.58f, 0.24f, 1f),
            new ControlPlacement(ControlIds.Special,  0.68f, 0.50f, 1f),
            new ControlPlacement(ControlIds.Dodge,    0.88f, 0.50f, 0.9f),
        };

        public ControlPlacement GetPlacement(string id)
        {
            if (controlLayout == null) return null;
            for (int i = 0; i < controlLayout.Count; i++)
                if (controlLayout[i] != null && controlLayout[i].controlId == id) return controlLayout[i];
            return null;
        }

        public void ResetLayout() => controlLayout = DefaultLayout();

        public void Sanitize()
        {
            version = CurrentVersion;
            musicVolume = Clamp01(musicVolume);
            sfxVolume = Clamp01(sfxVolume);
            if (controlLayout == null) controlLayout = DefaultLayout();
            controlLayout.RemoveAll(c => c == null || Array.IndexOf(ControlIds.All, c.controlId) < 0);
            // Drop duplicates, keep first.
            var seen = new HashSet<string>();
            controlLayout.RemoveAll(c => !seen.Add(c.controlId));
            var defaults = DefaultLayout();
            foreach (var d in defaults)
                if (GetPlacement(d.controlId) == null) controlLayout.Add(d);
            foreach (var c in controlLayout)
            {
                c.x = Clamp01(c.x);
                c.y = Clamp01(c.y);
                if (float.IsNaN(c.scale) || c.scale <= 0f) c.scale = 1f;
                c.scale = Math.Max(MinControlScale, Math.Min(MaxControlScale, c.scale));
            }
        }

        static float Clamp01(float v) => float.IsNaN(v) ? 0f : (v < 0f ? 0f : (v > 1f ? 1f : v));
    }
}
