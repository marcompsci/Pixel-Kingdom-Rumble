using System;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Main menu → season banner. What the live season is, how long it runs and what's next, the season's tokens and
    /// keepsake, its limited-time challenges (CLAIM pays Star Shards once) and the keepsakes earned so far (WEAR).
    /// </summary>
    public class SeasonPanel : MonoBehaviour
    {
        RectTransform _root;
        Action _onBack;

        public static Color ColorFor(SeasonState s) => s.Look == SeasonLook.Winter
            ? (Color)new Color32(70, 140, 210, 255)
            : (Color)new Color32(214, 116, 40, 255);

        public void Open(RectTransform root, Action onBack)
        {
            _root = root; _onBack = onBack;
            Build();
        }

        void Build()
        {
            var s = SettingsService.Season;
            var save = Services.Save != null ? Services.Save.Data : null;
            var p = UITheme.Current;
            UIFactory.Clear(_root);
            var panel = UIFactory.Panel(_root, new Vector2(960f, 1640f));
            var today = SeasonCalendar.Today;

            if (!s.Active)
            {
                UIFactory.Label(panel, "LIVE SEASONS", 72, TextAnchor.MiddleCenter, 120f, title: true);
                var next = SeasonCalendar.Next(today);
                bool off = Services.Settings != null && Services.Settings.Data.seasonalMode == SeasonCalendar.ModeOff;
                UIFactory.Label(panel, off ? "Live seasons are off (Settings > Display)."
                                           : $"{SeasonCalendar.Name(next.ev)} starts {SeasonCalendar.InDays((next.start - today).Days)}.",
                                36, TextAnchor.MiddleCenter, 120f).color = p.subtle;
                Keepsakes(panel, save);
                var back0 = UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
                UIFactory.Select(back0);
                return;
            }

            UIFactory.Label(panel, SeasonCalendar.Name(s.Event).ToUpperInvariant(), 72, TextAnchor.MiddleCenter, 110f, title: true);
            string when = s.AtPeak ? $"{SeasonCalendar.PeakName(s.Peak).ToUpperInvariant()} IS HERE!"
                                   : PeakLine(today);
            UIFactory.Label(panel, $"{SeasonCalendar.Tagline(s)}\n{when}", 32, TextAnchor.MiddleCenter, 100f).color = p.subtle;
            var next1 = SeasonCalendar.Next(today);
            int left = s.DaysLeft(today);
            UIFactory.Label(panel, $"{Plural.Of(left, "day")} left  ·  {SeasonCalendar.Name(next1.ev)} {SeasonCalendar.InDays((next1.start - today).Days)}",
                            30, TextAnchor.MiddleCenter, 50f).color = ColorFor(s);

            // Tokens and this season's keepsake.
            int levels = SeasonDirector.StoryLevelCount;
            int goal = Seasons.KeepsakeGoal(levels);
            int have = save != null ? Seasons.Tokens(save, s) : 0;
            var k = Seasons.KeepsakeFor(s.Event);
            var tokenRow = Row(panel, 150f);
            Icon(tokenRow, SeasonArt.Token(s.Event), 110f);
            string tokenName = Seasons.TokenName(s.Event);
            bool owned = save != null && k.HasValue && Seasons.Owns(save, k.Value.id);
            string reward = !k.HasValue ? "" : owned ? $"{k.Value.name} earned!" : $"Find {goal} for the {k.Value.name}";
            var tl = UIFactory.Label(tokenRow, $"{tokenName.ToUpperInvariant()}S  {have} / {levels}\n<size=28>{reward}  ·  one hidden in every Story level</size>",
                                     40, TextAnchor.MiddleLeft, 140f);
            tl.supportRichText = true;
            tl.GetComponent<LayoutElement>().flexibleWidth = 1f;
            if (k.HasValue) Icon(tokenRow, SeasonArt.HatSprite(SeasonArt.KeepsakeHat(k.Value.id) ?? SeasonArt.Hat.LeafCrown), 110f,
                                 owned ? 1f : 0.35f);

            UIFactory.Label(panel, "SEASON CHALLENGES", 40, TextAnchor.MiddleCenter, 64f).color = p.title;
            var list = Seasons.Challenges(s.Event);
            var rec = save != null ? Seasons.Find(save, s) : null;
            Button first = null;
            for (int i = 0; i < list.Length; i++)
            {
                int idx = i;
                var c = list[i];
                int prog = Mathf.Min(c.target, Seasons.Progress(rec, c.goal));
                bool done = prog >= c.target;
                bool claimed = save != null && Seasons.IsClaimed(save, s, i);
                var row = Row(panel, 110f);
                var label = UIFactory.Label(row, $"{c.text}\n<size=26>{prog} / {c.target}  ·  +{c.shards} Star Shards</size>", 34, TextAnchor.MiddleLeft, 100f);
                label.supportRichText = true;
                label.GetComponent<LayoutElement>().flexibleWidth = 1f;
                if (done && !claimed) label.color = ColorFor(s);
                var b = UIFactory.Button(row, claimed ? "DONE" : done ? "CLAIM" : "--", () => Claim(idx), 96f, interactable: done && !claimed);
                b.GetComponent<LayoutElement>().preferredWidth = 220f;
                if (done && !claimed)
                {
                    UIFactory.Tint(b, ColorFor(s), Color.white);
                    if (first == null) first = b;
                }
            }

            Keepsakes(panel, save);
            var back = UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(first != null ? first : back);
        }

        static string PeakLine(DateTime today)
        {
            var peak = SeasonCalendar.NextPeak(today);
            if (peak.peak == SeasonPeak.None) return "";
            int days = (peak.start - today).Days;
            return $"{SeasonCalendar.PeakName(peak.peak)} {SeasonCalendar.InDays(days)}";
        }

        void Claim(int index)
        {
            var save = Services.Save;
            if (save == null) return;
            int paid = Seasons.Claim(save.Data, SettingsService.Season, index);
            if (paid <= 0) return;
            save.SaveNow();
            Sfx.Play(Sfx.Purchase, 0.9f, 0f);
            EventBus<ProgressToast>.Raise(new ProgressToast { title = $"+{paid} STAR SHARDS", detail = "Season challenge reward", color = new Color32(255, 214, 90, 255) });
            Build();
        }

        void Keepsakes(RectTransform panel, SaveData save)
        {
            UIFactory.Label(panel, "KEEPSAKES  ·  worn in Story Quest", 34, TextAnchor.MiddleCenter, 56f).color = UITheme.Current.subtle;
            var row = Row(panel, 150f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            string worn = save != null && save.season != null ? save.season.worn : "";
            foreach (var k in Seasons.Keepsakes)
            {
                bool owned = save != null && Seasons.Owns(save, k.id);
                bool wearing = owned && worn == k.id;
                string id = k.id;
                var b = UIFactory.Button(row, owned ? (wearing ? "WEARING" : "WEAR") : "LOCKED", () => Wear(wearing ? "" : id), 140f, interactable: owned);
                var ble = b.GetComponent<LayoutElement>();
                ble.preferredWidth = 190f; ble.flexibleWidth = 1f;
                var t = b.GetComponentInChildren<Text>();
                if (t != null)
                {
                    t.resizeTextForBestFit = false;
                    t.fontSize = 22;
                    t.alignment = TextAnchor.LowerCenter;
                }
                var icon = UIFactory.Rect("Icon", (RectTransform)b.transform);
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.62f);
                icon.sizeDelta = new Vector2(84f, 60f);
                var img = icon.gameObject.AddComponent<Image>();
                img.sprite = SeasonArt.HatSprite(SeasonArt.KeepsakeHat(k.id) ?? SeasonArt.Hat.LeafCrown);
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                if (wearing) UIFactory.Highlight(b, UITheme.Current.title);
            }
        }

        void Wear(string id)
        {
            var save = Services.Save;
            if (save == null || !Seasons.Wear(save.Data, id)) return;
            save.SaveNow();
            Sfx.Play(Sfx.Click, 0.8f, 0f);
            Build();
        }

        static RectTransform Row(RectTransform panel, float height)
        {
            var row = UIFactory.Rect("Row", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            return row;
        }

        static void Icon(RectTransform row, Sprite sprite, float size, float alpha = 1f)
        {
            var r = UIFactory.Rect("Icon", row);
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = size;
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
