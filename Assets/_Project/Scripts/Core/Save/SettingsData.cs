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
        public const string Jetpack = "jetpack";
        /// <summary>Zombie Survival only: shoot the gun, and buy / repair / open (USE).</summary>
        public const string Fire = "fire";
        public const string Use = "use";
        public static readonly string[] All = { Joystick, Jump, Attack, Special, Dodge, Jetpack, Fire, Use };
    }

    [Serializable]
    public class SettingsData
    {
        public const int CurrentVersion = 2;
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
        /// <summary>D-pad mode: the stick snaps to 8 directions at full strength (crisp platforming).</summary>
        public bool dpadMode;
        /// <summary>The speaker button on the HUD: silences music and sound without touching the volume sliders.</summary>
        public bool muted;
        /// <summary>How solid the on-screen controls look (0.2 = faint, 1 = opaque). Default 65%.</summary>
        public float controlOpacity = DefaultControlOpacity;
        public const float DefaultControlOpacity = 0.65f;
        /// <summary>Assist mode: +2 max health in Story and Contracts, and guards take longer to notice the hero.</summary>
        public bool assistMode;
        /// <summary>Zombie modes: a wider, longer auto-aim.</summary>
        public bool strongAimAssist;
        /// <summary>Zombie modes: HUD size (0.85..1.3).</summary>
        public float hudScale = 1f;
        public const float MinHudScale = 0.85f, MaxHudScale = 1.3f;
        /// <summary>Settings > Graphics (GraphicsMode: auto, high, battery saver).</summary>
        public int graphicsMode = (int)GraphicsMode.Auto;
        /// <summary>A small frame-rate readout in the corner.</summary>
        public bool showFps;
        public List<ControlPlacement> controlLayout = DefaultLayout();

        public static List<ControlPlacement> DefaultLayout() => new List<ControlPlacement>
        {
            // Joystick: left half. Buttons: right half, thumb-arc arrangement.
            new ControlPlacement(ControlIds.Joystick, 0.30f, 0.30f, 1f),
            new ControlPlacement(ControlIds.Jump,     0.80f, 0.22f, 1.15f),
            new ControlPlacement(ControlIds.Attack,   0.58f, 0.24f, 1f),
            new ControlPlacement(ControlIds.Special,  0.68f, 0.50f, 1f),
            new ControlPlacement(ControlIds.Dodge,    0.88f, 0.50f, 0.9f),
            new ControlPlacement(ControlIds.Jetpack,  0.60f, 0.74f, 0.85f),
            new ControlPlacement(ControlIds.Fire,     0.80f, 0.76f, 1.05f),
            new ControlPlacement(ControlIds.Use,      0.93f, 0.80f, 0.8f),
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
            if (version < 2)
            {
                // October 2026 controls update: the stick follows the thumb and controls are 65% opaque for everyone.
                floatingJoystick = true;
                controlOpacity = DefaultControlOpacity;
            }
            version = CurrentVersion;
            controlOpacity = float.IsNaN(controlOpacity) ? DefaultControlOpacity : Math.Max(0.2f, Math.Min(1f, controlOpacity));
            hudScale = float.IsNaN(hudScale) || hudScale <= 0f ? 1f : Math.Max(MinHudScale, Math.Min(MaxHudScale, hudScale));
            if (graphicsMode < 0 || graphicsMode > (int)GraphicsMode.BatterySaver) graphicsMode = (int)GraphicsMode.Auto;
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
