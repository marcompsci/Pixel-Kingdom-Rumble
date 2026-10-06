using System;

namespace PKR.Core
{
    public enum TrapPhase { Falling, Arming, Armed, Spent }

    /// <summary>
    /// Life cycle of a placed trap (Luma's Spark Coil): it falls until it lands, arms after a short delay,
    /// stays armed for its lifetime, and is spent after it triggers once or times out.
    /// The runtime moves it and checks for targets; this class decides when it may fire.
    /// </summary>
    public class TrapLogic
    {
        public float ArmDelay { get; }
        public float Lifetime { get; }
        public float MaxFallTime { get; }
        public TrapPhase Phase { get; private set; } = TrapPhase.Falling;
        public bool IsArmed => Phase == TrapPhase.Armed;
        public bool IsSpent => Phase == TrapPhase.Spent;
        /// <summary>Seconds left while armed (for a fading visual).</summary>
        public float TimeLeft => Phase == TrapPhase.Armed ? Math.Max(0f, Lifetime - _timer) : 0f;

        float _timer;

        public TrapLogic(float armDelay, float lifetime, float maxFallTime = 3f)
        {
            if (armDelay < 0f) throw new ArgumentOutOfRangeException(nameof(armDelay));
            if (lifetime <= 0f) throw new ArgumentOutOfRangeException(nameof(lifetime));
            if (maxFallTime <= 0f) throw new ArgumentOutOfRangeException(nameof(maxFallTime));
            ArmDelay = armDelay;
            Lifetime = lifetime;
            MaxFallTime = maxFallTime;
        }

        /// <summary>Touched the ground: start arming. Ignored unless falling.</summary>
        public void Land()
        {
            if (Phase != TrapPhase.Falling) return;
            Phase = ArmDelay > 0f ? TrapPhase.Arming : TrapPhase.Armed;
            _timer = 0f;
        }

        /// <summary>Advance time. A trap that never lands (fell off the stage) is spent after MaxFallTime.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f || Phase == TrapPhase.Spent) return;
            _timer += dt;
            switch (Phase)
            {
                case TrapPhase.Falling:
                    if (_timer >= MaxFallTime) Phase = TrapPhase.Spent;
                    break;
                case TrapPhase.Arming:
                    if (_timer >= ArmDelay)
                    {
                        _timer -= ArmDelay;
                        Phase = TrapPhase.Armed;
                        if (_timer >= Lifetime) Phase = TrapPhase.Spent;
                    }
                    break;
                case TrapPhase.Armed:
                    if (_timer >= Lifetime) Phase = TrapPhase.Spent;
                    break;
            }
        }

        /// <summary>A target touched it. Returns true (and spends the trap) only if it was armed.</summary>
        public bool TryTrigger()
        {
            if (Phase != TrapPhase.Armed) return false;
            Phase = TrapPhase.Spent;
            return true;
        }

        /// <summary>Remove early (a newer trap from the same owner replaces it).</summary>
        public void Expire() => Phase = TrapPhase.Spent;
    }
}
