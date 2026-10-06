using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Portrait character select: big preview with arrows, name, tagline and lore, a row of hero cards, and
    /// Confirm / Back. Locked heroes show as black silhouettes and can be browsed but not picked.
    /// Confirm saves the choice and opens the scene for the chosen mode.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] CharacterRoster roster;

        RectTransform _root;
        int _index;
        Image _preview;
        Text _name, _tagline, _lore, _status;
        Button _confirm;
        readonly List<RectTransform> _cards = new List<RectTransform>();
        readonly List<Image> _cardImages = new List<Image>();

        static readonly Color Silhouette = new Color(0.05f, 0.05f, 0.08f, 1f);

        /// <summary>For tests and runtime setup; the scene builder assigns the serialized field instead.</summary>
        public void SetRoster(CharacterRoster r) => roster = r;
        public int SelectedIndex => _index;
        public bool ConfirmEnabled => _confirm != null && _confirm.interactable;

        /// <summary>Show hero i (wraps). Used by the arrows and cards.</summary>
        public void Show(int i) => SetIndex(i);

        int Count => roster != null ? roster.heroes.Count : 0;
        CharacterDefinition Current => Count > 0 ? roster.heroes[_index] : null;

        void Awake()
        {
            var safe = UIFactory.CreateCanvas(transform, "SelectCanvas", 10, landscape: false, out _);
            _root = UIFactory.Rect("Select", safe);
            UIFactory.Stretch(_root);
        }

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);
        void OnSettingsChanged(SettingsChanged e) => Build();

        void Start()
        {
            if (roster == null || Count == 0)
            {
                Debug.LogError("[CharacterSelectUI] No roster assigned. Run PKR > Build Menu Scenes.");
                return;
            }
            // Saves from before an unlock rule existed still earn the hero as soon as this screen opens.
            if (Services.Save != null && PKR.Core.UnlockRules.Apply(Services.Save.Data, roster.GetUnlockRules()).Count > 0)
                Services.Save.MarkDirty();
            var ids = new string[Count];
            var selectable = new bool[Count];
            for (int i = 0; i < Count; i++)
            {
                ids[i] = roster.heroes[i] != null ? roster.heroes[i].id : "";
                selectable[i] = roster.IsSelectable(roster.heroes[i]);
            }
            _index = RosterSelection.Initial(selectable, RosterSelection.IndexOf(ids, GameSession.SelectedCharacterId));
            Build();
        }

        void Build()
        {
            if (_root == null || Count == 0) return;
            UIFactory.Clear(_root);
            _cards.Clear();
            _cardImages.Clear();
            var p = UITheme.Current;
            var panel = UIFactory.Panel(_root, new Vector2(1000f, 1780f));

            string modeName = GameSession.Mode == SessionMode.ArenaClash ? "ARENA CLASH" : "STORY QUEST";
            UIFactory.Label(panel, $"{modeName}\nCHOOSE YOUR HERO", 56, TextAnchor.MiddleCenter, 150f, title: true);

            // Preview with arrows.
            var previewRow = UIFactory.Rect("PreviewRow", panel);
            previewRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 620f;
            var h = previewRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            Arrow(previewRow, "<", -1);
            var previewRt = UIFactory.Rect("Preview", previewRow);
            previewRt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _preview = previewRt.gameObject.AddComponent<Image>();
            _preview.preserveAspect = true;
            _preview.raycastTarget = false;
            Arrow(previewRow, ">", +1);

            _name = UIFactory.Label(panel, "", 72, TextAnchor.MiddleCenter, 96f, title: true);
            _tagline = UIFactory.Label(panel, "", 38, TextAnchor.MiddleCenter, 60f);
            _tagline.color = p.subtle;
            _lore = UIFactory.Label(panel, "", 32, TextAnchor.UpperCenter, 190f);
            _status = UIFactory.Label(panel, "", 36, TextAnchor.MiddleCenter, 56f);
            _status.color = p.accent;

            // Hero cards.
            var cardRow = UIFactory.Rect("Cards", panel);
            cardRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 200f;
            var ch = cardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ch.spacing = 20f; ch.childAlignment = TextAnchor.MiddleCenter;
            ch.childControlWidth = true; ch.childControlHeight = true;
            ch.childForceExpandWidth = true; ch.childForceExpandHeight = true;
            for (int i = 0; i < Count; i++)
            {
                int idx = i;
                var hero = roster.heroes[i];
                var card = UIFactory.Button(cardRow, "", () => SetIndex(idx), 200f);
                var cardRt = (RectTransform)card.transform;
                var art = UIFactory.Rect("Art", cardRt);
                UIFactory.Stretch(art);
                art.offsetMin = new Vector2(16f, 16f);
                art.offsetMax = new Vector2(-16f, -16f);
                var img = art.gameObject.AddComponent<Image>();
                img.sprite = hero != null ? hero.bodySprite : null;
                img.preserveAspect = true;
                img.raycastTarget = false;
                _cards.Add(cardRt);
                _cardImages.Add(img);
            }

            // Confirm / Back.
            var buttons = UIFactory.Rect("Buttons", panel);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var bh = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.spacing = 24f;
            bh.childControlWidth = true; bh.childControlHeight = true;
            bh.childForceExpandWidth = true; bh.childForceExpandHeight = true;
            UIFactory.Button(buttons, "BACK", () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); });
            _confirm = UIFactory.Button(buttons, "CONFIRM", Confirm);

            Refresh();
            UIFactory.Select(_confirm);
        }

        void Arrow(RectTransform parent, string label, int dir)
        {
            var b = UIFactory.Button(parent, label, () => SetIndex(RosterSelection.Step(_index, dir, Count)));
            var le = b.GetComponent<LayoutElement>();
            le.preferredWidth = 120f;
            le.flexibleWidth = 0f;
        }

        void SetIndex(int i)
        {
            _index = RosterSelection.Wrap(i, Count);
            Refresh();
        }

        void Refresh()
        {
            var hero = Current;
            if (hero == null) return;
            bool selectable = roster.IsSelectable(hero);

            _preview.sprite = hero.bodySprite;
            _preview.color = selectable ? Color.white : Silhouette;
            _name.text = hero.displayName.ToUpperInvariant();
            _tagline.text = hero.tagline;
            _lore.text = selectable || hero.playableInThisBuild ? hero.lore : "A new hero is on the way to the Sunspire Isles.";
            _status.text = selectable ? "READY"
                : !hero.playableInThisBuild ? "COMING IN A FUTURE UPDATE"
                : string.IsNullOrEmpty(hero.unlockHint) ? "LOCKED" : $"LOCKED: {hero.unlockHint.ToUpperInvariant()}";

            _confirm.interactable = selectable;
            var confirmText = _confirm.GetComponentInChildren<Text>();
            confirmText.text = selectable ? "CONFIRM" : "LOCKED";
            confirmText.color = selectable ? UITheme.Current.buttonText : new Color(1f, 1f, 1f, 0.6f);

            for (int i = 0; i < _cards.Count; i++)
            {
                var h = roster.heroes[i];
                _cardImages[i].color = roster.IsSelectable(h) ? Color.white : Silhouette;
                _cards[i].localScale = i == _index ? Vector3.one * 1.1f : Vector3.one * 0.92f;
            }
        }

        void Confirm()
        {
            var hero = Current;
            if (hero == null || !roster.IsSelectable(hero)) return;
            GameSession.SelectCharacter(hero.id);
            if (Services.Save != null) Services.Save.SaveNow();
            string scene = GameSession.TargetScene;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning($"[CharacterSelectUI] Scene '{scene}' isn't built yet.");
                return;
            }
            if (Services.Scenes != null) Services.Scenes.Load(scene);
        }
    }
}
