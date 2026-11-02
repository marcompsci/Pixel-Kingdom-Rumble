using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A CPU racer in the Item Race: runs right, jumps walls and gaps (double-jumping wide ones), and hops out when it
    /// gets stuck. Its speed is set by RaceDirector (a little catch-up so the race stays close).
    /// </summary>
    public class RacerBot : MonoBehaviour
    {
        PlatformerMotor2D _motor;
        float _stuck, _lastX, _holdUntil, _nextAirJump;
        System.Random _rng;
        public bool Running { get; set; }

        public void Init(int seed)
        {
            _motor = GetComponent<PlatformerMotor2D>();
            _rng = new System.Random(seed);
            _lastX = transform.position.x;
        }

        void Update()
        {
            if (_motor == null || _motor.Body == null) return;
            var intent = _motor.Intent;
            if (!Running)
            {
                intent.Move = Vector2.zero;
                return;
            }
            float now = Time.time;
            intent.Move = new Vector2(1f, 0f);
            Vector2 p = _motor.Body.position;
            int ground = PKRLayers.GroundMask;
            bool wall = Physics2D.Raycast(p + new Vector2(0f, -0.35f), Vector2.right, 1.0f, ground)
                        || Physics2D.Raycast(p + new Vector2(0f, 0.25f), Vector2.right, 1.0f, ground);
            bool gapAhead = !Physics2D.Raycast(p + new Vector2(1.1f, 0f), Vector2.down, 2.5f, ground);
            if (_motor.IsGrounded)
            {
                if (wall || gapAhead) Jump(now);
            }
            else if (_motor.Velocity.y < 0f && gapAhead && _motor.AirJumpsLeft > 0 && now >= _nextAirJump)
            {
                Jump(now); // a wide gap: double jump
                _nextAirJump = now + 0.4f;
            }
            float dx = Mathf.Abs(p.x - _lastX);
            _lastX = p.x;
            _stuck = dx < 0.01f ? _stuck + Time.deltaTime : 0f;
            if (_stuck > 0.5f && _motor.IsGrounded)
            {
                Jump(now);
                _stuck = 0f;
            }
            intent.Jump.SetHeld(now < _holdUntil);
        }

        void Jump(float now)
        {
            _motor.Intent.Jump.Press(now);
            _holdUntil = now + 0.28f + (float)_rng.NextDouble() * 0.08f;
        }
    }
}
