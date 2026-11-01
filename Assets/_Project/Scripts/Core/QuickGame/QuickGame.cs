using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Fewest taps to clear one Quick Game stage (as the widget saves it).</summary>
    [Serializable]
    public class QGBestRecord
    {
        public string id = "";
        public int steps;
    }

    /// <summary>
    /// The Quick Game widget's lifetime stats, exactly as the widget writes them to the shared App Group
    /// (QGStats in Native/QuickGameWidget/QuickGameEngine.swift). Read with JsonUtility.
    /// </summary>
    [Serializable]
    public class QuickGameStats
    {
        public int runs, clears, losses, shards, stomps, taps, streak, bestStreak;
        public string lastDay = "", lastStage = "";
        public List<QGBestRecord> best = new List<QGBestRecord>();

        public int BestSteps(string id)
        {
            if (best == null) return 0;
            foreach (var b in best) if (b != null && b.id == id) return b.steps;
            return 0;
        }

        public QuickGameStats Copy()
        {
            var c = (QuickGameStats)MemberwiseClone();
            c.best = new List<QGBestRecord>();
            if (best != null) foreach (var b in best) if (b != null) c.best.Add(new QGBestRecord { id = b.id, steps = b.steps });
            return c;
        }

        public void Sanitize()
        {
            if (best == null) best = new List<QGBestRecord>();
            best.RemoveAll(b => b == null || string.IsNullOrEmpty(b.id));
            if (lastDay == null) lastDay = "";
            if (lastStage == null) lastStage = "";
            runs = Math.Max(0, runs); clears = Math.Max(0, clears); losses = Math.Max(0, losses);
            shards = Math.Max(0, shards); stomps = Math.Max(0, stomps); taps = Math.Max(0, taps);
            streak = Math.Max(0, streak); bestStreak = Math.Max(0, bestStreak);
        }
    }

    /// <summary>What the game remembers about the widget: clears already paid for, and the last stats it read.</summary>
    [Serializable]
    public class QuickGameSave
    {
        public int paidClears;
        public QuickGameStats last = new QuickGameStats();

        public void Sanitize()
        {
            if (paidClears < 0) paidClears = 0;
            if (last == null) last = new QuickGameStats();
            last.Sanitize();
        }
    }

    /// <summary>
    /// The Quick Game (Story Quest's widget game) as the game sees it: stage names for the stats page and the Star
    /// Shards paid for clears made in the widget, collected the next time the game opens.
    /// </summary>
    public static class QuickGame
    {
        public const int ShardsPerClear = 20;
        /// <summary>At most this many shards per visit, however long the widget was played.</summary>
        public const int MaxPerSync = 200;

        /// <summary>Same order and ids as QGStages.all in QuickGameEngine.swift.</summary>
        public static readonly string[] StageIds =
        {
            "meadow_hop", "acorn_steps", "beetle_bridge", "spike_meadow", "high_road", "double_gap", "tower_climb", "last_light",
        };

        public static readonly string[] StageNames =
        {
            "Meadow Hop", "Acorn Steps", "Beetle Bridge", "Spike Meadow", "High Road", "Double Gap", "Tower Climb", "Last Light",
        };

        public static string StageName(string id)
        {
            int i = Array.IndexOf(StageIds, id);
            return i >= 0 ? StageNames[i] : "";
        }

        /// <summary>
        /// New stats read from the widget: pays for clears not paid yet (returns the shards) and keeps a copy for the
        /// stats page. If the widget's numbers went down (it was removed or the phone restored), they become the new
        /// starting point and nothing is paid.
        /// </summary>
        public static int Sync(SaveData save, QuickGameStats stats)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (stats == null) return 0;
            stats.Sanitize();
            if (save.quickGame == null) save.quickGame = new QuickGameSave();
            var q = save.quickGame;
            if (stats.clears < q.paidClears) q.paidClears = stats.clears;
            int fresh = stats.clears - q.paidClears;
            int pay = (int)Math.Min(MaxPerSync, (long)fresh * ShardsPerClear);
            q.paidClears = stats.clears;
            q.last = stats.Copy();
            if (pay > 0) Economy.Grant(save, pay);
            return pay;
        }
    }

    public enum TimeMedal { None = 0, Bronze = 1, Silver = 2, Gold = 3 }

    /// <summary>
    /// Story Quest time medals, from the level's par time: GOLD at par or faster, SILVER within 25% over par, BRONZE for
    /// any clear. Shown on the results screen, the level card and the stats page.
    /// </summary>
    public static class StoryMedals
    {
        public const float SilverSlack = 1.25f;

        public static TimeMedal For(float seconds, float par)
        {
            if (seconds <= 0f || float.IsNaN(seconds)) return TimeMedal.None;
            if (par <= 0f) return TimeMedal.Bronze;
            if (seconds <= par) return TimeMedal.Gold;
            if (seconds <= par * SilverSlack) return TimeMedal.Silver;
            return TimeMedal.Bronze;
        }

        /// <summary>The time to beat for a medal (0 for none/bronze, which only needs a clear).</summary>
        public static float Target(TimeMedal medal, float par) =>
            medal == TimeMedal.Gold ? par : medal == TimeMedal.Silver ? par * SilverSlack : 0f;

        public static TimeMedal Next(TimeMedal have) => have >= TimeMedal.Gold ? TimeMedal.Gold : (TimeMedal)((int)have + 1);

        public static string Name(TimeMedal m) => m switch
        {
            TimeMedal.Gold => "GOLD",
            TimeMedal.Silver => "SILVER",
            TimeMedal.Bronze => "BRONZE",
            _ => "",
        };
    }

    /// <summary>What's still left to get in a cleared Story level ("2 Star Shards", "1 secret", "GOLD: under 1:30").</summary>
    public static class StoryChecklist
    {
        public static List<string> Missing(int shards, int totalShards, int secrets, int totalSecrets, float bestSeconds, float par,
                                           string tokenName = null, bool tokenFound = true)
        {
            var list = new List<string>();
            if (totalShards > 0 && shards < totalShards) list.Add(Plural.Of(totalShards - shards, "Star Shard"));
            if (totalSecrets > 0 && secrets < totalSecrets) list.Add(Plural.Of(totalSecrets - secrets, "secret"));
            if (!string.IsNullOrEmpty(tokenName) && !tokenFound) list.Add(tokenName);
            var medal = StoryMedals.For(bestSeconds, par);
            if (par > 0f && medal != TimeMedal.Gold && medal != TimeMedal.None)
            {
                var next = StoryMedals.Next(medal);
                list.Add($"{StoryMedals.Name(next)}: under {ResultsMath.FormatTime(StoryMedals.Target(next, par))}");
            }
            return list;
        }
    }
}
