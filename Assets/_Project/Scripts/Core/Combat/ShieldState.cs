using System;

namespace PKR.Core
{
    public enum ShieldOutcome
    {
        /// <summary>Hit from behind (or the shield is down): the hit lands normally.</summary>
        Unshielded,
        /// <summary>Light hit into the shield: no damage, no knockback.</summary>
        Blocked,
        /// <summary>Heavy hit into the shield: it breaks, the hit lands, and the shield stays down for a while.</summary>
        Broken
    }

    /// <summary>
    /// A front shield (Bolt Knight). attackerFacing is the hit's push direction (+1 = pushes right), the convention
    /// Damageable uses; a hit is "from the front" when it pushes against the way the shield faces.
    /// </summary>
    public class ShieldState
    {
        public float BreakDuration { get; }
        public bool IsBroken => _broken > 0f;
        public float BrokenTimeLeft => Math.Max(0f, _broken);

        float _broken;

        public ShieldState(float breakDuration = 1.6f)
        {
            if (breakDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(breakDuration));
            BreakDuration = breakDuration;
        }

        /// <summary>Fallback when the attacker's position is unknown: judge "front" from the push direction.</summary>
        public ShieldOutcome Resolve(int shieldFacing, int attackerFacing, bool heavy)
        {
            int face = shieldFacing >= 0 ? 1 : -1;
            int push = attackerFacing >= 0 ? 1 : -1;
            return Resolve(push == -face, heavy);
        }

        /// <summary>
        /// Judge from where the hit came from: front when the source is on the side the shield faces.
        /// Hits from (nearly) straight above count as unshielded.
        /// </summary>
        public ShieldOutcome ResolveFromSource(int shieldFacing, float shieldX, float sourceX, bool heavy, float aboveTolerance = 0.3f)
        {
            float dx = sourceX - shieldX;
            if (Math.Abs(dx) < aboveTolerance) return Resolve(false, heavy);
            int face = shieldFacing >= 0 ? 1 : -1;
            return Resolve(Math.Sign(dx) == face, heavy);
        }

        public ShieldOutcome Resolve(bool fromFront, bool heavy)
        {
            if (IsBroken || !fromFront) return ShieldOutcome.Unshielded;
            if (!heavy) return ShieldOutcome.Blocked;
            _broken = BreakDuration;
            return ShieldOutcome.Broken;
        }

        public void Tick(float dt) { if (_broken > 0f && dt > 0f) _broken -= dt; }

        public void Reset() => _broken = 0f;
    }
}
