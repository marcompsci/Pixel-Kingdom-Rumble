using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// In-level pause: a pause button in the top-right of the safe area, and a menu with
    /// Resume, Restart, Settings and Main Menu. Opens whenever the game enters the Paused state
    /// (button, Esc/Start, or the app going to the background) and closes on resume.
    /// Settings can hand off to touch-layout editing, then come back here.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public int sortingOrder = 200;

        RectTransform _safe, _menu, _settingsRoot;
        GameObject _pauseButton;
        SettingsPanel _settings;
        bool _editing;
        TouchControlsUI _editTouch;

        void Awake()
        {
            _lastHighContrast = Services.Settings != null && Services.Settings.Data.highContrastUI;
            _safe = UIFactory.CreateCanvas(transform, "PauseCanvas", sortingOrder, landscape: true, out _);
            BuildPauseButton();
            _menu = UIFactory.Rect("Menu", _safe);
            UIFactory.Stretch(_menu);
            _settingsRoot = UIFactory.Rect("Settings", _safe);
            UIFactory.Stretch(_settingsRoot);
            _settings = gameObject.AddComponent<SettingsPanel>();
            Hide();
        }

        void OnEnable()
        {
            EventBus<GameStateChanged>.Subscribe(OnState);
            EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        }

        void OnDisable()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnState);
            EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);
        }

        void Start()
        {
            Refresh();
            if (BootLoader.TakeShotPanel("pause") != null) StartCoroutine(PauseForShot());
        }

        /// <summary>Screenshot runs ("SCENE@pause"): pause once the level is up.</summary>
        System.Collections.IEnumerator PauseForShot()
        {
            yield return new WaitForSecondsRealtime(4f);
            if (Services.State != null) Services.State.SetState(GameState.Paused);
        }

        void OnState(GameStateChanged e) => Refresh();

        bool _lastHighContrast;

        void OnSettingsChanged(SettingsChanged e)
        {
            // Rebuild the button/menu colors only when high contrast flips (settings panel rebuilds itself).
            if (e.settings.highContrastUI == _lastHighContrast) return;
            _lastHighContrast = e.settings.highContrastUI;
            if (_pauseButton != null) { Destroy(_pauseButton); BuildPauseButton(); }
            if (_menu.gameObject.activeSelf) BuildMenu();
            Refresh();
        }

        void Refresh()
        {
            var state = Services.State;
            bool paused = state != null && state.IsPaused;
            bool playing = state == null || state.State == GameState.Playing;
            // Resumed (e.g. Esc) while editing the layout: finish the edit cleanly.
            if (!paused && _editing && _editTouch != null) { _editTouch.EndEdit(); return; }
            _pauseButton.SetActive(playing && !_editing);
            if (!paused)
            {
                Hide();
                return;
            }
            if (_editing) return;
            if (!_menu.gameObject.activeSelf && !_settingsRoot.gameObject.activeSelf) ShowMenu();
        }

        void BuildPauseButton()
        {
            var holder = UIFactory.Rect("PauseButtonHolder", _safe);
            holder.anchorMin = holder.anchorMax = new Vector2(1f, 1f);
            holder.pivot = new Vector2(1f, 1f);
            holder.anchoredPosition = new Vector2(-30f, -24f);
            _pauseButton = holder.gameObject;
            _pauseButton.transform.SetAsFirstSibling(); // keep menus drawn on top of it

            var square = ControlArt.Square;
            var pauseIcon = ControlArt.Icon("pause");
            if (square == null || pauseIcon == null)
            {
                holder.sizeDelta = new Vector2(140f, 110f);
                var v = holder.gameObject.AddComponent<VerticalLayoutGroup>();
                v.childControlWidth = true; v.childControlHeight = true;
                v.childForceExpandWidth = true; v.childForceExpandHeight = true;
                UIFactory.Button(holder, "II", () => { if (Services.State != null) Services.State.Pause(); }, 110f);
                return;
            }

            // Square icon buttons, top right: sound on/off, then pause.
            holder.sizeDelta = new Vector2(250f, 116f);
            var h = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18f;
            h.childAlignment = TextAnchor.UpperRight;
            h.childControlWidth = false; h.childControlHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            bool muted = Services.Settings != null && Services.Settings.Data.muted;
            Image soundIcon = null;
            soundIcon = IconButton(holder, "Sound", square, ControlArt.Icon(muted ? "mute" : "sound"), () =>
            {
                var settings = Services.Settings;
                if (settings == null) return;
                bool m = !settings.Data.muted;
                settings.SetMuted(m);
                var spr = ControlArt.Icon(m ? "mute" : "sound");
                if (soundIcon != null && spr != null) soundIcon.sprite = spr;
            });
            IconButton(holder, "Pause", square, pauseIcon, () => { if (Services.State != null) Services.State.Pause(); });
        }

        static Image IconButton(RectTransform parent, string name, Sprite bg, Sprite icon, UnityEngine.Events.UnityAction onClick)
        {
            var hc = Services.Settings != null && Services.Settings.Data.highContrastUI;
            var rt = UIFactory.Rect("Btn_" + name, parent);
            rt.sizeDelta = new Vector2(110f, 110f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = bg;
            img.color = hc ? new Color(0.55f, 0.55f, 0.55f, 1f) : new Color(1f, 1f, 1f, 0.9f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.pressedColor = new Color(1f, 0.85f, 0.6f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            btn.onClick.AddListener(() => { if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light); });
            var iconRt = UIFactory.Rect("Icon", rt);
            iconRt.sizeDelta = new Vector2(76f, 76f);
            iconRt.anchoredPosition = new Vector2(0f, 3f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.raycastTarget = false;
            iconImg.color = hc ? new Color(1f, 0.92f, 0.1f, 1f) : Color.white;
            return iconImg;
        }

        void ShowMenu()
        {
            _settingsRoot.gameObject.SetActive(false);
            _menu.gameObject.SetActive(true);
            BuildMenu();
        }

        void BuildMenu()
        {
            UIFactory.Clear(_menu);
            UIFactory.Dim(_menu);
            var panel = UIFactory.Panel(_menu, new Vector2(820f, 940f));
            UIFactory.Label(panel, "PAUSED", 72, TextAnchor.MiddleCenter, 110f, title: true);
            var resume = UIFactory.Button(panel, "RESUME", () => { if (Services.State != null) Services.State.Resume(); });
            UIFactory.Button(panel, "RESTART LEVEL", () => { if (Services.Scenes != null) Services.Scenes.ReloadCurrent(); });
            UIFactory.Button(panel, "PHOTO MODE", () => PhotoMode.Begin(null));
            UIFactory.Button(panel, "SETTINGS", ShowSettings);
            bool hasMenu = Application.CanStreamedLevelBeLoaded(SceneIds.MainMenu);
            UIFactory.Button(panel, hasMenu ? "MAIN MENU" : "MAIN MENU (SOON)",
                             () => { if (Services.Scenes != null) Services.Scenes.Load(SceneIds.MainMenu); },
                             interactable: hasMenu);
            UIFactory.Select(resume);
        }

        void ShowSettings()
        {
            _menu.gameObject.SetActive(false);
            _settingsRoot.gameObject.SetActive(true);
            var touch = Object.FindAnyObjectByType<TouchControlsUI>();
            System.Action edit = null;
            if (touch != null) edit = () => BeginLayoutEdit(touch);
            _settings.Open(_settingsRoot, landscape: true, onBack: ShowMenu, onEditLayout: edit);
        }

        void BeginLayoutEdit(TouchControlsUI touch)
        {
            _editing = true;
            _editTouch = touch;
            Hide();
            _pauseButton.SetActive(false);
            touch.BeginEdit(() =>
            {
                _editing = false;
                _editTouch = null;
                Refresh();
                if (Services.State != null && Services.State.IsPaused) ShowSettings();
            });
        }

        void Hide()
        {
            if (_menu != null) _menu.gameObject.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.gameObject.SetActive(false);
        }
    }
}
