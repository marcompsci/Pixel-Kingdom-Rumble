using System;

namespace PKR.Core
{
    /// <summary>Settings > Graphics.</summary>
    public enum GraphicsMode { Auto = 0, High = 1, BatterySaver = 2 }

    /// <summary>How much decoration to draw: High draws everything; Low keeps only what gameplay needs.</summary>
    public enum PerfTier { High = 0, Medium = 1, Low = 2 }

    /// <summary>iOS ProcessInfo.thermalState (0 nominal .. 3 critical).</summary>
    public enum ThermalState { Nominal = 0, Fair = 1, Serious = 2, Critical = 3 }

    /// <summary>
    /// Rolling frame times (seconds): average FPS, the 50th/95th percentile, the worst frame and how many missed the
    /// budget. Fixed size, no allocation per frame.
    /// </summary>
    public class FrameStats
    {
        readonly float[] _t;
        readonly float[] _sorted;
        int _n, _head;

        public FrameStats(int window = 300) { _t = new float[window]; _sorted = new float[window]; }

        public int Count => _n;

        public void Add(float dt)
        {
            if (float.IsNaN(dt) || dt <= 0f) return;
            _t[_head] = Math.Min(dt, 1f);   // a 10 s stall (backgrounded) counts as one bad frame, not ten
            _head = (_head + 1) % _t.Length;
            if (_n < _t.Length) _n++;
        }

        public void Clear() { _n = 0; _head = 0; }

        public float AverageFps
        {
            get
            {
                if (_n == 0) return 0f;
                double sum = 0;
                for (int i = 0; i < _n; i++) sum += _t[i];
                return sum > 0 ? (float)(_n / sum) : 0f;
            }
        }

        /// <summary>The frame time (s) that p of frames are at or under (p in 0..1).</summary>
        public float Percentile(float p)
        {
            if (_n == 0) return 0f;
            Array.Copy(_t, _sorted, _n);
            Array.Sort(_sorted, 0, _n);
            int i = (int)Math.Ceiling(Math.Max(0f, Math.Min(1f, p)) * _n) - 1;
            return _sorted[Math.Max(0, Math.Min(_n - 1, i))];
        }

        public float Worst
        {
            get { float w = 0f; for (int i = 0; i < _n; i++) w = Math.Max(w, _t[i]); return w; }
        }

        /// <summary>Share of frames slower than <paramref name="budget"/> seconds (with a 10% allowance for jitter).</summary>
        public float MissedShare(float budget)
        {
            if (_n == 0) return 0f;
            int m = 0;
            for (int i = 0; i < _n; i++) if (_t[i] > budget * 1.1f) m++;
            return (float)m / _n;
        }

        public string Summary(float budget) =>
            $"fps {AverageFps:0.0}  p50 {Percentile(0.5f) * 1000f:0.0}ms  p95 {Percentile(0.95f) * 1000f:0.0}ms  worst {Worst * 1000f:0}ms  missed {MissedShare(budget) * 100f:0}%";
    }

    /// <summary>
    /// Picks the drawing tier. Auto steps down when frames keep missing the budget or the phone gets hot, and steps
    /// back up only after a long good stretch (so it doesn't flip back and forth). Pure: time comes in as dt.
    /// </summary>
    public class PerfGovernor
    {
        /// <summary>Judge over this long before stepping down.</summary>
        public const float DownWindow = 5f;
        /// <summary>Good for this long before stepping back up.</summary>
        public const float UpWindow = 30f;
        /// <summary>Step down when more than this share of frames miss the budget.</summary>
        public const float MissedToStepDown = 0.2f;
        /// <summary>Step up only when fewer than this share missed.</summary>
        public const float MissedToStepUp = 0.03f;

        public GraphicsMode Mode { get; private set; }
        public PerfTier Tier { get; private set; }
        public ThermalState Thermal { get; private set; }
        public int StepsDown { get; private set; }
        public readonly FrameStats Stats = new FrameStats(300);

        float _judge, _good;
        int _frames, _missed;

        public PerfGovernor(GraphicsMode mode = GraphicsMode.Auto) { SetMode(mode); }

        /// <summary>iOS Low Power Mode is on (Auto then runs at 30 FPS).</summary>
        public bool LowPower { get; set; }

        public int TargetFps => Mode == GraphicsMode.BatterySaver || (Mode == GraphicsMode.Auto && LowPower) ? 30 : 60;
        public float Budget => 1f / TargetFps;

        public void SetMode(GraphicsMode mode)
        {
            Mode = mode;
            Tier = mode == GraphicsMode.BatterySaver ? PerfTier.Low : PerfTier.High;
            _judge = _good = 0f;
            _frames = _missed = 0;
            Stats.Clear();
        }

        public void SetThermal(ThermalState t)
        {
            Thermal = t;
            if (Mode != GraphicsMode.Auto) return;
            // A hot phone throttles anyway; drawing less lets it cool down instead of stuttering.
            var cap = t >= ThermalState.Critical ? PerfTier.Low : t >= ThermalState.Serious ? PerfTier.Medium : PerfTier.High;
            if (cap > Tier) { Tier = cap; StepsDown++; _good = 0f; }
        }

        /// <summary>One frame took <paramref name="dt"/> seconds. Returns true when the tier changed.</summary>
        public bool Frame(float dt)
        {
            if (float.IsNaN(dt) || dt <= 0f) return false;
            Stats.Add(dt);
            if (Mode != GraphicsMode.Auto) return false;
            if (dt > 0.5f) return false;   // a load or a stall, not a frame rate problem
            _frames++;
            if (dt > Budget * 1.1f) _missed++;
            _judge += dt;
            if (_judge < DownWindow) return false;
            float share = _frames > 0 ? (float)_missed / _frames : 0f;
            _judge = 0f; _frames = 0; _missed = 0;
            if (share > MissedToStepDown && Tier < PerfTier.Low)
            {
                Tier++;
                StepsDown++;
                _good = 0f;
                return true;
            }
            if (share < MissedToStepUp) _good += DownWindow; else _good = 0f;
            var floor = Thermal >= ThermalState.Critical ? PerfTier.Low : Thermal >= ThermalState.Serious ? PerfTier.Medium : PerfTier.High;
            if (_good >= UpWindow && Tier > floor)
            {
                Tier--;
                _good = 0f;
                return true;
            }
            return false;
        }

        // ---- what each tier draws ---------------------------------------------------------------------

        /// <summary>The most short-lived effects (flashes, puffs, blood) alive at once.</summary>
        public static int MaxFx(PerfTier t) => t == PerfTier.High ? 160 : t == PerfTier.Medium ? 90 : 45;

        /// <summary>How long blood stays on the ground (s).</summary>
        public static float DecalLife(PerfTier t) => t == PerfTier.High ? 6f : t == PerfTier.Medium ? 3.5f : 1.5f;

        /// <summary>Purely decorative effects (muzzle flashes, hit puffs): share kept.</summary>
        public static float DecorShare(PerfTier t) => t == PerfTier.High ? 1f : t == PerfTier.Medium ? 0.6f : 0.3f;

        public static string Name(GraphicsMode m) => m == GraphicsMode.Auto ? "AUTO" : m == GraphicsMode.High ? "HIGH" : "BATTERY SAVER";

        public static string Info(GraphicsMode m) => m == GraphicsMode.Auto
            ? "60 FPS; draws fewer effects if the phone struggles or gets hot"
            : m == GraphicsMode.High ? "60 FPS and every effect, always" : "30 FPS and fewer effects: longer battery, cooler phone";
    }
}
