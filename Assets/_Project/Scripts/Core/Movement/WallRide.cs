using System;

namespace PKR.Core
{
    /// <summary>
    /// Wall-ride rules (Rex Rollo): hit a wall in the air fast enough while holding toward it and you ride up it
    /// for a short time; jump during the ride to kick off the other way. One ride per airtime.
    /// </summary>
    public class WallRide
    {
        public bool IsActive { get; private set; }
        /// <summary>+1 = wall on the right, -1 = wall on the left (valid while active).</summary>
        public int WallDir { get; private set; }
        public bool UsedThisAirtime { get; private set; }
        public float TimeLeft { get; private set; }

        /// <summary>Start a ride if every condition holds. Returns true if a ride started.</summary>
        public bool TryStart(bool airborne, bool touchingWall, int wallDir, float inputX, float speedIntoWall,
                             float rideTime, float minSpeed)
        {
            if (IsActive || UsedThisAirtime || rideTime <= 0f || !airborne || !touchingWall || wallDir == 0) return false;
            int dir = wallDir > 0 ? 1 : -1;
            if (Math.Abs(inputX) < 0.3f || Math.Sign(inputX) != dir) return false; // must hold toward the wall
            if (speedIntoWall < minSpeed) return false;
            IsActive = true;
            UsedThisAirtime = true;
            WallDir = dir;
            TimeLeft = rideTime;
            return true;
        }

        public void Tick(float dt)
        {
            if (!IsActive || dt <= 0f) return;
            TimeLeft -= dt;
            if (TimeLeft <= 0f) End();
        }

        /// <summary>Jump pressed during a ride: ends it. Returns the direction to kick off toward (-WallDir), or 0 if not riding.</summary>
        public int TryWallJump()
        {
            if (!IsActive) return 0;
            int away = -WallDir;
            End();
            return away;
        }

        public void End()
        {
            IsActive = false;
            TimeLeft = 0f;
        }

        /// <summary>Touching the ground restores the ride.</summary>
        public void Land()
        {
            End();
            UsedThisAirtime = false;
        }
    }
}
