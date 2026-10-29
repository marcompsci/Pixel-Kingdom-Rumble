using System;

namespace PKR.Core
{
    /// <summary>
    /// The game's one easing vocabulary for menus, toasts and other UI motion (not gameplay). Three strong cubic-bezier
    /// curves instead of linear, SmoothStep and ad-hoc quadratics:
    ///   Out     (0.23, 1, 0.32, 1)    things entering or leaving the screen; starts fast so it answers at once
    ///   InOut   (0.77, 0, 0.175, 1)   things moving or morphing while on screen
    ///   Drawer  (0.32, 0.72, 0, 1)    panels that slide in from an edge
    /// plus the durations the UI uses, all under 300 ms except rare celebratory moments.
    /// </summary>
    public static class Ease
    {
        public static float Out(float t) => Bezier(0.23f, 1f, 0.32f, 1f, t);
        public static float InOut(float t) => Bezier(0.77f, 0f, 0.175f, 1f, t);
        public static float Drawer(float t) => Bezier(0.32f, 0.72f, 0f, 1f, t);

        /// <summary>Press feedback, small popups, page swaps.</summary>
        public const float Quick = 0.16f;
        /// <summary>Modals and toasts coming in.</summary>
        public const float Enter = 0.2f;
        /// <summary>Modals and toasts going away (faster than they came).</summary>
        public const float Exit = 0.13f;
        /// <summary>A screen change: cover, then reveal.</summary>
        public const float Cover = 0.16f, Reveal = 0.22f;
        /// <summary>Modals start this big (never from nothing) and grow to full size.</summary>
        public const float ModalFrom = 0.97f;

        /// <summary>
        /// A timed message (a toast, a tip): how far in it is (0..1, ease-out over <paramref name="inTime"/> after it
        /// appeared) and how far it still is before it goes (1 down to 0, ease-out over the last <paramref name="outTime"/>
        /// seconds left). Visibility is the smaller of the two.
        /// </summary>
        public static (float enter, float exit) Presence(float since, float left, float inTime, float outTime)
        {
            float enter = inTime <= 0f ? 1f : Out(since / inTime);
            float exit = left <= 0f ? 0f : outTime <= 0f ? 1f : 1f - Out(1f - left / outTime);
            return (enter, exit);
        }

        /// <summary>
        /// CSS-style cubic-bezier(x1, y1, x2, y2) at progress t (0..1): solves x(s) = t for s, returns y(s).
        /// Newton steps with a bisection fallback, accurate to about 1e-5.
        /// </summary>
        public static float Bezier(float x1, float y1, float x2, float y2, float t)
        {
            if (float.IsNaN(t) || t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            double s = t;
            for (int i = 0; i < 8; i++)
            {
                double x = Curve(x1, x2, s) - t;
                if (Math.Abs(x) < 1e-6) return (float)Curve(y1, y2, s);
                double dx = Slope(x1, x2, s);
                if (Math.Abs(dx) < 1e-6) break;
                s -= x / dx;
                if (s < 0 || s > 1) break;
            }
            double lo = 0, hi = 1;
            s = t;
            for (int i = 0; i < 40; i++)
            {
                double x = Curve(x1, x2, s);
                if (Math.Abs(x - t) < 1e-6) break;
                if (x < t) lo = s; else hi = s;
                s = (lo + hi) * 0.5;
            }
            return (float)Curve(y1, y2, s);
        }

        // one axis of a bezier from (0,0) to (1,1) with control points a, b
        static double Curve(double a, double b, double s) => ((1 - 3 * b + 3 * a) * s + (3 * b - 6 * a)) * s * s + 3 * a * s;
        static double Slope(double a, double b, double s) => 3 * (1 - 3 * b + 3 * a) * s * s + 2 * (3 * b - 6 * a) * s + 3 * a;
    }

    /// <summary>"1 kill", "2 kills": counts in UI text read right.</summary>
    public static class Plural
    {
        public static string Of(int n, string one, string many = null) => n == 1 ? $"1 {one}" : $"{n} {many ?? one + "s"}";
    }
}
