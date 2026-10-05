namespace PKR.Core
{
    /// <summary>
    /// One logical button (Attack, Special, Dodge, Jump). Input sources call Press/SetHeld from Update;
    /// gameplay consumes from FixedUpdate. A press stays consumable for BufferWindow seconds so inputs
    /// made slightly early (e.g. during recovery) still come out.
    /// PressCount lets a consumer detect new presses without consuming them (used by JumpAssist).
    /// </summary>
    public class ActionBuffer
    {
        public float BufferWindow { get; set; }
        public bool Held { get; private set; }
        public int PressCount { get; private set; }

        float _pressedAt = float.NegativeInfinity;

        public ActionBuffer(float bufferWindow = 0.12f) { BufferWindow = bufferWindow; }

        public void Press(float now)
        {
            _pressedAt = now;
            Held = true;
            PressCount++;
        }

        public void SetHeld(bool held) => Held = held;

        public bool IsBuffered(float now) => now - _pressedAt <= BufferWindow;

        /// <summary>True once per press if still inside the buffer window.</summary>
        public bool Consume(float now)
        {
            if (!IsBuffered(now)) return false;
            _pressedAt = float.NegativeInfinity;
            return true;
        }

        public void Clear()
        {
            _pressedAt = float.NegativeInfinity;
            Held = false;
        }
    }
}
