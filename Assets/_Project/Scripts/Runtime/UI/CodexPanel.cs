using System;
using System.Collections.Generic;
using System.Text;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// The Codex (portrait, opened from the main menu): HEROES / FOES / PLACES tabs, a paged list of entries, and a
    /// page per entry with its art, text and stats. Entries unlock through play (see Core Codex); locked ones show
    /// a silhouette and how to unlock them. Opening an unlocked entry clears its NEW badge.
    /// </summary>
    public class CodexPanel : MonoBehaviour
    {
        public const int EntriesPerPage = 5;
        static readonly Color Silhouette = new Color(0.08f, 0.07f, 0.1f, 1f);

        RectTransform _container;
        CodexDefinition _codex;
        Action _onBack;
        CodexCategory _tab = CodexCategory.Hero;
        int _page;
        int _openIndex = -1; // index in the current tab's list, -1 = showing the list

        public CodexCategory Tab => _tab;
        public int Page => _page;
        public bool IsShowingEntry => _openIndex >= 0;
        /// <summary>Title of the open entry ("???" while locked), for tests.</summary>
        public string OpenTitle { get; private set; } = "";

        public void Open(RectTransform container, CodexDefinition codex, Action onBack)
        {
            _container = container;
            _codex = codex;
            _onBack = onBack;
            _tab = CodexCategory.Hero;
            _page = 0;
            _openIndex = -1;
            Rebuild();
        }

        public int EntryCount => _codex != null ? _codex.Entries(_tab).Count : 0;

        public void ShowTab(CodexCategory tab)
        {
            _tab = tab;
            _page = 0;
            _openIndex = -1;
            Rebuild();
        }

        public void StepPage(int dir)
        {
            _page = Codex.StepPage(_page, dir, EntryCount, EntriesPerPage);
            Rebuild();
        }

        /// <summary>Open the entry at this index of the current tab. Marks it seen (saved) if it was new.</summary>
        public void OpenEntry(int index)
        {
            var refs = _codex != null ? _codex.Entries(_tab) : new List<CodexEntryRef>();
            if (index < 0 || index >= refs.Count) return;
            _openIndex = index;
            var save = Services.Save;
            if (save != null && Codex.MarkSeen(save.Data, refs[index])) save.MarkDirty();
            Rebuild();
        }

        public void CloseEntry()
        {
            _openIndex = -1;
            Rebuild();
        }

        void Rebuild()
        {
            if (_container == null) return;
            UIFactory.Clear(_container);
            var panel = UIFactory.Panel(_container, new Vector2(1000f, 1780f));
            if (_codex == null)
            {
                UIFactory.Label(panel, "CODEX", 64, TextAnchor.MiddleCenter, 100f, title: true);
                UIFactory.Label(panel, "No codex data. Re-run PKR > Build All Scenes.", 36, TextAnchor.MiddleCenter, 120f);
                UIFactory.Select(UIFactory.Button(panel, "BACK", () => _onBack?.Invoke()));
                return;
            }
            if (_openIndex >= 0) BuildEntry(panel);
            else BuildList(panel);
        }

        // ---- List ----------------------------------------------------------------------------------

        void BuildList(RectTransform panel)
        {
            var p = UITheme.Current;
            var save = Services.Save != null ? Services.Save.Data : null;
            var all = _codex.Progress();
            UIFactory.Label(panel, "CODEX", 64, TextAnchor.MiddleCenter, 100f, title: true);
            UIFactory.Label(panel, $"FOUND {all.unlocked}/{all.total}", 40, TextAnchor.MiddleCenter, 60f).color = p.subtle;

            var tabs = Row(panel, "Tabs", 110f);
            TabButton(tabs, CodexCategory.Hero, "HEROES");
            TabButton(tabs, CodexCategory.Enemy, "FOES");
            TabButton(tabs, CodexCategory.Place, "PLACES");

            var refs = _codex.Entries(_tab);
            if (refs.Count == 0)
                UIFactory.Label(panel, "Nothing here yet.", 38, TextAnchor.MiddleCenter, 120f).color = p.subtle;
            Codex.PageRange(_page, EntriesPerPage, refs.Count, out int start, out int end);
            Button first = null;
            for (int i = start; i < end; i++)
            {
                var r = refs[i];
                bool unlocked = Codex.IsUnlocked(save, r);
                string name = unlocked ? NameOf(i) : "???";
                if (unlocked && Codex.IsNew(save, r)) name += "   NEW!";
                int index = i;
                var b = UIFactory.Button(panel, name, () => OpenEntry(index), 130f);
                var label = b.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(150f, 0f); // room for the icon
                Icon((RectTransform)b.transform, SpriteOf(i), unlocked ? TintOf(i) : Silhouette);
                if (first == null) first = b;
            }

            int pages = Codex.PageCount(refs.Count, EntriesPerPage);
            if (pages > 1)
            {
                var pager = Row(panel, "Pager", 110f);
                Small(pager, "<", () => StepPage(-1));
                var pageLabel = UIFactory.Label(pager, $"PAGE {_page + 1}/{pages}", 38, TextAnchor.MiddleCenter, 110f);
                pageLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
                Small(pager, ">", () => StepPage(+1));
            }

            var back = UIFactory.Button(panel, "BACK", () => _onBack?.Invoke());
            UIFactory.Select(first != null ? first : back);
        }

        void TabButton(RectTransform row, CodexCategory tab, string label)
        {
            var pr = _codex.Progress(tab);
            string text = $"{label} {pr.unlocked}/{pr.total}";
            var b = UIFactory.Button(row, text, () => ShowTab(tab), 110f);
            b.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var t = b.GetComponentInChildren<Text>();
            t.fontSize = 34;
            if (tab == _tab) t.color = UITheme.Current.title;
            else
            {
                t.color = new Color(t.color.r, t.color.g, t.color.b, 0.65f);
                if (pr.unseen > 0) t.text = text + " *"; // something new in another tab
            }
        }

        // ---- Entry page ----------------------------------------------------------------------------

        void BuildEntry(RectTransform panel)
        {
            var p = UITheme.Current;
            var refs = _codex.Entries(_tab);
            int i = Mathf.Clamp(_openIndex, 0, refs.Count - 1);
            var save = Services.Save != null ? Services.Save.Data : null;
            bool unlocked = Codex.IsUnlocked(save, refs[i]);

            var artRt = UIFactory.Rect("Art", panel);
            artRt.gameObject.AddComponent<LayoutElement>().preferredHeight = 320f;
            var art = artRt.gameObject.AddComponent<Image>();
            art.sprite = SpriteOf(i);
            art.enabled = art.sprite != null;
            art.preserveAspect = true;
            art.raycastTarget = false;
            art.color = unlocked ? TintOf(i) : Silhouette;

            OpenTitle = unlocked ? NameOf(i) : "???";
            UIFactory.Label(panel, OpenTitle.ToUpperInvariant(), 60, TextAnchor.MiddleCenter, 90f, title: true);

            if (!unlocked)
            {
                UIFactory.Label(panel, LockedHint(i), 38, TextAnchor.MiddleCenter, 200f).color = p.subtle;
            }
            else
            {
                string subtitle = Subtitle(i);
                if (!string.IsNullOrEmpty(subtitle))
                    UIFactory.Label(panel, subtitle, 32, TextAnchor.MiddleCenter, 90f).color = p.subtle;
                string body = Body(i);
                UIFactory.Label(panel, string.IsNullOrEmpty(body) ? "No notes yet." : body, 34, TextAnchor.UpperLeft, 300f);
                UIFactory.Label(panel, Stats(i, save), 32, TextAnchor.UpperLeft, 380f).color = p.accent;
            }

            var back = UIFactory.Button(panel, "BACK TO LIST", CloseEntry);
            UIFactory.Select(back);
        }

        // ---- Per-category content ------------------------------------------------------------------

        string NameOf(int i)
        {
            switch (_tab)
            {
                case CodexCategory.Hero: return _codex.Heroes()[i].displayName;
                case CodexCategory.Enemy: return _codex.Enemies()[i].displayName;
                default: return _codex.Places()[i].displayName;
            }
        }

        Sprite SpriteOf(int i)
        {
            switch (_tab)
            {
                case CodexCategory.Hero: return _codex.Heroes()[i].bodySprite;
                case CodexCategory.Enemy: return _codex.Enemies()[i].sprite;
                default: return null; // places have no art yet; the sky color shows as a swatch
            }
        }

        Color TintOf(int i)
        {
            switch (_tab)
            {
                case CodexCategory.Hero:
                    var h = _codex.Heroes()[i];
                    return _codex.roster != null ? _codex.roster.TintFor(h.id) : Color.white;
                case CodexCategory.Enemy: return Color.white;
                default: return _codex.Places()[i].skyColor;
            }
        }

        string LockedHint(int i)
        {
            switch (_tab)
            {
                case CodexCategory.Hero:
                    var h = _codex.Heroes()[i];
                    return string.IsNullOrEmpty(h.unlockHint) ? "Unlock this hero to read their story." : h.unlockHint;
                case CodexCategory.Enemy: return "Defeat one in Story Quest to fill in this page.";
                default: return "Reach this place in Story Quest to fill in this page.";
            }
        }

        string Subtitle(int i)
        {
            switch (_tab)
            {
                case CodexCategory.Hero: return _codex.Heroes()[i].tagline;
                case CodexCategory.Enemy: return BehaviorText(_codex.Enemies()[i]);
                default: return _codex.Places()[i].biomeName;
            }
        }

        string Body(int i)
        {
            switch (_tab)
            {
                case CodexCategory.Hero: return _codex.Heroes()[i].lore;
                case CodexCategory.Enemy: return _codex.Enemies()[i].codexEntry;
                default: return _codex.Places()[i].codexEntry;
            }
        }

        string Stats(int i, SaveData save)
        {
            switch (_tab)
            {
                case CodexCategory.Hero: return HeroStats(_codex.Heroes()[i]);
                case CodexCategory.Enemy:
                    var e = _codex.Enemies()[i];
                    return $"HEALTH {e.maxHealth}   ·   DROPS {e.shardDrop} SHARDS\nDEFEATED {Codex.DefeatCount(save, e.id)}";
                default:
                    var l = _codex.Places()[i];
                    var rec = save != null ? save.GetLevel(l.id) : null;
                    if (rec == null || !rec.completed) return "NOT CLEARED YET";
                    string rank = rec.BestRank.HasValue ? rec.BestRank.Value.ToString() : "-";
                    return $"BEST RANK {rank}   ·   BEST TIME {ResultsMath.FormatTime(rec.bestTimeSeconds)}\n" +
                           $"MOST SHARDS {rec.mostShardsCollected}   ·   SECRETS {rec.mostSecretsFound}";
            }
        }

        public static string BehaviorText(EnemyDefinition e)
        {
            switch (e.behavior)
            {
                case EnemyBehavior.Hopper: return "Hops toward you when you get close.";
                case EnemyBehavior.Flyer: return "Hovers, then swoops at you.";
                case EnemyBehavior.ShieldWalker: return "Blocks hits from the front. Heavy hits break the shield.";
                default: return "Patrols back and forth.";
            }
        }

        /// <summary>Health, weight, guard pips and the move list (names come from the moveset and kit).</summary>
        public static string HeroStats(CharacterDefinition h)
        {
            var sb = new StringBuilder();
            string weight = h.weight >= 1.3f ? "HEAVY" : h.weight <= 0.9f ? "LIGHT" : "MEDIUM";
            sb.Append($"HEALTH {h.maxHealth}   ·   {weight}   ·   GUARD {h.guardPips}\n");
            var m = h.moveset;
            var kit = h.kit as HeroKitDefinition;
            if (m != null)
            {
                Move(sb, "ATTACK", m.groundAttack);
                Move(sb, "UP + ATTACK", m.groundUpAttack);
                Move(sb, "AIR ATTACK", m.airAttack);
                Move(sb, "SPECIAL", m.groundSpecial);
                Move(sb, "SIDE + SPECIAL", m.sideSpecial);
                Move(sb, "DOWN + SPECIAL", m.downSpecial);
                if (kit == null || !kit.hasDive) Move(sb, "AIR SPECIAL", m.airSpecial);
            }
            if (kit != null)
            {
                if (kit.hasDive) sb.Append($"AIR SPECIAL: {kit.diveName}\n");
                sb.Append($"AIR DODGE: {kit.airDodgeName}\n");
            }
            return sb.ToString().TrimEnd('\n');
        }

        static void Move(StringBuilder sb, string input, MoveDefinition move)
        {
            if (move == null) return;
            sb.Append(input).Append(": ").Append(move.displayName);
            // Show a short combo chain (jab > sweep), guarding against loops.
            var seen = new HashSet<MoveDefinition> { move };
            MoveDefinition finisher = move.specialFollowUp;
            for (var next = move.followUp; next != null && seen.Add(next) && seen.Count <= 3; next = next.followUp)
            {
                sb.Append(" > ").Append(next.displayName);
                if (next.specialFollowUp != null) finisher = next.specialFollowUp;
            }
            if (finisher != null) sb.Append(" > (SPECIAL) ").Append(finisher.displayName);
            sb.Append('\n');
        }

        // ---- Widgets -------------------------------------------------------------------------------

        static RectTransform Row(RectTransform parent, string name, float height)
        {
            var row = UIFactory.Rect(name, parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            return row;
        }

        static void Small(RectTransform row, string label, UnityEngine.Events.UnityAction onClick)
        {
            var b = UIFactory.Button(row, label, onClick, 110f);
            var le = b.GetComponent<LayoutElement>();
            le.preferredWidth = 120f;
            le.flexibleWidth = 0f;
        }

        static void Icon(RectTransform button, Sprite sprite, Color color)
        {
            var rt = UIFactory.Rect("Icon", button);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(24f, 0f);
            rt.sizeDelta = new Vector2(100f, 100f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : UISprites.Circle; // places: a sky-colored dot
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
