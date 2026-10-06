using System;

namespace PKR.Core
{
    /// <summary>
    /// Everything a single hit needs to resolve. Built from a MoveDefinition at runtime.
    /// Angles are authored for a fighter facing right; CombatMath mirrors them for left-facing attackers.
    /// </summary>
    [Serializable]
    public struct HitData
    {
        /// <summary>HP damage in Story Quest.</summary>
        public int damage;
        /// <summary>Guard Pips removed in Arena Clash.</summary>
        public int pipDamage;
        /// <summary>Launch speed (units/sec) while the target still has pips.</summary>
        public float baseKnockback;
        /// <summary>Multiplier applied to knockback when the target is Exposed (0 pips).</summary>
        public float exposedMultiplier;
        /// <summary>Launch angle in degrees, 0 = forward, 90 = straight up.</summary>
        public float angleDegrees;
        /// <summary>Minimum hitstun frames (at 60 FPS).</summary>
        public int baseHitstunFrames;
        /// <summary>Freeze frames applied to attacker and target on contact.</summary>
        public int hitstopFrames;
        /// <summary>Heavy hits are the ones that can launch an Exposed fighter hard.</summary>
        public bool isHeavy;

        public static HitData Light(int damage = 1) => new HitData
        {
            damage = damage, pipDamage = 1, baseKnockback = 4f, exposedMultiplier = 1.5f,
            angleDegrees = 20f, baseHitstunFrames = 8, hitstopFrames = 3, isHeavy = false
        };

        public static HitData Heavy(int damage = 2) => new HitData
        {
            damage = damage, pipDamage = 2, baseKnockback = 7f, exposedMultiplier = 3f,
            angleDegrees = 40f, baseHitstunFrames = 14, hitstopFrames = 6, isHeavy = true
        };
    }

    /// <summary>The resolved outcome of a hit, ready for the runtime to apply.</summary>
    public struct HitResult
    {
        public Vec2 knockbackVelocity;
        public int hitstunFrames;
        public int hitstopFrames;
        public int pipsRemaining;
        public bool targetWasExposed;
        /// <summary>True when an Exposed target took a heavy hit (big launch + VFX).</summary>
        public bool isLaunch;
        public int hpDamage;
        /// <summary>True when super armor absorbed the knockback and hitstun (damage/pips still applied).</summary>
        public bool armored;
    }
}
