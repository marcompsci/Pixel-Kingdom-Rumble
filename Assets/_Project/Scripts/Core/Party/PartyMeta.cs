using System;
using System.Collections.Generic;
using System.Text;

namespace PKR.Core
{
    // =================================================================================================================
    // Party quest meta-progression: CPU difficulty, the daily party challenge, the Kingdom Cup, party ranks, and the
    // Item Race ghost. Engine-free so every rule is unit-tested; the game calls PartyProgress.Apply once per result.
    // =================================================================================================================

    /// <summary>How tough the CPU rivals are in party quests (chosen on the plaza's notice board).</summary>
    public enum PartyDifficulty { Easy = 0, Normal = 1, Hard = 2 }

    public static class PartyDifficulties
    {
        public static string Name(PartyDifficulty d) => d == PartyDifficulty.Easy ? "EASY" : d == PartyDifficulty.Hard ? "HARD" : "NORMAL";

        public static PartyDifficulty Next(PartyDifficulty d) =>
            d == PartyDifficulty.Easy ? PartyDifficulty.Normal : d == PartyDifficulty.Normal ? PartyDifficulty.Hard : PartyDifficulty.Easy;

        /// <summary>Arena CPU level for the four arena party quests.</summary>
        public static BotLevel Bots(PartyDifficulty d) =>
            d == PartyDifficulty.Easy ? BotLevel.Easy : d == PartyDifficulty.Hard ? BotLevel.Hard : BotLevel.Normal;

        /// <summary>
        /// Item Race CPU speed (share of a hero's run speed) by where the CPU is compared with you: far ahead, level, far
        /// behind. The catch-up keeps races close; Hard keeps it but runs faster everywhere.
        /// </summary>
        public static float RaceSpeed(PartyDifficulty d, float gapToHero)
        {
            float ahead, level, behind;
            switch (d)
            {
                case PartyDifficulty.Easy: ahead = 0.78f; level = 0.88f; behind = 1.0f; break;
                case PartyDifficulty.Hard: ahead = 0.96f; level = 1.03f; behind = 1.16f; break;
                default: ahead = 0.86f; level = 0.97f; behind = 1.1f; break;
            }
            return gapToHero > 14f ? ahead : gapToHero < -14f ? behind : level;
        }

        /// <summary>Star Shards scale with difficulty (Hard pays a quarter more, Easy a quarter less).</summary>
        public static int ScaleReward(int shards, PartyDifficulty d) =>
            d == PartyDifficulty.Easy ? (int)Math.Round(shards * 0.75) : d == PartyDifficulty.Hard ? (int)Math.Round(shards * 1.25) : shards;
    }

    // ---- one finished party quest --------------------------------------------------------------------------------

    /// <summary>Everything about one finished party quest the meta systems need.</summary>
    public struct PartyOutcome
    {
        public PartyMode mode;
        /// <summary>Your place, 1..entrants.</summary>
        public int placement;
        /// <summary>Coins (Coin Rush), seconds in the zone (King of the Hill), balloons left (Balloon Battle).</summary>
        public int score;
        /// <summary>Item Race finish time (0 if you didn't finish).</summary>
        public float raceSeconds;
        public PartyDifficulty difficulty;
        /// <summary>Every entrant's place, slot 0 = you (used by the Kingdom Cup). May be null.</summary>
        public int[] placements;
    }

    /// <summary>What a result earned, for the results screen.</summary>
    public class PartyReport
    {
        public int shards;
        public bool dailyDone;
        public int dailyBonus;
        public int dailyStreak;
        public bool rankUp;
        public PartyRank rank;
        public bool newBestTime;
        /// <summary>A new personal best score (coins, crown seconds, balloons left).</summary>
        public bool newBestScore;
        /// <summary>The cup moved on (an event finished inside a cup run).</summary>
        public bool cupEvent;
        /// <summary>The cup is over: final place 1..4, trophy and bonus shards.</summary>
        public bool cupFinished;
        public int cupPlace;
        public CupTrophy cupTrophy;
        public int cupBonus;
    }

    // ---- daily party challenge -----------------------------------------------------------------------------------

    public enum DailyGoal { Win = 0, Podium = 1, CoinsAtLeast = 2, CrownSecondsAtLeast = 3, RaceUnder = 4, BalloonsLeft = 5 }

    public struct DailyPartyChallenge
    {
        public int day;
        public PartyMode mode;
        public DailyGoal goal;
        /// <summary>Coins, seconds, balloons, or race seconds depending on the goal.</summary>
        public int target;
        public string text;
        public int reward;

        public bool Met(PartyOutcome o)
        {
            if (o.mode != mode || o.placement < 1) return false;
            switch (goal)
            {
                case DailyGoal.Win: return o.placement == 1;
                case DailyGoal.Podium: return o.placement <= 3;
                case DailyGoal.CoinsAtLeast:
                case DailyGoal.CrownSecondsAtLeast:
                case DailyGoal.BalloonsLeft: return o.score >= target;
                case DailyGoal.RaceUnder: return o.raceSeconds > 0f && o.raceSeconds <= target;
                default: return false;
            }
        }
    }

    public static class DailyParty
    {
        public const int Reward = 60;

        /// <summary>Today's challenge (the same for everyone on the same date). day = yyyymmdd.</summary>
        public static DailyPartyChallenge For(int day)
        {
            // A small deterministic hash so neighbouring days differ.
            uint h = unchecked((uint)day * 2654435761u);
            h ^= h >> 13;
            var options = new[]
            {
                (PartyMode.ItemRace, DailyGoal.Podium, 0),
                (PartyMode.ItemRace, DailyGoal.RaceUnder, 75),
                (PartyMode.BalloonBattle, DailyGoal.Win, 0),
                (PartyMode.BalloonBattle, DailyGoal.BalloonsLeft, 2),
                (PartyMode.RingOut, DailyGoal.Win, 0),
                (PartyMode.RingOut, DailyGoal.Podium, 0),
                (PartyMode.CoinRush, DailyGoal.CoinsAtLeast, 12),
                (PartyMode.CoinRush, DailyGoal.Win, 0),
                (PartyMode.KingOfTheHill, DailyGoal.CrownSecondsAtLeast, 25),
                (PartyMode.KingOfTheHill, DailyGoal.Podium, 0),
            };
            var pick = options[h % (uint)options.Length];
            var c = new DailyPartyChallenge { day = day, mode = pick.Item1, goal = pick.Item2, target = pick.Item3, reward = Reward };
            c.text = Describe(c);
            return c;
        }

        public static string Describe(DailyPartyChallenge c)
        {
            string m = PartyModes.Info(c.mode).name;
            switch (c.goal)
            {
                case DailyGoal.Win: return $"Win {m}";
                case DailyGoal.Podium: return $"Finish {m} in the top 3";
                case DailyGoal.CoinsAtLeast: return $"Grab {c.target}+ coins in {m}";
                case DailyGoal.CrownSecondsAtLeast: return $"Hold the crown {c.target}+ seconds in {m}";
                case DailyGoal.RaceUnder: return $"Finish {m} in under {c.target / 60}:{c.target % 60:00}";
                case DailyGoal.BalloonsLeft: return $"Win {m} with {c.target}+ balloons left";
                default: return m;
            }
        }

        /// <summary>The day before yyyymmdd (handles month and year ends).</summary>
        public static int PreviousDay(int day)
        {
            try
            {
                var d = new DateTime(day / 10000, day / 100 % 100, day % 100).AddDays(-1);
                return d.Year * 10000 + d.Month * 100 + d.Day;
            }
            catch (ArgumentOutOfRangeException) { return 0; }
        }
    }

    // ---- Kingdom Cup ---------------------------------------------------------------------------------------------

    public enum CupTrophy { None = 0, Bronze = 1, Silver = 2, Gold = 3 }

    /// <summary>
    /// The Kingdom Cup: four party quests in a row against the same three CPU rivals. Each event pays cup points by
    /// place (10/6/3/1); most points after the last event wins the cup (ties go to you).
    /// </summary>
    public static class KingdomCup
    {
        public static readonly PartyMode[] Events = { PartyMode.ItemRace, PartyMode.BalloonBattle, PartyMode.CoinRush, PartyMode.KingOfTheHill };
        public static readonly int[] Points = { 10, 6, 3, 1 };
        public const int Entrants = 4;

        public static int PointsFor(int place) => place >= 1 && place <= Points.Length ? Points[place - 1] : 0;

        /// <summary>Bonus Star Shards for the cup's final place, scaled by difficulty.</summary>
        public static int Bonus(int place, PartyDifficulty d) =>
            PartyDifficulties.ScaleReward(place == 1 ? 150 : place == 2 ? 80 : place == 3 ? 40 : 15, d);

        public static CupTrophy TrophyFor(int place) =>
            place == 1 ? CupTrophy.Gold : place == 2 ? CupTrophy.Silver : place == 3 ? CupTrophy.Bronze : CupTrophy.None;

        public static string TrophyName(CupTrophy t) => t == CupTrophy.Gold ? "GOLD" : t == CupTrophy.Silver ? "SILVER" : t == CupTrophy.Bronze ? "BRONZE" : "NO";

        /// <summary>Start a new cup run (replaces any run in progress).</summary>
        public static CupRun Start(PartySave party, PartyDifficulty d)
        {
            if (party == null) throw new ArgumentNullException(nameof(party));
            party.cup = new CupRun { active = true, eventIndex = 0, difficulty = (int)d, points = new int[Entrants] };
            return party.cup;
        }

        /// <summary>Final cup places from points, 1..4 (ties: you first, then lower slot).</summary>
        public static int[] Standings(int[] points)
        {
            int n = points.Length;
            var place = new int[n];
            for (int i = 0; i < n; i++)
            {
                int p = 1;
                for (int j = 0; j < n; j++)
                    if (j != i && (points[j] > points[i] || (points[j] == points[i] && j < i))) p++;
                place[i] = p;
            }
            return place;
        }
    }

    [Serializable]
    public class CupRun
    {
        public bool active;
        public int eventIndex;
        public int difficulty;
        public int[] points = new int[KingdomCup.Entrants];

        public PartyMode CurrentEvent => active && eventIndex >= 0 && eventIndex < KingdomCup.Events.Length ? KingdomCup.Events[eventIndex] : PartyMode.None;
        public PartyMode NextEvent => active && eventIndex + 1 < KingdomCup.Events.Length ? KingdomCup.Events[eventIndex + 1] : PartyMode.None;
    }

    // ---- party ranks ---------------------------------------------------------------------------------------------

    public enum PartyRank { Rookie = 0, Contender = 1, Star = 2, Champion = 3, Legend = 4 }

    /// <summary>Per-quest rank from rank points: a win is 3, any other podium 1.</summary>
    public static class PartyRanks
    {
        public static readonly int[] Thresholds = { 0, 6, 20, 45, 90 };

        public static int PointsFor(int placement) => placement == 1 ? 3 : placement <= 3 ? 1 : 0;

        public static PartyRank For(int points)
        {
            var r = PartyRank.Rookie;
            for (int i = 0; i < Thresholds.Length; i++) if (points >= Thresholds[i]) r = (PartyRank)i;
            return r;
        }

        public static string Name(PartyRank r) => r.ToString().ToUpperInvariant();

        /// <summary>Points still needed for the next rank (0 at Legend).</summary>
        public static int ToNext(int points)
        {
            int r = (int)For(points);
            return r + 1 < Thresholds.Length ? Thresholds[r + 1] - points : 0;
        }
    }

    // ---- applying a result ---------------------------------------------------------------------------------------

    public static class PartyProgress
    {
        /// <summary>
        /// Records a finished party quest: the base record and shards (scaled by difficulty), rank points, today's
        /// challenge, and the Kingdom Cup when a cup run is on. Call once per result.
        /// </summary>
        public static PartyReport Apply(SaveData save, PartyOutcome o, int today)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            var report = new PartyReport();
            if (o.mode == PartyMode.None || o.placement < 1) return report;
            if (save.party == null) save.party = new PartySave();
            var party = save.party;
            var before = party.Find(o.mode);
            float oldBest = before != null ? before.bestRaceSeconds : 0f;
            int oldPoints = before != null ? before.rankPoints : 0;

            // Base record (played/wins/podiums/best time) pays the normal-difficulty table; adjust to the difficulty.
            int baseShards = PartyResults.Record(save, o.mode, o.placement, o.raceSeconds);
            int scaled = PartyDifficulties.ScaleReward(baseShards, o.difficulty);
            if (scaled != baseShards) Economy.Grant(save, scaled - baseShards);
            report.shards = scaled;

            var rec = party.Find(o.mode);
            bool scored = o.mode == PartyMode.CoinRush || o.mode == PartyMode.KingOfTheHill || (o.mode == PartyMode.BalloonBattle && o.placement == 1);
            if (scored && o.score > rec.bestScore) { report.newBestScore = rec.bestScore > 0 || o.score > 0; rec.bestScore = o.score; }
            report.newBestTime = o.mode == PartyMode.ItemRace && o.raceSeconds > 0f && rec.bestRaceSeconds == o.raceSeconds &&
                                 (oldBest <= 0f || o.raceSeconds < oldBest);
            rec.rankPoints = oldPoints + PartyRanks.PointsFor(o.placement);
            report.rank = PartyRanks.For(rec.rankPoints);
            report.rankUp = report.rank > PartyRanks.For(oldPoints);

            // Daily challenge (once a day; a streak counts consecutive days).
            var daily = DailyParty.For(today);
            if (party.dailyDoneDay != today && daily.Met(o))
            {
                party.dailyStreak = party.dailyDoneDay == DailyParty.PreviousDay(today) ? party.dailyStreak + 1 : 1;
                party.dailyDoneDay = today;
                party.dailyCompleted++;
                Economy.Grant(save, daily.reward);
                report.dailyDone = true;
                report.dailyBonus = daily.reward;
            }
            report.dailyStreak = party.dailyStreak;

            // Kingdom Cup.
            var cup = party.cup;
            if (cup != null && cup.active && cup.CurrentEvent == o.mode)
            {
                report.cupEvent = true;
                if (cup.points == null || cup.points.Length != KingdomCup.Entrants) cup.points = new int[KingdomCup.Entrants];
                var places = o.placements;
                if (places == null || places.Length == 0) places = new[] { o.placement };
                for (int i = 0; i < KingdomCup.Entrants && i < places.Length; i++) cup.points[i] += KingdomCup.PointsFor(places[i]);
                cup.eventIndex++;
                if (cup.eventIndex >= KingdomCup.Events.Length)
                {
                    var standing = KingdomCup.Standings(cup.points);
                    var d = (PartyDifficulty)Math.Max(0, Math.Min(2, cup.difficulty));
                    report.cupFinished = true;
                    report.cupPlace = standing[0];
                    report.cupTrophy = KingdomCup.TrophyFor(standing[0]);
                    report.cupBonus = KingdomCup.Bonus(standing[0], d);
                    Economy.Grant(save, report.cupBonus);
                    party.cupsPlayed++;
                    party.AwardTrophy(d, report.cupTrophy);
                    cup.active = false;
                }
            }
            return report;
        }
    }

    // ---- Item Race ghost -----------------------------------------------------------------------------------------

    /// <summary>
    /// Your best Item Race run, sampled every Step seconds and packed into a short string for the save. Played back as
    /// a see-through ghost so you can race your own best time.
    /// </summary>
    public class GhostTrack
    {
        public const float Step = 0.1f;
        const float Quantum = 0.05f;
        /// <summary>About 3 minutes; longer runs stop recording (they won't be a best time anyway).</summary>
        public const int MaxSamples = 1800;
        readonly List<float> _x = new List<float>(), _y = new List<float>();
        float _sinceLast = Step;

        public int Count => _x.Count;
        public float Duration => _x.Count > 1 ? (_x.Count - 1) * Step : 0f;

        /// <summary>Feed the hero's position every frame; samples are kept every Step seconds.</summary>
        public void Record(float dt, float x, float y)
        {
            if (_x.Count >= MaxSamples) return;
            _sinceLast += dt;
            while (_sinceLast >= Step && _x.Count < MaxSamples)
            {
                _sinceLast -= Step;
                _x.Add(x); _y.Add(y);
            }
        }

        public void Add(float x, float y) { if (_x.Count < MaxSamples) { _x.Add(x); _y.Add(y); } }

        /// <summary>Position at time t (clamped to the ends), linearly between samples.</summary>
        public (float x, float y) At(float t)
        {
            if (_x.Count == 0) return (0f, 0f);
            float f = Math.Max(0f, t) / Step;
            int i = (int)Math.Floor(f);
            if (i >= _x.Count - 1) return (_x[_x.Count - 1], _y[_y.Count - 1]);
            float k = f - i;
            return (_x[i] + (_x[i + 1] - _x[i]) * k, _y[i] + (_y[i + 1] - _y[i]) * k);
        }

        /// <summary>Base64 of 16-bit deltas (first sample absolute), about 4 bytes a sample.</summary>
        public string Encode()
        {
            var bytes = new List<byte>(_x.Count * 4 + 8);
            int px = 0, py = 0;
            for (int i = 0; i < _x.Count; i++)
            {
                int qx = (int)Math.Round(_x[i] / Quantum), qy = (int)Math.Round(_y[i] / Quantum);
                int dx = Clamp16(qx - px), dy = Clamp16(qy - py);
                px += dx; py += dy;
                bytes.Add((byte)(dx & 0xFF)); bytes.Add((byte)((dx >> 8) & 0xFF));
                bytes.Add((byte)(dy & 0xFF)); bytes.Add((byte)((dy >> 8) & 0xFF));
            }
            return Convert.ToBase64String(bytes.ToArray());
        }

        public static GhostTrack Decode(string data)
        {
            var g = new GhostTrack();
            if (string.IsNullOrEmpty(data)) return g;
            byte[] b;
            try { b = Convert.FromBase64String(data); }
            catch (FormatException) { return g; }
            int px = 0, py = 0;
            for (int i = 0; i + 3 < b.Length && g.Count < MaxSamples; i += 4)
            {
                px += (short)(b[i] | (b[i + 1] << 8));
                py += (short)(b[i + 2] | (b[i + 3] << 8));
                g.Add(px * Quantum, py * Quantum);
            }
            return g;
        }

        static int Clamp16(int v) => Math.Max(short.MinValue, Math.Min(short.MaxValue, v));
    }
}
