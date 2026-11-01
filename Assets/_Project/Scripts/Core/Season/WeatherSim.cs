using System;

namespace PKR.Core
{
    public enum WeatherKind { None = 0, Leaves = 1, Snow = 2 }

    /// <summary>One falling leaf or snowflake.</summary>
    public struct WeatherFlake
    {
        public float x, y;        // position inside the view
        public float fall;        // units per second downward
        public float drift;       // steady sideways speed
        public float sway;        // side-to-side amplitude
        public float phase;       // sway / spin phase
        public float spin;        // radians per second (leaves)
        public float size;        // 0.6..1.4
        public float angle;       // current rotation (radians)
        public int variant;       // which leaf colour / flake shape
    }

    /// <summary>
    /// Falling leaves or snow, as plain numbers so the world view and the menu share it (and tests can run it). The
    /// flakes live in a box the size of the view and wrap around its edges, so the view can follow the camera and
    /// always be full. Reduced motion: fewer, slower flakes that fall straight and don't spin.
    /// </summary>
    public class WeatherSim
    {
        public readonly WeatherKind Kind;
        public readonly WeatherFlake[] Flakes;
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Wind;
        readonly bool _reduced;
        float _time;

        /// <summary>How many flakes to draw for a quality tier (0 high, 1 medium, 2 low) and the season's intensity.</summary>
        public static int CountFor(WeatherKind kind, int tier, float intensity, bool reduced)
        {
            if (kind == WeatherKind.None || intensity <= 0f) return 0;
            int baseCount = kind == WeatherKind.Snow ? 90 : 40;
            float tierScale = tier <= 0 ? 1f : tier == 1 ? 0.6f : 0.35f;
            float n = baseCount * tierScale * Math.Max(0.3f, Math.Min(1f, intensity)) * (reduced ? 0.4f : 1f);
            return Math.Max(4, (int)Math.Round(n));
        }

        public WeatherSim(WeatherKind kind, int count, float width, float height, int seed, bool reduced)
        {
            Kind = kind;
            _reduced = reduced;
            Width = Math.Max(1f, width);
            Height = Math.Max(1f, height);
            Flakes = new WeatherFlake[Math.Max(0, count)];
            var rng = new Random(seed);
            for (int i = 0; i < Flakes.Length; i++) Flakes[i] = Spawn(rng, (float)rng.NextDouble() * Height);
        }

        WeatherFlake Spawn(Random rng, float y)
        {
            float r() => (float)rng.NextDouble();
            var f = new WeatherFlake
            {
                x = r() * Width,
                y = y,
                size = 0.6f + r() * 0.8f,
                phase = r() * 6.2832f,
                variant = rng.Next(0, 4),
            };
            if (Kind == WeatherKind.Leaves)
            {
                f.fall = 0.9f + r() * 0.8f;
                f.drift = 0.25f + r() * 0.35f;
                f.sway = 0.7f + r() * 0.5f;
                f.spin = (r() < 0.5f ? -1f : 1f) * (1.5f + r() * 2f);
            }
            else
            {
                f.fall = (0.6f + r() * 0.7f) * (0.6f + f.size * 0.4f); // bigger (nearer) flakes fall faster
                f.drift = -0.1f + r() * 0.2f;
                f.sway = 0.15f + r() * 0.25f;
                f.spin = 0f;
            }
            if (_reduced)
            {
                f.fall *= 0.6f;
                f.sway = 0f;
                f.spin = 0f;
                f.drift *= 0.5f;
            }
            return f;
        }

        /// <summary>The view changed size (a different screen or zoom): spread the flakes over the new box.</summary>
        public void Resize(float width, float height)
        {
            width = Math.Max(1f, width); height = Math.Max(1f, height);
            if (Math.Abs(width - Width) < 0.01f && Math.Abs(height - Height) < 0.01f) return;
            float sx = width / Width, sy = height / Height;
            for (int i = 0; i < Flakes.Length; i++) { Flakes[i].x *= sx; Flakes[i].y *= sy; }
            Width = width; Height = height;
        }

        /// <summary>
        /// Advance by dt. <paramref name="viewDx"/>/<paramref name="viewDy"/> = how far the view moved since last
        /// step (camera follow): flakes shift the other way so they stay put in the world while the box wraps.
        /// </summary>
        public void Step(float dt, float viewDx = 0f, float viewDy = 0f)
        {
            if (dt <= 0f || float.IsNaN(dt)) return;
            dt = Math.Min(dt, 0.1f);
            _time += dt;
            for (int i = 0; i < Flakes.Length; i++)
            {
                ref var f = ref Flakes[i];
                f.phase += dt * (Kind == WeatherKind.Leaves ? 1.6f : 1.1f);
                float side = f.sway * (float)Math.Cos(f.phase);
                f.x += (f.drift + Wind + side) * dt - viewDx;
                f.y -= f.fall * dt + viewDy;
                f.angle += f.spin * dt;
                f.x = Wrap(f.x, Width);
                f.y = Wrap(f.y, Height);
            }
        }

        static float Wrap(float v, float span)
        {
            if (v < 0f || v >= span) v -= (float)Math.Floor(v / span) * span;
            return v;
        }
    }
}
