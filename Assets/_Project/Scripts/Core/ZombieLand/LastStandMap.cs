using System;

namespace PKR.Core
{
    /// <summary>
    /// A Last Stand map: the JSON it is built from (Resources/PKR/ZombieLand/{File}.json), its names, its boss, its
    /// hidden boss and what you collect to wake it. Every map plays by LastStandRules; only the place and the names
    /// change. Players asked for more maps more than anything else, so adding one is a new entry here plus a map file.
    /// </summary>
    public sealed class LastStandMap
    {
        public string Id, File, Title, Short, Blurb, BossName, HiddenBossName, HiddenSecret, EggName, Leaderboard;
        /// <summary>Menu colour (r, g, b).</summary>
        public byte R, G, B;

        public string FullTitle => "LAST STAND: " + Title;

        public static readonly LastStandMap HollowBlock = new LastStandMap
        {
            Id = LastStandRules.MapId, File = "block", Title = "HOLLOW BLOCK", Short = "Hollow Block",
            Blurb = "Round-based on a New Hollow city block",
            BossName = LastStandRules.BossName, HiddenBossName = LastStandRules.HiddenBossName,
            HiddenSecret = LastStandRules.HiddenBossSecret, EggName = "GRAYSTONE FRAGMENT",
            Leaderboard = Achievements.LeaderboardLastStand, R = 170, G = 70, B = 40,
        };

        public static readonly LastStandMap SaltPier = new LastStandMap
        {
            Id = "salt_pier", File = "pier", Title = "SALT PIER", Short = "Salt Pier",
            Blurb = "Docks, a container maze, the fish market and a lighthouse out on the water",
            BossName = "THE DROWNED FOREMAN", HiddenBossName = "THE TIDE MOTHER",
            HiddenSecret = "tide_mother", EggName = "BRASS BELL",
            Leaderboard = Achievements.LeaderboardPier, R = 40, G = 110, B = 150,
        };

        public static readonly LastStandMap[] All = { HollowBlock, SaltPier };

        /// <summary>The map with this id (Hollow Block if unknown or empty).</summary>
        public static LastStandMap Get(string id)
        {
            foreach (var m in All) if (string.Equals(m.Id, id, StringComparison.Ordinal)) return m;
            return HollowBlock;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return i;
            return 0;
        }

        public static LastStandMap At(int index) => index >= 0 && index < All.Length ? All[index] : HollowBlock;
    }
}
