using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Kinematic platform that ping-pongs between its start position and start + travel, easing at the ends
    /// and pausing briefly. PlatformerMotor2D reads Velocity to carry riders.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    // Runs after riders' motors so they compare against the velocity physics just used.
    [DefaultExecutionOrder(100)]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector2 travel = new Vector2(6f, 0f);
        [Tooltip("Seconds for one leg of the trip.")]
        public float legDuration = 2.5f;
        public float pauseAtEnds = 0.4f;
        [Tooltip("0..1 offset into the cycle, so several platforms can be out of sync.")]
        [Range(0f, 1f)] public float phaseOffset;

        public Vector2 Velocity { get; private set; }

        Rigidbody2D _rb;
        Vector2 _start;
        float _t;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _start = _rb.position;
            _t = phaseOffset * CycleLength;
        }

        float CycleLength => 2f * (Mathf.Max(0.05f, legDuration) + Mathf.Max(0f, pauseAtEnds));

        /// <summary>0..1 position along the path at cycle time t.</summary>
        float PathAt(float t)
        {
            float leg = Mathf.Max(0.05f, legDuration);
            float pause = Mathf.Max(0f, pauseAtEnds);
            t = Mathf.Repeat(t, CycleLength);
            if (t < leg) return Mathf.SmoothStep(0f, 1f, t / leg);
            t -= leg;
            if (t < pause) return 1f;
            t -= pause;
            if (t < leg) return Mathf.SmoothStep(1f, 0f, t / leg);
            return 0f;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _t += dt;
            Vector2 next = _start + travel * PathAt(_t);
            Velocity = (next - _rb.position) / dt;
            _rb.MovePosition(next);
        }

        void OnDrawGizmos()
        {
            Vector2 a = Application.isPlaying ? _start : (Vector2)transform.position;
            Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.8f);
            Gizmos.DrawLine(a, a + travel);
            Gizmos.DrawWireSphere(a + travel, 0.2f);
        }
    }
}
