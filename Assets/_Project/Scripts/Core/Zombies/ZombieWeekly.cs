using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>A weekly twist on the rules (Last Stand).</summary>
    public enum ZombieMutator { Sprinters, GlassCannon, BoxFever, Headhunter, HoundPack, IronRations, ThickHide, OpenDoors }

    /// <summary>
    /// The numbers a set of mutators changes. Plain multipliers so the game can apply them where the normal rule is
    /// used (zombie health and speed at spawn, points when earned, prices when paid).
    /// </summary>
    public sealed class MutatorSet
    {
        public static readonly MutatorSet None = new MutatorSet(new ZombieMutator[0]);

        readonly HashSet<ZombieMutator> _on;
        public IEnumerable<ZombieMutator> On => _on;
        public int Count => _on.Count;

        public float ZombieSpeed { get; } = 1f;
        public float ZombieHealth { get; } = 1f;
        public float HeroHealth { get; } = 1f;
        public float KillPoints { get; } = 1f;
        public float HitPoints { get; } = 1f;
        public float BoxPrice { get; } = 1f;
        public float DoorPrice { get; } = 1f;
        public bool NoWallGuns { get; }
        public bool NoDrops { get; }
        /// <summary>Hound rounds every N rounds instead of the 5th/15th/25th (0 = normal).</summary>
        public int HoundEvery { get; }

        public MutatorSet(IEnumerable<ZombieMutator> mutators)
        {
            _on = new HashSet<ZombieMutator>(mutators ?? new ZombieMutator[0]);
            foreach (var m in _on)
            {
                switch (m)
                {
                    case ZombieMutator.Sprinters: ZombieSpeed *= 1.25f; break;
                    case ZombieMutator.GlassCannon: ZombieHealth *= 0.5f; HeroHealth *= 0.5f; break;
                    case ZombieMutator.BoxFever: BoxPrice *= 0.5f; NoWallGuns = true; break;
                    case ZombieMutator.Headhunter: KillPoints *= 2f; HitPoints = 0f; break;
                    case ZombieMutator.HoundPack: HoundEvery = 3; break;
                    case ZombieMutator.IronRations: NoDrops = true; break;
                    case ZombieMutator.ThickHide: ZombieHealth *= 1.5f; KillPoints *= 1.5f; HitPoints *= 1.5f; break;
                    case ZombieMutator.OpenDoors: DoorPrice *= 0.5f; break;
                }
            }
        }

        public bool Has(ZombieMutator m) => _on.Contains(m);

        public int Price(int cost, float scale) => Math.Max(0, (int)Math.Round(cost * scale / 10f) * 10);
        public int BoxCost(int normal) => Price(normal, BoxPrice);
        public int DoorCost(int normal) => Price(normal, DoorPrice);
        public int Kill(int pts) => (int)Math.Round(pts * KillPoints);
        public int Hit(int pts) => (int)Math.Round(pts * HitPoints);
        public int Health(int hp) => Math.Max(1, (int)Math.Round(hp * ZombieHealth));
        public int HeroMaxHealth(int hp) => Math.Max(1, (int)Math.Round(hp * HeroHealth));

        /// <summary>Is this a hound round? Boss rounds never are.</summary>
        public bool IsHoundRound(SurvivalMode mode, int round)
        {
            if (HoundEvery <= 0) return LastStandRules.IsHoundRound(mode, round);
            return round > 0 && round % HoundEvery == 0 && !LastStandRules.IsBossRound(mode, round);
        }

        public static string Name(ZombieMutator m)
        {
            switch (m)
            {
                case ZombieMutator.Sprinters: return "SPRINTERS";
                case ZombieMutator.GlassCannon: return "GLASS CANNON";
                case ZombieMutator.BoxFever: return "BOX FEVER";
                case ZombieMutator.Headhunter: return "HEADHUNTER";
                case ZombieMutator.HoundPack: return "HOUND PACK";
                case ZombieMutator.IronRations: return "IRON RATIONS";
                case ZombieMutator.ThickHide: return "THICK HIDE";
                default: return "OPEN DOORS";
            }
        }

        public static string Info(ZombieMutator m)
        {
            switch (m)
            {
                case ZombieMutator.Sprinters: return "Every zombie is 25% faster";
                case ZombieMutator.GlassCannon: return "Zombies have half health. So do you";
                case ZombieMutator.BoxFever: return "The box costs half. Wall guns are boarded up";
                case ZombieMutator.Headhunter: return "Kills pay double. Hits pay nothing";
                case ZombieMutator.HoundPack: return "Hound rounds every 3rd round";
                case ZombieMutator.IronRations: return "No power-up drops (round caches still come)";
                case ZombieMutator.ThickHide: return "Zombies have 50% more health and pay 50% more";
                default: return "Debris costs half to clear";
            }
        }

        public string Names()
        {
            var parts = new List<string>();
            foreach (ZombieMutator m in Enum.GetValues(typeof(ZombieMutator))) if (_on.Contains(m)) parts.Add(Name(m));
            return parts.Count == 0 ? "NO MUTATORS" : string.Join(" + ", parts);
        }
    }

    /// <summary>One week's challenge: a map, a mode, two mutators and a round to reach.</summary>
    public sealed class WeeklyChallenge
    {
        public int Week;
        public string Key => "W" + Week;
        public string MapId;
        public SurvivalMode Mode;
        public ZombieMutator[] Mutators;
        public int GoalRound;
        public int RewardShards, RewardXp;
        public MutatorSet Set => new MutatorSet(Mutators);
    }

    /// <summary>
    /// WEEKLY CHALLENGE: the same for everyone, changing every Monday 00:00 UTC. Players asked for fresh reasons to come
    /// back (CODM's limited-time modes) without losing the permanent modes; this adds a rotating twist on top of them.
    /// The reward is paid once per week on reaching the goal round; never sold, never timed energy.
    /// </summary>
    public static class ZombieWeekly
    {
        /// <summary>Week 0 starts Monday 5 January 2026 (UTC).</summary>
        public static readonly DateTime Epoch = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        public const int RewardShards = 300, RewardXp = 600;

        public static int WeekOf(DateTime utc)
        {
            var d = (utc.ToUniversalTime().Date - Epoch).TotalDays;
            return (int)Math.Floor(d / 7.0);
        }

        public static DateTime StartOf(int week) => Epoch.AddDays(week * 7.0);
        public static DateTime EndOf(int week) => StartOf(week + 1);

        public static TimeSpan TimeLeft(DateTime utc) => EndOf(WeekOf(utc)) - utc.ToUniversalTime();

        public static string LeftText(DateTime utc)
        {
            var t = TimeLeft(utc);
            if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d {t.Hours}h left";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h left";
            return $"{Math.Max(1, (int)t.TotalMinutes)}m left";
        }

        /// <summary>Deterministic pseudo-random numbers from the week (same on every phone).</summary>
        static uint Hash(int week, uint salt)
        {
            uint x = (uint)week * 2654435761u ^ salt * 2246822519u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; x *= 3266489917u; x ^= x >> 16;
            return x;
        }

        public static WeeklyChallenge For(int week)
        {
            var maps = LastStandMap.All;
            var all = (ZombieMutator[])Enum.GetValues(typeof(ZombieMutator));
            int a = (int)(Hash(week, 1) % (uint)all.Length);
            int b = (int)(Hash(week, 2) % (uint)(all.Length - 1));
            if (b >= a) b++;
            var mode = week % 4 == 3 ? SurvivalMode.Hardcore : SurvivalMode.Endless;
            int goal = mode == SurvivalMode.Hardcore ? 10 : 12 + 2 * (int)(Hash(week, 3) % 3u);
            return new WeeklyChallenge
            {
                Week = week,
                MapId = maps[(int)(Hash(week, 4) % (uint)maps.Length)].Id,
                Mode = mode,
                Mutators = new[] { all[a], all[b] },
                GoalRound = goal,
                RewardShards = RewardShards,
                RewardXp = RewardXp,
            };
        }

        public static WeeklyChallenge Current(DateTime utc) => For(WeekOf(utc));

        /// <summary>
        /// A weekly run ended (or passed a round): records the best round for this week and, the first time the goal is
        /// reached, returns true (pay the reward once).
        /// </summary>
        public static bool Record(ZombieCareer c, WeeklyChallenge w, int round)
        {
            if (c == null || w == null) return false;
            c.Sanitize();
            if (c.weeklyKey != w.Key) { c.weeklyKey = w.Key; c.weeklyBest = 0; }
            if (round > c.weeklyBest) c.weeklyBest = round;
            if (round < w.GoalRound || c.weeklyDone.Contains(w.Key)) return false;
            c.weeklyDone.Add(w.Key);
            while (c.weeklyDone.Count > 12) c.weeklyDone.RemoveAt(0);
            c.weeklyWins++;
            return true;
        }

        public static bool Done(ZombieCareer c, WeeklyChallenge w) => c != null && c.weeklyDone != null && w != null && c.weeklyDone.Contains(w.Key);

        public static int BestThisWeek(ZombieCareer c, WeeklyChallenge w) => c != null && w != null && c.weeklyKey == w.Key ? c.weeklyBest : 0;
    }
}
