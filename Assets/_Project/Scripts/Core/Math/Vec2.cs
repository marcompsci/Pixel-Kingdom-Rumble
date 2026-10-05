using System;

namespace PKR.Core
{
    /// <summary>
    /// Minimal engine-free 2D vector so Core logic can be unit-tested outside Unity.
    /// Runtime code converts to/from UnityEngine.Vector2 at the boundary.
    /// </summary>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public float x;
        public float y;

        public Vec2(float x, float y) { this.x = x; this.y = y; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        public float Magnitude => (float)Math.Sqrt(x * x + y * y);

        public static Vec2 FromAngleDegrees(float degrees, float length = 1f)
        {
            double rad = degrees * Math.PI / 180.0;
            return new Vec2((float)Math.Cos(rad) * length, (float)Math.Sin(rad) * length);
        }

        public static Vec2 operator *(Vec2 v, float s) => new Vec2(v.x * s, v.y * s);
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);

        public bool Equals(Vec2 other) => x.Equals(other.x) && y.Equals(other.y);
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => (x.GetHashCode() * 397) ^ y.GetHashCode();
        public override string ToString() => $"({x:0.###}, {y:0.###})";
    }
}
