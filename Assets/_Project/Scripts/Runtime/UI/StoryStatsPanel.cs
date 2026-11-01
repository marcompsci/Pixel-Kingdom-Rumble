using System;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Story Quest > STORY STATS: totals across every world (levels cleared, Star Shards and secrets found, time medals,
    /// foes defeated, the live season's tokens) and the QUICK GAME widget's stats, which the game reads from the widget
    /// each time it opens.
    /// </summary>
    public class StoryStatsPanel : MonoBehaviour
    {
        RectTransform _root;
        List<LevelDefinition> _levels;
        Action _onBack;

        public void Open(RectTransform root, List<LevelDefinition> levels, Action onBack)
        {
            _root = root; _levels = levels ?? new List<LevelDefinition>(); _onBack = onBack;
            QuickGameSync.SyncNow(); // freshest widget numbers
            Build();
        }

        void Build()
        {
            var p = UITheme.Current;
            var save = Services.Save != null ? Services.Save.Data : null;
            UIFactory.Clear(_root);
            var panel = UIFactory.Panel(_root, new Vector2(1000f, 1700f));
            UIFactory.Label(panel, "STORY STATS", 72, TextAnchor.MiddleCenter, 110f, title: true);

            int cleared = 0, shards = 0, totalShards = 0, secrets = 0, totalSecrets = 0, gold = 0, silver = 0, bronze = 0;
            foreach (var l in _levels)
            {
                totalShards += l.totalShards;
                totalSecrets += l.totalSecrets;
                var rec = save != null ? save.GetLevel(l.id) : null;
                if (rec == null || !rec.completed) continue;
                cleared++;
                shards += Mathf.Min(rec.mostShardsCollected, l.totalShards);
                secrets += Mathf.Min(rec.mostSecretsFound, l.totalSecrets);
                switch (StoryMedals.For(rec.bestTimeSeconds, l.parTimeSeconds))
                {
                    case TimeMedal.Gold: gold++; break;
                    case TimeMedal.Silver: silver++; break;
                    case TimeMedal.Bronze: bronze++; break;
                }
            }
            int foes = 0;
            if (save != null) foreach (var t in save.enemyDefeats) if (t != null) foes += t.defeated;

            Section(panel, "STORY QUEST");
            Row(panel, "Levels cleared", $"{cleared} / {_levels.Count}");
            Row(panel, "Star Shards found", $"{shards} / {totalShards}");
            Row(panel, "Secrets found", $"{secrets} / {totalSecrets}");
            Row(panel, "Time medals", $"{gold} GOLD  ·  {silver} SILVER  ·  {bronze} BRONZE");
            Row(panel, "Foes defeated", foes.ToString("N0"));
            var season = SettingsService.Season;
            if (season.Active && save != null)
                Row(panel, $"{Seasons.TokenName(season.Event)}s ({SeasonCalendar.Name(season.Event)})",
                    $"{Seasons.Tokens(save, season)} / {SeasonDirector.StoryLevelCount}");

            Section(panel, "QUICK GAME  ·  HOME SCREEN WIDGET");
            var q = save != null && save.quickGame != null ? save.quickGame.last : null;
            if (q == null || (q.taps == 0 && q.runs == 0))
            {
                var how = UIFactory.Label(panel,
                    "Play a tiny Story Quest stage right on your Home Screen. Touch and hold an empty spot on the Home Screen, " +
                    "tap +, find Pixel Kingdom Rumble and add QUICK GAME. Tap left or right to step, JUMP to jump, and JUMP " +
                    "again in the air to double jump. Every stage you clear there pays " + QuickGame.ShardsPerClear + " Star Shards here.",
                    30, TextAnchor.MiddleCenter, 260f);
                how.color = p.subtle;
            }
            else
            {
                Row(panel, "Stages cleared", $"{q.clears}  ({Plural.Of(q.runs, "run")})");
                Row(panel, "Star Shards · stomps", $"{q.shards}  ·  {q.stomps}");
                Row(panel, "Days in a row", $"{q.streak}  (best {q.bestStreak})");
                Row(panel, "Taps", q.taps.ToString("N0"));
                var bests = new List<string>();
                for (int i = 0; i < QuickGame.StageIds.Length; i++)
                {
                    int b = q.BestSteps(QuickGame.StageIds[i]);
                    if (b > 0) bests.Add($"{QuickGame.StageNames[i]} {b}");
                }
                var list = UIFactory.Label(panel, bests.Count > 0 ? "Fewest taps:  " + string.Join("  ·  ", bests) : "Clear a stage to set a fewest-taps record.",
                                           28, TextAnchor.MiddleCenter, 110f);
                list.color = p.subtle;
            }

            var back = UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(back);
        }

        static void Section(RectTransform panel, string title)
        {
            UIFactory.Label(panel, title, 40, TextAnchor.MiddleCenter, 70f, title: true).color = UITheme.Current.title;
        }

        static void Row(RectTransform panel, string name, string value)
        {
            var row = UIFactory.Rect("Row", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Label(row, name, 34, TextAnchor.MiddleLeft, 58f);
            var v = UIFactory.Label(row, value, 34, TextAnchor.MiddleRight, 58f);
            v.color = UITheme.Current.title;
        }
    }
}
