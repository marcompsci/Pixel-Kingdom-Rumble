using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Portrait Story Quest route (03_LevelSelect), one page per Story world ("&lt;" / "&gt;" to switch).
    /// Each world is an animated map (WorldMapView): level nodes on a winding dotted path over the world's own map art,
    /// gold once cleared (with the rank), a marker that hops to the chosen level, and a detail card with best rank,
    /// time, Star Shards and secrets plus PLAY. Sectioned worlds (The Shattered Realms) colour their realm nodes.
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
            bool hasDescription = !string.IsNullOrEmpty(page.description);
            bool paged = _pages.Count > 1;
            float height = 80f + 150f + 18f + (paged ? UIFactory.RowHeight + 18f : 0f) +
                           (hasDescription ? 90f + 18f : 0f) + MapHeight(n) + 18f + DetailHeight + 18f + UIFactory.RowHeight;
            var panel = UIFactory.Panel(_root, new Vector2(1000f, Mathf.Min(1840f, height)));
            string worldNo = paged ? $"WORLD {_page + 1} · " : "";
            UIFactory.Label(panel, $"STORY QUEST\n{worldNo}{page.displayName.ToUpperInvariant()}", 56, TextAnchor.MiddleCenter, 150f, title: true);
            if (paged) PageRow(panel);
            if (hasDescription)
                UIFactory.Label(panel, page.description, 32, TextAnchor.MiddleCenter, 90f).color = p.subtle;

            // The route is drawn as an animated world map: one node per level on a winding path, a marker that hops
            // to the chosen level, and a card underneath with its details and PLAY (tap a chosen node again to play).
            int rows = (n + MapColumns - 1) / MapColumns;
            var mapRt = UIFactory.Rect("WorldMap", panel);
            mapRt.gameObject.AddComponent<LayoutElement>().preferredHeight = MapHeight(n);
            var bgImg = mapRt.gameObject.AddComponent<Image>();
            bgImg.sprite = MapSprite(page);
            bgImg.color = bgImg.sprite != null ? Color.white : new Color(0.2f, 0.3f, 0.5f, 1f);
            bgImg.raycastTarget = false;
            _map = mapRt.gameObject.AddComponent<WorldMapView>();

            var detail = UIFactory.Rect("LevelDetail", panel);
            detail.gameObject.AddComponent<LayoutElement>().preferredHeight = DetailHeight;
            var dImg = detail.gameObject.AddComponent<Image>();
            UISprites.ApplyPanelSprite(dImg);
            dImg.color = Color.Lerp(p.panel, Color.black, 0.25f);
            var dl = detail.gameObject.AddComponent<HorizontalLayoutGroup>();
            dl.padding = new RectOffset(28, 28, 22, 22);
            dl.spacing = 20f;
            dl.childControlWidth = true; dl.childControlHeight = true;
            dl.childForceExpandWidth = false; dl.childForceExpandHeight = true;
            _detailText = UIFactory.Label(detail, "", 32, TextAnchor.MiddleLeft, DetailHeight - 44f);
            _detailText.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _playButton = UIFactory.Button(detail, "PLAY", () => Play(_selected), DetailHeight - 44f);
            _playButton.GetComponent<LayoutElement>().preferredWidth = 240f;

            var nodes = new RectTransform[n];
            for (int i = 0; i < n; i++)
            {
                var b = Node(mapRt, page, i, rows);
                nodes[i] = (RectTransform)b.transform;
            }
            _map.Init(nodes, Marker(mapRt), SuggestedIndex);

            UIFactory.Button(panel, "BACK", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.CharacterSelect); });
            int start = SuggestedIndex >= 0 ? SuggestedIndex : FirstOpen();
            Choose(start, instant: true);
            if (start >= 0 && start < _cards.Count && _cards[start].interactable) UIFactory.Select(_cards[start]);
        }

        const int MapColumns = 4;
        const float RowStep = 200f, MapPad = 110f, DetailHeight = 220f, NodeSize = 128f;
        WorldMapView _map;
        Text _detailText;
        Button _playButton;
        int _selected = -1;

        static float MapHeight(int n) => ((n + MapColumns - 1) / MapColumns - 1) * RowStep + 2f * MapPad;

        /// <summary>Node centre as a fraction of the map width and a distance down from the top (winding rows).</summary>
        static Vector2 NodePos(int i, int rows)
        {
            int row = i / MapColumns, col = i % MapColumns;
            if (row % 2 == 1) col = MapColumns - 1 - col;
            return new Vector2(0.14f + col * 0.24f, MapPad + row * RowStep);
        }

        static Sprite MapSprite(WorldDefinition page)
        {
            string name = (page.displayName ?? "").ToLowerInvariant();
            string id = name.Contains("starfall") ? "map_starfall" : page.sectioned || name.Contains("realm") ? "map_realms" : "map_sunspire";
            return Resources.Load<Sprite>("PKR/Maps/" + id);
        }

        int FirstOpen()
        {
            for (int i = 0; i < _cards.Count; i++) if (_cards[i].interactable) return i;
            return _cards.Count > 0 ? 0 : -1;
        }

        /// <summary>Picks a level on the map: the marker hops there and the detail card shows it.</summary>
        public void Choose(int index, bool instant = false)
        {
            if (index < 0 || index >= _cards.Count || _pages.Count == 0) return;
            _selected = index;
            var page = _pages[_page];
            var level = page.levels[index];
            var slot = _slots[index];
            string note = page.sectioned && page.sectionNotes != null && index < page.sectionNotes.Count ? page.sectionNotes[index] : null;
            if (_detailText != null)
                _detailText.text = CardText(index, level, slot) + (string.IsNullOrEmpty(note) ? "" : "\n" + note);
            if (_playButton != null) _playButton.interactable = _cards[index].interactable;
            if (_map != null) _map.MoveTo(index, instant);
        }

        /// <summary>The level selected on the map (index into Cards).</summary>
        public int Selected => _selected;

        Button Node(RectTransform map, WorldDefinition page, int i, int rows)
        {
            var p = UITheme.Current;
            var level = page.levels[i];
            var slot = _slots[i];
            int idx = i;
            bool playable = slot.unlocked && level != null && Application.CanStreamedLevelBeLoaded(level.sceneName);
            string label = !slot.unlocked ? $"{i + 1}\nLOCKED" : slot.completed ? $"{i + 1}\n{LevelSelect.RankLabel(slot.rank)}" : $"{i + 1}";
            var node = UIFactory.Button(map, label, () =>
            {
                if (_selected == idx) Play(idx);
                else Choose(idx);
            }, NodeSize, interactable: playable);
            Destroy(node.GetComponent<LayoutElement>());
            var rt = (RectTransform)node.transform;
            var pos = NodePos(i, rows);
            rt.anchorMin = rt.anchorMax = new Vector2(pos.x, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(NodeSize, NodeSize);
            rt.anchoredPosition = new Vector2(0f, -pos.y);
            Color face;
            bool hc = Services.Settings != null && Services.Settings.Data.highContrastUI;
            if (!slot.unlocked) face = p.buttonDisabled;
            else if (page.sectioned && !hc && page.sectionColors != null && i < page.sectionColors.Count) face = page.sectionColors[i];
            else if (slot.completed) face = new Color32(255, 206, 84, 255);
            else face = ModeColor(level != null ? level.modeTag : null, p);
            UIFactory.Tint(node, face, slot.completed && !page.sectioned ? (Color)new Color32(60, 40, 10, 255) : ModeTextColor(level != null ? level.modeTag : null, p));
            var t = node.GetComponentInChildren<Text>();
            t.resizeTextMaxSize = slot.unlocked ? 44 : 22;
            if (i == SuggestedIndex && playable) UIFactory.Highlight(node, p.title);
            // Dotted path back to the previous node, drawn underneath.
            if (i > 0)
            {
                var prev = NodePos(i - 1, rows);
                Dots(map, prev, pos, slot.unlocked);
            }
            _cards.Add(node);
            return node;
        }

        void Dots(RectTransform map, Vector2 a, Vector2 b, bool open)
        {
            float w = map.rect.width > 1f ? map.rect.width : 904f;
            var pa = new Vector2(a.x * w, -a.y);
            var pb = new Vector2(b.x * w, -b.y);
            int count = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(pa, pb) / 34f));
            for (int k = 1; k < count; k++)
            {
                var d = UIFactory.Rect("Dot", map);
                d.SetAsFirstSibling();
                d.anchorMin = d.anchorMax = new Vector2(0f, 1f);
                d.sizeDelta = new Vector2(16f, 16f);
                d.anchoredPosition = Vector2.Lerp(pa, pb, k / (float)count);
                var img = d.gameObject.AddComponent<Image>();
                img.sprite = GlowHalo.Sprite;
                img.color = open ? new Color(1f, 0.95f, 0.7f, 0.95f) : new Color(0.3f, 0.3f, 0.35f, 0.8f);
                img.raycastTarget = false;
            }
        }

        static RectTransform Marker(RectTransform map)
        {
            var m = UIFactory.Rect("Marker", map);
            m.anchorMin = m.anchorMax = new Vector2(0f, 1f);
            m.sizeDelta = new Vector2(64f, 64f);
            var img = m.gameObject.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("PKR/UI/ui_heart");
            img.color = img.sprite != null ? Color.white : new Color(1f, 0.4f, 0.5f);
            img.preserveAspect = true;
            img.raycastTarget = false;
            m.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4f, -4f);
            return m;
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
