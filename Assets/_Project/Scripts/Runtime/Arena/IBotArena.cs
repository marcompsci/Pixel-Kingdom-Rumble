using UnityEngine;

namespace PKR
{
    /// <summary>
    /// What a CPU fighter (BotController) needs to know about the match it is in. Implemented by the Arena Clash
    /// brawl (ArenaMatchController) and the 1-on-1 Versus mode (VersusController).
    /// </summary>
    public interface IBotArena
    {
        /// <summary>True while fighters may act (not during intros, round breaks or results).</summary>
        bool IsLive { get; }
        /// <summary>Main stage area: x range = edges, yMax = floor height.</summary>
        Rect StageRect { get; }
        /// <summary>True (with its x range) while a hole in the stage is open or about to open.</summary>
        bool GapOpen(out float left, out float right);
        /// <summary>Closest active opponent of `self`.</summary>
        GameObject NearestOpponent(GameObject self, Vector2 from);
    }
}
