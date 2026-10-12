using System;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>What a rated match did to the player's Rumble Rating (null fields when the match wasn't rated).</summary>
    public class RumbleOutcome
    {
        public RumbleBreakdown points;
        public RumbleChange change;
        public bool daily;
        public int dailyStreak;
    }

    /// <summary>Raised after a rated match is recorded (the HUD, results screens and Game Center listen).</summary>
    public struct RumbleRated { public RumbleOutcome outcome; }

    /// <summary>
    /// Records rated Arena Clash / Versus / Daily Rumble matches into SaveData.rumble: scores the match,
    /// updates rating, tier and auto-difficulty skill, the Daily Rumble streak, saves, and posts to Game Center.
    /// </summary>
    public static class RumbleRecorder
    {
        public static RumbleProfile Profile => Services.Save != null ? Services.Save.Data.rumble : null;

        /// <summary>Today's key in the player's local calendar.</summary>
        public static int Today => DailyRumble.DayKey(DateTime.Now);

        /// <summary>CPU difficulty (0..1) for a match: the auto skill, or the named level.</summary>
        public static float Difficulty(bool auto, BotLevel level)
        {
            var p = Profile;
            return auto ? (p != null ? p.skill : RumbleProfile.StartingSkill) : RumblePoints.DifficultyFor(level);
        }

        public static BotDifficulty BotTuning(bool auto, BotLevel level) =>
            auto ? AdaptiveDifficulty.Blend(Difficulty(true, level)) : BotDifficulty.For(level);

        /// <summary>"AUTO (NORMAL)" or "HARD".</summary>
        public static string LevelLabel(bool auto, BotLevel level) =>
            auto ? $"AUTO ({AdaptiveDifficulty.Nearest(Difficulty(true, level)).ToString().ToUpperInvariant()})"
                 : level.ToString().ToUpperInvariant();

        /// <summary>Score and apply a match. daily = this was today's Daily Rumble (only the first attempt counts as daily).</summary>
        public static RumbleOutcome Record(RumbleMatchReport report, bool daily)
        {
            var save = Services.Save;
            if (save == null) return null;
            var p = save.Data.rumble ?? (save.Data.rumble = new RumbleProfile());
            int today = Today;
            bool countsAsDaily = daily && !DailyRumble.PlayedToday(p, today);
            if (countsAsDaily)
            {
                report.daily = true;
                report.dailyStreak = DailyRumble.StreakIfPlayed(p, today);
            }
            else report.daily = false;

            var points = RumblePoints.Score(report);
            var change = RumbleRating.Apply(p, points, report.placement, report.fighters);
            p.skill = AdaptiveDifficulty.Update(p.skill, report.placement, report.fighters);
            int streak = countsAsDaily ? DailyRumble.RecordPlayed(p, today) : p.dailyStreak;
            save.SaveNow();

            GameCenterService.SubmitRumble(p.rating);
            var outcome = new RumbleOutcome { points = points, change = change, daily = countsAsDaily, dailyStreak = streak };
            EventBus<RumbleRated>.Raise(new RumbleRated { outcome = outcome });
            return outcome;
        }
    }

    /// <summary>Shared UI bits for Rumble Rating: tier colors, the rank line and the itemised results block.</summary>
    public static class RumbleUI
    {
        public static Color TierColor(RumbleTier t)
        {
            switch (t)
            {
                case RumbleTier.Silver: return new Color32(200, 214, 230, 255);
                case RumbleTier.Gold: return new Color32(255, 204, 64, 255);
                case RumbleTier.Platinum: return new Color32(110, 236, 220, 255);
                case RumbleTier.Diamond: return new Color32(130, 170, 255, 255);
                case RumbleTier.Legend: return new Color32(255, 110, 200, 255);
                default: return new Color32(214, 140, 90, 255);
            }
        }

        /// <summary>"GOLD 720 · 380 to PLATINUM" (or "LEGEND 2600").</summary>
        public static string RankLine(RumbleProfile p)
        {
            if (p == null) return "RUMBLE RATING: BRONZE 0";
            var t = p.Tier;
            int next = RumbleTiers.NextThreshold(t);
            string tail = next < 0 ? $"best {p.bestRating}" : $"{next - p.rating} to {RumbleTiers.Name(t + 1)}";
            return $"{RumbleTiers.Name(t)} {p.rating}  ·  {tail}";
        }

        /// <summary>Adds the "RUMBLE POINTS" block to a results panel.</summary>
        public static void AddResults(RectTransform panel, RumbleOutcome o, int fontSize = 30)
        {
            var theme = UITheme.Current;
            if (o == null || o.points == null)
            {
                UIFactory.Label(panel, "Unrated match (couch play and Training don't change your Rumble Rating)", fontSize,
                                TextAnchor.MiddleCenter, 50f).color = theme.subtle;
                return;
            }

            var parts = new System.Text.StringBuilder();
            foreach (var l in o.points.lines)
            {
                if (parts.Length > 0) parts.Append("   ");
                parts.Append(l.label).Append(' ').Append(l.points >= 0 ? "+" : "").Append(l.points);
            }
            UIFactory.Label(panel, $"RUMBLE POINTS +{o.points.total}", fontSize + 10, TextAnchor.MiddleCenter, 54f, title: true);
            UIFactory.Label(panel, parts.ToString(), fontSize, TextAnchor.MiddleCenter, 84f).color = theme.subtle;

            var c = o.change;
            string sign = c.delta >= 0 ? "+" : "";
            string line = $"{RumbleTiers.Name(c.newTier)} {c.newRating}  ({sign}{c.delta}, entry {c.par})";
            if (!string.IsNullOrEmpty(c.note)) line += "  ·  " + c.note;
            var rank = UIFactory.Label(panel, line, fontSize + 4, TextAnchor.MiddleCenter, 52f);
            rank.color = TierColor(c.newTier);
            AddBar(panel, RumbleTiers.Progress(c.newRating), TierColor(c.newTier));
        }

        /// <summary>A thin progress bar through the current tier.</summary>
        public static void AddBar(RectTransform parent, float progress, Color color)
        {
            var bar = UIFactory.Rect("TierBar", parent);
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
            var bg = bar.gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.15f);
            var fill = UIFactory.Rect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.gameObject.AddComponent<Image>().color = color;
        }
    }
}
