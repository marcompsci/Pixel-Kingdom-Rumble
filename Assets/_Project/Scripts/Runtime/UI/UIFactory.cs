using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>Menu colors for normal and high-contrast modes.</summary>
    public static class UITheme
    {
        public struct Palette
        {
            public Color dim, panel, panelEdge, title, text, subtle, button, buttonText, buttonDisabled, accent, track;
        }

        public static readonly Palette Normal = new Palette
        {
            dim = new Color(0.04f, 0.04f, 0.1f, 0.72f),
            panel = new Color32(26, 30, 56, 245),
            panelEdge = new Color32(236, 186, 74, 255),
            title = new Color32(255, 214, 120, 255),
            text = Color.white,
            subtle = new Color(1f, 1f, 1f, 0.7f),
            button = new Color32(255, 196, 90, 255),
            buttonText = new Color32(30, 24, 40, 255),
            buttonDisabled = new Color32(90, 92, 110, 255),
            accent = new Color32(46, 220, 190, 255),
            track = new Color32(60, 66, 100, 255)
        };

        public static readonly Palette HighContrast = new Palette
        {
            dim = new Color(0f, 0f, 0f, 0.9f),
            panel = Color.black,
            panelEdge = Color.white,
            title = new Color(1f, 0.92f, 0.1f),
            text = new Color(1f, 0.92f, 0.1f),
            subtle = Color.white,
            button = new Color(1f, 0.92f, 0.1f),
            buttonText = Color.black,
            buttonDisabled = new Color(0.35f, 0.35f, 0.35f),
            accent = Color.white,
            track = new Color(0.3f, 0.3f, 0.3f)
        };

        public static Palette Current =>
            Services.Settings != null && Services.Settings.Data.highContrastUI ? HighContrast : Normal;
    }

    /// <summary>
    /// Builds simple, touch-sized uGUI controls in code (no prefabs): canvases with a safe area, panels with a
    /// vertical layout, buttons, ON/OFF toggles, sliders and labels. Everything uses the current UITheme.
    /// Minimum touch target: 96 px tall at the reference resolution.
    /// </summary>
    public static class UIFactory
    {
        public const float RowHeight = 104f;

        /// <summary>Screen-space overlay canvas sized for landscape (1920x1080) or portrait (1080x1920) reference.</summary>
        public static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder, bool landscape, out Canvas canvas)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = landscape ? new Vector2(1920f, 1080f) : new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = landscape ? 1f : 0f;

            var safe = Rect("SafeArea", go.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return safe;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Full-screen dark layer that also blocks touches to whatever is underneath.</summary>
        public static Image Dim(RectTransform parent)
        {
            var rt = Rect("Dim", parent);
            Stretch(rt);
            // Extend past the safe area to the real screen edges.
            rt.offsetMin = new Vector2(-400f, -400f);
            rt.offsetMax = new Vector2(400f, 400f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UITheme.Current.dim;
            img.raycastTarget = true;
            return img;
        }

        /// <summary>Centered rounded panel with a vertical layout for rows.</summary>
        public static RectTransform Panel(RectTransform parent, Vector2 size)
        {
            var p = UITheme.Current;
            var edge = Rect("PanelEdge", parent);
            edge.sizeDelta = size + new Vector2(12f, 12f);
            var edgeImg = edge.gameObject.AddComponent<Image>();
            edgeImg.sprite = UISprites.RoundedRect;
            edgeImg.type = Image.Type.Sliced;
            edgeImg.color = p.panelEdge;

            var rt = Rect("Panel", edge);
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.RoundedRect;
            img.type = Image.Type.Sliced;
            img.color = p.panel;

            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 40);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rt;
        }

        public static Text Label(RectTransform parent, string text, int size, TextAnchor align = TextAnchor.MiddleCenter,
                                 float height = 70f, bool title = false)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UISprites.Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = title ? UITheme.Current.title : UITheme.Current.text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return t;
        }

        public static Button Button(RectTransform parent, string text, UnityAction onClick, float height = RowHeight,
                                    bool interactable = true)
        {
            var p = UITheme.Current;
            var rt = Rect("Button_" + text, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.RoundedRect;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = p.button;
            colors.highlightedColor = p.button;
            colors.selectedColor = p.button;
            colors.pressedColor = Color.Lerp(p.button, Color.white, 0.4f);
            colors.disabledColor = p.buttonDisabled;
            colors.colorMultiplier = 1f;
            btn.colors = colors;
            btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(onClick);
            btn.onClick.AddListener(() => { if (Services.Haptics != null) Services.Haptics.Play(HapticStrength.Light); });

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var labelRt = Rect("Text", rt);
            Stretch(labelRt);
            var t = labelRt.gameObject.AddComponent<Text>();
            t.font = UISprites.Font;
            t.text = text;
            t.fontSize = 44;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = interactable ? p.buttonText : new Color(1f, 1f, 1f, 0.6f);
            t.raycastTarget = false;
            return btn;
        }

        /// <summary>A button that reads "Label: ON/OFF" and flips on tap (large and unambiguous on phones).</summary>
        public static Button Toggle(RectTransform parent, string label, bool value, Action<bool> onChanged)
        {
            bool state = value;
            Button b = null;
            b = Button(parent, $"{label}: {(state ? "ON" : "OFF")}", () =>
            {
                state = !state;
                b.GetComponentInChildren<Text>().text = $"{label}: {(state ? "ON" : "OFF")}";
                onChanged?.Invoke(state);
            });
            return b;
        }

        /// <summary>Label on the left, slider on the right. Value range min..max; onChanged fires on release-free drag.</summary>
        public static Slider Slider(RectTransform parent, string label, float value, float min, float max,
                                    Action<float> onChanged, Func<float, string> format = null)
        {
            var p = UITheme.Current;
            var row = Rect("Row_" + label, parent);
            var rowLe = row.gameObject.AddComponent<LayoutElement>();
            rowLe.preferredHeight = RowHeight;
            rowLe.minHeight = RowHeight;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 24f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var text = Label(row, "", 40, TextAnchor.MiddleLeft, RowHeight);
            var textLe = text.GetComponent<LayoutElement>();
            textLe.preferredWidth = 360f;
            textLe.flexibleWidth = 0f;
            format = format ?? (v => Mathf.RoundToInt(v * 100f) + "%");
            void SetText(float v) => text.text = $"{label}  {format(v)}";
            SetText(value);

            var sliderRt = Rect("Slider", row);
            var sliderLe = sliderRt.gameObject.AddComponent<LayoutElement>();
            sliderLe.flexibleWidth = 1f;

            var bg = Rect("Background", sliderRt);
            bg.anchorMin = new Vector2(0f, 0.38f); bg.anchorMax = new Vector2(1f, 0.62f);
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = UISprites.RoundedRect; bgImg.type = Image.Type.Sliced; bgImg.color = p.track;

            var fillArea = Rect("Fill Area", sliderRt);
            fillArea.anchorMin = new Vector2(0f, 0.38f); fillArea.anchorMax = new Vector2(1f, 0.62f);
            fillArea.offsetMin = new Vector2(30f, 0f); fillArea.offsetMax = new Vector2(-30f, 0f); // match the handle inset
            var fill = Rect("Fill", fillArea);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = UISprites.RoundedRect; fillImg.type = Image.Type.Sliced; fillImg.color = p.accent;

            var handleArea = Rect("Handle Slide Area", sliderRt);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(30f, 0f); handleArea.offsetMax = new Vector2(-30f, 0f);
            var handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(64f, 0f);
            handle.anchorMin = new Vector2(0f, 0.1f); handle.anchorMax = new Vector2(0f, 0.9f);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = UISprites.Circle; handleImg.color = p.button;
            handleImg.preserveAspect = true;

            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
            slider.onValueChanged.AddListener(v =>
            {
                SetText(v);
                onChanged?.Invoke(v);
            });
            return slider;
        }

        /// <summary>Give keyboard/gamepad navigation a starting point when a menu opens.</summary>
        public static void Select(Selectable s)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && s != null) es.SetSelectedGameObject(s.gameObject);
        }

        /// <summary>Destroy all children (to rebuild a panel's contents).</summary>
        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
