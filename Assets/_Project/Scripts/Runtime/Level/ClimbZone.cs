using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A climbable vine. Push up (or down in the air) while overlapping it to grab on; climb with the stick,
    /// jump to let go, and climbing off the top hops you onto the ledge beside it.
    /// </summary>
    public class ClimbZone : MonoBehaviour
    {
        public Vector2 size = new Vector2(1f, 6f);
        [Tooltip("+1: the ledge at the top is to the right; -1: to the left.")]
        public int hopDirection = 1;
        public float climbSpeed = 4.2f;

        bool _climbing;
        float _cooldown;

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_cooldown > 0f) _cooldown -= dt;
            if (!PropUtil.TryPlayer(out var motor, out var b)) { _climbing = false; return; }

            Vector2 c = transform.position;
            float top = c.y + size.y * 0.5f, bottom = c.y - size.y * 0.5f;
            Vector2 p = motor.Body.position;
            bool inside = Mathf.Abs(p.x - c.x) <= size.x * 0.5f + 0.35f && b.min.y < top && b.max.y > bottom;
            var intent = motor.Intent;

            if (_climbing)
            {
                if (!inside || motor.IsControlLocked) { Release(motor, null); return; }
                if (intent.Jump.Consume(Time.time))
                {
                    Release(motor, new Vector2(intent.Move.x * 6f, 9f));
                    return;
                }
                float vy = Mathf.Abs(intent.Move.y) > 0.25f ? Mathf.Sign(intent.Move.y) * climbSpeed : 0f;
                if (b.min.y >= top - 0.35f && vy > 0f)
                {
                    // Off the top: hop sideways onto the ledge.
                    motor.SetFacing(hopDirection);
                    Release(motor, new Vector2(hopDirection * 4.5f, 7.5f));
                    return;
                }
                if (vy < 0f && motor.IsGrounded) { Release(motor, Vector2.zero); return; }
                float pullX = (c.x - p.x) * 10f; // keep centered on the vine
                motor.SetOverride(new Vector2(pullX, vy));
                return;
            }

            if (inside && _cooldown <= 0f && !motor.IsControlLocked && !motor.HasOverride &&
                (intent.Move.y > 0.5f || (intent.Move.y < -0.5f && !motor.IsGrounded)))
            {
                _climbing = true;
                motor.SetOverride(Vector2.zero);
            }
        }

        void Release(PlatformerMotor2D motor, Vector2? exit)
        {
            _climbing = false;
            _cooldown = 0.3f;
            motor.ClearOverride(exit);
        }

        void OnDisable() => _climbing = false;
    }
}
