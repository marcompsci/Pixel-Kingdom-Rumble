using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Portrait Story Quest route (03_LevelSelect), one page per Story world ("&lt;" / "&gt;" to switch).
    /// Normal worlds: one big card per level in play order, showing best rank, time, Star Shards and secrets.
    /// Sectioned worlds (The Shattered Realms): the first four levels are drawn as one map split into four colored
    /// realm sections, with the remaining levels (the crossing and the boss) as cards underneath.
    /// Every level is open (2026-10-10); the suggested (first uncleared) level is outlined. Opened after Character Select.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [SerializeField] WorldDefinition world;
        [SerializeField] List<WorldDefinition> moreWorlds = new List<WorldDefinition>();
        public int sortingOrder = 10;

        RectTransform _root;
        LevelSlot[] _allSlots = new LevelSlot[0];
        LevelSlot[] _slots = new LevelSlot[0];
        readonly List<Button> _cards = new List<Button>();
        readonly List<WorldDefinition> _pages = new List<WorldDefinition>();
        static int _page = -1; // remembered for the session

        /// <summary>Cards on the current page, in the page's level order.</summary>
        public IReadOnlyList<Button> Cards => _cards;
        /// <summary>Slots for the current page's levels.</summary>
        public LevelSlot[] Slots => _slots;
        /// <summary>Suggested level on the current page (index into Cards/Slots), or -1 if it's on another page.</summary>
        public int SuggestedIndex { get; private set; }
        public int Page => _page;
        public int PageCount => _pages.Count;

        /// <summary>Runtime setup (tests). The menu builder assigns the field instead.</summary>
        public void SetWorld(WorldDefinition w) => world = w;
        public void SetMoreWorlds(params WorldDefinition[] worlds)
        {
            moreWorlds = new List<WorldDefinition>();
            foreach (var w in worlds) if (w != null) moreWorlds.Add(w);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _page = -1;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "LevelSelectCanvas", sortingOrder, landscape: false, out _);
            _root = UIFactory.Rect("LevelSelect", safe);
            UIFactory.Stretch(_root);
        }

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettings);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettings);
        void OnSettings(SettingsChanged e) => Build();

        void Start()
        {
            if (world == null || world.levels.Count == 0)
            {
                Debug.LogError("[LevelSelectUI] No world assigned. Run PKR > Build Menu Scenes.");
                return;
            }
            Build();
        }

        public void ShowPage(int page)
        {
            if (_pages.Count == 0) return;
            _page = Mathf.Clamp(page, 0, _pages.Count - 1);
            Build();
        }

        void Build()
        {
            if (_root == null || world == null) return;
            _pages.Clear();
            _pages.Add(world);
            if (moreWorlds != null) foreach (var w in moreWorlds) if (w != null && w.levels.Count > 0 && w != world) _pages.Add(w);

            // One route across every world, so the suggestion (and the old in-order rule) carries over between pages.
            var ids = new List<string>();
            foreach (var w in _pages) ids.AddRange(w.LevelIds());
            _allSlots = LevelSelect.Build(ids.ToArray(), Services.Save != null ? Services.Save.Data : null, openAll);
            int suggestedAll = LevelSelect.Suggested(_allSlots);

            if (_page < 0 || _page >= _pages.Count) _page = PageOf(suggestedAll);
            var page = _pages[_page];
            int first = FirstIndex(_page);
            _slots = new LevelSlot[page.levels.Count];
            System.Array.Copy(_allSlots, first, _slots, 0, _slots.Length);
            SuggestedIndex = suggestedAll >= first && suggestedAll < first + _slots.Length ? suggestedAll - first : -1;

            UIFactory.Clear(_root);
            _cards.Clear();
            var p = UITheme.Current;
            int n = page.levels.Count;
            bool sectioned = page.sectioned && n >= 4;
            int listed = sectioned ? n - 4 : n;
            bool compact = n > 4;
            bool tight = listed > 6;
            float cardHeight = sectioned ? 150f : tight ? 128f : compact ? 158f : 230f;
            bool hasDescription = !string.IsNullOrEmpty(page.description);
            bool paged = _pages.Count > 1;
            float mapHeight = sectioned ? 2f * 300f + 16f + 60f + 18f : 0f;
            float height = 80f + 150f + 18f + (paged ? UIFactory.RowHeight + 18f : 0f) +
                           (hasDescription ? (compact ? 80f : 90f) + 18f : 0f) + mapHeight +
                           listed * (cardHeight + 18f) + UIFactory.RowHeight;
            var panel = UIFactory.Panel(_root, new Vector2(1000f, Mathf.Min(1840f, height)));
            string worldNo = paged ? $"WORLD {_page + 1} · " : "";
            UIFactory.Label(panel, $"STORY QUEST\n{worldNo}{page.displayName.ToUpperInvariant()}", 56, TextAnchor.MiddleCenter, 150f, title: true);
            if (paged) PageRow(panel);
            if (hasDescription)
                UIFactory.Label(panel, page.description, 32, TextAnchor.MiddleCenter, compact ? 80f : 90f).color = p.subtle;

            Button suggested = null;
            if (sectioned)
            {
                UIFactory.Label(panel, "THE FOUR REALMS", 36, TextAnchor.MiddleCenter, 60f, title: true);
                var map = UIFactory.Rect("RealmMap", panel);
                map.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f * 300f + 16f;
                var grid = map.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(444f, 300f);
                grid.spacing = new Vector2(16f, 16f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                grid.childAlignment = TextAnchor.MiddleCenter;
                for (int i = 0; i < 4; i++)
                {
                    var b = Card(map, page, i, 300f, sectioned: true);
                    if (i == SuggestedIndex && b.interactable) suggested = b;
                }
            }
            for (int i = sectioned ? 4 : 0; i < n; i++)
            {
                var b = Card(panel, page, i, cardHeight, sectioned: false);
                b.GetComponentInChildren<Text>().fontSize = tight ? 29 : compact ? 32 : 38;
                if (i == SuggestedIndex && b.interactable) suggested = b;
            }

            UIFactory.Button(panel, "BACK", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect); });
            if (suggested != null) UIFactory.Select(suggested);
        }

        Button Card(RectTransform parent, WorldDefinition page, int i, float height, bool sectioned)
        {
            var p = UITheme.Current;
            var level = page.levels[i];
            var slot = _slots[i];
            int idx = i;
            bool playable = slot.unlocked && level != null && Application.CanStreamedLevelBeLoaded(level.sceneName);
            string note = sectioned && page.sectionNotes != null && i < page.sectionNotes.Count ? page.sectionNotes[i] : null;
            var card = UIFactory.Button(parent, sectioned ? SectionText(i, level, slot, note) : CardText(i, level, slot),
                                        () => Play(idx), height, interactable: playable);
            if (playable && level != null)
            {
                bool hc = Services.Settings != null && Services.Settings.Data.highContrastUI;
                if (sectioned && !hc && page.sectionColors != null && i < page.sectionColors.Count)
                    UIFactory.Tint(card, page.sectionColors[i], Color.white);
                else UIFactory.Tint(card, ModeColor(level.modeTag, p), ModeTextColor(level.modeTag, p));
            }
            var text = card.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            if (sectioned) { text.fontSize = 30; text.horizontalOverflow = HorizontalWrapMode.Wrap; }
            if (i == SuggestedIndex && playable) UIFactory.Highlight(card, p.title);
            _cards.Add(card);
            return card;
        }

        void PageRow(RectTransform panel)
        {
            var row = UIFactory.Rect("Pages", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Button(row, "<", () => ShowPage(_page - 1), interactable: _page > 0);
            var label = UIFactory.Label(row, $"{_page + 1} / {_pages.Count}", 40, TextAnchor.MiddleCenter, UIFactory.RowHeight);
            label.color = UITheme.Current.subtle;
            UIFactory.Button(row, ">", () => ShowPage(_page + 1), interactable: _page < _pages.Count - 1);
        }

        int FirstIndex(int page)
        {
            int i = 0;
            for (int k = 0; k < page && k < _pages.Count; k++) i += _pages[k].levels.Count;
            return i;
        }

        int PageOf(int globalIndex)
        {
            int i = 0;
            for (int k = 0; k < _pages.Count; k++)
            {
                i += _pages[k].levels.Count;
                if (globalIndex < i) return k;
            }
            return 0;
        }

        /// <summary>False only in tests that check the old unlock-in-order rule.</summary>
        public bool openAll = true;

        // Play-style colors so the route reads at a glance (high-contrast mode keeps the theme button color).
        static Color ModeColor(string tag, UITheme.Palette p)
        {
            if (Services.Settings != null && Services.Settings.Data.highContrastUI) return p.button;
            switch ((tag ?? "").ToLowerInvariant())
            {
                case "platform": return new Color32(84, 196, 120, 255);
                case "boss": return new Color32(232, 104, 84, 255);
                case "stealth": return new Color32(124, 104, 214, 255);
                default: return p.button;
            }
        }

        static Color ModeTextColor(string tag, UITheme.Palette p)
        {
            if (Services.Settings != null && Services.Settings.Data.highContrastUI) return p.buttonText;
            return (tag ?? "").ToLowerInvariant() == "stealth" ? Color.white : p.buttonText;
        }

        static string CardText(int index, LevelDefinition level, LevelSlot slot)
        {
            string name = level != null ? level.displayName.ToUpperInvariant() : "???";
            if (level != null && !string.IsNullOrEmpty(level.modeTag)) name += $"  [{level.modeTag.ToUpperInvariant()}]";
            if (!slot.unlocked) return $"{index + 1}. {name}\nLOCKED: CLEAR THE LEVEL BEFORE IT";
            if (!slot.completed) return $"{index + 1}. {name}\nNEW!";
            // Enemy drops can push the collected count past the placed total; cap it for display.
            string shards = level != null && level.totalShards > 0 ? $"   SHARDS {Mathf.Min(slot.shards, level.totalShards)}/{level.totalShards}" : "";
            string secrets = level != null && level.totalSecrets > 0 ? $"   SECRETS {slot.secrets}/{level.totalSecrets}" : "";
            return $"{index + 1}. {name}   RANK {LevelSelect.RankLabel(slot.rank)}\n" +
                   $"BEST {ResultsMath.FormatTime(slot.bestTimeSeconds)}{shards}{secrets}";
        }

        /// <summary>One realm section on the map: name, its rule, and progress.</summary>
        static string SectionText(int index, LevelDefinition level, LevelSlot slot, string note)
        {
            string name = level != null ? level.displayName.ToUpperInvariant() : "???";
            string rule = string.IsNullOrEmpty(note) ? "" : $"\n{note}";
            if (!slot.unlocked) return $"{index + 1}. {name}{rule}\nLOCKED";
            if (!slot.completed) return $"{index + 1}. {name}{rule}\nNEW!";
            string shards = level != null && level.totalShards > 0 ? $"  {Mathf.Min(slot.shards, level.totalShards)}/{level.totalShards}" : "";
            return $"{index + 1}. {name}{rule}\nRANK {LevelSelect.RankLabel(slot.rank)}  {ResultsMath.FormatTime(slot.bestTimeSeconds)}{shards}";
        }

        void Play(int index)
        {
            if (_page < 0 || _page >= _pages.Count) return;
            var page = _pages[_page];
            if (index < 0 || index >= page.levels.Count || !_slots[index].unlocked) return;
            var level = page.levels[index];
            if (level == null || Services.Scenes == null) return;
            Services.Scenes.Load(level.sceneName);
        }
    }
}
