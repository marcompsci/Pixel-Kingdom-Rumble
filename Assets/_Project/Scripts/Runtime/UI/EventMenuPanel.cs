using System;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Main menu sub-panels for Holiday Mode (four holiday runs) and Halloween (the Haunted Escape run and Zombie
    /// Survival). Picking a level goes through Character Select like every other mode. The holiday in season (by the
    /// device date) is marked IN SEASON.
    /// </summary>
    public class EventMenuPanel : MonoBehaviour
    {
        public struct EventLevel
        {
            public string scene, levelId, name, blurb;
            public Color32 color;
            public int[] months; // months this holiday is "in season" (1-12)
        }

        public static readonly EventLevel[] Holidays =
        {
            new EventLevel { scene = SceneIds.WinterGiftRun, levelId = "ev_winter_gift_run", name = "WINTER GIFT RUN",
                             blurb = "Snow bridges, toy-shop saws, a key in the chimney", color = new Color32(90, 170, 120, 255), months = new[] { 12 } },
            new EventLevel { scene = SceneIds.SweetheartSprint, levelId = "ev_sweetheart_sprint", name = "SWEETHEART SPRINT",
                             blurb = "Sun and moon blocks swap on every switch", color = new Color32(230, 90, 140, 255), months = new[] { 2 } },
            new EventLevel { scene = SceneIds.HarvestHustle, levelId = "ev_harvest_hustle", name = "HARVEST HUSTLE",
                             blurb = "Mill belts, cider presses, a corn maze", color = new Color32(214, 130, 50, 255), months = new[] { 11 } },
            new EventLevel { scene = SceneIds.CountdownClimb, levelId = "ev_countdown_climb", name = "COUNTDOWN CLIMB",
                             blurb = "Reach the rooftop party before midnight", color = new Color32(110, 100, 210, 255), months = new[] { 12, 1 } },
        };

        public static readonly EventLevel HauntedEscape = new EventLevel
        {
            scene = SceneIds.HauntedEscape, levelId = "ev_haunted_escape", name = "HAUNTED ESCAPE",
            blurb = "Outrun the horde through the graveyard", color = new Color32(120, 170, 90, 255), months = new[] { 10 }
        };

        RectTransform _root;
        Action _onBack;

        public void OpenHoliday(RectTransform root, Action onBack)
        {
            _root = root; _onBack = onBack;
            UIFactory.Clear(root);
            var panel = UIFactory.Panel(root, new Vector2(960f, 1560f));
            UIFactory.Label(panel, "HOLIDAY MODE", 84, TextAnchor.MiddleCenter, 130f, title: true);
            UIFactory.Label(panel, "Festive runs with their own worlds, foes and treats", 36, TextAnchor.MiddleCenter, 60f).color = UITheme.Current.subtle;
            Button first = null;
            foreach (var ev in Holidays)
            {
                var b = Card(panel, ev, SessionMode.Holiday);
                if (first == null) first = b;
            }
            UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
            if (first != null) UIFactory.Select(first);
        }

        public void OpenHalloween(RectTransform root, Action onBack)
        {
            _root = root; _onBack = onBack;
            UIFactory.Clear(root);
            var panel = UIFactory.Panel(root, new Vector2(960f, 1420f));
            var title = UIFactory.Label(panel, "HALLOWEEN", 96, TextAnchor.MiddleCenter, 140f, title: true);
            title.color = new Color32(255, 150, 50, 255);
            UIFactory.Label(panel, "Zombies are loose in the Sunspire Isles", 36, TextAnchor.MiddleCenter, 60f).color = UITheme.Current.subtle;

            var save = Services.Save != null ? Services.Save.Data : null;
            string best = save != null && save.zombieBestRound > 0
                ? $"Best: round {save.zombieBestRound} · {save.zombieBestPoints} pts · {save.zombieBestKills} kills"
                : "Hold the manor windows for as many rounds as you can";
            var survival = Big(panel, "ZOMBIE SURVIVAL", best, new Color32(200, 70, 50, 255), () => Go(SessionMode.Halloween, SceneIds.ZombieSurvival));
            UIFactory.Label(panel, "Guns on the walls · mystery crate · perk shrines · the upgrade forge · power-ups · a boss every 5 rounds · a secret",
                            28, TextAnchor.MiddleCenter, 80f).color = UITheme.Current.subtle;
            Card(panel, HauntedEscape, SessionMode.Halloween);
            UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(survival);
        }

        /// <summary>Today's three daily challenges and the player level.</summary>
        public void OpenDaily(RectTransform root, Action onBack)
        {
            _root = root; _onBack = onBack;
            UIFactory.Clear(root);
            var panel = UIFactory.Panel(root, new Vector2(960f, 1300f));
            UIFactory.Label(panel, "DAILY CHALLENGES", 76, TextAnchor.MiddleCenter, 120f, title: true);
            var save = Services.Save != null ? Services.Save.Data : null;
            int xp = save != null ? save.playerXp : 0;
            int lv = PlayerLevel.LevelFor(xp);
            int into = xp - PlayerLevel.XpAtLevel(lv);
            UIFactory.Label(panel, $"PLAYER LEVEL {lv}   ·   {into}/{PlayerLevel.XpToNext(lv)} XP", 38, TextAnchor.MiddleCenter, 60f)
                .color = UITheme.Current.title;
            UIFactory.Label(panel, "New challenges every day. Finish them for Star Shards and XP.", 30, TextAnchor.MiddleCenter, 50f)
                .color = UITheme.Current.subtle;
            foreach (var rec in ProgressTracker.Today())
            {
                var spec = DailyChallenges.Find(rec.id);
                if (!spec.HasValue) continue;
                var c = spec.Value;
                string state = rec.done ? "DONE!" : $"{rec.progress}/{c.target}";
                var row = UIFactory.Button(panel, c.text, null, 150f);
                UIFactory.Tint(row, rec.done ? new Color32(60, 150, 90, 255) : new Color32(60, 70, 110, 255), Color.white);
                var t = row.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.supportRichText = true;
                    t.resizeTextForBestFit = false;
                    t.fontSize = 38;
                    t.text = $"{c.text}\n<size=26>{state}   ·   +{c.shards} shards  +{c.xp} XP</size>";
                }
            }
            UIFactory.Label(panel, $"Every level pays Star Shards; every 5th level pays +{PlayerLevel.MilestoneBonus} more.", 28,
                            TextAnchor.MiddleCenter, 60f).color = UITheme.Current.subtle;
            var back = UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(back);
        }

        Button Card(RectTransform panel, EventLevel ev, SessionMode mode)
        {
            var save = Services.Save != null ? Services.Save.Data : null;
            var rec = save != null ? save.GetLevel(ev.levelId) : null;
            string sub = rec != null && rec.completed
                ? $"Best {ResultsMath.FormatTime(rec.bestTimeSeconds)} · {rec.mostShardsCollected} treats"
                : ev.blurb;
            string name = InSeason(ev) ? ev.name + "  · IN SEASON!" : ev.name;
            return Big(panel, name, sub, ev.color, () => Go(mode, ev.scene));
        }

        static Button Big(RectTransform panel, string name, string sub, Color32 color, UnityEngine.Events.UnityAction go)
        {
            var b = UIFactory.Button(panel, name, go, 150f);
            UIFactory.Tint(b, color, Color.white);
            var t = b.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.supportRichText = true;
                t.resizeTextForBestFit = false;
                t.fontSize = 40;
                t.text = $"{name}\n<size=26>{sub}</size>";
            }
            return b;
        }

        static bool InSeason(EventLevel ev)
        {
            int m = DateTime.Now.Month;
            if (ev.months == null) return false;
            foreach (int x in ev.months) if (x == m) return true;
            return false;
        }

        /// <summary>True in October (the Halloween button glows).</summary>
        public static bool IsHalloweenSeason => DateTime.Now.Month == 10;

        static void Go(SessionMode mode, string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning($"[PKR] Event scene {scene} isn't in the build yet.");
                return;
            }
            GameSession.Mode = mode;
            GameSession.EventScene = scene;
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect);
        }
    }
}
