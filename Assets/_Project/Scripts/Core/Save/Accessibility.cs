using System;

namespace PKR.Core
{
    /// <summary>Settings > Colour vision.</summary>
    public enum ColorAssist { Off = 0, RedGreen = 1, BlueYellow = 2 }

    /// <summary>The colours that carry meaning in play.</summary>
    public enum ColorRole { Danger, Health, Good, Warning, Squad0, Squad1, Squad2, Squad3 }

    /// <summary>
    /// Accessibility rules that don't need Unity: colour palettes that stay apart for common colour-vision types,
    /// left-handed mirroring, bigger touch targets and how much motion to show. Colours are 0-255 RGB.
    /// </summary>
    public static class Accessibility
    {
        /// <summary>Default look (unchanged from before these options existed).</summary>
        static readonly byte[][] Default =
        {
            new byte[] { 230, 13, 13 },    // danger: red
            new byte[] { 230, 64, 64 },    // health bar: red
            new byte[] { 140, 230, 140 },  // good: green
            new byte[] { 255, 210, 90 },   // warning: yellow
            new byte[] { 255, 217, 77 },   // squad: gold
            new byte[] { 89, 217, 255 },   //        sky
            new byte[] { 255, 115, 191 },  //        pink
            new byte[] { 128, 255, 128 },  //        green
        };

        // Okabe-Ito colours: danger and good never rely on red vs green.
        static readonly byte[][] RedGreen =
        {
            new byte[] { 230, 159, 0 },    // danger: orange
            new byte[] { 230, 159, 0 },    // health: orange
            new byte[] { 86, 180, 233 },   // good: sky blue
            new byte[] { 240, 228, 66 },   // warning: yellow
            new byte[] { 240, 228, 66 },   // squad: yellow
            new byte[] { 86, 180, 233 },   //        sky blue
            new byte[] { 204, 121, 167 },  //        reddish purple
            new byte[] { 255, 255, 255 },  //        white
        };

        static readonly byte[][] BlueYellow =
        {
            new byte[] { 213, 94, 0 },     // danger: vermillion
            new byte[] { 213, 94, 0 },     // health: vermillion
            new byte[] { 0, 158, 115 },    // good: bluish green
            new byte[] { 204, 121, 167 },  // warning: reddish purple
            new byte[] { 213, 94, 0 },     // squad: vermillion
            new byte[] { 0, 158, 115 },    //        bluish green
            new byte[] { 204, 121, 167 },  //        reddish purple
            new byte[] { 255, 255, 255 },  //        white
        };

        public static (byte r, byte g, byte b) Color(ColorRole role, ColorAssist mode)
        {
            var table = mode == ColorAssist.RedGreen ? RedGreen : mode == ColorAssist.BlueYellow ? BlueYellow : Default;
            var c = table[(int)role];
            return (c[0], c[1], c[2]);
        }

        public static ColorRole SquadRole(int slot) => ColorRole.Squad0 + ((slot % 4) + 4) % 4;

        public static string Name(ColorAssist m) => m == ColorAssist.RedGreen ? "RED-GREEN" : m == ColorAssist.BlueYellow ? "BLUE-YELLOW" : "STANDARD";

        public static string Info(ColorAssist m) => m == ColorAssist.RedGreen
            ? "For protanopia and deuteranopia: danger is orange, good is blue"
            : m == ColorAssist.BlueYellow ? "For tritanopia: danger is vermillion, good is green" : "The game's own colours";

        /// <summary>
        /// A simulated colour difference that matches how the two colours look to someone with that colour vision
        /// (a simple model: red-green drops the red/green difference, blue-yellow drops the blue/yellow one).
        /// Used by tests to keep each palette's important pairs apart.
        /// </summary>
        public static float SeenDifference((byte r, byte g, byte b) a, (byte r, byte g, byte b) b, ColorAssist vision)
        {
            (float x, float y, float z) Seen((byte r, byte g, byte b) c)
            {
                float r = c.r, g = c.g, bl = c.b;
                if (vision == ColorAssist.RedGreen) { float m = (r + g) * 0.5f; return (m, m, bl); }
                if (vision == ColorAssist.BlueYellow) { float y = (g + bl) * 0.5f; return (r, y, y); }
                return (r, g, bl);
            }
            var p = Seen(a); var q = Seen(b);
            float dx = p.x - q.x, dy = p.y - q.y, dz = p.z - q.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Left-handed: the stick goes right and the buttons left (normalized x mirrored).</summary>
        public static float MirrorX(float x, bool leftHanded) => leftHanded ? 1f - x : x;

        /// <summary>How much bigger a touch button's hit area is than its art.</summary>
        public static float HitPad(bool bigTargets) => bigTargets ? 1.4f : 1f;

        /// <summary>Camera shake strength: off with screen shake off or reduced motion on.</summary>
        public static float ShakeScale(bool shakeOn, bool reducedMotion) => !shakeOn || reducedMotion ? 0f : 1f;

        /// <summary>Full-screen flashes and pulses (low health, hits): softer and steady with reduced motion.</summary>
        public static float FlashScale(bool reducedMotion) => reducedMotion ? 0.45f : 1f;
    }
}
