using System;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>A pop-up line for the progress toast (level up, challenge done).</summary>
    public struct ProgressToast
    {
        public string title, detail;
        public Color color;
    }

    /// <summary>
    /// Gameplay reports progress here: XP for the player level and counts for today's daily challenges. Rewards are paid
    /// into the save at once and announced with a toast (ProgressToastUI). Safe to call when there is no save service.
    /// </summary>
    public static class ProgressTracker
    {
        static readonly Color LevelColor = new Color32(255, 214, 90, 255);
        static readonly Color ChallengeColor = new Color32(120, 230, 160, 255);

        public static int TodayKey => DailyChallenges.DayKey(DateTime.Now);

        static SaveData Save => Services.Save != null ? Services.Save.Data : null;

        /// <summary>Today's challenges in the save (rolled over to today first).</summary>
        public static List<ChallengeRecord> Today()
        {
            var save = Save;
            if (save == null) return new List<ChallengeRecord>();
            if (DailyChallenges.EnsureToday(save, TodayKey)) Services.Save.MarkDirty();
            return save.dailyChallenges;
        }

        public static void AddXp(int xp)
        {
            var save = Save;
            if (save == null || xp <= 0) return;
            foreach (int level in PlayerLevel.AddXp(save, xp))
                Toast("LEVEL " + level + "!", $"+{PlayerLevel.ShardReward(level)} Star Shards" +
                      (PlayerLevel.IsMilestone(level) ? "  (milestone bonus)" : ""), LevelColor);
            Services.Save.MarkDirty();
        }

        public static void Report(ChallengeKind kind, int amount)
        {
            var save = Save;
            if (save == null || amount <= 0) return;
            if (DailyChallenges.EnsureToday(save, TodayKey)) Services.Save.MarkDirty();
            var done = DailyChallenges.Report(save, kind, amount, out int xp);
            foreach (var c in done) Toast("CHALLENGE DONE!", $"{c.text}  +{c.shards} shards", ChallengeColor);
            if (done.Count > 0) Services.Save.MarkDirty();
            AddXp(xp);
        }

        /// <summary>Versus / Arena: a match finished.</summary>
        public static void MatchPlayed(bool won, bool versus)
        {
            Report(ChallengeKind.PlayMatches, 1);
            if (versus && won) Report(ChallengeKind.WinVersus, 1);
            AddXp(versus ? (won ? PlayerLevel.VersusWinXp : PlayerLevel.VersusLossXp) : PlayerLevel.ArenaXp);
            if (Services.Save != null) Services.Save.SaveNow();
        }

        static readonly Color RankColor = new Color32(200, 60, 60, 255);

        /// <summary>
        /// Survivor Rank XP (every zombie mode). Each new rank pays Star Shards once and pops a toast. Returns the
        /// ranks reached.
        /// </summary>
        public static List<int> AddZombieXp(int xp)
        {
            var save = Save;
            if (save == null || xp <= 0) return new List<int>();
            if (save.zombieCareer == null) save.zombieCareer = new ZombieCareer();
            var reached = SurvivorRank.Grant(save.zombieCareer, xp);
            foreach (int r in reached)
            {
                int shards = SurvivorRank.ShardsFor(r);
                Economy.Grant(save, shards);
                string unlock = "";
                foreach (var u in SurvivorRank.Unlocks) if (u.rank == r) unlock = "  ·  " + u.name;
                Toast($"SURVIVOR RANK {r}: {SurvivorRank.Title(r)}", $"+{shards} Star Shards{unlock}", RankColor);
            }
            Services.Save.MarkDirty();
            return reached;
        }

        static void Toast(string title, string detail, Color color)
        {
            EventBus<ProgressToast>.Raise(new ProgressToast { title = title, detail = detail, color = color });
            Sfx.Play(Sfx.PowerUp, 0.6f, 0f);
        }
    }
}
