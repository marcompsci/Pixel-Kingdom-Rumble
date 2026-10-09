using System;

namespace PKR.Core
{
    public enum GuardAwareness { Calm, Suspicious, Alerted }

    /// <summary>Pure stealth rules (unit-tested): what a guard can see, how fast it notices, takedowns, ranks.</summary>
    public static class StealthRules
    {
        /// <summary>Is the target inside a guard's sight cone (range + half angle, facing +1/-1)?</summary>
        public static bool InCone(Vec2 guard, int facing, Vec2 target, float range, float halfAngleDegrees)
        {
            float dx = target.x - guard.x, dy = target.y - guard.y;
            float dist2 = dx * dx + dy * dy;
            if (dist2 > range * range) return false;
            if (dist2 < 0.0001f) return true;
            float forward = dx * (facing >= 0 ? 1f : -1f);
            if (forward <= 0f) return false; // behind or level with the guard
            float angle = (float)(Math.Atan2(Math.Abs(dy), forward) * 180.0 / Math.PI);
            return angle <= halfAngleDegrees;
        }

        /// <summary>A silent takedown: the guard hasn't spotted anyone and the attacker is behind it.</summary>
        public static bool IsTakedown(GuardAwareness awareness, int guardFacing, float guardX, float attackerX)
        {
            if (awareness == GuardAwareness.Alerted) return false;
            float side = attackerX - guardX;
            return guardFacing >= 0 ? side < 0f : side > 0f;
        }

        /// <summary>Stealth grade for the results screen.</summary>
        public static string Rank(int timesSpotted, int takedowns)
        {
            if (timesSpotted == 0) return takedowns == 0 ? "GHOST" : "SHADOW";
            if (timesSpotted <= 2) return "AGENT";
            return "BRAWLER";
        }
    }

    /// <summary>
    /// How aware one guard is. Seeing the hero fills the meter (faster up close); not seeing it drains it.
    /// Full meter = Alerted (the guard chases) for AlertSeconds after it last saw the hero, then it calms down
    /// to Suspicious with a half-full meter.
    /// </summary>
    public class DetectionMeter
    {
        public float FillPerSecond { get; }
        public float DrainPerSecond { get; }
        public float AlertSeconds { get; }

        public float Value { get; private set; }
        public GuardAwareness Awareness { get; private set; } = GuardAwareness.Calm;
        float _alertTimer;

        public DetectionMeter(float fillPerSecond = 1.6f, float drainPerSecond = 0.5f, float alertSeconds = 4f)
        {
            FillPerSecond = fillPerSecond;
            DrainPerSecond = drainPerSecond;
            AlertSeconds = alertSeconds;
        }

        /// <summary>
        /// Advance by dt. closeness 0..1 (1 = right next to the guard) speeds up noticing.
        /// Returns true on the step the guard becomes Alerted.
        /// </summary>
        public bool Tick(float dt, bool seesHero, float closeness = 0f)
        {
            if (dt <= 0f) return false;
            if (Awareness == GuardAwareness.Alerted)
            {
                if (seesHero) _alertTimer = AlertSeconds;
                else _alertTimer -= dt;
                if (_alertTimer <= 0f)
                {
                    Awareness = GuardAwareness.Suspicious;
                    Value = 0.5f;
                }
                return false;
            }

            if (seesHero) Value += dt * FillPerSecond * (1f + 2f * Clamp01(closeness));
            else Value -= dt * DrainPerSecond;
            Value = Clamp01(Value);

            if (Value >= 1f)
            {
                Awareness = GuardAwareness.Alerted;
                _alertTimer = AlertSeconds;
                return true;
            }
            Awareness = Value > 0.15f ? GuardAwareness.Suspicious : GuardAwareness.Calm;
            return false;
        }

        /// <summary>Snap straight to Alerted (the guard was hit from the front). True if it wasn't already.</summary>
        public bool Alert()
        {
            _alertTimer = AlertSeconds;
            Value = 1f;
            if (Awareness == GuardAwareness.Alerted) return false;
            Awareness = GuardAwareness.Alerted;
            return true;
        }

        public void Reset()
        {
            Value = 0f;
            _alertTimer = 0f;
            Awareness = GuardAwareness.Calm;
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
