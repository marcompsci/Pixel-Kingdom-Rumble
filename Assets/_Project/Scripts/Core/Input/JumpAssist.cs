using System;

namespace PKR.Core
{
    /// <summary>
    /// Coyote time + jump buffering. Feed it the grounded state and jump presses each frame;
    /// ask TryConsumeJump() to know whether a jump should start this frame.
    /// </summary>
    public class JumpAssist
    {
        public float CoyoteTime { get; set; }
        public float BufferTime { get; set; }

        float _sinceGrounded = float.MaxValue;
        float _sincePressed = float.MaxValue;
        bool _jumpedSinceGrounded;

        public JumpAssist(float coyoteTime = 0.1f, float bufferTime = 0.12f)
        {
            if (coyoteTime < 0f) throw new ArgumentOutOfRangeException(nameof(coyoteTime));
            if (bufferTime < 0f) throw new ArgumentOutOfRangeException(nameof(bufferTime));
            CoyoteTime = coyoteTime;
            BufferTime = bufferTime;
        }

        /// <summary>Call once per physics step before TryConsumeJump.</summary>
        public void Tick(float deltaTime, bool grounded)
        {
            if (grounded)
            {
                _sinceGrounded = 0f;
                _jumpedSinceGrounded = false;
            }
            else if (_sinceGrounded < float.MaxValue)
            {
                _sinceGrounded += deltaTime;
            }
            if (_sincePressed < float.MaxValue) _sincePressed += deltaTime;
        }

        /// <summary>Register a jump press (call on button down).</summary>
        public void PressJump() => _sincePressed = 0f;

        public bool HasBufferedJump => _sincePressed <= BufferTime;
        public bool CanUseGroundJump => !_jumpedSinceGrounded && _sinceGrounded <= CoyoteTime;

        /// <summary>Returns true (and clears the buffer) if a ground/coyote jump should fire now.</summary>
        public bool TryConsumeJump()
        {
            if (!HasBufferedJump || !CanUseGroundJump) return false;
            _sincePressed = float.MaxValue;
            _jumpedSinceGrounded = true;
            _sinceGrounded = float.MaxValue;
            return true;
        }

        /// <summary>Consume the buffered press for an air action (e.g. double jump) without needing ground.</summary>
        public bool TryConsumeBufferedPress()
        {
            if (!HasBufferedJump) return false;
            _sincePressed = float.MaxValue;
            return true;
        }

        public void Reset()
        {
            _sinceGrounded = float.MaxValue;
            _sincePressed = float.MaxValue;
            _jumpedSinceGrounded = false;
        }
    }
}
