using System;
using System.Collections.Generic;

namespace PKR.Core
{
    [Serializable]
    public class LevelSplits
    {
        public string levelId = "";
        /// <summary>Seconds at each checkpoint (index = checkpoint order reached) in your best clear.</summary>
        public List<float> times = new List<float>();
        public float total;
    }

    [Serializable]
    public class StandRunRecord
    {
        public string map = "";
        public int mode, round, kills, day;
        public int shots, hits;
    }

    /// <summary>Story Quest checkpoint splits: your times at each checkpoint in your best clear, compared live.</summary>
    public static class Splits
    {
        public const int MaxLevels = 60;

        public static LevelSplits Find(SaveData s, string levelId)
        {
            if (s == null || s.storySplits == null || string.IsNullOrEmpty(levelId)) return null;
            foreach (var l in s.storySplits) if (l != null && l.levelId == levelId) return l;
            return null;
        }

        /// <summary>Your time at this checkpoint minus the best clear's (negative = ahead). Null when there's nothing to compare.</summary>
        public static float? Delta(SaveData s, string levelId, int index, float seconds)
        {
            var l = Find(s, levelId);
            if (l == null || index < 0 || index >= l.times.Count) return null;
            return seconds - l.times[index];
        }

        public static string Format(float delta) => (delta <= 0f ? "-" : "+") + Math.Abs(delta).ToString("0.0") + "s";

        /// <summary>Keeps this clear's splits when it's the fastest (or first) clear. Returns true when stored.</summary>
        public static bool Offer(SaveData s, string levelId, List<float> times, float total)
        {
            if (s == null || string.IsNullOrEmpty(levelId) || times == null || !(total > 0f)) return false;
            if (s.storySplits == null) s.storySplits = new List<LevelSplits>();
            var l = Find(s, levelId);
            if (l != null && l.total > 0f && l.total <= total) return false;
            if (l == null)
            {
                l = new LevelSplits { levelId = levelId };
                s.storySplits.Add(l);
                while (s.storySplits.Count > MaxLevels) s.storySplits.RemoveAt(0);
            }
            l.times = new List<float>(times);
            l.total = total;
            return true;
        }

        public static void Sanitize(SaveData s)
        {
            if (s.storySplits == null) s.storySplits = new List<LevelSplits>();
            s.storySplits.RemoveAll(l => l == null || string.IsNullOrEmpty(l.levelId) || l.times == null || !(l.total > 0f));
        }
    }

    /// <summary>BRAVE: the one-heart challenge (Settings). Clear a level with a single heart for a badge and Star Shards once.</summary>
    public static class StoryBrave
    {
        public const int Shards = 30;

        public static int Record(LevelRecord rec, bool braveRun, bool assisted)
        {
            if (rec == null || !braveRun || assisted || rec.brave) return 0;
            rec.brave = true;
            return Shards;
        }

        public static int Count(SaveData s)
        {
            int n = 0;
            if (s != null) foreach (var l in s.levels) if (l != null && l.brave) n++;
            return n;
        }
    }

    /// <summary>Last Stand run history (the last few runs on each map) and accuracy.</summary>
    public static class StandHistory
    {
        public const int PerMap = 5;

        public static void Add(SaveData s, StandRunRecord r)
        {
            if (s == null || r == null || string.IsNullOrEmpty(r.map)) return;
            var list = s.lifetime.standRuns;
            list.Add(r);
            int onMap = 0;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].map == r.map && ++onMap > PerMap) list.RemoveAt(i);
        }

        public static List<StandRunRecord> For(SaveData s, string map)
        {
            var o = new List<StandRunRecord>();
            if (s != null) foreach (var r in s.lifetime.standRuns) if (r != null && r.map == map) o.Add(r);
            return o;
        }

        public static float AverageRound(SaveData s, string map)
        {
            var l = For(s, map);
            if (l.Count == 0) return 0f;
            float t = 0f;
            foreach (var r in l) t += r.round;
            return t / l.Count;
        }

        /// <summary>Hits / shots as a whole percent (0 when no shots).</summary>
        public static int Accuracy(int shots, int hits) => shots <= 0 ? 0 : (int)Math.Round(100.0 * Math.Min(hits, shots) / shots);
    }

    /// <summary>A tip on the main menu, a different one each day.</summary>
    public static class TipOfTheDay
    {
        public static readonly string[] Tips =
        {
            "Break all three Guard Pips to EXPOSE a rival, then land a heavy hit to launch them.",
            "In Story Quest, a ghost of your best clear races you on replays. Beat it for a new best.",
            "The Bargain Bell makes mystery box spins cost 10 points for 20 seconds. Spin fast!",
            "Stuck on a level? Settings > ASSISTS has a slower game speed and an invincible assist.",
            "Hard CPUs wait at the ledge. Recover high or use your air dash early.",
            "The Field Guide pays Star Shards when you master a kind of zombie.",
            "Every hero gains mastery in every mode. Check PROFILE to see who's your favorite.",
            "Open the game every day for the daily gift. Day 7 of a streak pays the most.",
            "In Coin Rush, getting hit drops coins. Grab them back before the CPUs do.",
            "The Versus ladder gets harder each rung. Clear it for a big Star Shard bonus.",
            "Replaying a cleared level? The shard compass points to the shards you missed.",
            "Faint red arrows in a level mark where you fell last time. Watch your step!",
        };

        public static string For(int dayKey) => Tips[(int)((uint)(dayKey * 2654435761u) % (uint)Tips.Length)];
    }

    /// <summary>PROFILE badges: milestones earned from what's already in the save. Newly earned ones get a toast.</summary>
    public static class Badges
    {
        public struct Badge { public string id, name, how; public Func<SaveData, bool> earned; }

        static int Cleared(SaveData s) { int n = 0; foreach (var l in s.levels) if (l != null && l.completed) n++; return n; }
        static int PartyWins(SaveData s) { int n = 0; if (s.party != null) foreach (var p in s.party.records) if (p != null) n += p.wins; return n; }
        static int MaxMastery(SaveData s) { int m = 1; foreach (var h in s.lifetime.heroXp) m = Math.Max(m, HeroMastery.Level(h.xp)); return m; }
        static int BestRound(SaveData s) { int b = 0; if (s.zombieCareer != null) foreach (var r in s.zombieCareer.bestRounds) b = Math.Max(b, r); return b; }

        public static readonly Badge[] All =
        {
            new Badge { id = "first_clear", name = "FIRST STEPS", how = "Clear a Story level", earned = s => Cleared(s) >= 1 },
            new Badge { id = "ten_clears", name = "TRAILBLAZER", how = "Clear 10 Story levels", earned = s => Cleared(s) >= 10 },
            new Badge { id = "flawless5", name = "UNTOUCHABLE", how = "5 FLAWLESS clears", earned = s => StoryFlawless.Count(s) >= 5 },
            new Badge { id = "brave1", name = "BRAVE HEART", how = "A one-heart BRAVE clear", earned = s => StoryBrave.Count(s) >= 1 },
            new Badge { id = "party10", name = "LIFE OF THE PARTY", how = "Win 10 party quests", earned = s => PartyWins(s) >= 10 },
            new Badge { id = "ladder", name = "CLIMBER", how = "Clear a Versus ladder", earned = s => s.ladder != null && s.ladder.clears >= 1 },
            new Badge { id = "rumble10", name = "BRAWLER", how = "Win 10 rated matches", earned = s => s.rumble != null && s.rumble.wins >= 10 },
            new Badge { id = "zombies500", name = "UNDERTAKER", how = "500 zombie kills", earned = s => s.zombieCareer != null && s.zombieCareer.totalKills >= 500 },
            new Badge { id = "round20", name = "LAST ONE STANDING", how = "Reach round 20 in Last Stand", earned = s => BestRound(s) >= 20 },
            new Badge { id = "guide", name = "FIELD SCHOLAR", how = "Find every Field Guide entry", earned = s => ZombieFieldGuide.Count(s.zombieCareer).seen >= ZombieFieldGuide.All.Length },
            new Badge { id = "mastery5", name = "DEVOTED", how = "A hero at mastery 5", earned = s => MaxMastery(s) >= 5 },
            new Badge { id = "streak7", name = "REGULAR", how = "A 7-day daily gift streak", earned = s => s.lifetime.bestLoginStreak >= 7 },
        };

        /// <summary>Badges earned now that weren't recorded before. Records them.</summary>
        public static List<Badge> Update(SaveData s)
        {
            var fresh = new List<Badge>();
            if (s == null) return fresh;
            foreach (var b in All)
            {
                if (s.lifetime.badges.Contains(b.id)) continue;
                bool ok;
                try { ok = b.earned(s); } catch (Exception) { ok = false; }
                if (!ok) continue;
                s.lifetime.badges.Add(b.id);
                fresh.Add(b);
            }
            return fresh;
        }

        public static bool Has(SaveData s, string id) => s != null && s.lifetime.badges.Contains(id);

        /// <summary>Mastery levels plus badges: when it grows past what PROFILE last showed, the menu marks PROFILE NEW.</summary>
        public static int ProfileScore(SaveData s)
        {
            if (s == null) return 0;
            int n = s.lifetime.badges.Count;
            foreach (var h in s.lifetime.heroXp) n += HeroMastery.Level(h.xp);
            return n;
        }
    }

    /// <summary>Weekly recap: on the first open of a new week, how last week went (from a snapshot taken when it began).</summary>
    public static class WeeklyRecap
    {
        /// <summary>Monday-based week key (yyyy*100 + week of year-ish, stable for a given Monday).</summary>
        public static int WeekKey(DateTime d)
        {
            int back = ((int)d.DayOfWeek + 6) % 7; // days since Monday
            var monday = d.Date.AddDays(-back);
            return monday.Year * 10000 + monday.Month * 100 + monday.Day;
        }

        public static int[] Snapshot(SaveData s)
        {
            int cleared = 0; foreach (var l in s.levels) if (l != null && l.completed) cleared++;
            int party = 0; if (s.party != null) foreach (var p in s.party.records) if (p != null) party += p.wins;
            return new[] { cleared, s.zombieCareer != null ? s.zombieCareer.totalKills : 0, s.rumble != null ? s.rumble.wins : 0, party, (int)PlayTime.Total(s) };
        }

        public static readonly string[] Labels = { "Story levels cleared", "Zombies put down", "Rated wins", "Party wins", "Time played" };

        /// <summary>Starts a new week if needed. Returns last week's deltas when a recap should be shown, else null.</summary>
        public static int[] Roll(SaveData s, int weekKey)
        {
            if (s == null) return null;
            var l = s.lifetime;
            var now = Snapshot(s);
            if (l.weekKey == 0 || l.weekSnap == null || l.weekSnap.Count != now.Length)
            {
                l.weekKey = weekKey; l.weekSnap = new List<int>(now);
                return null;
            }
            if (l.weekKey == weekKey) return null;
            var delta = new int[now.Length];
            bool any = false;
            for (int i = 0; i < now.Length; i++) { delta[i] = Math.Max(0, now[i] - l.weekSnap[i]); if (delta[i] > 0) any = true; }
            l.weekKey = weekKey; l.weekSnap = new List<int>(now);
            return any ? delta : null;
        }
    }

    /// <summary>An optional, gentle break reminder after long sessions (Settings, off by default).</summary>
    public static class BreakReminder
    {
        public const float Every = 60f * 60f;
        public static bool Due(bool on, float sessionSeconds, int alreadyShown) => on && sessionSeconds >= Every * (alreadyShown + 1);
    }
}
