using PKR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PKR
{
    /// <summary>What CONTINUE on the main menu goes back to.</summary>
    public enum ContinueKind { None = 0, StoryLevel = 1, ArenaClash = 2, Versus = 3, Zombies = 4, Plaza = 5 }

    /// <summary>
    /// Player-wide records written from gameplay: hero mastery XP (with a toast on level-up and the level 5 / 10
    /// shards), CONTINUE's "last played", Versus head-to-head and Story fall markers.
    /// </summary>
    public static class MetaTracker
    {
        static readonly Color MasteryColor = new Color32(255, 196, 80, 255);

        /// <summary>Mastery XP for a hero (any mode). Grants and announces the level 5 / 10 Star Shards.</summary>
        public static HeroMastery.Award Mastery(string heroId, int xp, CharacterRoster roster = null)
        {
            var save = Services.Save;
            if (save == null || string.IsNullOrEmpty(heroId) || xp <= 0) return default;
            var a = HeroMastery.Add(save.Data, heroId, xp);
            if (a.shards > 0) Economy.Grant(save.Data, a.shards);
            if (a.LevelledUp)
            {
                var def = roster != null ? roster.Find(heroId) : null;
                string name = def != null ? def.displayName.ToUpperInvariant() : heroId.ToUpperInvariant();
                EventBus<ProgressToast>.Raise(new ProgressToast
                {
                    title = $"{name} MASTERY {a.levelAfter}",
                    detail = HeroMastery.Title(a.levelAfter) + (a.shards > 0 ? $"  ·  +{a.shards} STAR SHARDS" : ""),
                    color = MasteryColor
                });
            }
            return a;
        }

        /// <summary>Remembers what was played last, for CONTINUE.</summary>
        public static void Played(ContinueKind kind, string levelId = "")
        {
            var save = Services.Save;
            if (save == null) return;
            var l = save.Data.lifetime;
            l.lastKind = (int)kind;
            l.lastLevelId = levelId ?? "";
            l.lastScene = SceneManager.GetActiveScene().name;
        }

        public static void Versus(string rivalId, bool won, string heroId = null, string rivalName = null)
        {
            if (Services.Save == null) return;
            var d = Services.Save.Data;
            var before = HeadToHead.Get(d, rivalId);
            bool firstWin = won && !string.IsNullOrEmpty(rivalId) && (before == null || before.wins == 0);
            HeadToHead.Record(d, rivalId, won);
            HeroRecords.Record(d, heroId, won);
            int streak = VersusStreak.Record(d, won);
            if (firstWin && !string.IsNullOrEmpty(rivalName))
                EventBus<ProgressToast>.Raise(new ProgressToast { title = $"FIRST WIN VS {rivalName.ToUpperInvariant()}!", detail = "A new rival beaten.", color = new Color32(255, 150, 90, 255) });
            if (won && VersusStreak.Milestone(streak))
                EventBus<ProgressToast>.Raise(new ProgressToast { title = $"{streak} WINS IN A ROW!", detail = streak >= d.lifetime.vsBestStreak ? "Your best Versus streak!" : $"Best: {d.lifetime.vsBestStreak}", color = new Color32(255, 110, 60, 255) });
            CheckBadges();
        }

        /// <summary>Records any newly earned PROFILE badges, with a toast and a tap for each. Call after something is won or cleared.</summary>
        public static int CheckBadges()
        {
            var save = Services.Save;
            if (save == null) return 0;
            var fresh = Badges.Update(save.Data);
            foreach (var b in fresh)
                EventBus<ProgressToast>.Raise(new ProgressToast { title = "BADGE: " + b.name, detail = b.how, color = new Color32(120, 220, 255, 255) });
            if (fresh.Count > 0) { if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium); save.MarkDirty(); }
            return fresh.Count;
        }
    }
}
