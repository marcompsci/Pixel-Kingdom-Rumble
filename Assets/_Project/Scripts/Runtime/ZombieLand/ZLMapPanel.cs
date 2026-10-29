using System.Collections.Generic;
using System.Text;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The full map of New Hollow with three tabs: MAP (the city and every marker you know), MISSIONS (leads, what's
    /// done, briefings) and CLUES (what you've learned, and which relics and lairs are still possible). Pauses the game.
    /// </summary>
    public class ZLMapPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        ZLGame _g;
        RectTransform _root, _body;
        Canvas _canvas;
        readonly List<(RectTransform, Image, System.Func<bool>, System.Func<Color>)> _dots =
            new List<(RectTransform, Image, System.Func<bool>, System.Func<Color>)>();
        string _tab = "map";
        float _prevScale = 1f;
        int _newGameTaps;
        TouchControlsUI _touch;

        public void Open()
        {
            _g = ZLGame.Current;
            if (_g == null) return;
            if (_root == null)
            {
                var safe = UIFactory.CreateCanvas(transform, "ZLMapCanvas", 60, landscape: true, out _canvas);
                _root = UIFactory.Rect("Map", safe);
                UIFactory.Stretch(_root);
            }
            _canvas.enabled = true;
            IsOpen = true;
            _touch = Object.FindAnyObjectByType<TouchControlsUI>();
            if (_touch != null) _touch.gameObject.SetActive(false);
            _prevScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _newGameTaps = 0;
            Build();
            UiMotion.Restore(_root);
            UiMotion.ModalIn(_root);   // in from 97% with the scrim; switching tabs inside doesn't replay this
        }

        public void Close()
        {
            IsOpen = false;
            // fades out quickly (taps stop at once), then the canvas goes
            if (_canvas != null) UiMotion.ModalOut(_root, () => { if (!IsOpen && _canvas != null) _canvas.enabled = false; });
            if (_touch != null) _touch.gameObject.SetActive(true);
            Time.timeScale = _prevScale;
        }

        void OnDestroy()
        {
            if (IsOpen) { IsOpen = false; Time.timeScale = 1f; }
        }

        void Build()
        {
            UIFactory.Clear(_root);
            _dots.Clear();
            UIFactory.Dim(_root).color = new Color(0f, 0f, 0f, 0.8f);
            var panel = UIFactory.Panel(_root, new Vector2(1800f, 1000f));
            var tabs = Row(panel, 100f);
            foreach (var (id, label) in new[] { ("map", "MAP"), ("missions", "MISSIONS"), ("clues", "CLUES") })
            {
                string t = id;
                var b = UIFactory.Button(tabs, label, () => { _tab = t; Build(); });
                if (_tab == id) UIFactory.Tint(b, UITheme.Current.title, new Color(0.15f, 0.1f, 0.05f));
            }
            UIFactory.Button(tabs, "CLOSE", Close);

            _body = UIFactory.Rect("Body", panel);
            var le = _body.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 760f;
            le.flexibleHeight = 1f;
            if (_tab == "map") MapTab();
            else if (_tab == "missions") MissionsTab();
            else CluesTab();
        }

        RectTransform Row(RectTransform parent, float h)
        {
            var row = UIFactory.Rect("Row", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16f;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            return row;
        }

        void MapTab()
        {
            var city = _g.City;
            float px = 740f / city.h;
            var holder = UIFactory.Rect("MapHolder", _body);
            holder.anchorMin = holder.anchorMax = new Vector2(0.32f, 0.5f);
            holder.sizeDelta = new Vector2(city.w * px, city.h * px);
            var img = holder.gameObject.AddComponent<Image>();
            img.sprite = _g.Minimap;
            var inner = UIFactory.Rect("Inner", holder);
            inner.anchorMin = inner.anchorMax = Vector2.zero;
            inner.pivot = Vector2.zero;
            inner.sizeDelta = holder.sizeDelta;
            ZLHUD.BuildDots(inner, px, 16f, _dots);
            ZLHUD.UpdateDots(_dots, px);
            // you are here
            var you = UIFactory.Rect("You", inner);
            you.anchorMin = you.anchorMax = Vector2.zero;
            you.anchoredPosition = _g.TargetPos * px;
            you.sizeDelta = new Vector2(26f, 26f);
            var yi = you.gameObject.AddComponent<Image>();
            yi.sprite = _g.Sprite("Props/pk_arrow");
            // district names
            foreach (var d in city.districts)
            {
                var dist = ZLCatalog.District(d.id);
                var rt = UIFactory.Rect("District", inner);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.anchoredPosition = new Vector2((d.x0 + d.x1) * 0.5f, (d.y0 + d.y1) * 0.5f) * px;
                rt.sizeDelta = new Vector2(300f, 30f);
                var t = rt.gameObject.AddComponent<Text>();
                t.font = UISprites.DisplayFont;
                t.fontSize = 17;
                t.alignment = TextAnchor.MiddleCenter;
                t.text = dist.name.ToUpperInvariant() + "\n" + new string('*', dist.threat);
                if (d.id == "harbor") rt.anchoredPosition += new Vector2(20f, -40f);
                t.color = new Color(1f, 1f, 1f, 0.85f);
                t.raycastTarget = false;
                rt.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            }
            // legend
            var legend = UIFactory.Rect("Legend", _body);
            legend.anchorMin = new Vector2(0.62f, 0f); legend.anchorMax = new Vector2(1f, 1f);
            legend.offsetMin = legend.offsetMax = Vector2.zero;
            var lt = legend.gameObject.AddComponent<Text>();
            lt.font = UISprites.Font;
            lt.fontSize = 30;
            lt.fontStyle = FontStyle.Bold;
            lt.color = Color.white;
            lt.alignment = TextAnchor.UpperLeft;
            lt.horizontalOverflow = HorizontalWrapMode.Wrap;
            lt.verticalOverflow = VerticalWrapMode.Overflow;
            var p = _g.Progress;
            var sb = new StringBuilder();
            sb.AppendLine($"<color=#FFD966>GRAYSTONE SHARDS {p.Shards}/{ZLCatalog.ShardsNeeded}</color>" +
                          (p.chapter2Begun ? $"   <color=#7FE8D0>SEA-GLASS SIGILS {p.Sigils}/{ZLCatalog.SigilsNeeded}</color>" : "") +
                          (p.chapter3Begun ? $"   <color=#B8FF9A>KEYCARDS {p.Keycards}/{ZLCatalog.KeycardsNeeded}</color>" : "") +
                          (p.chapter4Begun ? $"   <color=#FFC680>CLEARANCES {p.Clearances}/{ZLCatalog.ClearancesNeeded}</color>" : ""));
            sb.AppendLine($"Missions done {p.completed.Count}/{ZLCatalog.Missions.Length + (p.chapter2Begun ? ZLCatalog.Chapter2Missions.Length : 0) + (p.chapter3Begun ? ZLCatalog.Chapter3Missions.Length : 0) + (p.chapter4Begun ? ZLCatalog.Chapter4Missions.Length : 0)}    Kills {p.kills}    Deaths {p.deaths}");
            sb.AppendLine();
            sb.AppendLine("<color=#FFD966>O</color> Story mission (gives a shard)");
            sb.AppendLine("<color=#80D8FF>O</color> Bonus mission (gives a reward)");
            sb.AppendLine("<color=#FF6655>O</color> Your current objective");
            sb.AppendLine("<color=#80FF99>#</color> Safehouse (checkpoint, no spawns)");
            sb.AppendLine("<color=#C9A0FF>#</color> Relic case (a relic weapon)");
            sb.AppendLine("<color=#FFFFFF>X</color> A possible lair (sealed until 10 shards)");
            sb.AppendLine();
            sb.AppendLine("Stars show how dangerous a district is.");
            sb.AppendLine("Walk near a \"?\" to learn a new lead.");
            lt.text = sb.ToString();
            lt.supportRichText = true;
            if (_g.Active != null)
            {
                var ab = UIFactory.Rect("Abandon", legend);
                ab.anchorMin = ab.anchorMax = new Vector2(0.5f, 0f);
                ab.pivot = new Vector2(0.5f, 0f);
                ab.sizeDelta = new Vector2(560f, 100f);
                var b = UIFactory.Button(ab, "ABANDON " + _g.Active.Spec.name, () => { _g.AbandonMission(); Build(); }, 100f);
                UIFactory.Stretch((RectTransform)b.transform);
            }
        }

        void MissionsTab()
        {
            var text = Scroll();
            var p = _g.Progress;
            var sb = new StringBuilder();
            foreach (var d in ZLCatalog.Districts)
            {
                bool any = false;
                foreach (var m in ZLCatalog.Missions)
                {
                    if (m.district != d.id) continue;
                    bool known = p.leads.Contains(m.id) || p.Done(m.id);
                    if (!any) { sb.AppendLine($"<color=#FFD966>{d.name.ToUpperInvariant()}</color>  {new string('*', d.threat)}"); any = true; }
                    string tag = m.required ? "STORY" : "BONUS";
                    if (p.Done(m.id)) sb.AppendLine($"  <color=#88FF99>DONE</color>  {m.name}  ({tag})");
                    else if (_g.Active != null && _g.Active.Spec == m) sb.AppendLine($"  <color=#FF7766>NOW</color>  {m.name}  ({tag}): {_g.Active.Objective}");
                    else if (known) sb.AppendLine($"  <color=#FFFFFF>LEAD</color>  {m.name}  ({tag}, {m.giver}): {m.briefing}");
                    else sb.AppendLine("  ???  Somewhere in " + d.name + "...");
                }
                if (any) sb.AppendLine();
            }
            text.text = sb.ToString();
        }

        void CluesTab()
        {
            var text = Scroll();
            var p = _g.Progress;
            var puzzle = _g.Puzzle;
            var sb = new StringBuilder();
            sb.AppendLine("<color=#FFD966>WHAT CAN HURT HIM?</color>");
            var relicsLeft = puzzle.RelicsLeft(p.completed);
            foreach (var r in ZLCatalog.Relics)
            {
                bool possible = relicsLeft.Contains(r.id);
                sb.AppendLine(possible ? $"  {r.name}  ({r.element})" + (relicsLeft.Count == 1 ? "  <color=#88FF99>THIS ONE</color>" : "")
                                       : $"  <color=#777777>{r.name}  ({r.element})  RULED OUT</color>");
            }
            sb.AppendLine();
            sb.AppendLine("<color=#FFD966>WHERE IS HIS LAIR?</color>");
            var lairsLeft = puzzle.LairsLeft(p.completed);
            foreach (var l in ZLCatalog.Lairs)
            {
                bool possible = lairsLeft.Contains(l.id) && !p.lairsSearched.Contains(l.id);
                sb.AppendLine(possible ? $"  {l.name}" + (lairsLeft.Count == 1 ? "  <color=#88FF99>HERE</color>" : "")
                                       : $"  <color=#777777>{l.name}  RULED OUT</color>");
            }
            sb.AppendLine();
            sb.AppendLine("<color=#FFD966>CLUES FOUND</color>");
            int n = 0;
            foreach (var id in p.completed)
                if (puzzle.Clues.TryGetValue(id, out var c)) { sb.AppendLine("  - " + c.text); n++; }
            if (n == 0) sb.AppendLine("  None yet. Every story mission gives one.");
            sb.AppendLine();
            sb.AppendLine($"Shards {p.Shards}/{ZLCatalog.ShardsNeeded}: the lair stays sealed until you carry all ten.");
            text.text = sb.ToString();

            var row = Row((RectTransform)_body.parent, 90f);
            UIFactory.Button(row, _newGameTaps == 0 ? "NEW GAME (NEW MYSTERY)" : "TAP AGAIN: ERASE ZOMBIE LAND PROGRESS", () =>
            {
                if (++_newGameTaps >= 2) { Close(); _g.NewGame(); }
                else Build();
            });
        }

        Text Scroll()
        {
            var sv = UIFactory.Rect("Scroll", _body);
            UIFactory.Stretch(sv);
            var scroll = sv.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            sv.gameObject.AddComponent<RectMask2D>();
            sv.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = UIFactory.Rect("Content", sv);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = content.gameObject.AddComponent<Text>();
            t.font = UISprites.Font;
            t.fontSize = 30;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            scroll.content = content;
            return t;
        }
    }
}
