using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>How a Last Stand squad is made up (picked on the ZOMBIES menu).</summary>
    public enum SquadKind { Solo, AISquad, Online }

    /// <summary>
    /// Co-op rules for Last Stand (up to 4: humans online and/or AI teammates). More players means more of the dead,
    /// a downed player can be revived, and the run only ends when nobody is left standing.
    /// </summary>
    public static class SquadRules
    {
        public const int MaxSquad = 4;
        public const float BleedOutSeconds = 30f, ReviveSeconds = 3f, ReviveRange = 1.6f;
        /// <summary>Revived players come back with this share of their health.</summary>
        public const float ReviveHealth = 0.5f;

        /// <summary>The round grows with the squad (half again per extra player, like the classic mode).</summary>
        public static int ZombiesInRound(int round, int squad) =>
            (int)Math.Round(LastStandRules.ZombiesInRound(round) * (1f + 0.5f * (Math.Max(1, squad) - 1)));

        public static int MaxAlive(int round, int squad) => Math.Min(36, LastStandRules.MaxAlive(round) + 4 * (Math.Max(1, squad) - 1));

        public static int HoundsInRound(int round, int squad) => (int)Math.Round(LastStandRules.HoundsInRound(round) * (1f + 0.5f * (Math.Max(1, squad) - 1)));

        /// <summary>Bosses are tougher with more guns on them.</summary>
        public static int BossHealth(int baseHp, int squad) => (int)Math.Round(baseHp * (1f + 0.6f * (Math.Max(1, squad) - 1)));

        /// <summary>The gun an AI teammate carries at a round: pistol, then the SMG, the shotgun, the rifle.</summary>
        public static string AIGunFor(int round)
        {
            if (round >= 12) return "assault_rifle";
            if (round >= 8) return "riot_shotgun";
            if (round >= 4) return "street_smg";
            return ZLCatalog.Pistol.id;
        }

        /// <summary>The run ends when nobody is standing (downed players still count until they bleed out).</summary>
        public static bool AllDown(IList<bool> standing)
        {
            foreach (bool s in standing) if (s) return false;
            return true;
        }

        /// <summary>Host election for online play: the lowest player id hosts (every peer computes the same answer).</summary>
        public static string Host(IEnumerable<string> playerIds)
        {
            string best = null;
            foreach (var id in playerIds)
                if (best == null || string.CompareOrdinal(id, best) < 0) best = id;
            return best;
        }

        /// <summary>Slots in a stable order: the host is slot 0, the rest by id.</summary>
        public static List<string> Slots(IEnumerable<string> playerIds)
        {
            var list = new List<string>(playerIds);
            list.Sort(string.CompareOrdinal);
            return list;
        }
    }
}
