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
        /// <summary>The Easter egg piece's sprite (Props/...), its prompt, what finding one says and what finding all three says.</summary>
        public string EggSprite = "fragment", EggPrompt = "A HUMMING STONE", EggFound = "HUMS", EggAll = "THE FRAGMENTS SING. SOMETHING UNDER THE BLOCK IS AWAKE.";
        /// <summary>A wonder weapon the hidden boss drops for the whole squad ("" = none).</summary>
        public string HiddenDrop = "";
        /// <summary>The hidden boss's colour (r, g, b, 0..255).</summary>
        public byte HR = 190, HG = 165, HB = 255;
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
            EggSprite = "bell", EggPrompt = "RING THE BRASS BELL", EggFound = "RINGS", EggAll = "THE BELLS TOLL. SOMETHING RISES FROM THE WATER.",
            HR = 140, HG = 240, HB = 215,
            Leaderboard = Achievements.LeaderboardPier, R = 40, G = 110, B = 150,
        };

        public static readonly LastStandMap RailYard = new LastStandMap
        {
            Id = "rail_yard", File = "rail", Title = "RAIL YARD", Short = "the Rail Yard",
            Blurb = "A freight yard: the platform, the switchyard, the roundhouse and the signal tower",
            BossName = "THE CONDUCTOR", HiddenBossName = "THE IRON WIDOW",
            HiddenSecret = "iron_widow", EggName = "SIGNAL LEVER",
            EggSprite = "lever", EggPrompt = "THROW THE SIGNAL LEVER", EggFound = "CLANKS", EggAll = "EVERY SIGNAL TURNS RED. A WHISTLE SCREAMS DOWN THE LINE.",
            HiddenDrop = "singularity", HR = 255, HG = 150, HB = 90,
            Leaderboard = Achievements.LeaderboardRail, R = 120, G = 80, B = 50,
        };

        public static readonly LastStandMap Gravemoor = new LastStandMap
        {
            Id = "gravemoor", File = "gardens", Title = "GRAVEMOOR", Short = "Gravemoor Park",
            Blurb = "An old city park: the gatehouse, the rose garden, the boathouse pond and the graves on Chapel Hill",
            BossName = "THE GROUNDSKEEPER", HiddenBossName = "THE BRIAR QUEEN",
            HiddenSecret = "briar_queen", EggName = "MOURNING LANTERN",
            EggSprite = "lantern", EggPrompt = "LIGHT THE MOURNING LANTERN", EggFound = "FLICKERS GREEN",
            EggAll = "THE LANTERNS BURN GREEN. EVERY ROSE IN THE PARK TURNS TO FACE YOU.",
            HiddenDrop = "kindler", HR = 120, HG = 215, HB = 110,
            Leaderboard = Achievements.LeaderboardGravemoor, R = 60, G = 110, B = 60,
        };

        public static readonly LastStandMap[] All = { HollowBlock, SaltPier, RailYard, Gravemoor };

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
