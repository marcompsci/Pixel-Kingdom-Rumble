using System;

namespace PKR.Core
{
    public enum BotLevel { Easy, Normal, Hard }

    /// <summary>Tuning for one CPU level. Higher reaction time = slower decisions.</summary>
    public struct BotDifficulty
    {
        public float reactionTime;   // seconds between decisions
        public float accuracy;       // chance to attack when a hit is possible
        public float aggression;     // 0..1 stick strength when approaching
        public float recoverySkill;  // chance to use the air dash when recovering
        public float dodgeChance;    // chance to dodge away when Exposed and threatened

        public static BotDifficulty For(BotLevel level)
        {
            switch (level)
            {
                case BotLevel.Easy: return new BotDifficulty { reactionTime = 0.36f, accuracy = 0.35f, aggression = 0.6f, recoverySkill = 0.5f, dodgeChance = 0.1f };
                case BotLevel.Hard: return new BotDifficulty { reactionTime = 0.12f, accuracy = 0.85f, aggression = 1f, recoverySkill = 1f, dodgeChance = 0.5f };
                default: return new BotDifficulty { reactionTime = 0.22f, accuracy = 0.6f, aggression = 0.85f, recoverySkill = 0.85f, dodgeChance = 0.25f };
            }
        }
    }

    /// <summary>What the bot can see this frame.</summary>
    public struct BotView
    {
        public Vec2 position;
        public Vec2 velocity;
        public bool grounded;
        public bool exposed;
        public bool canAirDash;
        public bool hasTarget;
        public Vec2 targetPosition;
        public bool targetExposed;
        /// <summary>Main stage edges (x) and floor height (y).</summary>
        public float stageLeft, stageRight, stageTop;
        /// <summary>An open hole in the stage (Skyforge bridge retracted), treated like two extra edges.</summary>
        public bool hasGap;
        public float gapLeft, gapRight;
    }

    /// <summary>One decision. Presses are one-shot; Move persists until the next decision.</summary>
    public struct BotCommand
    {
        public float moveX, moveY;
        public bool jump, jumpHeld, attack, special, dodge;
    }

    /// <summary>
    /// Simple, readable arena AI: recover to the stage, don't walk off edges, approach, attack in range,
    /// use the projectile at mid range, and back off when Exposed. Deterministic for a given Random seed.
    /// </summary>
    public static class BotBrain
    {
        public const float AttackRangeX = 1.4f;
        public const float AttackRangeY = 1.2f;
        public const float EdgeMargin = 0.8f;
        public const float ProjectileMin = 3f, ProjectileMax = 7f;

        public static bool IsOffstage(in BotView v) =>
            v.position.x < v.stageLeft || v.position.x > v.stageRight || v.position.y < v.stageTop - 0.6f;

        /// <summary>X to steer toward when recovering: stage center, or the nearest side platform when the gap is open.</summary>
        public static float RecoveryTargetX(in BotView v)
        {
            float center = (v.stageLeft + v.stageRight) * 0.5f;
            if (!v.hasGap) return center;
            float leftMid = (v.stageLeft + v.gapLeft) * 0.5f;
            float rightMid = (v.gapRight + v.stageRight) * 0.5f;
            return Math.Abs(v.position.x - leftMid) <= Math.Abs(v.position.x - rightMid) ? leftMid : rightMid;
        }

        public static BotCommand Decide(in BotView v, in BotDifficulty d, Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var c = new BotCommand();

            // 1) Recovery beats everything.
            if (!v.grounded && IsOffstage(v))
            {
                float toCenter = RecoveryTargetX(v) - v.position.x;
                c.moveX = Math.Abs(toCenter) < 0.3f ? 0f : Math.Sign(toCenter);
                if (v.velocity.y <= 0f && v.canAirDash && rng.NextDouble() < d.recoverySkill)
                {
                    c.moveY = 1f;
                    c.dodge = true; // Nova's air dash, aimed up and inward
                }
                return c;
            }

            if (!v.hasTarget) return c; // nothing to fight (e.g. last one standing)

            float dx = v.targetPosition.x - v.position.x;
            float dy = v.targetPosition.y - v.position.y;
            int toward = dx >= 0f ? 1 : -1;

            // 2) Exposed and threatened: back off or dodge.
            if (v.exposed && Math.Abs(dx) < 2.5f && Math.Abs(dy) < 2f)
            {
                if (rng.NextDouble() < d.dodgeChance) { c.dodge = true; c.moveX = -toward; }
                else c.moveX = -toward * d.aggression;
                return SafeEdges(v, c);
            }

            // 3) In range: attack (Up+Attack when the target is above).
            bool inRangeX = Math.Abs(dx) <= AttackRangeX;
            if (inRangeX && dy > 0.8f && dy < 3f && v.grounded)
            {
                c.moveX = 0f;
                c.moveY = 1f;
                c.attack = rng.NextDouble() < d.accuracy;
                return c;
            }
            if (inRangeX && Math.Abs(dy) <= AttackRangeY)
            {
                c.moveX = toward * 0.3f; // face the target without running past it
                c.attack = rng.NextDouble() < d.accuracy;
                return SafeEdges(v, c);
            }

            // 4) Mid range on the same level: sometimes throw the projectile.
            if (v.grounded && Math.Abs(dx) >= ProjectileMin && Math.Abs(dx) <= ProjectileMax && Math.Abs(dy) < 1f
                && rng.NextDouble() < d.accuracy * 0.25)
            {
                c.moveX = toward * 0.3f;
                c.special = true;
                return SafeEdges(v, c);
            }

            // 5) Approach; jump toward targets above.
            c.moveX = toward * d.aggression;
            if (v.grounded && dy > 1.5f && Math.Abs(dx) < 4f)
            {
                c.jump = true;
                c.jumpHeld = true;
            }
            return SafeEdges(v, c);
        }

        /// <summary>Don't walk off the main stage while grounded.</summary>
        public static BotCommand SafeEdges(in BotView v, BotCommand c)
        {
            if (!v.grounded) return c;
            bool nearLeft = v.position.x < v.stageLeft + EdgeMargin;
            bool nearRight = v.position.x > v.stageRight - EdgeMargin;
            if ((nearLeft && c.moveX < 0f) || (nearRight && c.moveX > 0f)) c.moveX = 0f;
            if (v.hasGap)
            {
                // Standing left of the gap: don't walk right into it; right of it: don't walk left.
                bool atGapFromLeft = v.position.x <= v.gapLeft && v.position.x > v.gapLeft - EdgeMargin;
                bool atGapFromRight = v.position.x >= v.gapRight && v.position.x < v.gapRight + EdgeMargin;
                if ((atGapFromLeft && c.moveX > 0f) || (atGapFromRight && c.moveX < 0f)) c.moveX = 0f;
            }
            return c;
        }
    }
}
