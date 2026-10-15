using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// Per-hero Versus health adjustments from the balance lab (Oct 2026: 22 heroes, each against four sparring
    /// partners on both sides, CPU Hard vs CPU Hard). Grizz and Volt won over 90% of their games, Nova under 10%;
    /// health nudges plus a few move fixes
    /// (Editor BalancePatch) pull the outliers toward 50%. Applied on top of the
    /// weight-based health (VersusRules.HealthFor).
    /// </summary>
    public static class VersusBalance
    {
        public static readonly IReadOnlyDictionary<string, int> HealthAdjust = new Dictionary<string, int>
        {
            { "grizz", -3 },
            { "volt_ramirez", -3 },
            { "bo_tidewell", -2 },
            { "blaze_torres", -1 },
            { "mara_vex", 2 },
            { "rex_rollo", 2 },
            { "brick", 2 },
            { "luma", 2 },
            { "zuri_starling", 3 },
            { "nova", 4 },
        };

        public static int Adjust(string heroId) =>
            heroId != null && HealthAdjust.TryGetValue(heroId, out int d) ? d : 0;
    }
}
