using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Versus fighter select (portrait): a classic grid of every fighter plus RANDOM. Tap a portrait to pick your
    /// fighter, then the CPU's; the big preview shows the highlighted fighter's name, style and full move list.
    /// CPU level cycles Easy / Normal / Hard. FIGHT! loads the dojo.
    /// </summary>
    public class VersusSelectUI : MonoBehaviour
    {
        public const int Columns = 5;
        static readonly Color Silhouette = new Color(0.05f, 0.05f, 0.08f, 1f);

        [SerializeField] CharacterRoster roster;

        RectTransform _root;
        int _picking;            // 0 = choosing P1, 1 = choosing CPU
        int _highlight;          // index into roster.heroes, or -1 for RANDOM
        string _p1, _cpu;        // chosen ids (_cpu null = random)
        bool _cpuChosen;

        public void SetRoster(CharacterRoster r) => roster = r;
        public string PlayerChoice => _p1;
        public string OpponentChoice => _cpu;
        public bool OpponentChosen => _cpuChosen;
        public int Picking => _picking;

        int Count => roster != null ? roster.heroes.Count : 0;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "VersusSelectCanvas", 10, landscape: false, out _);
            _root = UIFactory.Rect("VersusSelect", safe);
            UIFactory.Stretch(_root);
        }

        void Start()
        {
            GameSession.Mode = SessionMode.Versus;
            if (roster == null || Count == 0) { Debug.LogError("[VersusSelectUI] No roster assigned. Run PKR > Build All Scenes."); return; }
            if (Services.Save != null && UnlockRules.Apply(Services.Save.Data, roster.GetUnlockRules()).Count > 0)
                Services.Save.MarkDirty();
            var current = roster.Find(GameSession.SelectedCharacterId);
            _highlight = current != null && roster.IsSelectable(current) ? roster.heroes.IndexOf(current) : FirstSelectable();
            Build();
        }

        int FirstSelectable()
        {
            for (int i = 0; i < Count; i++) if (roster.IsSelectable(roster.heroes[i])) return i;
            return 0;
        }

        /// <summary>Tap on a grid cell: first tap highlights, a second tap on the same cell picks it.</summary>
        public void Tap(int index)
        {
            if (index >= Count) return;
            if (index >= 0 && !roster.IsSelectable(roster.heroes[index])) { _highlight = index; Build(); return; }
            if (_highlight == index) Pick(index);
            else { _highlight = index; Build(); }
        }

        /// <summary>Choose the fighter at index (-1 = random) for whoever is picking now.</summary>
        public void Pick(int index)
        {
            if (index >= Count || (index >= 0 && !roster.IsSelectable(roster.heroes[index]))) return;
            if (_picking == 0)
            {
                if (index < 0) index = RandomSelectable(null);
                if (index < 0) return;
                _p1 = roster.heroes[index].id;
                _picking = 1;
                _highlight = -1;
            }
            else
            {
                _cpu = index >= 0 ? roster.heroes[index].id : null;
                _cpuChosen = true;
                _highlight = index;
            }
            if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Medium);
            Build();
        }

        int RandomSelectable(string avoid)
        {
            var pool = new List<int>();
            for (int i = 0; i < Count; i++)
                if (roster.IsSelectable(roster.heroes[i]) && roster.heroes[i].id != avoid) pool.Add(i);
            return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : -1;
        }

        public void Back()
        {
            if (_cpuChosen) { _cpuChosen = false; _cpu = null; }
            else if (_picking == 1) { _picking = 0; _highlight = Mathf.Max(0, roster.heroes.FindIndex(h => h.id == _p1)); _p1 = null; }
            else { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); return; }
            Build();
        }

        public void Fight()
        {
            if (string.IsNullOrEmpty(_p1) || !_cpuChosen) return;
            GameSession.Mode = SessionMode.Versus;
            GameSession.SelectCharacter(_p1);
            GameSession.VersusOpponentId = _cpu;
            if (Services.Scenes != null) Services.Scenes.Load(SceneIds.VersusStage);
        }

        void CycleLevel()
        {
            GameSession.VersusBotLevel = GameSession.VersusBotLevel == BotLevel.Easy ? BotLevel.Normal
                                       : GameSession.VersusBotLevel == BotLevel.Normal ? BotLevel.Hard : BotLevel.Easy;
            Build();
        }

        // ---- Layout --------------------------------------------------------------------------------

        void Build()
        {
            if (_root == null || Count == 0) return;
            UIFactory.Clear(_root);
            var p = UITheme.Current;
            var panel = UIFactory.Panel(_root, new Vector2(1000f, 1780f));

            string step = _cpuChosen ? "READY?" : _picking == 0 ? "PLAYER 1: CHOOSE YOUR FIGHTER" : "CHOOSE YOUR OPPONENT";
            UIFactory.Label(panel, "VERSUS", 64, TextAnchor.MiddleCenter, 84f, title: true);
            UIFactory.Label(panel, step, 36, TextAnchor.MiddleCenter, 50f).color = p.accent;

            // Matchup strip: P1 vs CPU.
            var vs = UIFactory.Rect("Matchup", panel);
            vs.gameObject.AddComponent<LayoutElement>().preferredHeight = 330f;
            var hl = vs.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10f; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            var left = _picking == 0 ? HighlightDef() : roster.Find(_p1);
            var right = _picking == 0 ? null : (_cpuChosen ? (string.IsNullOrEmpty(_cpu) ? null : roster.Find(_cpu)) : HighlightDef());
            bool rightRandom = _picking == 1 && ((_cpuChosen && string.IsNullOrEmpty(_cpu)) || (!_cpuChosen && _highlight < 0));
            Portrait(vs, left, false, VersusController.PlayerColor);
            UIFactory.Label(vs, "VS", 80, TextAnchor.MiddleCenter, 330f, title: true).GetComponent<LayoutElement>().preferredWidth = 140f;
            Portrait(vs, right, rightRandom, VersusController.OpponentColor);

            var focus = HighlightDef() ?? (_picking == 0 ? left : right);
            bool focusLocked = focus != null && !roster.IsSelectable(focus);
            UIFactory.Label(panel, focus == null ? "RANDOM" : focusLocked ? "???" : focus.displayName.ToUpperInvariant(),
                            56, TextAnchor.MiddleCenter, 70f, title: true);
            string info = focus == null ? "The CPU picks any fighter."
                        : focusLocked ? (string.IsNullOrEmpty(focus.unlockHint) ? "Locked" : "LOCKED: " + focus.unlockHint.ToUpperInvariant())
                        : focus.tagline;
            UIFactory.Label(panel, info, 30, TextAnchor.MiddleCenter, 50f).color = p.subtle;
            if (focus != null && !focusLocked)
                UIFactory.Label(panel, CodexPanel.HeroStats(focus), 26, TextAnchor.UpperLeft, 300f);
            else UIFactory.Label(panel, "", 26, TextAnchor.UpperLeft, 300f);

            // Fighter grid.
            int cells = Count + 1;
            int rows = (cells + Columns - 1) / Columns;
            var grid = UIFactory.Rect("Grid", panel);
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * 128f + (rows - 1) * 10f;
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(166f, 128f);
            g.spacing = new Vector2(11f, 10f);
            g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = Columns;
            for (int i = 0; i < Count; i++) Cell(grid, i);
            Cell(grid, -1);

            // Bottom row: CPU level, back, fight.
            var row = UIFactory.Rect("Buttons", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
            var bh = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.spacing = 14f; bh.childControlWidth = true; bh.childControlHeight = true;
            bh.childForceExpandWidth = true; bh.childForceExpandHeight = true;
            UIFactory.Button(row, "BACK", Back, 110f).GetComponentInChildren<Text>().fontSize = 36;
            UIFactory.Button(row, $"CPU: {GameSession.VersusBotLevel.ToString().ToUpperInvariant()}", CycleLevel, 110f)
                     .GetComponentInChildren<Text>().fontSize = 36;
            string pickLabel = _cpuChosen ? "FIGHT!" : "PICK";
            var go = UIFactory.Button(row, pickLabel, () => { if (_cpuChosen) Fight(); else Pick(_highlight); }, 110f,
                                      interactable: _cpuChosen || HighlightPickable());
            go.GetComponentInChildren<Text>().fontSize = 40;
            UIFactory.Select(go);
        }

        CharacterDefinition HighlightDef() => _highlight >= 0 && _highlight < Count ? roster.heroes[_highlight] : null;

        bool HighlightPickable() => _highlight < 0 || roster.IsSelectable(roster.heroes[_highlight]);

        void Cell(RectTransform grid, int index)
        {
            var hero = index >= 0 ? roster.heroes[index] : null;
            bool selectable = hero == null || roster.IsSelectable(hero);
            var b = UIFactory.Button(grid, hero == null ? "?" : "", () => Tap(index), 128f);
            var img = b.GetComponent<Image>();
            bool isP1 = hero != null && hero.id == _p1;
            bool isCpu = _cpuChosen && ((hero == null && string.IsNullOrEmpty(_cpu)) || (hero != null && hero.id == _cpu));
            if (index == _highlight) img.color = UITheme.Current.title;
            else if (isP1) img.color = VersusController.PlayerColor;
            else if (isCpu) img.color = VersusController.OpponentColor;
            var label = b.GetComponentInChildren<Text>();
            if (hero == null) { label.fontSize = 64; return; }
            var art = UIFactory.Rect("Art", (RectTransform)b.transform);
            UIFactory.Stretch(art);
            art.offsetMin = new Vector2(10f, 30f);
            art.offsetMax = new Vector2(-10f, -6f);
            var a = art.gameObject.AddComponent<Image>();
            a.sprite = hero.bodySprite;
            a.preserveAspect = true;
            a.raycastTarget = false;
            a.color = selectable ? (roster != null ? roster.TintFor(hero.id) : Color.white) : Silhouette;
            label.text = selectable ? Short(hero.displayName) : "???";
            label.fontSize = 22;
            label.alignment = TextAnchor.LowerCenter;
            label.rectTransform.offsetMin = new Vector2(2f, 2f);
        }

        static string Short(string name)
        {
            var s = name.ToUpperInvariant();
            int space = s.IndexOf(' ');
            return space > 0 && s.Length > 10 ? s.Substring(0, space) : s;
        }

        void Portrait(RectTransform parent, CharacterDefinition def, bool random, Color frame)
        {
            var rt = UIFactory.Rect("Portrait", parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = UISprites.RoundedRect;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(frame.r, frame.g, frame.b, 0.35f);
            bg.raycastTarget = false;
            var art = UIFactory.Rect("Art", rt);
            UIFactory.Stretch(art);
            art.offsetMin = new Vector2(24f, 24f);
            art.offsetMax = new Vector2(-24f, -24f);
            if (def == null)
            {
                var q = UIFactory.Label(art, random ? "?" : "", 160, TextAnchor.MiddleCenter, 280f, title: true);
                UIFactory.Stretch(q.rectTransform);
                return;
            }
            if (frame == VersusController.OpponentColor) art.localScale = new Vector3(-1f, 1f, 1f); // faces player 1
            var img = art.gameObject.AddComponent<Image>();
            img.sprite = def.bodySprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = roster.IsSelectable(def) ? roster.TintFor(def.id) : Silhouette;
        }
    }
}
