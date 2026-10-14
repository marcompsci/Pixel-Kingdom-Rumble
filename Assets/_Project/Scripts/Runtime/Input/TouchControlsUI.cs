using System;
using System.Collections.Generic;
using PKR.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Builds the landscape touch overlay at runtime (no prefab needed):
    /// safe-area root -> left half (joystick zone) + right half (Jump, Attack, Special, Dodge).
    /// Positions/scales come from SettingsData.controlLayout; high-contrast theme follows settings.
    /// Call SetEditMode(true) to let the player drag controls; positions save on release.
    /// </summary>
    public class TouchControlsUI : MonoBehaviour
    {
        [Tooltip("Show even without a touchscreen (useful in the Editor without Device Simulator).")]
        public bool forceShow;
        public int sortingOrder = 100;

        const float StickSize = 280f;
        const float KnobSize = 120f;
        const float ButtonSize = 170f;

        public VirtualJoystick Joystick { get; private set; }
        public bool EditMode { get; private set; }
        public bool Visible => _canvas != null && _canvas.enabled;

        readonly Dictionary<string, TouchActionButton> _buttons = new Dictionary<string, TouchActionButton>();
        Canvas _canvas;
        RectTransform _leftHalf, _rightHalf;
        Text _tag;
        float _layoutScale = 1f;

        /// <summary>True while this overlay covers only part of the screen (couch play).</summary>
        public bool IsSplit { get; private set; }

        /// <summary>
        /// Couch play on one screen: confine this overlay to [xMin, xMax] of the screen (stick in the left part,
        /// buttons in the right part, same saved layout), shrink it, and label it "P1"/"P2".
        /// SetRegion(0, 1, 1, null) restores the normal full-screen controls.
        /// </summary>
        public void SetRegion(float xMin, float xMax, float scale, string playerTag)
        {
            xMin = Mathf.Clamp01(xMin); xMax = Mathf.Clamp(xMax, xMin + 0.1f, 1f);
            float mid = (xMin + xMax) * 0.5f;
            if (_leftHalf != null) { _leftHalf.anchorMin = new Vector2(xMin, 0f); _leftHalf.anchorMax = new Vector2(mid, 1f); }
            if (_rightHalf != null) { _rightHalf.anchorMin = new Vector2(mid, 0f); _rightHalf.anchorMax = new Vector2(xMax, 1f); }
            _layoutScale = Mathf.Clamp(scale, 0.4f, 1.5f);
            IsSplit = xMin > 0.001f || xMax < 0.999f;
            if (_tag != null)
            {
                _tag.text = playerTag ?? "";
                _tag.enabled = !string.IsNullOrEmpty(playerTag);
                var rt = _tag.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(mid, 0.93f);
                rt.anchoredPosition = Vector2.zero;
            }
            ApplyLayout();
        }

        public Vector2 StickValue => Joystick != null && Visible ? Joystick.Value : Vector2.zero;

        public bool IsHeld(string id) => Visible && _buttons.TryGetValue(id, out var b) && b.Held;

        public bool ConsumePressed(string id) => Visible && _buttons.TryGetValue(id, out var b) && b.ConsumePressed();

        void Awake()
        {
            EnsureEventSystem();
            Build();
            ApplyLayout();
            ApplyTheme();
            RefreshVisibility();
        }

        void OnEnable()
        {
            EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
            EventBus<GameStateChanged>.Subscribe(OnGameStateChanged);
        }

        void OnDisable()
        {
            EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);
            EventBus<GameStateChanged>.Unsubscribe(OnGameStateChanged);
        }

        void OnGameStateChanged(GameStateChanged e) => RefreshVisibility();

        void OnSettingsChanged(SettingsChanged e)
        {
            if (EditMode) return; // don't fight the user's drag
            ApplyLayout();
            ApplyTheme();
        }

        void Update()
        {
            // Touchscreen can appear later (Device Simulator toggled on).
            if (Time.frameCount % 30 == 0) RefreshVisibility();
        }

        void RefreshVisibility()
        {
            // Hidden while paused or on the results screen, unless the player is editing the layout.
            var state = Services.State;
            bool playing = state == null || state.State == GameState.Playing;
            bool show = EditMode || (playing && (forceShow || Touchscreen.current != null || Application.isMobilePlatform));
            if (_canvas != null && _canvas.enabled != show) _canvas.enabled = show;
            if (_editBar != null) _editBar.gameObject.SetActive(EditMode);
        }

        Action _onEditDone;
        RectTransform _editBar;

        /// <summary>Enter layout editing; onDone runs when the player taps DONE (e.g. to reopen the pause menu).</summary>
        public void BeginEdit(Action onDone)
        {
            _onEditDone = onDone;
            SetEditMode(true);
        }

        /// <summary>Leave edit mode and run the BeginEdit callback (same as tapping DONE).</summary>
        public void EndEdit() => FinishEdit();

        void FinishEdit()
        {
            SetEditMode(false);
            ApplyLayout();
            ApplyTheme();
            var cb = _onEditDone;
            _onEditDone = null;
            cb?.Invoke();
        }

        public void SetEditMode(bool on)
        {
            EditMode = on;
            if (Joystick) Joystick.editMode = on;
            foreach (var b in _buttons.Values) b.editMode = on;
            RefreshVisibility();
        }

        public void SetControlScale(string id, float scale)
        {
            var settings = Services.Settings;
            if (settings == null) return;
            var p = settings.Data.GetPlacement(id);
            if (p == null) return;
            p.scale = scale;
            settings.Commit();
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            var data = Services.Settings != null ? Services.Settings.Data : new SettingsData();
            foreach (var p in data.controlLayout)
            {
                var pos = new Vector2(p.x, p.y);
                float scale = p.scale * _layoutScale;
                if (p.controlId == ControlIds.Joystick)
                {
                    if (Joystick) Joystick.SetHome(pos, scale);
                }
                else if (_buttons.TryGetValue(p.controlId, out var b))
                {
                    b.SetHome(pos, scale);
                }
            }
            if (Joystick) { Joystick.floating = data.floatingJoystick; Joystick.eightWay = data.dpadMode; }
        }

        public void ApplyTheme()
        {
            var palette = TouchTheme.Current;
            if (Joystick) Joystick.ApplyTheme(palette);
            foreach (var b in _buttons.Values) b.ApplyTheme(palette);
        }

        void SavePlacement(string id, Vector2 normalized)
        {
            var settings = Services.Settings;
            if (settings == null) return;
            var p = settings.Data.GetPlacement(id);
            if (p == null) return;
            p.x = normalized.x;
            p.y = normalized.y;
            settings.Commit();
        }

        // ---- Construction -----------------------------------------------------------------------

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            // Per-scene (not DontDestroyOnLoad) so it never duplicates a scene's own EventSystem.
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        void Build()
        {
            var canvasGo = new GameObject("TouchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // landscape: keep button size tied to screen height

            var safe = NewRect("SafeArea", canvasGo.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var left = NewRect("LeftHalf", safe);
            left.anchorMin = new Vector2(0f, 0f); left.anchorMax = new Vector2(0.5f, 1f);
            left.offsetMin = left.offsetMax = Vector2.zero;
            var right = NewRect("RightHalf", safe);
            right.anchorMin = new Vector2(0.5f, 0f); right.anchorMax = new Vector2(1f, 1f);
            right.offsetMin = right.offsetMax = Vector2.zero;

            _leftHalf = left;
            _rightHalf = right;
            BuildJoystick(left);
            BuildButton(right, ControlIds.Jump, "JUMP");
            BuildButton(right, ControlIds.Attack, "ATTACK");
            BuildButton(right, ControlIds.Special, "SPECIAL");
            BuildButton(right, ControlIds.Dodge, "DODGE");
            BuildEditBar(safe);

            var tagRt = NewRect("PlayerTag", safe);
            tagRt.sizeDelta = new Vector2(200f, 80f);
            _tag = tagRt.gameObject.AddComponent<Text>();
            _tag.font = UISprites.Font;
            _tag.fontSize = 56;
            _tag.fontStyle = FontStyle.Bold;
            _tag.alignment = TextAnchor.MiddleCenter;
            _tag.color = new Color(1f, 1f, 1f, 0.55f);
            _tag.raycastTarget = false;
            _tag.enabled = false;
        }

        void BuildJoystick(RectTransform leftHalf)
        {
            // The whole left half is the touch zone so a floating stick can start anywhere.
            var zoneImg = leftHalf.gameObject.AddComponent<Image>();
            zoneImg.color = new Color(0f, 0f, 0f, 0f);
            zoneImg.raycastTarget = true;

            bool art = ControlArt.Available && ControlArt.PadBase != null;
            var stickBase = NewRect("StickBase", leftHalf);
            stickBase.sizeDelta = new Vector2(StickSize, StickSize);
            var baseFill = AddImage(stickBase.gameObject, art ? ControlArt.PadBase : UISprites.Circle, false);
            Image direction = null;
            if (art && ControlArt.PadGlow != null)
            {
                var dirRt = NewRect("Direction", stickBase); Stretch(dirRt);
                direction = AddImage(dirRt.gameObject, ControlArt.PadGlow, false);
                direction.color = new Color(1f, 1f, 1f, 0f);
            }
            var ringRt = NewRect("Ring", stickBase); Stretch(ringRt);
            var baseRing = AddImage(ringRt.gameObject, UISprites.Ring, false);

            var knob = NewRect("Knob", stickBase);
            knob.sizeDelta = art ? new Vector2(KnobSize * 1.15f, KnobSize * 1.15f) : new Vector2(KnobSize, KnobSize);
            var knobImg = AddImage(knob.gameObject, art && ControlArt.PadKnob != null ? ControlArt.PadKnob : UISprites.Circle, false);

            Joystick = leftHalf.gameObject.AddComponent<VirtualJoystick>();
            Joystick.zone = leftHalf;
            Joystick.stickBase = stickBase;
            Joystick.knob = knob;
            Joystick.baseFill = baseFill;
            Joystick.baseRing = baseRing;
            Joystick.knobImage = knobImg;
            Joystick.direction = direction;
            Joystick.artStyle = art;
            Joystick.HomeMoved += n => SavePlacement(ControlIds.Joystick, n);
        }

        void BuildButton(RectTransform parent, string id, string text)
        {
            var rt = NewRect("Btn_" + id, parent);
            rt.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            var normal = ControlArt.Button;
            var iconSprite = ControlArt.Icon(id);
            bool art = normal != null && iconSprite != null;
            var fill = AddImage(rt.gameObject, art ? normal : UISprites.Circle, true);

            var ringRt = NewRect("Ring", rt); Stretch(ringRt);
            if (art) { ringRt.offsetMin = new Vector2(-8f, -8f); ringRt.offsetMax = new Vector2(8f, 8f); }
            var ring = AddImage(ringRt.gameObject, UISprites.Ring, false);

            Image icon = null;
            if (art)
            {
                var iconRt = NewRect("Icon", rt);
                iconRt.sizeDelta = new Vector2(ButtonSize * 0.56f, ButtonSize * 0.56f);
                icon = AddImage(iconRt.gameObject, iconSprite, false);
            }

            // Plain circles: the label is the button face. Generated art: a small caption under the icon disc.
            var labelRt = NewRect("Label", rt); Stretch(labelRt);
            if (art)
            {
                labelRt.anchorMin = new Vector2(0f, 0f); labelRt.anchorMax = new Vector2(1f, 0f);
                labelRt.pivot = new Vector2(0.5f, 1f);
                labelRt.sizeDelta = new Vector2(60f, 40f);
                labelRt.anchoredPosition = new Vector2(0f, 2f);
            }
            var label = labelRt.gameObject.AddComponent<Text>();
            label.font = UISprites.Font;
            label.text = text;
            label.fontSize = art ? 26 : 34;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            if (art)
            {
                var sh = labelRt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
                sh.effectDistance = new Vector2(2f, -2f);
            }

            var btn = rt.gameObject.AddComponent<TouchActionButton>();
            btn.controlId = id;
            btn.fill = fill;
            btn.ring = ring;
            btn.label = label;
            btn.icon = icon;
            if (art) { btn.normalSprite = normal; btn.pressedSprite = ControlArt.ButtonPressed; }
            btn.HomeMoved += n => SavePlacement(id, n);
            _buttons[id] = btn;
        }

        void BuildEditBar(RectTransform safe)
        {
            _editBar = NewRect("EditBar", safe);
            _editBar.anchorMin = new Vector2(0.5f, 1f);
            _editBar.anchorMax = new Vector2(0.5f, 1f);
            _editBar.pivot = new Vector2(0.5f, 1f);
            _editBar.anchoredPosition = new Vector2(0f, -130f); // below the HUD timer
            _editBar.sizeDelta = new Vector2(1100f, 250f);
            var bg = _editBar.gameObject.AddComponent<Image>();
            bg.sprite = UISprites.RoundedRect;
            bg.type = Image.Type.Sliced;
            bg.color = UITheme.Current.panel;
            var v = _editBar.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(24, 24, 14, 14);
            v.spacing = 12f;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            UIFactory.Label(_editBar, "Drag the stick and buttons to move them", 40, TextAnchor.MiddleCenter, 60f);
            var row = NewRect("Buttons", _editBar);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UIFactory.RowHeight;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            UIFactory.Button(row, "RESET", () =>
            {
                if (Services.Settings != null) Services.Settings.ResetControlLayout();
                ApplyLayout();
            });
            UIFactory.Button(row, "DONE", FinishEdit);
            _editBar.gameObject.SetActive(false);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Image AddImage(GameObject go, Sprite sprite, bool raycast)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }
    }
}
