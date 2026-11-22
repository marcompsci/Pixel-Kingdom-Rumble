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
            "Turn on BRAVE in Settings > ASSISTS: clear a level on one heart for bonus shards.",
            "Every checkpoint shows your split against your best run. Green means ahead!",
            "A PERFECT clear is S rank, FLAWLESS, and every shard and secret. It pays bonus shards.",
            "Every 5 badges you earn pays 25 Star Shards. Check PROFILE for the ones you're closest to.",
            "Versus win streaks are saved. Five in a row earns the ON FIRE badge.",
            "In Last Stand, your best round is called out at the start. Beat it for a NEW BEST alert.",
            "Tap the tip on the main menu to read the next one.",
            "Story Stats adds up your best times. Shave seconds off any level to lower your total.",
            "The fighter with the most KOs in an Arena Clash match is the MVP.",
            "Too many pop-ups? Settings > ASSISTS > Quiet celebrations hides the cheers and keeps the important news.",
            "Need more time to read? Settings > DISPLAY > Longer messages.",
            "Every party quest counts toward PARTY ANIMAL. Try all five in the Kingdom Plaza.",
            "Can't decide? Hero select has a RANDOM button.",
            "PROFILE > ALL BADGES shows every badge and how close you are.",
            "The shop shows what you're saving for. Upgrades help Story Quest and Shadow Contracts only.",
            "Play at least once a week to build a week streak. Four in a row earns SEASONED.",
            "Badges raise your title: SQUIRE, ADVENTURER, KNIGHT, HERO, LEGEND, MYTH. See PROFILE.",
            "Win a Versus round without losing a heart for a PERFECT round. Five earn FLAWLESS FIGHTER.",
            "The training lab remembers your best combo, even after you close the game.",
            "Down to your last heart? The game will warn you. Slow down and look for a checkpoint.",
        };

        public static string For(int dayKey) => Tips[Index(dayKey)];
        public static int Index(int dayKey) => (int)((uint)(dayKey * 2654435761u) % (uint)Tips.Length);
        /// <summary>The tip after the one shown (tap the tip on the menu to cycle).</summary>
        public static string Next(ref int index) { index = ((index + 1) % Tips.Length + Tips.Length) % Tips.Length; return Tips[index]; }
    }

    /// <summary>PROFILE badges: milestones earned from what's already in the save. Newly earned ones get a toast.</summary>
    public static class Badges
    {
        /// <summary>A badge: earned when have(save) reaches need. Progress shows on PROFILE and as the menu's NEXT GOAL.</summary>
        public struct Badge
        {
            public string id, name, how; public int need; public Func<SaveData, int> have;
            public bool Earned(SaveData s) { try { return have(s) >= need; } catch (Exception) { return false; } }
            public int Have(SaveData s) { try { return Math.Max(0, Math.Min(need, have(s))); } catch (Exception) { return 0; } }
        }

        static int Cleared(SaveData s) { int n = 0; foreach (var l in s.levels) if (l != null && l.completed) n++; return n; }
        static int SRanks(SaveData s) { int n = 0; foreach (var l in s.levels) if (l != null && l.completed && l.BestRank == ClearRank.S) n++; return n; }
        static int Secrets(SaveData s) { int n = 0; foreach (var l in s.levels) if (l != null) n += Math.Max(0, l.mostSecretsFound); return n; }
        static int PartyWins(SaveData s) { int n = 0; if (s.party != null) foreach (var p in s.party.records) if (p != null) n += p.wins; return n; }
        static int MaxMastery(SaveData s) { int m = 1; foreach (var h in s.lifetime.heroXp) m = Math.Max(m, HeroMastery.Level(h.xp)); return m; }
        static int BestRound(SaveData s) { int b = 0; if (s.zombieCareer != null) foreach (var r in s.zombieCareer.bestRounds) b = Math.Max(b, r); return b; }
        static int Kills(SaveData s) => s.zombieCareer != null ? s.zombieCareer.totalKills : 0;
        static int RivalsBeaten(SaveData s) { int n = 0; foreach (var h in s.lifetime.headToHead) if (h != null && h.wins > 0) n++; return n; }

        public static readonly Badge[] All =
        {
            new Badge { id = "first_clear", name = "FIRST STEPS", how = "Clear a Story level", need = 1, have = Cleared },
            new Badge { id = "ten_clears", name = "TRAILBLAZER", how = "Clear 10 Story levels", need = 10, have = Cleared },
            new Badge { id = "flawless5", name = "UNTOUCHABLE", how = "5 FLAWLESS clears", need = 5, have = StoryFlawless.Count },
            new Badge { id = "brave1", name = "BRAVE HEART", how = "A one-heart BRAVE clear", need = 1, have = StoryBrave.Count },
            new Badge { id = "party10", name = "LIFE OF THE PARTY", how = "Win 10 party quests", need = 10, have = PartyWins },
            new Badge { id = "ladder", name = "CLIMBER", how = "Clear a Versus ladder", need = 1, have = s => s.ladder != null ? s.ladder.clears : 0 },
            new Badge { id = "rumble10", name = "BRAWLER", how = "Win 10 rated matches", need = 10, have = s => s.rumble != null ? s.rumble.wins : 0 },
            new Badge { id = "zombies500", name = "UNDERTAKER", how = "500 zombie kills", need = 500, have = Kills },
            new Badge { id = "round20", name = "LAST ONE STANDING", how = "Reach round 20 in Last Stand", need = 20, have = BestRound },
            new Badge { id = "guide", name = "FIELD SCHOLAR", how = "Find every Field Guide entry", need = ZombieFieldGuide.All.Length, have = s => ZombieFieldGuide.Count(s.zombieCareer).seen },
            new Badge { id = "mastery5", name = "DEVOTED", how = "A hero at mastery 5", need = 5, have = MaxMastery },
            new Badge { id = "streak7", name = "REGULAR", how = "A 7-day daily gift streak", need = 7, have = s => s.lifetime.bestLoginStreak },
            // Round 5.
            new Badge { id = "sranks10", name = "STAR PUPIL", how = "10 S-rank Story clears", need = 10, have = SRanks },
            new Badge { id = "brave10", name = "LIONHEART", how = "10 BRAVE clears", need = 10, have = StoryBrave.Count },
            new Badge { id = "secrets10", name = "SECRET KEEPER", how = "Find 10 Story secrets", need = 10, have = Secrets },
            new Badge { id = "zombies2000", name = "GRAVEKEEPER", how = "2,000 zombie kills", need = 2000, have = Kills },
            new Badge { id = "rivals6", name = "RIVALRY", how = "Beat 6 different Versus heroes", need = 6, have = RivalsBeaten },
            new Badge { id = "streak_vs5", name = "ON FIRE", how = "Win 5 Versus matches in a row", need = 5, have = s => s.lifetime.vsBestStreak },
            new Badge { id = "days30", name = "LOYAL SUBJECT", how = "Play on 30 different days", need = 30, have = s => s.lifetime.loginGifts },
            new Badge { id = "tries100", name = "NEVER GIVE UP", how = "Start Story levels 100 times", need = 100, have = StoryRecords.TotalTries },
            new Badge { id = "runs50", name = "NIGHT SHIFT", how = "Play 50 Last Stand runs", need = 50, have = s => s.zombieCareer != null ? s.zombieCareer.lastStandRuns : 0 },
            new Badge { id = "perfect5", name = "PERFECTIONIST", how = "5 PERFECT Story clears", need = 5, have = StoryPerfect.Count },
            new Badge { id = "world100", name = "COMPLETIONIST", how = "A Story world at 100%", need = 1, have = s => s.lifetime.worlds100.Count },
            new Badge { id = "streak_vs10", name = "UNSTOPPABLE", how = "Win 10 Versus matches in a row", need = 10, have = s => s.lifetime.vsBestStreak },
            new Badge { id = "sharpshooter", name = "SHARPSHOOTER", how = "60% accuracy in a Last Stand run (50+ shots)", need = 60, have = s => s.lifetime.bestAccuracy },
            new Badge { id = "arena_ko250", name = "KNOCKOUT ARTIST", how = "250 Arena Clash KOs", need = 250, have = s => s.lifetime.arenaKOs },
            new Badge { id = "combo8", name = "COMBO KING", how = "An 8-hit combo in Arena Clash", need = 8, have = s => s.lifetime.arenaBestCombo },
            new Badge { id = "party_all", name = "PARTY ANIMAL", how = "Play every party quest", need = PartyModes.All.Length, have = PartyMeta.ModesPlayed },
            new Badge { id = "weekly3", name = "WEEKLY WARRIOR", how = "Beat 3 Zombies weekly challenges", need = 3, have = s => s.zombieCareer != null ? s.zombieCareer.weeklyDone.Count : 0 },
            new Badge { id = "daily30", name = "DUTIFUL", how = "Complete 30 daily challenges", need = 30, have = s => s.lifetime.dailyDone },
            new Badge { id = "arena_win50", name = "CHAMPION", how = "Win 50 Arena Clash matches", need = 50, have = s => s.lifetime.arenaWins },
            new Badge { id = "shopper5", name = "SHOPPER", how = "Buy 5 things in the Star Shard shop", need = 5, have = ShopMeta.Purchases },
            new Badge { id = "codex30", name = "LOREKEEPER", how = "Find 30 Codex entries", need = 30, have = s => s.unlockedCodexEntries.Count },
            new Badge { id = "weeks4", name = "SEASONED", how = "Play 4 weeks in a row", need = 4, have = s => s.lifetime.bestWeekStreak },
            new Badge { id = "heroes6", name = "HERO COLLECTOR", how = "Unlock 6 heroes", need = 6, have = s => s.unlockedCharacters.Count },
            new Badge { id = "contracts10", name = "SHADOW AGENT", how = "Score on 10 Shadow Contracts", need = 10, have = ContractMeta.Cleared },
            new Badge { id = "vs_perfect5", name = "FLAWLESS FIGHTER", how = "Win 5 Versus rounds without a scratch", need = 5, have = s => s.lifetime.vsPerfectRounds },
            new Badge { id = "night_owl", name = "NIGHT OWL", how = "Play between midnight and 4 a.m.", need = 1, have = s => s.lifetime.nightOwl ? 1 : 0 },
            new Badge { id = "early_bird", name = "EARLY BIRD", how = "Play between 5 and 8 a.m.", need = 1, have = s => s.lifetime.earlyBird ? 1 : 0 },
            new Badge { id = "hours10", name = "KINGDOM CITIZEN", how = "Play for 10 hours", need = 10, have = s => (int)(PlayTime.Total(s) / 3600f) },
        };

        /// <summary>The unearned badge you're closest to (by share of the way there), or null when all are earned.</summary>
        public static Badge? Closest(SaveData s)
        {
            if (s == null) return null;
            Badge? best = null; double bestRatio = -1;
            foreach (var b in All)
            {
                if (Has(s, b.id) || b.Earned(s)) continue;
                double r = b.need <= 0 ? 0 : (double)b.Have(s) / b.need;
                if (r > bestRatio) { bestRatio = r; best = b; }
            }
            return best;
        }

        /// <summary>The menu's NEXT GOAL line.</summary>
        public static string NextGoal(SaveData s)
        {
            var b = Closest(s);
            if (b == null) return "Every badge earned. You're a legend of the kingdom!";
            var x = b.Value;
            string pay = BadgeRewards.ToNext(s) == 1 ? $"  ·  +{BadgeRewards.Shards} shards!" : "";
            return (x.need > 1 ? $"NEXT BADGE: {x.name}  ·  {x.how}  ({x.Have(s):N0}/{x.need:N0})" : $"NEXT BADGE: {x.name}  ·  {x.how}") + pay;
        }

        /// <summary>Badges earned now that weren't recorded before. Records them.</summary>
        public static List<Badge> Update(SaveData s)
        {
            var fresh = new List<Badge>();
            if (s == null) return fresh;
            foreach (var b in All)
            {
                if (s.lifetime.badges.Contains(b.id)) continue;
                if (!b.Earned(s)) continue;
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
            int vs = 0; foreach (var h in s.lifetime.heroRecords) if (h != null) vs += h.wins;
            return new[] { cleared, s.zombieCareer != null ? s.zombieCareer.totalKills : 0, s.rumble != null ? s.rumble.wins : 0, party, (int)PlayTime.Total(s),
                           s.lifetime.badges.Count, vs };
        }

        public static readonly string[] Labels = { "Story levels cleared", "Zombies put down", "Rated wins", "Party wins", "Time played", "Badges earned", "Versus wins" };

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
