using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A heavy block that hangs from the ceiling, shakes as a warning, slams down to the floor, waits, and grinds back
    /// up. Its underside hurts while it falls; at rest it is solid (stand on it, or use it as a timed bridge).
    /// The builder sets drop (cells to the floor) and an offset so rows of crushers fall in a wave.
    /// </summary>
    public class Crusher : MonoBehaviour
    {
        public float drop = 3f;
        public float idle = 1.6f, shake = 0.5f, fallSpeed = 18f, hold = 0.6f, riseSpeed = 2.2f;
        public float offset;
        public ContactDamage hurt;
        public Transform art;

        enum Phase { Idle, Shake, Fall, Hold, Rise }
        Phase _phase;
        float _t, _y;
        Vector3 _top;
        Rigidbody2D _rb;

        public bool IsDangerous => _phase == Phase.Fall;

        void Awake()
        {
            _top = transform.position;
            _rb = GetComponent<Rigidbody2D>();
            _t = -offset;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _t += dt;
            switch (_phase)
            {
                case Phase.Idle: if (_t >= idle) Next(Phase.Shake); break;
                case Phase.Shake: if (_t >= shake) Next(Phase.Fall); break;
                case Phase.Fall:
                    _y = Mathf.Min(drop, _y + fallSpeed * dt);
                    if (_y >= drop)
                    {
                        Next(Phase.Hold);
                        if (CameraFollow2D.Main != null && Vector2.Distance(CameraFollow2D.Main.transform.position, transform.position) < 9f)
                            CameraFollow2D.Main.Shake(0.12f, 0.15f);
                        Sfx.Play(Sfx.Crate, 0.6f);
                    }
                    break;
                case Phase.Hold: if (_t >= hold) Next(Phase.Rise); break;
                case Phase.Rise:
                    _y = Mathf.Max(0f, _y - riseSpeed * dt);
                    if (_y <= 0f) Next(Phase.Idle);
                    break;
            }
            Vector3 p = _top + Vector3.down * _y;
            if (_rb != null) _rb.MovePosition(p); else transform.position = p;
            if (art != null) art.localPosition = _phase == Phase.Shake ? (Vector3)(Random.insideUnitCircle * 0.05f) : Vector3.zero;
            if (hurt != null && hurt.enabled != IsDangerous) hurt.enabled = IsDangerous;
        }

        void Next(Phase p) { _phase = p; _t = 0f; }
    }
}
