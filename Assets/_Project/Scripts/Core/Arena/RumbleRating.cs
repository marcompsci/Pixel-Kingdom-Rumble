using System;
using System.Collections.Generic;

namespace PKR.Core
{
    // =============================================================================================
    // Rumble Rating: the ranked point system for Arena Clash, Versus and the Daily Rumble.
    //
    // How it compares to other brawlers:
    //  * Placement first (battle-royale style), KOs and falls second (platform-fighter style).
    //  * Fair by design: the performance bonus is capped below the gap between placements, so a
    //    better placement ALWAYS earns more than a worse one in the same match (a common complaint
    //    about other ranked modes is 2nd place out-scoring 1st).
    //  * Every point is itemised on the results screen. Nothing hidden.
    //  * No tilt: Bronze can't lose points, a fresh promotion is shielded for 3 matches, and after
    //    two bad matches in a row losses are halved.
    //  * Harder CPUs pay more. Auto difficulty adapts to how you're doing.
    // Everything here is pure C# and unit-tested.
    // =============================================================================================

    public enum RumbleTier { Bronze, Silver, Gold, Platinum, Diamond, Legend }

    /// <summary>Match twists for the Daily Rumble (and custom arena matches).</summary>
    [Flags]
    public enum Mutator
    {
        None = 0,
        LowGravity = 1,     // floaty jumps, long recoveries
        HeavyHitters = 2,   // 35% more knockback
        Giants = 4,         // every fighter is 30% bigger
        SuddenDeath = 8,    // one stock each
    }

    /// <summary>Everything the scorer needs to know about one finished match, from the player's point of view.</summary>
    public struct RumbleMatchReport
    {
        public int fighters;          // 2..4
        public int placement;         // 1-based (ties share a placement)
        public int knockouts;
        public int falls;             // includes self-destructs
        public int selfDestructs;
        public bool firstBlood;
        public int revengeKOs;
        public bool comeback;         // won after being in last place
        public bool clutch;           // won on the last stock (Stock, started with 2+)
        public float difficulty;      // 0 (easiest CPU) .. 1 (hardest)
        public bool daily;            // the day's Daily Rumble attempt
        public int dailyStreak;       // consecutive days including today (only used when daily)
    }

    public struct RumbleLine
    {
        public string label;
        public int points;
        public RumbleLine(string label, int points) { this.label = label; this.points = points; }
        public override string ToString() => $"{label} {(points >= 0 ? "+" : "")}{points}";
    }

    /// <summary>The itemised result: placement, capped performance bonus, multiplier, extras, total.</summary>
    public class RumbleBreakdown
    {
        public readonly List<RumbleLine> lines = new List<RumbleLine>();
        public int placementPoints;
        public int performanceRaw;     // before the cap
        public int performance;        // after the cap (0..PerformanceCap)
        public float multiplier = 1f;
        public int dailyBonus;
        public int total;              // points earned this match (never negative)
    }

    public static class RumblePoints
    {
        /// <summary>The performance bonus can never exceed this, and it is smaller than every placement gap.</summary>
        public const int PerformanceCap = 35;
        public const int PerKO = 8;
        public const int PerFall = -4;
        public const int SelfDestructExtra = -4; // on top of the fall: a self-destruct costs 8 in total
        public const int FirstBlood = 5;
        public const int PerRevenge = 4;
        public const int Untouchable = 10;
        public const int Comeback = 8;
        public const int Clutch = 6;
        public const int DailyBase = 20;
        public const int DailyPerStreakDay = 5;
        public const int DailyStreakCap = 5;

        static readonly int[] Two = { 120, 0 };
        static readonly int[] Three = { 130, 70, 0 };
        static readonly int[] Four = { 130, 80, 40, 0 };

        /// <summary>Placement points for a match of this size (1-based placement).</summary>
        public static int ForPlacement(int fighters, int placement)
        {
            var table = fighters <= 2 ? Two : fighters == 3 ? Three : Four;
            if (placement < 1) placement = 1;
            if (placement > table.Length) placement = table.Length;
            return table[placement - 1];
        }

        /// <summary>Smallest gap between neighbouring placements for this match size.</summary>
        public static int MinPlacementGap(int fighters)
        {
            var table = fighters <= 2 ? Two : fighters == 3 ? Three : Four;
            int gap = int.MaxValue;
            for (int i = 1; i < table.Length; i++) gap = Math.Min(gap, table[i - 1] - table[i]);
            return gap;
        }

        /// <summary>CPU difficulty (0..1) to point multiplier: 0.7x for the easiest, 1.35x for the hardest.</summary>
        public static float Multiplier(float difficulty)
        {
            if (float.IsNaN(difficulty)) difficulty = 0.5f;
            difficulty = Math.Max(0f, Math.Min(1f, difficulty));
            return 0.7f + 0.65f * difficulty;
        }

        public static float DifficultyFor(BotLevel level) =>
            level == BotLevel.Easy ? 0f : level == BotLevel.Hard ? 1f : 0.5f;

        public static int DailyBonusFor(int streak) =>
            DailyBase + DailyPerStreakDay * Math.Max(0, Math.Min(DailyStreakCap, streak) - 1);

        public static RumbleBreakdown Score(RumbleMatchReport r)
        {
            var b = new RumbleBreakdown();
            int fighters = Math.Max(2, Math.Min(4, r.fighters));
            b.placementPoints = ForPlacement(fighters, r.placement);
            b.lines.Add(new RumbleLine(Ordinal(r.placement) + " place", b.placementPoints));

            int ko = Math.Max(0, r.knockouts);
            int falls = Math.Max(0, r.falls);
            int sd = Math.Max(0, Math.Min(falls, r.selfDestructs));
            int perf = 0;
            void Add(string label, int pts) { if (pts == 0) return; perf += pts; b.lines.Add(new RumbleLine(label, pts)); }

            Add(ko == 1 ? "1 knockout" : $"{ko} knockouts", ko * PerKO);
            Add(falls == 1 ? "1 fall" : $"{falls} falls", falls * PerFall);
            Add(sd == 1 ? "Self-destruct" : $"{sd} self-destructs", sd * SelfDestructExtra);
            if (r.firstBlood) Add("First blood", FirstBlood);
            Add(r.revengeKOs == 1 ? "Revenge" : $"Revenge x{r.revengeKOs}", Math.Max(0, r.revengeKOs) * PerRevenge);
            if (falls == 0) Add("Untouchable", Untouchable);
            if (r.comeback && r.placement == 1) Add("Comeback", Comeback);
            if (r.clutch && r.placement == 1) Add("Clutch", Clutch);

            b.performanceRaw = perf;
            b.performance = Math.Max(0, Math.Min(PerformanceCap, perf));
            if (perf > PerformanceCap) b.lines.Add(new RumbleLine("Style cap", PerformanceCap - perf));
            if (perf < 0) b.lines.Add(new RumbleLine("Floor (no negative style)", -perf));

            b.multiplier = Multiplier(r.difficulty);
            int subtotal = b.placementPoints + b.performance;
            int scaled = (int)Math.Round(subtotal * b.multiplier, MidpointRounding.AwayFromZero);
            if (scaled != subtotal) b.lines.Add(new RumbleLine($"CPU level x{b.multiplier:0.00}", scaled - subtotal));

            if (r.daily)
            {
                b.dailyBonus = DailyBonusFor(r.dailyStreak);
                b.lines.Add(new RumbleLine(r.dailyStreak > 1 ? $"Daily Rumble ({r.dailyStreak}-day streak)" : "Daily Rumble", b.dailyBonus));
            }
            b.total = Math.Max(0, scaled + b.dailyBonus);
            return b;
        }

        public static string Ordinal(int n) => n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : n + "th";
    }

    /// <summary>Tiers, the per-tier entry cost ("par") and protection rules.</summary>
    public static class RumbleTiers
    {
        public static readonly int[] Thresholds = { 0, 250, 600, 1100, 1700, 2400 };
        static readonly int[] Par = { 40, 55, 70, 80, 90, 100 };
        public const int PromotionShieldMatches = 3;
        public const int StreakProtectionAfter = 2;

        public static RumbleTier TierFor(int rating)
        {
            for (int i = Thresholds.Length - 1; i >= 0; i--)
                if (rating >= Thresholds[i]) return (RumbleTier)i;
            return RumbleTier.Bronze;
        }

        public static int ParFor(RumbleTier tier) => Par[(int)tier];
        public static int FloorOf(RumbleTier tier) => Thresholds[(int)tier];
        public static int NextThreshold(RumbleTier tier) => tier == RumbleTier.Legend ? -1 : Thresholds[(int)tier + 1];

        public static string Name(RumbleTier tier) => tier.ToString().ToUpperInvariant();

        /// <summary>0..1 progress through the current tier (1 at Legend).</summary>
        public static float Progress(int rating)
        {
            var t = TierFor(rating);
            int next = NextThreshold(t);
            if (next < 0) return 1f;
            int floor = FloorOf(t);
            return Math.Max(0f, Math.Min(1f, (rating - floor) / (float)(next - floor)));
        }
    }

    /// <summary>Saved ranked state.</summary>
    [Serializable]
    public class RumbleProfile
    {
        public const float StartingSkill = 0.35f;

        public int rating;
        public int bestRating;
        public int matches;
        public int wins;
        public int lossStreak;          // matches in a row placed in the bottom half
        public int shieldMatches;       // promotion shield left
        public float skill = StartingSkill; // auto difficulty, 0..1
        public int lastDailyDay;        // yyyymmdd of the last rated Daily Rumble
        public int dailyStreak;
        public int bestDailyStreak;
        public List<int> recentDeltas = new List<int>();

        public RumbleTier Tier => RumbleTiers.TierFor(rating);

        public void Sanitize()
        {
            if (rating < 0) rating = 0;
            if (bestRating < rating) bestRating = rating;
            if (matches < 0) matches = 0;
            if (wins < 0 || wins > matches) wins = Math.Max(0, Math.Min(matches, wins));
            if (lossStreak < 0) lossStreak = 0;
            if (shieldMatches < 0 || shieldMatches > RumbleTiers.PromotionShieldMatches) shieldMatches = 0;
            if (float.IsNaN(skill) || skill < 0f || skill > 1f) skill = StartingSkill;
            if (dailyStreak < 0) dailyStreak = 0;
            if (bestDailyStreak < dailyStreak) bestDailyStreak = dailyStreak;
            if (recentDeltas == null) recentDeltas = new List<int>();
            while (recentDeltas.Count > RumbleRating.HistoryLength) recentDeltas.RemoveAt(0);
        }
    }

    /// <summary>The outcome of applying one match to the profile.</summary>
    public struct RumbleChange
    {
        public int earned, par, delta;
        public int oldRating, newRating;
        public RumbleTier oldTier, newTier;
        public bool promoted, demoted;
        public bool shielded;            // a loss was absorbed by Bronze or the promotion shield
        public bool streakProtected;     // a loss was halved by losing-streak protection
        public string note;
    }

    public static class RumbleRating
    {
        public const int HistoryLength = 10;

        /// <summary>Apply a scored match. Bottom-half placements count toward the losing streak.</summary>
        public static RumbleChange Apply(RumbleProfile p, RumbleBreakdown b, int placement, int fighters)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            var c = new RumbleChange { oldRating = p.rating, oldTier = p.Tier, earned = b != null ? b.total : 0 };
            c.par = RumbleTiers.ParFor(c.oldTier);
            int delta = c.earned - c.par;
            bool badMatch = placement * 2 > Math.Max(2, fighters);

            if (delta < 0)
            {
                if (c.oldTier == RumbleTier.Bronze) { delta = 0; c.shielded = true; c.note = "Bronze never loses points"; }
                else if (p.shieldMatches > 0) { delta = 0; c.shielded = true; c.note = "Promotion shield"; }
                else if (p.lossStreak >= RumbleTiers.StreakProtectionAfter)
                {
                    delta = -(-delta / 2);
                    c.streakProtected = true;
                    c.note = "Losing-streak protection: loss halved";
                }
            }

            int next = Math.Max(0, p.rating + delta);
            c.newRating = next;
            c.delta = next - p.rating;
            c.newTier = RumbleTiers.TierFor(next);
            c.promoted = c.newTier > c.oldTier;
            c.demoted = c.newTier < c.oldTier;

            if (p.shieldMatches > 0) p.shieldMatches--;
            if (c.promoted) p.shieldMatches = RumbleTiers.PromotionShieldMatches;

            p.rating = next;
            if (next > p.bestRating) p.bestRating = next;
            p.matches++;
            if (placement == 1) p.wins++;
            p.lossStreak = badMatch ? p.lossStreak + 1 : 0;
            p.recentDeltas.Add(c.delta);
            while (p.recentDeltas.Count > HistoryLength) p.recentDeltas.RemoveAt(0);
            if (c.promoted) c.note = "Promoted to " + RumbleTiers.Name(c.newTier) + "!";
            return c;
        }
    }

    /// <summary>
    /// Auto CPU level: a 0..1 skill estimate nudged after every match. Winning pushes it up, finishing last
    /// pulls it down, so the CPU settles where you win roughly a fair share of matches.
    /// </summary>
    public static class AdaptiveDifficulty
    {
        public const float Step = 0.07f;

        /// <summary>+1 for 1st, -1 for last, linear in between.</summary>
        public static float PlacementScore(int placement, int fighters)
        {
            fighters = Math.Max(2, fighters);
            placement = Math.Max(1, Math.Min(fighters, placement));
            return 1f - 2f * (placement - 1) / (fighters - 1);
        }

        public static float Update(float skill, int placement, int fighters)
        {
            if (float.IsNaN(skill)) skill = RumbleProfile.StartingSkill;
            float s = skill + Step * PlacementScore(placement, fighters);
            return Math.Max(0f, Math.Min(1f, s));
        }

        /// <summary>Nearest named level, for display.</summary>
        public static BotLevel Nearest(float skill) => skill < 0.25f ? BotLevel.Easy : skill > 0.75f ? BotLevel.Hard : BotLevel.Normal;

        /// <summary>CPU tuning blended between the named levels: 0 = Easy, 0.5 = Normal, 1 = Hard.</summary>
        public static BotDifficulty Blend(float skill)
        {
            skill = float.IsNaN(skill) ? 0.5f : Math.Max(0f, Math.Min(1f, skill));
            BotDifficulty a, b; float t;
            if (skill <= 0.5f) { a = BotDifficulty.For(BotLevel.Easy); b = BotDifficulty.For(BotLevel.Normal); t = skill / 0.5f; }
            else { a = BotDifficulty.For(BotLevel.Normal); b = BotDifficulty.For(BotLevel.Hard); t = (skill - 0.5f) / 0.5f; }
            float L(float x, float y) => x + (y - x) * t;
            return new BotDifficulty
            {
                reactionTime = L(a.reactionTime, b.reactionTime),
                accuracy = L(a.accuracy, b.accuracy),
                aggression = L(a.aggression, b.aggression),
                recoverySkill = L(a.recoverySkill, b.recoverySkill),
                dodgeChance = L(a.dodgeChance, b.dodgeChance),
            };
        }
    }

    /// <summary>
    /// Watches KOs during an arena match (player = slot 0) for First Blood, Revenge, Comeback and self-destructs.
    /// Feed it every fall in order with the placements right after that fall.
    /// </summary>
    public class RumbleTracker
    {
        public int PlayerSlot { get; }
        public bool FirstBlood { get; private set; }
        public int RevengeKOs { get; private set; }
        public int SelfDestructs { get; private set; }
        public bool WasLast { get; private set; }

        bool _anyKO;
        int _lastKiller = -1;

        public RumbleTracker(int playerSlot = 0) { PlayerSlot = playerSlot; }

        public void OnFall(int victim, int attacker, int[] placementsAfter)
        {
            if (attacker >= 0 && attacker != victim)
            {
                if (!_anyKO && attacker == PlayerSlot) FirstBlood = true;
                _anyKO = true;
                if (attacker == PlayerSlot && victim == _lastKiller) { RevengeKOs++; _lastKiller = -1; }
                if (victim == PlayerSlot) _lastKiller = attacker;
            }
            else if (victim == PlayerSlot) SelfDestructs++;

            if (placementsAfter != null && PlayerSlot < placementsAfter.Length && placementsAfter.Length > 1)
            {
                int worst = 0;
                foreach (int p in placementsAfter) worst = Math.Max(worst, p);
                // Last on its own (not a tie with everyone).
                bool allTied = true;
                foreach (int p in placementsAfter) if (p != placementsAfter[0]) allTied = false;
                if (!allTied && placementsAfter[PlayerSlot] == worst) WasLast = true;
            }
        }
    }

    /// <summary>The Daily Rumble: the same twist for everyone on a given day, one rated attempt per day.</summary>
    public struct DailyRumbleSpec
    {
        public int day;           // yyyymmdd
        public Mutator mutators;
        public MatchMode mode;
        public int bots;
        public int stocks;
        public int minutes;
        public string title;
        public string description;
    }

    public static class DailyRumble
    {
        static readonly (Mutator m, string title, string desc)[] Menu =
        {
            (Mutator.LowGravity, "MOON BOUNCE", "Low gravity: floaty jumps and huge recoveries."),
            (Mutator.HeavyHitters, "HEAVY HITTERS", "Every hit launches 35% farther."),
            (Mutator.Giants, "GIANT RUMBLE", "Everyone is 30% bigger. Nowhere to hide."),
            (Mutator.SuddenDeath | Mutator.HeavyHitters, "ONE SHOT", "One stock each, and hits launch farther."),
            (Mutator.LowGravity | Mutator.HeavyHitters, "ORBIT", "Low gravity AND heavy hits. Fly far."),
            (Mutator.Giants | Mutator.SuddenDeath, "TITAN DUEL", "Giant fighters, one stock each."),
            (Mutator.LowGravity | Mutator.Giants, "PARADE FLOATS", "Big, floaty fighters."),
        };

        public static int DayKey(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

        public static DailyRumbleSpec For(int dayKey)
        {
            // Deterministic hash so everyone gets the same twist on the same day.
            uint h = (uint)dayKey * 2654435761u;
            h ^= h >> 15;
            var pick = Menu[(int)(h % (uint)Menu.Length)];
            bool timed = ((h >> 8) & 3) == 0 && (pick.m & Mutator.SuddenDeath) == 0;
            return new DailyRumbleSpec
            {
                day = dayKey, mutators = pick.m, title = pick.title, description = pick.desc,
                mode = timed ? MatchMode.Timed : MatchMode.Stock,
                bots = 3,
                stocks = (pick.m & Mutator.SuddenDeath) != 0 ? 1 : 2,
                minutes = 2,
            };
        }

        public static bool PlayedToday(RumbleProfile p, int today) => p != null && p.lastDailyDay == today;

        /// <summary>Streak the player will have if they play today (consecutive calendar days).</summary>
        public static int StreakIfPlayed(RumbleProfile p, int today)
        {
            if (p == null) return 1;
            if (p.lastDailyDay == today) return Math.Max(1, p.dailyStreak);
            return p.lastDailyDay == PreviousDay(today) ? p.dailyStreak + 1 : 1;
        }

        /// <summary>Records today's rated attempt. Returns the new streak.</summary>
        public static int RecordPlayed(RumbleProfile p, int today)
        {
            int s = StreakIfPlayed(p, today);
            p.dailyStreak = s;
            p.lastDailyDay = today;
            if (s > p.bestDailyStreak) p.bestDailyStreak = s;
            return s;
        }

        public static int PreviousDay(int dayKey)
        {
            int y = dayKey / 10000, m = dayKey / 100 % 100, d = dayKey % 100;
            try { return DayKey(new DateTime(y, m, d).AddDays(-1)); }
            catch (ArgumentOutOfRangeException) { return 0; }
        }

        public static string Describe(Mutator m)
        {
            if (m == Mutator.None) return "No twist";
            var parts = new List<string>();
            if ((m & Mutator.LowGravity) != 0) parts.Add("Low gravity");
            if ((m & Mutator.HeavyHitters) != 0) parts.Add("Heavy hitters");
            if ((m & Mutator.Giants) != 0) parts.Add("Giants");
            if ((m & Mutator.SuddenDeath) != 0) parts.Add("One stock");
            return string.Join(" + ", parts);
        }
    }
}
