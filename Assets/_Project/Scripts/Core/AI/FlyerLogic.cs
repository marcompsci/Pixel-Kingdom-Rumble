using System;

namespace PKR.Core
{
    public enum FlyerState { Hover, Windup, Swoop, Return }

    /// <summary>Tuning for a swooping flyer (Gyro Moth).</summary>
    public struct FlyerTuning
    {
        public float bobHeight, bobSpeed, aggroRange, windup, swoopSpeed, swoopTime, returnSpeed, cooldown;

        public static FlyerTuning Default => new FlyerTuning
        {
            bobHeight = 0.4f, bobSpeed = 2.5f, aggroRange = 6f, windup = 0.55f,
            swoopSpeed = 9f, swoopTime = 0.8f, returnSpeed = 4f, cooldown = 1.4f
        };
    }

    /// <summary>
    /// Flyer brain: bob around home; when the hero comes in range, shake (windup), then swoop in a straight line at
    /// where the hero was when the windup ended, then fly back home and wait a cooldown. Being hit sends it home.
    /// Returns the velocity the flyer should hold this step.
    /// </summary>
    public class FlyerLogic
    {
        public FlyerTuning Tuning { get; }
        public FlyerState State { get; private set; } = FlyerState.Hover;
        public Vec2 SwoopDirection { get; private set; }

        /// <summary>Give up flying home after this long (stuck behind something) and hover where it is.</summary>
        public const float MaxReturnTime = 3f;

        float _timer, _cooldown, _clock;

        public FlyerLogic(FlyerTuning tuning, float initialCooldown = 0.5f)
        {
            if (tuning.swoopSpeed <= 0f || tuning.swoopTime <= 0f || tuning.returnSpeed <= 0f)
                throw new ArgumentException("flyer speeds and times must be positive");
            Tuning = tuning;
            _cooldown = Math.Max(0f, initialCooldown);
        }

        public Vec2 Step(float dt, Vec2 position, Vec2 home, bool hasTarget, Vec2 target)
        {
            if (dt <= 0f) return Vec2.Zero;
            _clock += dt;
            var t = Tuning;
            switch (State)
            {
                case FlyerState.Hover:
                {
                    if (_cooldown > 0f) _cooldown -= dt;
                    if (hasTarget && _cooldown <= 0f && (target - position).Magnitude <= t.aggroRange)
                    {
                        State = FlyerState.Windup;
                        _timer = t.windup;
                        return Vec2.Zero;
                    }
                    var bobPoint = home + new Vec2(0f, (float)Math.Sin(_clock * t.bobSpeed) * t.bobHeight);
                    return Seek(position, bobPoint, t.returnSpeed, 3f);
                }
                case FlyerState.Windup:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        var dir = hasTarget ? (target - position).Normalized : Vec2.Zero;
                        if (dir.Magnitude < 0.5f) { GoHome(); return Vec2.Zero; }
                        SwoopDirection = dir;
                        State = FlyerState.Swoop;
                        _timer = t.swoopTime;
                        return dir * t.swoopSpeed;
                    }
                    return Vec2.Zero;
                case FlyerState.Swoop:
                    _timer -= dt;
                    if (_timer <= 0f) { GoHome(); return Seek(position, home, t.returnSpeed, 3f); }
                    return SwoopDirection * t.swoopSpeed;
                default: // Return
                    _timer += dt;
                    if ((home - position).Magnitude <= 0.3f || _timer >= MaxReturnTime)
                    {
                        State = FlyerState.Hover;
                        _cooldown = t.cooldown;
                        return Vec2.Zero;
                    }
                    return Seek(position, home, t.returnSpeed, 3f);
            }
        }

        /// <summary>Hit, bumped a wall, or the target vanished: abort and fly home.</summary>
        public void Interrupt() { if (State != FlyerState.Hover) GoHome(); }

        void GoHome() { State = FlyerState.Return; _timer = 0f; }

        /// <summary>Velocity toward a point: proportional when close (gain), capped at maxSpeed.</summary>
        static Vec2 Seek(Vec2 from, Vec2 to, float maxSpeed, float gain)
        {
            var d = to - from;
            var v = d * gain;
            float m = v.Magnitude;
            return m > maxSpeed ? v * (maxSpeed / m) : v;
        }
    }
}
