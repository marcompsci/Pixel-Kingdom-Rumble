using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>The party quests in the Kingdom Plaza (original modes; nothing borrowed from other games).</summary>
    public enum PartyMode { None = 0, ItemRace = 1, BalloonBattle = 2, RingOut = 3, CoinRush = 4, KingOfTheHill = 5 }

    public struct PartyModeInfo
    {
        public PartyMode mode;
        public string name, blurb, building;
        /// <summary>Game Center player group used to count live players in this quest.</summary>
        public int playerGroup;
        /// <summary>Round length (0 = until someone wins).</summary>
        public float seconds;
    }

    public static class PartyModes
    {
        public static readonly PartyModeInfo[] All =
        {
            new PartyModeInfo { mode = PartyMode.ItemRace, name = "ITEM RACE", building = "Racing Gate", playerGroup = 9111, seconds = 0f,
                                blurb = "Race three rivals to the flag. Item boxes hold boosts, shields and tricks." },
            new PartyModeInfo { mode = PartyMode.BalloonBattle, name = "BALLOON BATTLE", building = "Balloon Dome", playerGroup = 9112, seconds = 0f,
                                blurb = "Three balloons each. Every hard hit pops one. Last hero floating wins." },
            new PartyModeInfo { mode = PartyMode.RingOut, name = "RING-OUT BRAWL", building = "Brawl Ring", playerGroup = 9113, seconds = 0f,
                                blurb = "Three lives, item boxes, and a ring that shrinks after a minute." },
            new PartyModeInfo { mode = PartyMode.CoinRush, name = "COIN RUSH", building = "Coin Vault", playerGroup = 9114, seconds = 90f,
                                blurb = "Grab the most coins in 90 seconds. Get hit and you drop some." },
            new PartyModeInfo { mode = PartyMode.KingOfTheHill, name = "KING OF THE HILL", building = "Crown Keep", playerGroup = 9115, seconds = 90f,
                                blurb = "Stand alone in the crown zone to score. It moves every 15 seconds." },
        };

        public static PartyModeInfo Info(PartyMode m)
        {
            foreach (var i in All) if (i.mode == m) return i;
            return default;
        }

        /// <summary>Star Shards for a placement (1st..4th) in a party quest.</summary>
        public static int Reward(int placement) => placement switch { 1 => 40, 2 => 25, 3 => 15, _ => 8 };
    }

    // ---- items ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Item box contents. All take effect the moment they're picked up (no extra button on a phone):
    /// Dash Fizz = a speed burst, Bubble Shield = blocks the next hit, Seeker Star = flies at the nearest rival,
    /// Slick Puddle = dropped behind you, makes whoever runs into it spin out, Thunder Bell = stuns every rival briefly.
    /// </summary>
    public enum PartyItem { DashFizz = 0, BubbleShield = 1, SeekerStar = 2, SlickPuddle = 3, ThunderBell = 4 }

    public static class PartyItems
    {
        public static string Name(PartyItem i) => i switch
        {
            PartyItem.DashFizz => "DASH FIZZ",
            PartyItem.BubbleShield => "BUBBLE SHIELD",
            PartyItem.SeekerStar => "SEEKER STAR",
            PartyItem.SlickPuddle => "SLICK PUDDLE",
            PartyItem.ThunderBell => "THUNDER BELL",
            _ => "",
        };

        /// <summary>
        /// Odds by standing (catch-up): leaders mostly get defence and puddles, the back of the pack gets speed and the
        /// Thunder Bell. place = 1..fighters.
        /// </summary>
        public static int[] Weights(int place, int fighters)
        {
            float back = fighters <= 1 ? 0f : (place - 1) / (float)(fighters - 1); // 0 = leading, 1 = last
            // DashFizz, BubbleShield, SeekerStar, SlickPuddle, ThunderBell
            return new[]
            {
                (int)Math.Round(15 + 30 * back),
                (int)Math.Round(30 - 15 * back),
                (int)Math.Round(20 + 10 * back),
                (int)Math.Round(35 - 30 * back),
                back >= 0.99f ? 15 : back >= 0.5f ? 6 : 0,
            };
        }

        public static PartyItem Roll(Random rng, int place, int fighters)
        {
            var w = Weights(place, fighters);
            int total = 0;
            foreach (int x in w) total += x;
            int r = rng.Next(0, Math.Max(1, total));
            for (int i = 0; i < w.Length; i++)
            {
                if (r < w[i]) return (PartyItem)i;
                r -= w[i];
            }
            return PartyItem.DashFizz;
        }
    }

    // ---- race ----------------------------------------------------------------------------------------------------

    /// <summary>Who's where in an Item Race: places from distance along the course, and the finish order.</summary>
    public class RaceStandings
    {
        readonly float[] _progress;
        readonly List<int> _finished = new List<int>();
        public readonly float FinishX;

        public RaceStandings(int racers, float finishX)
        {
            if (racers < 1) throw new ArgumentOutOfRangeException(nameof(racers));
            _progress = new float[racers];
            FinishX = finishX;
        }

        public int Count => _progress.Length;
        public IReadOnlyList<int> FinishOrder => _finished;
        public bool IsFinished(int racer) => _finished.Contains(racer);
        public bool AllFinished => _finished.Count == _progress.Length;

        /// <summary>Report a racer's x. Returns true the moment they cross the line.</summary>
        public bool Report(int racer, float x)
        {
            if (IsFinished(racer)) return false;
            _progress[racer] = Math.Max(_progress[racer], x);
            if (x < FinishX) return false;
            _finished.Add(racer);
            return true;
        }

        /// <summary>1-based place: finishers by finish order, then everyone else by distance.</summary>
        public int Place(int racer)
        {
            int f = _finished.IndexOf(racer);
            if (f >= 0) return f + 1;
            int place = _finished.Count + 1;
            for (int i = 0; i < _progress.Length; i++)
                if (i != racer && !IsFinished(i) && (_progress[i] > _progress[racer] || (_progress[i] == _progress[racer] && i < racer))) place++;
            return place;
        }

        public float Progress01(int racer, float startX) =>
            FinishX <= startX ? 1f : Math.Max(0f, Math.Min(1f, (_progress[racer] - startX) / (FinishX - startX)));
    }

    // ---- crown zone and coins ------------------------------------------------------------------------------------

    /// <summary>King of the Hill: who scores this tick, and where the zone sits.</summary>
    public static class CrownZone
    {
        public const float MoveEvery = 15f;

        /// <summary>The zone scores only for a fighter who is alone in it (contested = nobody scores).</summary>
        public static int Scorer(IList<bool> inZone)
        {
            int who = -1;
            for (int i = 0; i < inZone.Count; i++)
            {
                if (!inZone[i]) continue;
                if (who >= 0) return -1;
                who = i;
            }
            return who;
        }

        /// <summary>Which of the spots the zone is on after this many seconds (never the same spot twice in a row).</summary>
        public static int SpotAt(float seconds, int spots, int seed)
        {
            if (spots <= 1) return 0;
            int step = (int)Math.Floor(Math.Max(0f, seconds) / MoveEvery);
            int spot = Math.Abs(seed) % spots;
            for (int i = 0; i < step; i++)
            {
                int jump = 1 + Math.Abs(unchecked(seed * 31 + i * 17)) % (spots - 1);
                spot = (spot + jump) % spots;
            }
            return spot;
        }
    }

    public static class CoinRules
    {
        public const int DropOnHit = 2, DropOnFall = 5;

        /// <summary>Coins actually dropped (you can't drop more than you have).</summary>
        public static int Drop(int have, bool fell) => Math.Min(Math.Max(0, have), fell ? DropOnFall : DropOnHit);
    }

    // ---- the Kingdom Plaza hub -----------------------------------------------------------------------------------

    public enum HubKind { World = 0, Party = 1 }

    public struct HubBuilding
    {
        public string id, name;
        public HubKind kind;
        /// <summary>Story world index (StoryWorlds.All) for World buildings.</summary>
        public int world;
        public PartyMode party;
        /// <summary>Distance along the plaza (tiles from the left edge).</summary>
        public float x;
        public int playerGroup;
    }

    /// <summary>The Kingdom Plaza: the roamable Story Quest world. Each building is a quest.</summary>
    public static class KingdomPlaza
    {
        public const int PlazaGroup = 9100;
        public const string PlazaLevelId = "sq_kingdom_plaza";
        public const string RaceLevelId = "sq_item_race";

        /// <summary>The Game Center group for a Story world's building (players inside that world's levels count there).</summary>
        public static int WorldGroup(int world)
        {
            foreach (var b in Buildings) if (b.kind == HubKind.World && b.world == world) return b.playerGroup;
            return -1;
        }

        public static readonly HubBuilding[] Buildings =
        {
            new HubBuilding { id = "w0", name = "Sunspire Isles", kind = HubKind.World, world = 0, x = 12f, playerGroup = 9101 },
            new HubBuilding { id = "race", name = "Racing Gate", kind = HubKind.Party, party = PartyMode.ItemRace, x = 26f, playerGroup = 9111 },
            new HubBuilding { id = "w1", name = "Frostgear Peaks", kind = HubKind.World, world = 1, x = 40f, playerGroup = 9102 },
            new HubBuilding { id = "balloon", name = "Balloon Dome", kind = HubKind.Party, party = PartyMode.BalloonBattle, x = 54f, playerGroup = 9112 },
            new HubBuilding { id = "w2", name = "The Shattered Realms", kind = HubKind.World, world = 2, x = 68f, playerGroup = 9103 },
            new HubBuilding { id = "brawl", name = "Brawl Ring", kind = HubKind.Party, party = PartyMode.RingOut, x = 82f, playerGroup = 9113 },
            new HubBuilding { id = "w3", name = "Starfall Observatory", kind = HubKind.World, world = 3, x = 96f, playerGroup = 9104 },
            new HubBuilding { id = "coin", name = "Coin Vault", kind = HubKind.Party, party = PartyMode.CoinRush, x = 110f, playerGroup = 9114 },
            new HubBuilding { id = "crown", name = "Crown Keep", kind = HubKind.Party, party = PartyMode.KingOfTheHill, x = 124f, playerGroup = 9115 },
        };
    }

    // ---- live player counts --------------------------------------------------------------------------------------

    /// <summary>
    /// Live player counts come from Game Center: each place has a player group, the game announces itself in the place
    /// it's in, and asks how many players Game Center saw there recently. Unknown (offline, not signed in, not iOS)
    /// shows "--", never a made-up number.
    /// </summary>
    public static class LiveCount
    {
        /// <summary>Ask Game Center at most this often per place.</summary>
        public const float RefreshSeconds = 30f;
        /// <summary>Announce presence in the current place this often.</summary>
        public const float AnnounceSeconds = 45f;

        public static string Label(int? count)
        {
            if (!count.HasValue || count.Value < 0) return "-- LIVE";
            int n = count.Value;
            if (n < 1000) return $"{n} LIVE";
            if (n < 10000) return $"{n / 1000}.{n / 100 % 10}K LIVE";
            return $"{n / 1000}K LIVE";
        }

        /// <summary>The plaza total: everyone seen in the plaza or in any of its quests (unknowns don't count).</summary>
        public static int? Total(IEnumerable<int?> counts)
        {
            int sum = 0;
            bool any = false;
            foreach (var c in counts)
            {
                if (!c.HasValue || c.Value < 0) continue;
                sum += c.Value;
                any = true;
            }
            return any ? sum : (int?)null;
        }
    }
}

namespace PKR.Core
{
    /// <summary>One party quest's record in the save (shown on STORY STATS).</summary>
    [System.Serializable]
    public class PartyRecord
    {
        public int mode;
        public int played, wins, podiums;
        /// <summary>Item Race only: fastest finish in seconds (0 = none yet).</summary>
        public float bestRaceSeconds;
    }

    [System.Serializable]
    public class PartySave
    {
        public System.Collections.Generic.List<PartyRecord> records = new System.Collections.Generic.List<PartyRecord>();

        public PartyRecord Find(PartyMode m)
        {
            foreach (var r in records) if (r != null && r.mode == (int)m) return r;
            return null;
        }

        public void Sanitize()
        {
            if (records == null) records = new System.Collections.Generic.List<PartyRecord>();
            records.RemoveAll(r => r == null || r.mode <= 0 || r.mode > (int)PartyMode.KingOfTheHill);
            foreach (var r in records)
            {
                r.played = System.Math.Max(0, r.played); r.wins = System.Math.Max(0, r.wins); r.podiums = System.Math.Max(0, r.podiums);
                if (r.bestRaceSeconds < 0f || float.IsNaN(r.bestRaceSeconds)) r.bestRaceSeconds = 0f;
            }
        }
    }

    public static class PartyResults
    {
        /// <summary>Records a finished party quest and pays its Star Shards. Returns the shards paid.</summary>
        public static int Record(SaveData save, PartyMode mode, int placement, float raceSeconds = 0f)
        {
            if (save == null) throw new System.ArgumentNullException(nameof(save));
            if (mode == PartyMode.None || placement < 1) return 0;
            if (save.party == null) save.party = new PartySave();
            var r = save.party.Find(mode);
            if (r == null) { r = new PartyRecord { mode = (int)mode }; save.party.records.Add(r); }
            r.played++;
            if (placement == 1) r.wins++;
            if (placement <= 3) r.podiums++;
            if (mode == PartyMode.ItemRace && raceSeconds > 0f && (r.bestRaceSeconds <= 0f || raceSeconds < r.bestRaceSeconds))
                r.bestRaceSeconds = raceSeconds;
            int shards = PartyModes.Reward(placement);
            Economy.Grant(save, shards);
            return shards;
        }
    }
}
