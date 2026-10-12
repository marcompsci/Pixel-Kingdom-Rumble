using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Match twists (Daily Rumble mutators) applied globally while an arena match runs:
    /// gravity for every PlatformerMotor2D, knockback for arena-model hits, and fighter size.
    /// Always reset when the match is cleared, so Story and Contracts never see them.
    /// </summary>
    public static class ArenaMods
    {
        public const float LowGravityScale = 0.6f;
        public const float HeavyKnockbackScale = 1.35f;
        public const float GiantSizeScale = 1.3f;

        public static Mutator Active { get; private set; }
        public static float GravityScale { get; private set; } = 1f;
        public static float KnockbackScale { get; private set; } = 1f;
        public static float SizeScale { get; private set; } = 1f;

        public static void Apply(Mutator m)
        {
            Active = m;
            GravityScale = (m & Mutator.LowGravity) != 0 ? LowGravityScale : 1f;
            KnockbackScale = (m & Mutator.HeavyHitters) != 0 ? HeavyKnockbackScale : 1f;
            SizeScale = (m & Mutator.Giants) != 0 ? GiantSizeScale : 1f;
        }

        public static void Reset() => Apply(Mutator.None);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();
    }
}
