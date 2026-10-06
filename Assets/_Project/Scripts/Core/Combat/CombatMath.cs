using System;

namespace PKR.Core
{
    /// <summary>
    /// Pure combat calculations shared by Story Quest and Arena Clash.
    /// No engine types, so every rule here is covered by EditMode / dotnet tests.
    /// </summary>
    public static class CombatMath
    {
        public const float MinWeight = 0.5f;
        public const float MaxWeight = 2.5f;
        /// <summary>Extra hitstun frames per unit of knockback speed.</summary>
        public const float HitstunPerKnockback = 0.6f;
        public const int MaxHitstunFrames = 60;
        public const int MaxHitstopFrames = 12;
        public const float MaxKnockback = 40f;

        /// <summary>
        /// Knockback velocity for a hit. attackerFacing is +1 (right) or -1 (left).
        /// Heavier fighters (weight &gt; 1) fly less far. Exposed targets take the exposed multiplier,
        /// but only heavy hits get the full multiplier; light hits get half the bonus.
        /// </summary>
        public static Vec2 Knockback(in HitData hit, bool targetExposed, float targetWeight, int attackerFacing)
        {
            float weight = Clamp(targetWeight, MinWeight, MaxWeight);
            float mult = 1f;
            if (targetExposed)
            {
                float bonus = Math.Max(0f, hit.exposedMultiplier - 1f);
                mult = 1f + (hit.isHeavy ? bonus : bonus * 0.5f);
            }
            float speed = Clamp(Math.Max(0f, hit.baseKnockback) * mult / weight, 0f, MaxKnockback);
            Vec2 dir = Vec2.FromAngleDegrees(hit.angleDegrees);
            int facing = attackerFacing >= 0 ? 1 : -1;
            return new Vec2(dir.x * facing * speed, dir.y * speed);
        }

        /// <summary>Hitstun grows with how hard the target was sent flying.</summary>
        public static int HitstunFrames(in HitData hit, float knockbackSpeed)
        {
            int frames = Math.Max(0, hit.baseHitstunFrames) + (int)Math.Round(Math.Max(0f, knockbackSpeed) * HitstunPerKnockback);
            return Math.Min(frames, MaxHitstunFrames);
        }

        public static int HitstopFrames(in HitData hit) => Math.Max(0, Math.Min(hit.hitstopFrames, MaxHitstopFrames));

        /// <summary>
        /// Story Quest damage after armor. armorPercent in [0,0.9]. Any hit with damage &gt; 0 deals at least 1.
        /// </summary>
        public static int DamageAfterArmor(int damage, float armorPercent)
        {
            if (damage <= 0) return 0;
            float armor = Clamp(armorPercent, 0f, 0.9f);
            int reduced = (int)Math.Floor(damage * (1f - armor));
            return Math.Max(1, reduced);
        }

        /// <summary>Full Arena resolution: applies pips, then computes launch and stun.</summary>
        public static HitResult ResolveArenaHit(in HitData hit, GuardPipState pips, float targetWeight, int attackerFacing)
        {
            if (pips == null) throw new ArgumentNullException(nameof(pips));
            bool exposedBefore = pips.IsExposed;
            pips.ApplyHit(hit.pipDamage);
            // A hit that breaks the last pip does not also get the exposed bonus; the *next* one does.
            Vec2 kb = Knockback(hit, exposedBefore, targetWeight, attackerFacing);
            return new HitResult
            {
                knockbackVelocity = kb,
                hitstunFrames = HitstunFrames(hit, kb.Magnitude),
                hitstopFrames = HitstopFrames(hit),
                pipsRemaining = pips.Current,
                targetWasExposed = exposedBefore,
                isLaunch = exposedBefore && hit.isHeavy,
                hpDamage = 0
            };
        }

        /// <summary>Story Quest resolution: HP damage with armor, fixed (non-exposed) knockback.</summary>
        public static HitResult ResolveStoryHit(in HitData hit, float armorPercent, float targetWeight, int attackerFacing)
        {
            Vec2 kb = Knockback(hit, false, targetWeight, attackerFacing);
            return new HitResult
            {
                knockbackVelocity = kb,
                hitstunFrames = HitstunFrames(hit, kb.Magnitude),
                hitstopFrames = HitstopFrames(hit),
                pipsRemaining = -1,
                targetWasExposed = false,
                isLaunch = false,
                hpDamage = DamageAfterArmor(hit.damage, armorPercent)
            };
        }

        /// <summary>
        /// Super armor (e.g. Brick's Bulwark Charge): the fighter keeps going through a hit. Damage and pips still
        /// apply, but knockback and hitstun are removed. A launch (heavy hit on an Exposed fighter) breaks armor.
        /// Returns true if the armor held.
        /// </summary>
        public static bool ApplySuperArmor(ref HitResult result)
        {
            if (result.isLaunch) return false;
            result.knockbackVelocity = new Vec2(0f, 0f);
            result.hitstunFrames = 0;
            result.armored = true;
            return true;
        }

        /// <summary>
        /// Momentum hits (Rex Rollo's Momentum Ram): the faster the attacker was moving when the move started, the harder
        /// it hits. ratio = speed / referenceSpeed (clamped 0..1); knockback x (1 + ratio * maxBonus);
        /// at ratio &gt;= 0.8 the hit also deals +1 damage and +1 Guard Pip.
        /// </summary>
        public static HitData ScaleBySpeed(in HitData hit, float speed, float referenceSpeed, float maxBonus)
        {
            var h = hit;
            if (referenceSpeed <= 0f || maxBonus <= 0f) return h;
            float ratio = Clamp(Math.Abs(speed) / referenceSpeed, 0f, 1f);
            h.baseKnockback = hit.baseKnockback * (1f + ratio * maxBonus);
            if (ratio >= 0.8f)
            {
                h.damage = hit.damage + 1;
                h.pipDamage = hit.pipDamage + 1;
            }
            return h;
        }

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}
