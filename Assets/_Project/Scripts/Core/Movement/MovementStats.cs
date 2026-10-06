using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// Designer-facing movement tuning for one character. Jump is authored as height + time-to-apex
    /// (what designers think in); gravity and launch speed are derived by JumpPhysics.
    /// Units: world units (1 unit = 16 px) and seconds.
    /// </summary>
    [Serializable]
    public class MovementStats
    {
        public float runSpeed = 7.5f;
        public float groundAcceleration = 70f;
        public float groundDeceleration = 80f;
        public float airAcceleration = 45f;
        public float airDeceleration = 25f;

        public float jumpHeight = 3.2f;
        public float timeToApex = 0.38f;
        /// <summary>Gravity multiplier while falling (snappier descent).</summary>
        public float fallGravityMultiplier = 1.7f;
        /// <summary>Vertical speed multiplier applied once when jump is released while rising.</summary>
        public float jumpCutMultiplier = 0.45f;
        public float maxFallSpeed = 18f;
        /// <summary>Below this |vy| near the apex, gravity is reduced while jump is held (floaty apex).</summary>
        public float apexHangThreshold = 1.2f;
        public float apexGravityMultiplier = 0.55f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;
        public int airJumps = 0;

        // ---- Momentum (Rex Rollo). Negative = off, i.e. use the normal rates. -------------------------
        /// <summary>Ground deceleration with no input, and how fast speed above runSpeed bleeds off. &lt; 0 = groundDeceleration.</summary>
        public float coastDeceleration = -1f;
        /// <summary>Ground deceleration when pushing against the direction of travel (a skid). &lt; 0 = normal turnaround.</summary>
        public float skidDeceleration = -1f;

        // ---- Wall ride (Rex Rollo). wallRideTime 0 = off. --------------------------------------------
        /// <summary>Seconds a wall ride lasts (once per airtime). 0 disables wall riding.</summary>
        public float wallRideTime = 0f;
        /// <summary>Upward speed while riding a wall.</summary>
        public float wallRideSpeed = 6f;
        /// <summary>Horizontal speed needed when hitting the wall to start a ride.</summary>
        public float wallRideMinSpeed = 4f;
        /// <summary>Horizontal push away from the wall on a wall jump (vertical uses the normal jump).</summary>
        public float wallJumpSpeedX = 8f;

        /// <summary>Returns human-readable problems; empty when valid.</summary>
        public List<string> Validate()
        {
            var e = new List<string>();
            if (runSpeed <= 0f) e.Add("runSpeed must be > 0");
            if (groundAcceleration <= 0f || groundDeceleration <= 0f) e.Add("ground acceleration/deceleration must be > 0");
            if (airAcceleration <= 0f || airDeceleration < 0f) e.Add("air acceleration must be > 0, deceleration >= 0");
            if (jumpHeight <= 0f) e.Add("jumpHeight must be > 0");
            if (timeToApex <= 0.05f) e.Add("timeToApex must be > 0.05");
            if (fallGravityMultiplier < 1f) e.Add("fallGravityMultiplier should be >= 1");
            if (jumpCutMultiplier <= 0f || jumpCutMultiplier > 1f) e.Add("jumpCutMultiplier must be in (0,1]");
            if (maxFallSpeed <= 0f) e.Add("maxFallSpeed must be > 0");
            if (apexHangThreshold < 0f) e.Add("apexHangThreshold must be >= 0");
            if (apexGravityMultiplier <= 0f || apexGravityMultiplier > 1f) e.Add("apexGravityMultiplier must be in (0,1]");
            if (coyoteTime < 0f || coyoteTime > 0.3f) e.Add("coyoteTime must be in [0,0.3]");
            if (jumpBufferTime < 0f || jumpBufferTime > 0.3f) e.Add("jumpBufferTime must be in [0,0.3]");
            if (airJumps < 0) e.Add("airJumps must be >= 0");
            if (wallRideTime < 0f) e.Add("wallRideTime must be >= 0");
            if (wallRideTime > 0f && (wallRideSpeed <= 0f || wallJumpSpeedX < 0f || wallRideMinSpeed < 0f))
                e.Add("wall ride speeds must be positive");
            return e;
        }
    }

    /// <summary>Pure movement math used by PlatformerMotor2D.</summary>
    public static class JumpPhysics
    {
        /// <summary>Gravity magnitude so that a jump at JumpVelocity peaks at height after timeToApex.</summary>
        public static float Gravity(float height, float timeToApex) => 2f * height / (timeToApex * timeToApex);

        public static float JumpVelocity(float height, float timeToApex) => 2f * height / timeToApex;

        /// <summary>
        /// Gravity multiplier for the current vertical speed: fall multiplier when descending,
        /// apex hang when near the top with jump still held, else 1.
        /// </summary>
        public static float GravityMultiplier(float verticalSpeed, bool jumpHeld, MovementStats s)
        {
            if (jumpHeld && Math.Abs(verticalSpeed) < s.apexHangThreshold) return s.apexGravityMultiplier;
            if (verticalSpeed < 0f) return s.fallGravityMultiplier;
            return 1f;
        }

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }

        /// <summary>
        /// Next horizontal velocity. inputX in [-1,1]. Uses acceleration when pushing in the direction of travel,
        /// deceleration when releasing; turning around uses the larger of the two so reversals feel crisp.
        /// Momentum heroes (coast/skid set) glide when released, skid when reversing and keep extra speed longer.
        /// </summary>
        public static float NextHorizontalVelocity(float current, float inputX, MovementStats s, bool grounded, float dt)
        {
            float x = inputX < -1f ? -1f : (inputX > 1f ? 1f : inputX);
            float target = x * s.runSpeed;
            float accel = grounded ? s.groundAcceleration : s.airAcceleration;
            float decel = grounded ? s.groundDeceleration : s.airDeceleration;
            bool coast = grounded && s.coastDeceleration >= 0f;
            bool skid = grounded && s.skidDeceleration >= 0f;
            float rate;
            if (Math.Abs(x) < 0.01f) rate = coast ? s.coastDeceleration : decel;
            else if (current != 0f && Math.Sign(target) != Math.Sign(current)) rate = skid ? s.skidDeceleration : Math.Max(accel, decel);
            else if (coast && Math.Abs(current) > Math.Abs(target)) rate = s.coastDeceleration; // overspeed bleeds off slowly
            else rate = accel;
            return MoveTowards(current, target, rate * dt);
        }

        /// <summary>True while pushing against the direction of travel on the ground (for skid visuals/sparks).</summary>
        public static bool IsSkidding(float current, float inputX, bool grounded)
        {
            return grounded && Math.Abs(inputX) > 0.3f && Math.Abs(current) > 1f && Math.Sign(inputX) != Math.Sign(current);
        }

        /// <summary>Snap an analog direction to one of 8 directions (unit length). Returns false if below deadzone.</summary>
        public static bool SnapToEightWay(float x, float y, float deadzone, out Vec2 dir)
        {
            dir = Vec2.Zero;
            float mag = (float)Math.Sqrt(x * x + y * y);
            if (mag < deadzone) return false;
            double angle = Math.Atan2(y, x);
            double step = Math.PI / 4.0;
            double snapped = Math.Round(angle / step) * step;
            dir = new Vec2((float)Math.Cos(snapped), (float)Math.Sin(snapped));
            // Clean up float noise so cardinal directions are exact.
            if (Math.Abs(dir.x) < 1e-5f) dir.x = 0f;
            if (Math.Abs(dir.y) < 1e-5f) dir.y = 0f;
            return true;
        }
    }
}
