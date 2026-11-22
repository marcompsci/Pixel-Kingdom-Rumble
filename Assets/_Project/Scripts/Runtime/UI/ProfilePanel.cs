using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Main menu > PROFILE: your whole game in one page. Time played, hero mastery (every hero's level and title),
    /// Story Quest (levels, FLAWLESS clears, ghosts), Arena and Versus (rating record, ladder, head-to-head),
    /// party quests, zombies (kills, best round, Field Guide) and the daily login streak, with a SHARE button.
    /// Everything is read from the save on this device.
    /// </summary>
    public class ProfilePanel : MonoBehaviour
    {
        RectTransform _root;
        CharacterRoster _roster;
        System.Action _onBack;

        public void Open(RectTransform root, CharacterRoster roster, System.Action onBack)
        {
            _root = root; _roster = roster; _onBack = onBack;
            var save = Services.Save;
            if (save != null) { Badges.Update(save.Data); save.Data.lifetime.profileSeen = Badges.ProfileScore(save.Data); }
            Build();
        }

        public static List<string> Lines(SaveData s, CharacterRoster roster)
        {
            var lines = new List<string>();
            if (s == null) return lines;
            lines.Add($"Time played: {PlayTime.Format(PlayTime.Total(s))}" + TopMode(s));
            int cleared = 0;
            foreach (var l in s.levels) if (l != null && l.completed) cleared++;
            lines.Add($"Story Quest: {Plural.Of(cleared, "level")} cleared  ·  {StoryFlawless.Count(s)} FLAWLESS  ·  {Plural.Of(s.storyGhosts.Count, "ghost")}");
            var r = s.rumble;
            if (r != null) lines.Add($"Arena & Versus: {r.wins} wins in {Plural.Of(r.matches, "rated match", "rated matches")}  ·  best rating {r.bestRating}");
            string arena = ArenaCareer.Line(s);
            if (arena.Length > 0) lines.Add(arena);
            var lad = s.ladder;
            int vsW = 0, vsL = 0;
            foreach (var h in s.lifetime.heroRecords) if (h != null) { vsW += h.wins; vsL += h.losses; }
            if (vsW + vsL > 0) lines.Add($"Versus: {Plural.Of(vsW + vsL, "match", "matches")}  ·  {vsW * 100 / (vsW + vsL)}% won  ·  {vsW}W-{vsL}L  ·  {s.lifetime.vsPerfectRounds} perfect rounds");
            var top = HeroRecords.Best(s);
            string topHero = top != null ? $"  ·  top hero {HeroName(roster, top.id)} {top.wins}W-{top.losses}L" : "";
            if (lad != null) lines.Add($"Versus ladder: {Plural.Of(lad.clears, "clear")}  ·  best climb {lad.bestRungs} rungs{topHero}");
            HeadToHeadRecord nemesis = null;
            foreach (var h in s.lifetime.headToHead) if (h.losses > 0 && (nemesis == null || h.losses - h.wins > nemesis.losses - nemesis.wins)) nemesis = h;
            var beaten = Rivalry.MostBeaten(s);
            if (nemesis != null || beaten != null)
                lines.Add((nemesis != null ? $"Toughest rival: {HeroName(roster, nemesis.id)}  ({HeadToHead.Line(nemesis)})" : "") +
                          (nemesis != null && beaten != null ? "  ·  " : "") + (beaten != null ? $"most beaten: {HeroName(roster, beaten.id)} ({beaten.wins})" : ""));
            int pWins = 0, pPlayed = 0;
            if (s.party != null) foreach (var p in s.party.records) if (p != null) { pWins += p.wins; pPlayed += p.played; }
            var favParty = PartyMeta.Favorite(s);
            lines.Add($"Party quests: {pWins} wins in {Plural.Of(pPlayed, "match", "matches")}  ·  {s.party?.cupsPlayed ?? 0} cups" +
                      (favParty != null ? $"  ·  favorite {PartyModes.Info((PartyMode)favParty.mode).name}" : ""));
            var c = s.zombieCareer;
            if (c != null)
            {
                int best = 0;
                foreach (var b in c.bestRounds) best = Mathf.Max(best, b);
                var (seen, mastered) = ZombieFieldGuide.Count(c);
                lines.Add($"Zombies: {c.totalKills:N0} kills  ·  best round {best}  ·  {Plural.Of(c.lastStandRuns, "run")}" +
                          (s.lifetime.bestAccuracy > 0 ? $"  ·  best accuracy {s.lifetime.bestAccuracy}%" : "") +
                          (s.lifetime.longestStand > 0f ? $"  ·  longest run {ResultsMath.FormatTime(s.lifetime.longestStand)}" : "") +
                          (c.weeklyDone.Count > 0 ? $"  ·  {Plural.Of(c.weeklyDone.Count, "weekly", "weeklies")} beaten" : "") + $"  ·  Field Guide {seen}/{ZombieFieldGuide.All.Length}");
            }
            var lt = s.lifetime;
            lines.Add($"Daily gift streak: {GiftDots(lt.loginStreak)}  {Plural.Of(lt.loginStreak, "day")}  ·  best {lt.bestLoginStreak}  ·  {lt.dailyDone} daily challenges done");
            lines.Add($"Story challenges: {StoryPerfect.Count(s)} PERFECT  ·  {StoryBrave.Count(s)} BRAVE  ·  {StoryRecords.SRanks(s)} S ranks  ·  {Plural.Of(StoryRecords.TotalTries(s), "try", "tries")}");
            var fast = StoryRecords.Fastest(s);
            string since = PlayDates.Since(lt.firstDay);
            lines.Add($"{(since.Length > 0 ? since + "  ·  " : "")}days played: {lt.loginGifts}  ·  week streak {lt.weekStreak} (best {lt.bestWeekStreak})  ·  best Versus streak {lt.vsBestStreak}" + (lt.vsStreak > 0 ? $" (now {lt.vsStreak})" : "") +
                      (fast != null ? $"  ·  fastest clear {ResultsMath.FormatTime(fast.bestTimeSeconds)}" : ""));
            return lines;
        }

        /// <summary>Earned badges by name, then the three you're closest to with progress ("TRAILBLAZER 7/10").</summary>
        public static string BadgeLines(SaveData s)
        {
            var earned = new List<string>();
            var open = new List<Badges.Badge>();
            foreach (var b in Badges.All)
            {
                if (Badges.Has(s, b.id)) earned.Add(b.name);
                else open.Add(b);
            }
            if (s != null) open.Sort((a, b) => ((double)b.Have(s) / b.need).CompareTo((double)a.Have(s) / a.need));
            var close = new List<string>();
            for (int i = 0; i < open.Count && i < 3; i++)
                close.Add(open[i].need > 1 && s != null ? $"{open[i].name} {open[i].Have(s):N0}/{open[i].need:N0}" : $"{open[i].name} ({open[i].how})");
            var recent = RecentBadges.Names(s, 3);
            string a1 = earned.Count > 0 ? "Latest: " + string.Join("  ·  ", recent) + (earned.Count > 3 ? $"  (+{earned.Count - 3} more)" : "") : "No badges yet: they come from playing every mode.";
            return close.Count > 0 ? a1 + "\nClosest: " + string.Join("  ·  ", close) : a1;
        }

        /// <summary>This week of the 7-day gift cycle: ● claimed, ○ to come.</summary>
        static string GiftDots(int streak)
        {
            int inCycle = streak <= 0 ? 0 : ((streak - 1) % LoginStreak.Gifts.Length) + 1;
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i <= LoginStreak.Gifts.Length; i++) sb.Append(i <= inCycle ? '●' : '○');
            return sb.ToString();
        }

        static string TopMode(SaveData s)
        {
            int best = -1; float bt = 0f;
            for (int i = 0; i < PlayTime.Menus; i++) { float t = PlayTime.Seconds(s, i); if (t > bt) { bt = t; best = i; } }
            return best >= 0 && bt >= 60f ? $"  ·  most in {PlayTime.Modes[best]}" : "";
        }

        static string HeroName(CharacterRoster roster, string id)
        {
            var d = roster != null ? roster.Find(id) : null;
            return d != null ? d.displayName.ToUpperInvariant() : (id ?? "").ToUpperInvariant();
        }

        void Build()
        {
            UIFactory.Clear(_root);
            var s = Services.Save != null ? Services.Save.Data : null;
            var panel = UIFactory.Panel(_root, new Vector2(980f, 1800f));
            UIFactory.Label(panel, "PROFILE", 84, TextAnchor.MiddleCenter, 110f, title: true);
            int xp = s != null ? s.playerXp : 0;
            int bc = s != null ? s.lifetime.badges.Count : 0;
            string nextTitle = RankTitle.Next(bc);
            UIFactory.Label(panel, $"{RankTitle.For(bc)}" + (nextTitle.Length > 0 ? $"   <size=26>({nextTitle})</size>" : ""), 44, TextAnchor.MiddleCenter, 56f, title: true).color = UITheme.Current.accent;
            UIFactory.Label(panel, $"PLAYER LEVEL {PlayerLevel.LevelFor(xp)}   ·   STAR SHARDS {(s != null ? s.starShards : 0)}", 38, TextAnchor.MiddleCenter, 56f)
                .color = UITheme.Current.title;

            // Hero mastery: favorite first, then the rest you've played.
            UIFactory.Label(panel, "HERO MASTERY", 44, TextAnchor.MiddleCenter, 60f, title: true).color = UITheme.Current.title;
            var heroes = new List<HeroXp>(s != null ? s.lifetime.heroXp : new List<HeroXp>());
            heroes.Sort((a, b) => b.xp.CompareTo(a.xp));
            if (heroes.Count == 0)
                UIFactory.Label(panel, "Play any mode to start mastering your heroes.", 28, TextAnchor.MiddleCenter, 44f).color = UITheme.Current.subtle;
            for (int i = 0; i < heroes.Count && i < 4; i++)
            {
                var h = heroes[i];
                int lv = HeroMastery.Level(h.xp);
                string next = lv < HeroMastery.MaxLevel ? $"  ·  {HeroMastery.ToNext(h.xp)} XP to {lv + 1}" : "  ·  MAX";
                var t = UIFactory.Label(panel, $"{(i == 0 ? "★ " : "")}{HeroName(_roster, h.id)}   MASTERY {lv} · {HeroMastery.Title(lv)}{next}", 30, TextAnchor.MiddleCenter, 46f);
                t.color = lv >= HeroMastery.MaxLevel ? UITheme.Current.title : UITheme.Current.text;
            }

            int earned = s != null ? s.lifetime.badges.Count : 0;
            UIFactory.Label(panel, $"BADGES  {earned}/{Badges.All.Length}   <size=26>{BadgeRewards.ToNext(s)} more for +{BadgeRewards.Shards} shards</size>", 44, TextAnchor.MiddleCenter, 60f, title: true).color = UITheme.Current.title;
            UIFactory.Label(panel, BadgeLines(s), 24, TextAnchor.MiddleCenter, 120f).color = UITheme.Current.text;

            UIFactory.Label(panel, "RECORDS", 44, TextAnchor.MiddleCenter, 60f, title: true).color = UITheme.Current.title;
            foreach (var line in Lines(s, _roster)) UIFactory.Label(panel, line, 24, TextAnchor.MiddleCenter, 40f);

            var row = UIFactory.Rect("Buttons", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h2 = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h2.spacing = 24f;
            h2.childControlWidth = true; h2.childControlHeight = true;
            h2.childForceExpandWidth = true; h2.childForceExpandHeight = true;
            UIFactory.Button(row, "SHARE", () => ShareService.Share(ShareText(s)));
            UIFactory.Button(row, "BADGES", ShowBadges);
            UIFactory.Button(row, "ABOUT", ShowAbout);
            var back = UIFactory.Button(row, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(back);
        }

        /// <summary>ABOUT: the version, that the art and music are original, and that progress stays on this device.</summary>
        public void ShowBadges() => BadgesPanel.Show(transform);

        public void ShowAbout()
        {
            InfoCard.Show(transform, "ABOUT", $"Pixel Kingdom Rumble  ·  version {Application.version}", AboutLines(), "OK");
        }

        public static string[] AboutLines() => new[]
        {
            "Every hero, level, sprite and song is original.",
            "No ads. No tracking. No analytics.",
            "Your progress is saved on this device only.",
            "Game Center (optional) handles scores, achievements and online co-op.",
            "Privacy policy and support: see the App Store page.",
        };

        string ShareText(SaveData s)
        {
            var fav = HeroMastery.Favorite(s);
            string hero = fav != null ? $"{HeroName(_roster, fav.id)} at mastery {HeroMastery.Level(fav.xp)}" : "my heroes";
            int cleared = 0;
            if (s != null) foreach (var l in s.levels) if (l != null && l.completed) cleared++;
            int badges = s != null ? s.lifetime.badges.Count : 0;
            int days = s != null ? s.lifetime.loginGifts : 0;
            return $"Pixel Kingdom Rumble: a {RankTitle.For(badges)} with {Plural.Of(cleared, "level")} cleared, {hero}, {badges}/{Badges.All.Length} badges, {Plural.Of(days, "day")} in the kingdom, {PlayTime.Format(PlayTime.Total(s))} played! " + ShareCard.Link;
        }
    }
}
