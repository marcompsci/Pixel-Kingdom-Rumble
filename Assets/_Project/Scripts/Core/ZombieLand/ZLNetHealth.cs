using System;

namespace PKR.Core
{
    public enum NetQuality { Good, Laggy, Lost }

    /// <summary>
    /// How one link is doing: when we last heard from the other side, and the round trip from pings. Pure, so the
    /// thresholds are unit-tested; times are seconds from any clock that only goes forward.
    /// </summary>
    public class NetHealth
    {
        /// <summary>No packet for this long: show "connection trouble".</summary>
        public const float LaggyAfter = 1.5f;
        /// <summary>A round trip slower than this also counts as laggy (ms).</summary>
        public const float LaggyRttMs = 250f;
        /// <summary>Host: a guest silent this long is handed to an AI until they come back.</summary>
        public const float GuestDropAfter = 6f;
        /// <summary>Guest: the host silent this long ends the run.</summary>
        public const float HostLostAfter = 15f;

        public double LastHeard { get; private set; } = -1;
        /// <summary>Smoothed round trip in ms (-1 before the first pong).</summary>
        public float RttMs { get; private set; } = -1f;

        public void Heard(double now) { if (now > LastHeard) LastHeard = now; }

        /// <summary>Start counting from <paramref name="now"/> (a new clock, or the run just began).</summary>
        public void Reset(double now) { LastHeard = now; }

        /// <summary>A pong came back for a ping sent at <paramref name="sentAt"/>.</summary>
        public void Pong(double now, double sentAt)
        {
            Heard(now);
            float ms = (float)Math.Max(0.0, (now - sentAt) * 1000.0);
            if (ms > 10000f) return;   // a stale pong from before a long stall
            RttMs = RttMs < 0f ? ms : RttMs + (ms - RttMs) * 0.25f;
        }

        public float Silence(double now) => LastHeard < 0 ? 0f : (float)Math.Max(0.0, now - LastHeard);

        public NetQuality Quality(double now, float lostAfter = HostLostAfter)
        {
            float s = Silence(now);
            if (s >= lostAfter) return NetQuality.Lost;
            if (s >= LaggyAfter || RttMs > LaggyRttMs) return NetQuality.Laggy;
            return NetQuality.Good;
        }

        public string Label(double now)
        {
            switch (Quality(now))
            {
                case NetQuality.Lost: return "CONNECTION LOST";
                case NetQuality.Laggy: return Silence(now) >= LaggyAfter ? "RECONNECTING..." : $"LAGGY  {Math.Round(RttMs)} ms";
                default: return RttMs >= 0f ? $"ONLINE  {Math.Round(RttMs)} ms" : "ONLINE";
            }
        }
    }

    /// <summary>
    /// Snapshot interpolation for things the network moves (the host's zombies on a guest, a remote hero on the host):
    /// draws them a little in the past, between the two samples around that moment, so uneven packet timing doesn't
    /// make them stutter. Short gaps are bridged by extrapolating; a big jump (respawn, teleport) snaps.
    /// </summary>
    public class NetInterp
    {
        struct S { public double t; public float x, y; }

        const int Cap = 8;
        readonly S[] _s = new S[Cap];
        int _n, _head;   // _head = index of the newest

        /// <summary>How far in the past to draw (a bit more than one send interval).</summary>
        public float Delay = 0.12f;
        /// <summary>How long to keep moving past the newest sample before holding still.</summary>
        public float MaxExtrapolate = 0.15f;
        /// <summary>Jumps longer than this (tiles) snap instead of sliding.</summary>
        public float SnapDistance = 5f;

        public int Count => _n;

        S At(int back) => _s[(_head - back + Cap) % Cap];

        public void Clear() { _n = 0; }

        public void Push(double t, float x, float y)
        {
            if (_n > 0)
            {
                var last = At(0);
                if (t <= last.t) return;   // out of order or duplicate
                float dx = x - last.x, dy = y - last.y;
                if (dx * dx + dy * dy > SnapDistance * SnapDistance) _n = 0;
            }
            _head = (_head + 1) % Cap;
            _s[_head] = new S { t = t, x = x, y = y };
            if (_n < Cap) _n++;
        }

        public bool Sample(double now, out float x, out float y)
        {
            x = y = 0f;
            if (_n == 0) return false;
            var newest = At(0);
            if (_n == 1) { x = newest.x; y = newest.y; return true; }
            double rt = now - Delay;
            // find the pair around rt (newest first)
            for (int i = 0; i < _n - 1; i++)
            {
                var b = At(i);
                var a = At(i + 1);
                if (rt >= a.t || i == _n - 2)
                {
                    double span = b.t - a.t;
                    if (span <= 1e-6) { x = b.x; y = b.y; return true; }
                    double u = (rt - a.t) / span;
                    double maxU = i == 0 ? 1.0 + MaxExtrapolate / span : 1.0;
                    if (u < 0) u = 0;
                    if (u > maxU) u = maxU;
                    x = (float)(a.x + (b.x - a.x) * u);
                    y = (float)(a.y + (b.y - a.y) * u);
                    return true;
                }
            }
            x = newest.x; y = newest.y;
            return true;
        }
    }
}
