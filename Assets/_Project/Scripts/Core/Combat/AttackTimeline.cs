using System;
using System.Collections.Generic;

namespace PKR.Core
{
    public enum AttackPhase { Startup, Active, Recovery, Done }

    /// <summary>
    /// Frame data for one move, counted in 60 Hz physics steps.
    /// Startup: wind-up (no hitbox). Active: hitbox live. Recovery: can't act unless inside the cancel window.
    /// </summary>
    [Serializable]
    public struct FrameData
    {
        public int startup;
        public int active;
        public int recovery;
        /// <summary>
        /// Frames from the END of the move during which a follow-up attack or a dodge may cancel it.
        /// 0 = no cancel window.
        /// </summary>
        public int cancelWindow;

        public FrameData(int startup, int active, int recovery, int cancelWindow = 0)
        {
            this.startup = startup; this.active = active; this.recovery = recovery; this.cancelWindow = cancelWindow;
        }

        public int Total => Math.Max(0, startup) + Math.Max(1, active) + Math.Max(0, recovery);

        public List<string> Validate()
        {
            var e = new List<string>();
            if (startup < 0) e.Add("startup must be >= 0");
            if (active < 1) e.Add("active must be >= 1");
            if (recovery < 0) e.Add("recovery must be >= 0");
            if (cancelWindow < 0 || cancelWindow > Math.Max(0, recovery)) e.Add("cancelWindow must be within recovery");
            return e;
        }
    }

    public static class AttackTimeline
    {
        /// <summary>Phase at a 0-based frame index since the attack began.</summary>
        public static AttackPhase PhaseAt(in FrameData f, int frame)
        {
            if (frame < 0) return AttackPhase.Startup;
            int s = Math.Max(0, f.startup);
            int a = Math.Max(1, f.active);
            if (frame < s) return AttackPhase.Startup;
            if (frame < s + a) return AttackPhase.Active;
            if (frame < f.Total) return AttackPhase.Recovery;
            return AttackPhase.Done;
        }

        /// <summary>True during the last cancelWindow frames of recovery.</summary>
        public static bool InCancelWindow(in FrameData f, int frame)
        {
            if (f.cancelWindow <= 0) return false;
            return frame >= f.Total - f.cancelWindow && frame < f.Total;
        }

        /// <summary>True on the first active frame (used to fire projectiles / one-shot effects exactly once).</summary>
        public static bool IsFirstActiveFrame(in FrameData f, int frame) => frame == Math.Max(0, f.startup);
    }

    /// <summary>
    /// Tracks which targets a single swing already hit, so a multi-frame hitbox hits each target once.
    /// Targets are identified by any stable int (Unity instance ID at runtime).
    /// </summary>
    public class SwingHitLog
    {
        readonly HashSet<int> _hit = new HashSet<int>();
        public int Count => _hit.Count;

        /// <summary>Returns true if this target hasn't been hit by the current swing yet (and records it).</summary>
        public bool TryRegister(int targetId) => _hit.Add(targetId);

        public void Reset() => _hit.Clear();
    }
}
