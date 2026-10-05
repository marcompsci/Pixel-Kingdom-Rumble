using UnityEngine;

namespace PKR
{
    /// <summary>Colors for on-screen controls in normal and high-contrast modes.</summary>
    public static class TouchTheme
    {
        public struct Palette
        {
            public Color fill;
            public Color ring;
            public Color label;
            public Color pressedFill;
        }

        public static readonly Palette Normal = new Palette
        {
            fill = new Color(1f, 1f, 1f, 0.16f),
            ring = new Color(1f, 1f, 1f, 0.55f),
            label = new Color(1f, 1f, 1f, 0.9f),
            pressedFill = new Color(1f, 0.85f, 0.4f, 0.45f)
        };

        /// <summary>Opaque dark fill, bright yellow ring and label: readable over any background.</summary>
        public static readonly Palette HighContrast = new Palette
        {
            fill = new Color(0f, 0f, 0f, 0.85f),
            ring = new Color(1f, 0.92f, 0.1f, 1f),
            label = new Color(1f, 0.92f, 0.1f, 1f),
            pressedFill = new Color(1f, 0.92f, 0.1f, 0.9f)
        };

        public static Palette Current
        {
            get
            {
                var s = Services.Settings != null ? Services.Settings.Data : null;
                return s != null && s.highContrastUI ? HighContrast : Normal;
            }
        }
    }
}
