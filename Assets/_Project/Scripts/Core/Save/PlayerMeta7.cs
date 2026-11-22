using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Weeks in a row with at least one session (Monday-based weeks).</summary>
    public static class WeekStreak
    {
        /// <summary>Counts this week as played. Returns the streak after it.</summary>
        public static int Update(SaveData s, DateTime now)
        {
            if (s == null) return 0;
            var l = s.lifetime;
            int week = WeeklyRecap.WeekKey(now), last = WeeklyRecap.WeekKey(now.AddDays(-7));
            if (l.weekStreakKey == week) return l.weekStreak;
            l.weekStreak = l.weekStreakKey == last ? Math.Max(0, l.weekStreak) + 1 : 1;
            l.weekStreakKey = week;
            l.bestWeekStreak = Math.Max(l.bestWeekStreak, l.weekStreak);
            return l.weekStreak;
        }
    }

    /// <summary>The Star Shard shop: what you've bought and what you can afford.</summary>
    public static class ShopMeta
    {
        public static int Purchases(SaveData s)
        {
            if (s == null) return 0;
            int n = s.ownedCosmetics != null ? s.ownedCosmetics.Count : 0;
            foreach (var u in Upgrades.All) n += Upgrades.Level(s, u.id);
            return n;
        }

        public static int AffordableUpgrades(SaveData s)
        {
            if (s == null) return 0;
            int n = 0;
            foreach (var u in Upgrades.All) { int c = Upgrades.NextCost(s, u.id); if (c >= 0 && c <= s.starShards) n++; }
            return n;
        }

        /// <summary>"Next: MAGNET LV 2 in 40 shards" for the cheapest upgrade you can't afford yet ("" when none).</summary>
        public static string NextUpgradeGoal(SaveData s)
        {
            if (s == null) return "";
            string best = null; int bestCost = int.MaxValue;
            foreach (var u in Upgrades.All)
            {
                int c = Upgrades.NextCost(s, u.id);
                if (c < 0 || c <= s.starShards || c >= bestCost) continue;
                bestCost = c; best = $"{u.name.ToUpperInvariant()} LV {Upgrades.Level(s, u.id) + 1}";
            }
            return best == null ? "" : $"Saving for: {best}  ·  {bestCost - s.starShards} shards to go";
        }
    }

    /// <summary>Hero select: mastery, Versus record and the favorite mark.</summary>
    public static class HeroMeta
    {
        public static string Status(SaveData s, string heroId)
        {
            int xp = HeroMastery.Xp(s, heroId);
            int lv = HeroMastery.Level(xp);
            string line = $"MASTERY {lv} · {HeroMastery.Title(lv).ToUpperInvariant()}";
            var r = HeroRecords.Get(s, heroId);
            if (r != null && r.wins + r.losses > 0) line += $"  ·  VERSUS {r.wins}W-{r.losses}L";
            var fav = HeroMastery.Favorite(s);
            if (fav != null && fav.id == heroId && fav.xp > 0) line = "★ FAVORITE  ·  " + line;
            return line;
        }

        /// <summary>A random index among the selectable ones (not the current one when there's a choice), or -1.</summary>
        public static int RandomPick(IList<bool> selectable, int current, Random rng)
        {
            var pool = new List<int>();
            for (int i = 0; i < selectable.Count; i++) if (selectable[i] && i != current) pool.Add(i);
            if (pool.Count == 0) return current >= 0 && current < selectable.Count && selectable[current] ? current : -1;
            return pool[rng.Next(pool.Count)];
        }
    }

    /// <summary>Versus head-to-head extras.</summary>
    public static class Rivalry
    {
        /// <summary>A grudge match: this rival leads you by 3 or more.</summary>
        public static bool IsGrudge(HeadToHeadRecord r) => r != null && r.losses - r.wins >= 3;

        public static HeadToHeadRecord MostBeaten(SaveData s)
        {
            HeadToHeadRecord best = null;
            if (s != null) foreach (var h in s.lifetime.headToHead) if (h != null && h.wins > 0 && (best == null || h.wins > best.wins)) best = h;
            return best;
        }

        public static string Overall(SaveData s)
        {
            int w = 0, l = 0;
            if (s != null) foreach (var h in s.lifetime.heroRecords) if (h != null) { w += h.wins; l += h.losses; }
            if (w + l == 0) return "";
            return $"YOUR VERSUS RECORD {w}W-{l}L" + (s.lifetime.vsStreak >= 2 ? $"  ·  STREAK {s.lifetime.vsStreak}" : "");
        }
    }

    /// <summary>Story Quest: after a few falls in one run, a one-time pointer to the assists.</summary>
    public static class StruggleHint
    {
        public const int Falls = 3;
        public static bool Due(int deaths, bool assisted, bool shown) => !shown && !assisted && deaths >= Falls;
        public const string Text = "Tough spot? Settings > ASSISTS has extra hearts and a slower game speed.";
    }

    /// <summary>"Tap again to confirm" for buttons that lose progress.</summary>
    public static class TwoTap
    {
        public const float Window = 2.5f;
        public static bool Confirmed(float armedAt, float now) => armedAt > 0f && now - armedAt <= Window;
    }

    /// <summary>STORY STATS share line.</summary>
    public static class StoryShare
    {
        public static string Text(int cleared, int levels, int gold, int perfect, float bestTotal) =>
            $"Pixel Kingdom Rumble Story Quest: {cleared}/{levels} levels cleared, {gold} GOLD medals, {perfect} PERFECT" +
            (bestTotal > 0f ? $", best times total {ResultsMath.FormatTime(bestTotal)}" : "") + "! " + ShareCard.Link;
    }
}
