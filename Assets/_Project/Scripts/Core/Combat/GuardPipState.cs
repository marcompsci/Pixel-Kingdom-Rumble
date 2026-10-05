using System;

namespace PKR.Core
{
    /// <summary>
    /// Arena Clash defensive resource. Hits remove pips; at 0 the fighter is Exposed and
    /// heavy hits launch hard. Pips regenerate one at a time after a quiet period.
    /// </summary>
    [Serializable]
    public class GuardPipState
    {
        public int MaxPips { get; }
        public int Current { get; private set; }
        public float RegenDelay { get; }
        public float RegenInterval { get; }

        float _timeSinceHit;
        float _regenAccumulator;

        public bool IsExposed => Current <= 0;

        public GuardPipState(int maxPips = 3, float regenDelay = 3f, float regenInterval = 1f)
        {
            if (maxPips < 1) throw new ArgumentOutOfRangeException(nameof(maxPips));
            if (regenDelay < 0f) throw new ArgumentOutOfRangeException(nameof(regenDelay));
            if (regenInterval <= 0f) throw new ArgumentOutOfRangeException(nameof(regenInterval));
            MaxPips = maxPips;
            RegenDelay = regenDelay;
            RegenInterval = regenInterval;
            Reset();
        }

        public void Reset()
        {
            Current = MaxPips;
            _timeSinceHit = 0f;
            _regenAccumulator = 0f;
        }

        /// <summary>Removes pips. Returns true if this hit made the fighter Exposed.</summary>
        public bool ApplyHit(int pipDamage)
        {
            bool wasExposed = IsExposed;
            Current = Math.Max(0, Current - Math.Max(0, pipDamage));
            _timeSinceHit = 0f;
            _regenAccumulator = 0f;
            return !wasExposed && IsExposed;
        }

        /// <summary>Advance regen. Returns number of pips restored this tick.</summary>
        public int Tick(float deltaTime)
        {
            if (deltaTime <= 0f || Current >= MaxPips) return 0;
            _timeSinceHit += deltaTime;
            if (_timeSinceHit < RegenDelay) return 0;

            // Only the portion of time past the delay counts toward regen.
            float effective = Math.Min(deltaTime, _timeSinceHit - RegenDelay);
            _regenAccumulator += effective;
            int restored = 0;
            while (_regenAccumulator >= RegenInterval && Current < MaxPips)
            {
                _regenAccumulator -= RegenInterval;
                Current++;
                restored++;
            }
            if (Current >= MaxPips) _regenAccumulator = 0f;
            return restored;
        }
    }
}
