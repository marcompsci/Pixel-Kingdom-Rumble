using System;
using System.Collections.Generic;

namespace PKR.Core
{
    public enum FieldGuideTier { Unknown, Seen, Studied, Mastered }

    /// <summary>
    /// ZOMBIES > FIELD GUIDE: every kind of the dead you've put down, with how to beat it. Entries unlock on your first
    /// kill; Studied and Mastered come with more kills (fewer for the rare elites). Mastering a kind pays Star Shards once.
    /// Kills from Last Stand and Zombie Land both count.
    /// </summary>
    public static class ZombieFieldGuide
    {
        public const int MasteryShards = 50;

        public struct Entry
        {
            public ZLFoe kind;
            public string name, tip;
            public int studied, mastered;
        }

        public static readonly Entry[] All =
        {
            E(ZLFoe.Shambler, "SHAMBLER", "Slow alone, deadly in a crowd. Keep moving and never let them box you in.", 50, 300),
            E(ZLFoe.Runner, "RUNNER", "Fast and loud: it calls others when it sees you. Drop it before it reaches the pack.", 25, 150),
            E(ZLFoe.Screamer, "SCREAMER", "A living alarm. Its scream pulls a horde to you, so kill it first.", 10, 50),
            E(ZLFoe.Bloater, "BLOATER", "Bursts into a rot cloud when it dies. Shoot it from range, never up close.", 15, 80),
            E(ZLFoe.Spitter, "SPITTER", "Lobs acid from range. Step sideways when it rears back.", 15, 80),
            E(ZLFoe.Brute, "BRUTE", "Slow and huge, and hits hard. Kite it in circles and pour on the damage.", 10, 50),
            E(ZLFoe.Hound, "BLOOD HOUND", "Hunts in packs. Back into a corner so they come at you one way.", 25, 150),
            E(ZLFoe.Herald, "HERALD", "An armored lieutenant. Explosives and wonder weapons crack it fastest.", 3, 10),
            E(ZLFoe.FirstTurned, "THE FIRST TURNED", "Only one relic weapon can hurt it, and it changes every new game.", 1, 3),
            E(ZLFoe.Riot, "RIOT", "Bullets glance off its shield. Flank it or blow it up.", 10, 60),
            E(ZLFoe.Leaper, "LEAPER", "Crouches, then springs. Dodge sideways the moment it drops low.", 10, 60),
            E(ZLFoe.Splitter, "SPLITTER", "Two bodies in one coat: kill it and both halves keep coming.", 10, 60),
            E(ZLFoe.SplitterHalf, "SPLITTER HALF", "Small, quick and fragile. One good burst each.", 20, 120),
            E(ZLFoe.Burrower, "BURROWER", "Tunnels under the street. Step away when you see the '!' mound.", 10, 60),
        };

        static Entry E(ZLFoe k, string name, string tip, int studied, int mastered) =>
            new Entry { kind = k, name = name, tip = tip, studied = studied, mastered = mastered };

        public static Entry Get(ZLFoe k)
        {
            foreach (var e in All) if (e.kind == k) return e;
            return E(k, k.ToString().ToUpperInvariant(), "", 10, 50);
        }

        public static int Kills(ZombieCareer c, ZLFoe k)
        {
            int i = (int)k;
            return c != null && c.foeKills != null && i >= 0 && i < c.foeKills.Count ? c.foeKills[i] : 0;
        }

        public static FieldGuideTier Tier(ZLFoe k, int kills)
        {
            var e = Get(k);
            if (kills <= 0) return FieldGuideTier.Unknown;
            if (kills >= e.mastered) return FieldGuideTier.Mastered;
            if (kills >= e.studied) return FieldGuideTier.Studied;
            return FieldGuideTier.Seen;
        }

        public static string TierName(FieldGuideTier t) =>
            t == FieldGuideTier.Mastered ? "MASTERED" : t == FieldGuideTier.Studied ? "STUDIED" : t == FieldGuideTier.Seen ? "SEEN" : "???";

        /// <summary>Kills needed for the next tier (0 when mastered).</summary>
        public static int ToNext(ZLFoe k, int kills)
        {
            var e = Get(k);
            if (kills < e.studied) return e.studied - kills;
            if (kills < e.mastered) return e.mastered - kills;
            return 0;
        }

        /// <summary>Counts kills of a kind. Returns the Star Shards earned (MasteryShards the first time it's mastered).</summary>
        public static int Add(ZombieCareer c, ZLFoe k, int n = 1)
        {
            if (c == null || n <= 0) return 0;
            int i = (int)k;
            if (i < 0 || i >= 31) return 0;
            if (c.foeKills == null) c.foeKills = new List<int>();
            while (c.foeKills.Count <= i) c.foeKills.Add(0);
            c.foeKills[i] = (int)Math.Min(int.MaxValue, (long)c.foeKills[i] + n);
            int bit = 1 << i;
            if ((c.foeMastered & bit) != 0 || Tier(k, c.foeKills[i]) != FieldGuideTier.Mastered) return 0;
            c.foeMastered |= bit;
            return MasteryShards;
        }

        /// <summary>Entries unlocked (at least one kill) and mastered.</summary>
        public static (int seen, int mastered) Count(ZombieCareer c)
        {
            int seen = 0, mastered = 0;
            foreach (var e in All)
            {
                var t = Tier(e.kind, Kills(c, e.kind));
                if (t != FieldGuideTier.Unknown) seen++;
                if (t == FieldGuideTier.Mastered) mastered++;
            }
            return (seen, mastered);
        }
    }
}
