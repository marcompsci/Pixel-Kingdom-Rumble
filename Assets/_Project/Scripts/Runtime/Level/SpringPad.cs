using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>A bounce pad: landing on it launches the hero high (keeps their sideways speed).</summary>
    public class SpringPad : MonoBehaviour
    {
        [Tooltip("Launch height in world units.")]
        public float launchHeight = 9.5f;
        public Transform art;
        float _squash;

        void FixedUpdate()
        {
            if (_squash > 0f) _squash -= Time.fixedDeltaTime;
            if (art != null) art.localScale = new Vector3(1f, _squash > 0f ? 0.6f : 1f, 1f);
            if (!PropUtil.TryPlayer(out var motor, out var b)) return;
            Vector2 c = transform.position;     // pad top
            bool over = Mathf.Abs(b.center.x - c.x) <= 0.75f && b.min.y >= c.y - 0.15f && b.min.y <= c.y + 0.3f;
            if (!over || motor.Velocity.y > 0.5f || motor.IsControlLocked) return;
            var st = motor.Stats;
            float g = JumpPhysics.Gravity(st.jumpHeight, st.timeToApex);
            float vy = Mathf.Sqrt(2f * g * launchHeight);
            motor.ApplyKnockback(new Vector2(motor.Velocity.x, vy), 0f);
            _squash = 0.15f;
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light);
        }
    }
}
