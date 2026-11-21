using System;
using System.Collections.Generic;

namespace PKR.Core
{
    [Serializable]
    public class HeroXp
    {
        public string id = "";
        public int xp;
        /// <summary>Highest mastery level already paid out.</summary>
        public int paidLevel;
    }

    [Serializable]
    public class HeadToHeadRecord
    {
        public string id = "";
        public int wins, losses;
    }

    [Serializable]
    public class FallMarks
    {
        public string levelId = "";
        public List<float> xs = new List<float>(), ys = new List<float>();
    }

    /// <summary>Player-wide records for the PROFILE page, CONTINUE, the daily login gift and WHAT'S NEW.</summary>
    [Serializable]
    public class LifetimeSave
    {
        /// <summary>Seconds played per mode (index = PlayTime mode index).</summary>
        public List<float> playSeconds = new List<float>();
        /// <summary>CONTINUE: the last thing played (a ContinueKind) and the Story level, if any.</summary>
        public int lastKind;
        public string lastLevelId = "";
        public string lastScene = "";
        /// <summary>Daily login gift: last day claimed (yyyymmdd), the streak, the best streak, gifts claimed.</summary>
        public int loginDay, loginStreak, bestLoginStreak, loginGifts;
        /// <summary>WHAT'S NEW: the version whose card was last shown.</summary>
        public string whatsNewSeen = "";
        /// <summary>Settings > RESET HINTS & GUIDES: show the welcome guide again next time.</summary>
        public bool replayWelcome;
        public List<HeroXp> heroXp = new List<HeroXp>();
        public List<HeadToHeadRecord> headToHead = new List<HeadToHeadRecord>();
        /// <summary>Versus: your record with each hero you've played as.</summary>
        public List<HeadToHeadRecord> heroRecords = new List<HeadToHeadRecord>();
        public List<FallMarks> falls = new List<FallMarks>();
        public List<StandRunRecord> standRuns = new List<StandRunRecord>();
        /// <summary>PROFILE badges earned (Badges ids).</summary>
        public List<string> badges = new List<string>();
        /// <summary>Weekly recap: the week (Monday yyyymmdd) and the snapshot taken when it began.</summary>
        public int weekKey;
        public List<int> weekSnap = new List<int>();
        /// <summary>PROFILE's NEW dot: mastery levels + badges the last time PROFILE was opened.</summary>
        public int profileSeen;
        /// <summary>Versus: wins in a row now, and the best run.</summary>
        public int vsStreak, vsBestStreak;

        public void Sanitize()
        {
            if (playSeconds == null) playSeconds = new List<float>();
            for (int i = 0; i < playSeconds.Count; i++) if (float.IsNaN(playSeconds[i]) || playSeconds[i] < 0f) playSeconds[i] = 0f;
            if (lastLevelId == null) lastLevelId = "";
            if (lastScene == null) lastScene = "";
            if (whatsNewSeen == null) whatsNewSeen = "";
            loginStreak = Math.Max(0, loginStreak); bestLoginStreak = Math.Max(loginStreak, bestLoginStreak); loginGifts = Math.Max(0, loginGifts);
            if (heroXp == null) heroXp = new List<HeroXp>();
            heroXp.RemoveAll(h => h == null || string.IsNullOrEmpty(h.id));
            foreach (var h in heroXp) { h.xp = Math.Max(0, h.xp); h.paidLevel = Math.Max(0, Math.Min(HeroMastery.MaxLevel, h.paidLevel)); }
            if (headToHead == null) headToHead = new List<HeadToHeadRecord>();
            headToHead.RemoveAll(h => h == null || string.IsNullOrEmpty(h.id));
            foreach (var h in headToHead) { h.wins = Math.Max(0, h.wins); h.losses = Math.Max(0, h.losses); }
            if (heroRecords == null) heroRecords = new List<HeadToHeadRecord>();
            heroRecords.RemoveAll(h => h == null || string.IsNullOrEmpty(h.id));
            foreach (var h in heroRecords) { h.wins = Math.Max(0, h.wins); h.losses = Math.Max(0, h.losses); }
            if (falls == null) falls = new List<FallMarks>();
            falls.RemoveAll(f => f == null || string.IsNullOrEmpty(f.levelId) || f.xs == null || f.ys == null || f.xs.Count != f.ys.Count);
            while (falls.Count > FallMemory.MaxLevels) falls.RemoveAt(0);
            if (standRuns == null) standRuns = new List<StandRunRecord>();
            standRuns.RemoveAll(r => r == null || string.IsNullOrEmpty(r.map));
            if (badges == null) badges = new List<string>();
            if (weekSnap == null) weekSnap = new List<int>();
            vsStreak = Math.Max(0, vsStreak); vsBestStreak = Math.Max(vsStreak, vsBestStreak);
        }
    }

    /// <summary>Time played per mode (for PROFILE).</summary>
    public static class PlayTime
    {
        public static readonly string[] Modes = { "STORY QUEST", "ARENA CLASH", "VERSUS", "SHADOW CONTRACTS", "EVENTS", "ZOMBIES", "MENUS" };
        public const int Menus = 6;

        public static void Add(SaveData s, int mode, float seconds)
        {
            if (s == null || mode < 0 || mode >= Modes.Length || !(seconds > 0f) || seconds > 3600f) return;
            var l = s.lifetime.playSeconds;
            while (l.Count < Modes.Length) l.Add(0f);
            l[mode] += seconds;
        }

        public static float Seconds(SaveData s, int mode) =>
            s != null && s.lifetime != null && mode >= 0 && mode < s.lifetime.playSeconds.Count ? s.lifetime.playSeconds[mode] : 0f;

        public static float Total(SaveData s)
        {
            float t = 0f;
            if (s != null && s.lifetime != null) foreach (var x in s.lifetime.playSeconds) t += x;
            return t;
        }

        /// <summary>"3h 12m", "45m", "under a minute".</summary>
        public static string Format(float seconds)
        {
            int m = (int)(Math.Max(0f, seconds) / 60f);
            if (m < 1) return "under a minute";
            int h = m / 60;
            return h > 0 ? $"{h}h {m % 60}m" : $"{m}m";
        }
    }

    /// <summary>The daily login gift: Star Shards the first time you open the game each day, more for a streak.</summary>
    public static class LoginStreak
    {
        public static readonly int[] Gifts = { 10, 15, 20, 25, 30, 40, 60 };

        /// <summary>Gift for a streak day (1-based; the 7-day cycle repeats).</summary>
        public static int GiftFor(int streakDay) => Gifts[(Math.Max(1, streakDay) - 1) % Gifts.Length];

        /// <summary>Claims today's gift. Returns the shards (0 when already claimed today). The caller grants them.</summary>
        public static int Claim(SaveData s, int today, int yesterday)
        {
            if (s == null || today <= 0) return 0;
            var l = s.lifetime;
            if (l.loginDay == today) return 0;
            l.loginStreak = l.loginDay == yesterday ? l.loginStreak + 1 : 1;
            l.loginDay = today;
            l.bestLoginStreak = Math.Max(l.bestLoginStreak, l.loginStreak);
            l.loginGifts++;
            return GiftFor(l.loginStreak);
        }
    }

    /// <summary>
    /// HERO MASTERY: every hero earns mastery XP from all modes (Story clears, Arena and Versus wins, party quests,
    /// Last Stand rounds). Levels 1-10 have titles; reaching 5 and 10 pays Star Shards once.
    /// </summary>
    public static class HeroMastery
    {
        public const int MaxLevel = 10;
        /// <summary>Total XP needed for each level (level 1 = 0).</summary>
        static readonly int[] Need = { 0, 0, 60, 150, 280, 450, 670, 950, 1300, 1750, 2300 };
        static readonly string[] Titles = { "", "ROOKIE", "TRAINEE", "SCRAPPER", "CONTENDER", "VETERAN", "EXPERT", "ACE", "HERO", "CHAMPION", "LEGEND" };
        public const int Level5Shards = 100, Level10Shards = 300;

        // XP per event
        public const int StoryClearXp = 30, StoryFirstClearXp = 20, ArenaWinXp = 40, ArenaMatchXp = 10, VersusWinXp = 45, VersusMatchXp = 10,
                         PartyWinXp = 35, PartyMatchXp = 10, LastStandRoundXp = 4, LadderClearXp = 120;

        public static int Level(int xp)
        {
            int l = 1;
            for (int i = 2; i <= MaxLevel; i++) if (xp >= Need[i]) l = i;
            return l;
        }

        public static string Title(int level) => Titles[Math.Max(1, Math.Min(MaxLevel, level))];

        /// <summary>XP from this level to the next, and progress 0..1 (1 at max level).</summary>
        public static float Progress01(int xp)
        {
            int l = Level(xp);
            if (l >= MaxLevel) return 1f;
            return (float)(xp - Need[l]) / (Need[l + 1] - Need[l]);
        }

        public static int ToNext(int xp)
        {
            int l = Level(xp);
            return l >= MaxLevel ? 0 : Need[l + 1] - xp;
        }

        public static HeroXp Find(SaveData s, string id)
        {
            if (s == null || s.lifetime == null || string.IsNullOrEmpty(id)) return null;
            foreach (var h in s.lifetime.heroXp) if (h.id == id) return h;
            return null;
        }

        public static int Xp(SaveData s, string id) => Find(s, id)?.xp ?? 0;

        public struct Award
        {
            public int levelBefore, levelAfter, shards;
            public bool LevelledUp => levelAfter > levelBefore;
        }

        /// <summary>Adds XP to a hero. Pays (and returns) the level 5 / 10 shards once; the caller grants them.</summary>
        public static Award Add(SaveData s, string id, int xp)
        {
            var a = new Award();
            if (s == null || string.IsNullOrEmpty(id) || xp <= 0) return a;
            var h = Find(s, id);
            if (h == null) { h = new HeroXp { id = id }; s.lifetime.heroXp.Add(h); }
            a.levelBefore = Level(h.xp);
            h.xp = (int)Math.Min(int.MaxValue, (long)h.xp + xp);
            a.levelAfter = Level(h.xp);
            for (int l = h.paidLevel + 1; l <= a.levelAfter; l++)
            {
                if (l == 5) a.shards += Level5Shards;
                if (l == 10) a.shards += Level10Shards;
            }
            h.paidLevel = Math.Max(h.paidLevel, a.levelAfter);
            return a;
        }

        /// <summary>The hero with the most mastery XP (null when none).</summary>
        public static HeroXp Favorite(SaveData s)
        {
            HeroXp best = null;
            if (s != null && s.lifetime != null) foreach (var h in s.lifetime.heroXp) if (best == null || h.xp > best.xp) best = h;
            return best;
        }
    }

    /// <summary>Versus head-to-head: your wins and losses against each CPU fighter.</summary>
    public static class HeadToHead
    {
        public static HeadToHeadRecord Get(SaveData s, string rival)
        {
            if (s == null || s.lifetime == null || string.IsNullOrEmpty(rival)) return null;
            foreach (var r in s.lifetime.headToHead) if (r.id == rival) return r;
            return null;
        }

        public static void Record(SaveData s, string rival, bool won)
        {
            if (s == null || string.IsNullOrEmpty(rival)) return;
            var r = Get(s, rival);
            if (r == null) { r = new HeadToHeadRecord { id = rival }; s.lifetime.headToHead.Add(r); }
            if (won) r.wins++; else r.losses++;
        }

        public static string Line(HeadToHeadRecord r) => r == null || r.wins + r.losses == 0 ? "not met yet" : $"YOU {r.wins} - {r.losses}";
    }

    /// <summary>Story Quest practice aid: where you fell last time in a level (shown as faint markers on the next try).</summary>
    public static class FallMemory
    {
        public const int PerLevel = 8, MaxLevels = 40;

        public static FallMarks Get(SaveData s, string levelId)
        {
            if (s == null || s.lifetime == null || string.IsNullOrEmpty(levelId)) return null;
            foreach (var f in s.lifetime.falls) if (f.levelId == levelId) return f;
            return null;
        }

        public static void Add(SaveData s, string levelId, float x, float y)
        {
            if (s == null || string.IsNullOrEmpty(levelId) || float.IsNaN(x) || float.IsNaN(y)) return;
            var f = Get(s, levelId);
            if (f == null)
            {
                f = new FallMarks { levelId = levelId };
                s.lifetime.falls.Add(f);
                while (s.lifetime.falls.Count > MaxLevels) s.lifetime.falls.RemoveAt(0);
            }
            f.xs.Add(x); f.ys.Add(y);
            while (f.xs.Count > PerLevel) { f.xs.RemoveAt(0); f.ys.RemoveAt(0); }
        }

        /// <summary>A clean clear forgets that level's falls.</summary>
        public static void Clear(SaveData s, string levelId)
        {
            var f = Get(s, levelId);
            if (f != null) s.lifetime.falls.Remove(f);
        }
    }

    /// <summary>WHAT'S NEW: a one-time card per update with its headline features.</summary>
    public static class WhatsNew
    {
        public const string Version = "1.3";
        public static readonly string[] Items =
        {
            "22 BADGES with progress, a NEXT BADGE goal on the menu, and badges that pop the moment you earn them.",
            "STORY: tries per level, world completion %, CLOSE CALL and ON A ROLL callouts, and time vs your best.",
            "VERSUS: win streaks, FIRST WIN callouts, best of 5, RANDOM MATCHUP and your record with each hero.",
            "ZOMBIES: YOUR BEST and NEW BEST callouts, accuracy, kills per round and your last run.",
            "PROFILE: days played, fastest clear, streaks and an ABOUT page.  THIS SESSION recap on the menu.",
            "BRAVE one-heart challenge, checkpoint splits, weekly recap, tips and a welcome-back gift.",
        };

        /// <summary>Show to returning players once per version (brand-new players get the welcome guide instead).</summary>
        public static bool ShouldShow(SaveData s, bool newPlayer) =>
            s != null && !newPlayer && s.lifetime.whatsNewSeen != Version;

        public static void MarkSeen(SaveData s) { if (s != null) s.lifetime.whatsNewSeen = Version; }
    }

    /// <summary>Item Race medal times (seconds) and the party quests' personal bests.</summary>
    public static class RaceMedals
    {
        public const float Gold = 70f, Silver = 85f, Bronze = 100f;

        public static TimeMedal For(float seconds) =>
            !(seconds > 0f) ? TimeMedal.None : seconds <= Gold ? TimeMedal.Gold : seconds <= Silver ? TimeMedal.Silver : seconds <= Bronze ? TimeMedal.Bronze : TimeMedal.None;
    }
}
