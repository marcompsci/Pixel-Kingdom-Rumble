using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Hurts whatever (of another team) overlaps this area: enemy bodies, spikes, saw blades.
    /// The target's post-hit invulnerability stops repeated hits while it stays in contact.
    /// </summary>
    public class ContactDamage : MonoBehaviour
    {
        public HitData hit = new HitData
        {
            damage = 1, pipDamage = 1, baseKnockback = 7f, exposedMultiplier = 1f,
            angleDegrees = 45f, baseHitstunFrames = 18, hitstopFrames = 4, isHeavy = false
        };
        public int team = TeamIds.Enemy;
        public Vector2 size = new Vector2(0.9f, 0.8f);
        public Vector2 offset;
        [Tooltip("Spikes: always push straight up instead of away from the center.")]
        public bool launchUpward;
        [Tooltip("Don't hurt anyone while this fighter is in hitstun (fairer when you've just hit it).")]
        public bool inactiveWhileStunned = true;

        Damageable _self;
        PlatformerMotor2D _motor;
        readonly List<Damageable> _targets = new List<Damageable>();

        void Awake()
        {
            _self = GetComponent<Damageable>();
            _motor = GetComponent<PlatformerMotor2D>();
        }

        void FixedUpdate()
        {
            if (_self != null && _self.IsDead) return;
            if (inactiveWhileStunned && _motor != null && _motor.IsControlLocked) return;

            Vector2 center = (Vector2)transform.position + offset;
            CombatQuery.OverlapBox(center, size, _targets);
            foreach (var t in _targets)
            {
                if (t == _self) continue;
                int dir = t.transform.position.x >= center.x ? 1 : -1;
                var h = hit;
                if (launchUpward) h.angleDegrees = 80f;
                if (!t.TakeHit(h, dir, team, out var result)) continue;
                if (t.GetComponent<PlayerMarker>() != null)
                {
                    HitStop.Request(result.hitstopFrames);
                    if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
                    if (CameraFollow2D.Main != null) CameraFollow2D.Main.Shake(0.12f, 0.15f);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.6f);
            Gizmos.DrawWireCube((Vector2)transform.position + offset, size);
        }
    }
}
