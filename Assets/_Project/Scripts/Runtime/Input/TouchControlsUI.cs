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

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);

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
            bool show = forceShow || EditMode || Touchscreen.current != null || Application.isMobilePlatform;
            if (_canvas != null && _canvas.enabled != show) _canvas.enabled = show;
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
                if (p.controlId == ControlIds.Joystick)
                {
                    if (Joystick) Joystick.SetHome(pos, p.scale);
                }
                else if (_buttons.TryGetValue(p.controlId, out var b))
                {
                    b.SetHome(pos, p.scale);
                }
            }
            if (Joystick) Joystick.floating = data.floatingJoystick;
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
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
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

            BuildJoystick(left);
            BuildButton(right, ControlIds.Jump, "JUMP");
            BuildButton(right, ControlIds.Attack, "ATK");
            BuildButton(right, ControlIds.Special, "SPL");
            BuildButton(right, ControlIds.Dodge, "DODGE");
        }

        void BuildJoystick(RectTransform leftHalf)
        {
            // The whole left half is the touch zone so a floating stick can start anywhere.
            var zoneImg = leftHalf.gameObject.AddComponent<Image>();
            zoneImg.color = new Color(0f, 0f, 0f, 0f);
            zoneImg.raycastTarget = true;

            var stickBase = NewRect("StickBase", leftHalf);
            stickBase.sizeDelta = new Vector2(StickSize, StickSize);
            var baseFill = AddImage(stickBase.gameObject, UISprites.Circle, false);
            var ringRt = NewRect("Ring", stickBase); Stretch(ringRt);
            var baseRing = AddImage(ringRt.gameObject, UISprites.Ring, false);

            var knob = NewRect("Knob", stickBase);
            knob.sizeDelta = new Vector2(KnobSize, KnobSize);
            var knobImg = AddImage(knob.gameObject, UISprites.Circle, false);

            Joystick = leftHalf.gameObject.AddComponent<VirtualJoystick>();
            Joystick.zone = leftHalf;
            Joystick.stickBase = stickBase;
            Joystick.knob = knob;
            Joystick.baseFill = baseFill;
            Joystick.baseRing = baseRing;
            Joystick.knobImage = knobImg;
            Joystick.HomeMoved += n => SavePlacement(ControlIds.Joystick, n);
        }

        void BuildButton(RectTransform parent, string id, string text)
        {
            var rt = NewRect("Btn_" + id, parent);
            rt.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            var fill = AddImage(rt.gameObject, UISprites.Circle, true);

            var ringRt = NewRect("Ring", rt); Stretch(ringRt);
            var ring = AddImage(ringRt.gameObject, UISprites.Ring, false);

            var labelRt = NewRect("Label", rt); Stretch(labelRt);
            var label = labelRt.gameObject.AddComponent<Text>();
            label.font = UISprites.Font;
            label.text = text;
            label.fontSize = 34;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            var btn = rt.gameObject.AddComponent<TouchActionButton>();
            btn.controlId = id;
            btn.fill = fill;
            btn.ring = ring;
            btn.label = label;
            btn.HomeMoved += n => SavePlacement(id, n);
            _buttons[id] = btn;
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
