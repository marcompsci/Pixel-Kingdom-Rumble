using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The glue between a finished party quest and the meta systems (PartyProgress): records the result once, then
    /// adds the extra lines (daily challenge, rank, best time, Kingdom Cup) and the right buttons to a results panel.
    /// Used by the Item Race (RaceDirector) and the arena quests (ArenaResultsScreen).
    /// </summary>
    public static class PartyFlow
    {
        public static int Today => DailyChallenges.DayKey(System.DateTime.Now);

        public static PartySave Party => Services.Save != null ? Services.Save.Data.party : null;

        public static PartyDifficulty Difficulty => Party != null ? Party.Difficulty : PartyDifficulty.Normal;

        public static bool CupActive => Party != null && Party.cup != null && Party.cup.active;

        /// <summary>The last result's report (the results screen reads it).</summary>
        public static PartyReport Last { get; private set; }

        /// <summary>Record a finished quest (shards, rank, daily, cup) and save. Returns what it earned.</summary>
        public static PartyReport Finish(PartyOutcome o)
        {
            o.difficulty = Difficulty;
            if (Services.Save == null) { Last = new PartyReport(); return Last; }
            Last = PartyProgress.Apply(Services.Save.Data, o, Today);
            Services.Save.SaveNow();
            return Last;
        }

        public static void StartCup()
        {
            if (Party == null) return;
            KingdomCup.Start(Party, Difficulty);
            if (Services.Save != null) Services.Save.SaveNow();
            GameSession.PlazaReturnX = -1f;
            GameSession.StartPartyQuest(KingdomCup.Events[0]);
        }

        public static void QuitCup()
        {
            if (Party != null && Party.cup != null) Party.cup.active = false;
            if (Services.Save != null) Services.Save.SaveNow();
        }

        // ---- results panel extras ----------------------------------------------------------------------------------

        /// <summary>Lines under the standings: daily challenge, rank, best time and the cup.</summary>
        public static void AddLines(RectTransform panel, PartyReport r, PartyMode mode)
        {
            if (r == null) return;
            var theme = UITheme.Current;
            var lines = new List<string>();
            if (r.dailyDone) lines.Add($"DAILY CHALLENGE DONE!  +{r.dailyBonus} Star Shards  ·  {Plural.Of(r.dailyStreak, "day")} in a row");
            if (r.rankUp) lines.Add($"RANK UP: {PartyModes.Info(mode).name} {PartyRanks.Name(r.rank)}");
            if (r.newBestTime) lines.Add("NEW BEST TIME! Your ghost will race you next time.");
            if (r.cupFinished)
                lines.Add($"KINGDOM CUP: {RumblePoints.Ordinal(r.cupPlace).ToUpperInvariant()} PLACE  ·  {KingdomCup.TrophyName(r.cupTrophy)} TROPHY  ·  +{r.cupBonus} Star Shards");
            else if (r.cupEvent && Party != null && Party.cup != null)
            {
                var pts = Party.cup.points;
                var standing = KingdomCup.Standings(pts);
                lines.Add($"KINGDOM CUP  ·  EVENT {Party.cup.eventIndex} OF {KingdomCup.Events.Length}  ·  YOU {pts[0]} PTS ({RumblePoints.Ordinal(standing[0]).ToUpperInvariant()})");
            }
            if (lines.Count == 0) return;
            var t = UIFactory.Label(panel, string.Join("\n", lines), 32, TextAnchor.MiddleCenter, 46f * lines.Count + 10f);
            t.color = theme.title;
            UiMotion.Enter(t.rectTransform, 0.35f, 0.9f, Vector2.zero);
        }

        /// <summary>
        /// The buttons for a party result: in a cup, NEXT EVENT (or BACK TO PLAZA after the last event) and QUIT CUP;
        /// otherwise PLAY AGAIN, SHARE and BACK TO PLAZA. Returns the button to select first.
        /// </summary>
        public static Button AddButtons(RectTransform buttons, PartyReport r, PartyMode mode, System.Action playAgain, string shareText)
        {
            if (r != null && r.cupEvent && !r.cupFinished && Party != null && Party.cup != null && Party.cup.active)
            {
                var next = Party.cup.CurrentEvent;
                var go = UIFactory.Button(buttons, "NEXT: " + PartyModes.Info(next).name, () => GameSession.StartPartyQuest(next));
                UIFactory.Button(buttons, "QUIT CUP", () => { QuitCup(); BackToPlaza(); });
                return go;
            }
            Button first;
            if (r != null && r.cupFinished) first = UIFactory.Button(buttons, "BACK TO PLAZA", BackToPlaza);
            else
            {
                first = UIFactory.Button(buttons, "PLAY AGAIN", () => playAgain?.Invoke());
                UIFactory.Button(buttons, "BACK TO PLAZA", BackToPlaza);
            }
            if (!string.IsNullOrEmpty(shareText)) UIFactory.Button(buttons, "SHARE", () => ShareService.Share(shareText));
            return first;
        }

        static void BackToPlaza()
        {
            GameSession.PlazaReturnX = -1f;
            GameSession.ReturnToPlaza();
        }

        /// <summary>A short, spoiler-free result to share (no personal data; the player chooses where it goes).</summary>
        public static string ShareText(PartyMode mode, int placement, int entrants, PartyReport r, string detail)
        {
            string medal = placement == 1 ? "🥇" : placement == 2 ? "🥈" : placement == 3 ? "🥉" : "🎈";
            var s = $"Pixel Kingdom Rumble · {PartyModes.Info(mode).name}\n{medal} {RumblePoints.Ordinal(placement)} of {entrants}";
            if (!string.IsNullOrEmpty(detail)) s += " · " + detail;
            if (r != null && r.cupFinished) s += $"\n🏆 Kingdom Cup: {KingdomCup.TrophyName(r.cupTrophy).ToLowerInvariant()} ({PartyDifficulties.Name(Difficulty).ToLowerInvariant()})";
            if (r != null && r.dailyDone) s += $"\n📅 Daily party challenge done · {Plural.Of(r.dailyStreak, "day")} streak";
            return s;
        }
    }
}
