using System;

namespace PKR.Core
{
    public enum WardenPhase { One = 1, Two = 2, Three = 3 }

    public enum WardenAction
    {
        Idle,
        /// <summary>Gear rolls in from one side and crosses the floor: jump it.</summary>
        SweepWindup, Sweep,
        /// <summary>Piston tracks the hero, locks on, then slams. The Warden is stuck afterwards: core exposed.</summary>
        SlamWindup, Slam, Stagger,
        /// <summary>Phase 2+: cogs drop from the ceiling around the hero.</summary>
        VolleyWindup, Volley,
        Defeated
    }

    /// <summary>Timings for one fight (seconds at phase 1; later phases run faster).</summary>
    public struct WardenTuning
    {
        public float idle, sweepWindup, sweep, slamWindup, slam, stagger, volleyWindup, volley;
        /// <summary>Health fractions where phase 2 and 3 begin.</summary>
        public float phaseTwoAt, phaseThreeAt;
        /// <summary>Fraction of the slam windup during which the piston follows the hero before locking on.</summary>
        public float slamTrackFraction;

        public static WardenTuning Default => new WardenTuning
        {
            idle = 0.8f, sweepWindup = 0.9f, sweep = 1.6f, slamWindup = 0.9f, slam = 0.3f, stagger = 2.5f,
            volleyWindup = 0.7f, volley = 1.2f, phaseTwoAt = 0.6f, phaseThreeAt = 0.25f, slamTrackFraction = 0.6f
        };
    }

    /// <summary>
    /// The Clockwork Warden's fight logic, engine-free. Cycles an attack pattern that depends on the phase,
    /// and is only vulnerable while staggered after a Piston Slam. The runtime moves the parts and deals damage;
    /// this class decides what happens when.
    /// </summary>
    public class WardenBrain
    {
        static readonly WardenAction[] PatternOne = { WardenAction.SlamWindup, WardenAction.SweepWindup, WardenAction.SlamWindup };
        static readonly WardenAction[] PatternTwo = { WardenAction.SweepWindup, WardenAction.SlamWindup, WardenAction.VolleyWindup, WardenAction.SlamWindup };
        static readonly WardenAction[] PatternThree = { WardenAction.SlamWindup, WardenAction.VolleyWindup, WardenAction.SweepWindup, WardenAction.SlamWindup };

        public WardenTuning Tuning { get; }
        public float ArenaLeft { get; }
        public float ArenaRight { get; }

        public WardenAction Current { get; private set; } = WardenAction.Idle;
        public WardenPhase Phase { get; private set; } = WardenPhase.One;
        public bool IsVulnerable => Current == WardenAction.Stagger;
        public bool IsDefeated => Current == WardenAction.Defeated;
        /// <summary>Where the slam lands (follows the hero early in the windup, then locks).</summary>
        public float SlamX { get; private set; }
        /// <summary>+1 = sweep travels left→right, -1 = right→left. Alternates each sweep.</summary>
        public int SweepDir { get; private set; } = 1;
        /// <summary>0..1 progress through the current action (for moving parts and telegraph visuals).</summary>
        public float Progress => _duration > 0f ? Math.Min(1f, _timer / _duration) : 1f;

        float _timer, _duration;
        int _patternIndex;

        public WardenBrain(float arenaLeft, float arenaRight) : this(arenaLeft, arenaRight, WardenTuning.Default) { }

        public WardenBrain(float arenaLeft, float arenaRight, WardenTuning tuning)
        {
            if (arenaRight <= arenaLeft) throw new ArgumentException("arenaRight must be greater than arenaLeft");
            ArenaLeft = arenaLeft;
            ArenaRight = arenaRight;
            Tuning = tuning;
            Reset();
        }

        /// <summary>Back to the start of the fight (the hero died).</summary>
        public void Reset()
        {
            Phase = WardenPhase.One;
            _patternIndex = 0;
            SweepDir = 1;
            SlamX = (ArenaLeft + ArenaRight) * 0.5f;
            Enter(WardenAction.Idle);
        }

        /// <summary>Speed multiplier for durations: phase 2 is 15% faster, phase 3 is 30% faster.</summary>
        public float SpeedScale => Phase == WardenPhase.One ? 1f : (Phase == WardenPhase.Two ? 0.85f : 0.7f);

        /// <summary>Report remaining health (0..1). Phases only move forward. Returns true if the phase changed.</summary>
        public bool SetHealthFraction(float fraction)
        {
            if (IsDefeated) return false;
            var next = fraction <= Tuning.phaseThreeAt ? WardenPhase.Three
                     : fraction <= Tuning.phaseTwoAt ? WardenPhase.Two : WardenPhase.One;
            if (next <= Phase) return false;
            Phase = next;
            _patternIndex = 0; // start the new phase's pattern after the current action
            return true;
        }

        public void Defeat() => Enter(WardenAction.Defeated);

        /// <summary>Advance time. Returns true when the action changed this tick.</summary>
        public bool Tick(float dt, float heroX)
        {
            if (IsDefeated || dt <= 0f) return false;
            _timer += dt;

            if (Current == WardenAction.SlamWindup && _timer <= _duration * Tuning.slamTrackFraction)
                SlamX = ClampX(heroX);

            if (_timer < _duration) return false;
            Enter(NextAfter(Current));
            return true;
        }

        WardenAction NextAfter(WardenAction a)
        {
            switch (a)
            {
                case WardenAction.SweepWindup: return WardenAction.Sweep;
                case WardenAction.SlamWindup: return WardenAction.Slam;
                case WardenAction.Slam: return WardenAction.Stagger;
                case WardenAction.VolleyWindup: return WardenAction.Volley;
                case WardenAction.Idle:
                    var pattern = Phase == WardenPhase.One ? PatternOne : (Phase == WardenPhase.Two ? PatternTwo : PatternThree);
                    var next = pattern[_patternIndex % pattern.Length];
                    _patternIndex++;
                    return next;
                default: return WardenAction.Idle; // Sweep, Stagger, Volley
            }
        }

        void Enter(WardenAction a)
        {
            if (a == WardenAction.SweepWindup) SweepDir = -SweepDir; // alternate sides each sweep (the first goes right to left)
            Current = a;
            _timer = 0f;
            _duration = DurationOf(a) * (a == WardenAction.Defeated ? 1f : SpeedScale);
        }

        float DurationOf(WardenAction a)
        {
            var t = Tuning;
            switch (a)
            {
                case WardenAction.Idle: return t.idle;
                case WardenAction.SweepWindup: return t.sweepWindup;
                case WardenAction.Sweep: return t.sweep;
                case WardenAction.SlamWindup: return t.slamWindup;
                case WardenAction.Slam: return t.slam;
                case WardenAction.Stagger: return t.stagger;
                case WardenAction.VolleyWindup: return t.volleyWindup;
                case WardenAction.Volley: return t.volley;
                default: return 0f;
            }
        }

        float ClampX(float x) => x < ArenaLeft ? ArenaLeft : (x > ArenaRight ? ArenaRight : x);

        /// <summary>Where cogs fall during a volley: on the hero and evenly around them, clamped to the arena.</summary>
        public float[] VolleyPositions(float heroX, int count, float spacing)
        {
            if (count <= 0) return Array.Empty<float>();
            var xs = new float[count];
            float start = -spacing * (count - 1) * 0.5f;
            for (int i = 0; i < count; i++) xs[i] = ClampX(heroX + start + spacing * i);
            return xs;
        }
    }
}
