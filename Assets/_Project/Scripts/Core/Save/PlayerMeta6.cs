using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>Arena Clash career (not Training or party quests): matches, wins, KOs and your bests.</summary>
    public static class ArenaCareer
    {
        public struct Result { public bool newBestKOs, newBestCombo; public int koMilestone; }

        public static Result Record(SaveData s, bool won, int kos, int combo)
        {
            var r = new Result();
            if (s == null) return r;
            var l = s.lifetime;
            int before = l.arenaKOs;
            l.arenaMatches++;
            if (won) l.arenaWins++;
            l.arenaKOs += Math.Max(0, kos);
            if (kos > l.arenaBestKOs) { r.newBestKOs = l.arenaBestKOs > 0; l.arenaBestKOs = kos; }
            if (combo > l.arenaBestCombo) { r.newBestCombo = l.arenaBestCombo > 0 && combo >= 2; l.arenaBestCombo = combo; }
            r.koMilestone = l.arenaKOs / 100 > before / 100 ? l.arenaKOs / 100 * 100 : 0;
            return r;
        }

        public static string Line(SaveData s)
        {
            if (s == null || s.lifetime.arenaMatches == 0) return "";
            var l = s.lifetime;
            return $"Arena Clash: {l.arenaWins} wins in {Plural.Of(l.arenaMatches, "match", "matches")}  ·  {l.arenaKOs:N0} KOs  ·  best {l.arenaBestKOs} KOs, combo {l.arenaBestCombo}";
        }

        /// <summary>The slot with the most KOs (MVP), or -1 when nobody scored one or it's a tie at the top.</summary>
        public static int Mvp(int[] knockouts)
        {
            if (knockouts == null) return -1;
            int best = -1, top = 0; bool tie = false;
            for (int i = 0; i < knockouts.Length; i++)
            {
                if (knockouts[i] > top) { top = knockouts[i]; best = i; tie = false; }
                else if (knockouts[i] == top && top > 0) tie = true;
            }
            return tie ? -1 : best;
        }
    }

    /// <summary>Party quests: how many modes you've tried and your favorite.</summary>
    public static class PartyMeta
    {
        public static int ModesPlayed(SaveData s)
        {
            int n = 0;
            if (s != null && s.party != null) foreach (var r in s.party.records) if (r != null && r.played > 0) n++;
            return n;
        }

        public static PartyRecord Favorite(SaveData s)
        {
            PartyRecord best = null;
            if (s != null && s.party != null) foreach (var r in s.party.records) if (r != null && r.played > 0 && (best == null || r.played > best.played)) best = r;
            return best;
        }

        /// <summary>Wins worth a toast: the first, then 5, 10, 25, 50, every 50.</summary>
        public static bool WinMilestone(int wins) => wins == 1 || wins == 5 || wins == 10 || wins == 25 || (wins >= 50 && wins % 50 == 0);
    }

    /// <summary>Badges earned most recently (the save keeps them in the order earned).</summary>
    public static class RecentBadges
    {
        public static List<string> Names(SaveData s, int count)
        {
            var o = new List<string>();
            if (s == null) return o;
            var ids = s.lifetime.badges;
            for (int i = ids.Count - 1; i >= 0 && o.Count < count; i--)
            {
                var b = Array.Find(Badges.All, x => x.id == ids[i]);
                if (b.id != null) o.Add(b.name);
            }
            return o;
        }
    }

    /// <summary>Lifetime daily challenges completed.</summary>
    public static class DailyCareer
    {
        public static void Add(SaveData s, int n) { if (s != null && n > 0) s.lifetime.dailyDone += n; }
    }
}
