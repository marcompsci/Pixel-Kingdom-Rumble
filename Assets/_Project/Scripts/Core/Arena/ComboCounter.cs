using System;

namespace PKR.Core
{
    /// <summary>What the CPU does in Training (Arena Clash > MODE: TRAINING > CPU ACTS).</summary>
    public enum TrainingAct { Stand, Walk, Jump, Fight }

    public static class TrainingActs
    {
        public static TrainingAct Next(TrainingAct a) => a == TrainingAct.Fight ? TrainingAct.Stand : a + 1;

        public static string Name(TrainingAct a)
        {
            switch (a)
            {
                case TrainingAct.Walk: return "WALK";
                case TrainingAct.Jump: return "JUMP";
                case TrainingAct.Fight: return "FIGHT BACK";
                default: return "STAND";
            }
        }
    }

    /// <summary>
    /// Training lab combo counter (pure): hits that land while the target is still reeling from the previous one
    /// (hitstun plus a small grace) chain into a combo. Tracks the current combo, the best one and launches.
    /// </summary>
    public class ComboCounter
    {
        /// <summary>Extra time after hitstun in which the next hit still counts as a follow-up.</summary>
        public const float Grace = 0.2f;

        public int Hits { get; private set; }
        public int Best { get; private set; }
        public int PipsBroken { get; private set; }
        public int Launches { get; private set; }
        public int TotalHits { get; private set; }
        float _chainUntil = float.NegativeInfinity;

        /// <summary>Registers a hit at time now (seconds) that caused hitstunSeconds of stun. Returns the combo length.</summary>
        public int Register(float now, float hitstunSeconds, int pipsBroken = 0, bool launch = false)
        {
            if (now > _chainUntil) Hits = 0;
            Hits++;
            TotalHits++;
            PipsBroken += Math.Max(0, pipsBroken);
            if (launch) Launches++;
            if (Hits > Best) Best = Hits;
            _chainUntil = now + Math.Max(0f, hitstunSeconds) + Grace;
            return Hits;
        }

        /// <summary>The combo shown right now (0 once the chain has dropped).</summary>
        public int Current(float now) => now <= _chainUntil ? Hits : 0;

        public void Reset()
        {
            Hits = Best = PipsBroken = Launches = TotalHits = 0;
            _chainUntil = float.NegativeInfinity;
        }
    }
}
