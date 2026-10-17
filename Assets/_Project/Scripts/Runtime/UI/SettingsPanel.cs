using System;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Settings contents, built into any container (pause menu, main menu), on two pages:
    /// SOUND & CONTROLS (volume, haptics, stick, assists, control size/opacity, layout) and DISPLAY & ACCESS
    /// (graphics mode, FPS readout, colour vision, high contrast, screen shake, reduced motion, left-handed controls,
    /// bigger touch targets, sound cues). Rebuilds itself when high contrast changes so colors update.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        RectTransform _container;
        Action _onBack;
        Action _onEditLayout;
        bool _landscape;
        /// <summary>0: sound and controls. 1: display and accessibility.</summary>
        int _page;

        /// <summary>Build into container. onEditLayout may be null (hides that button, e.g. menus without controls).</summary>
        public void Open(RectTransform container, bool landscape, Action onBack, Action onEditLayout)
        {
            _container = container;
            _landscape = landscape;
            _onBack = onBack;
            _onEditLayout = onEditLayout;
            Rebuild();
        }

        void OnEnable() => EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        void OnDisable() => EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);

        bool _lastHighContrast;

        void OnSettingsChanged(SettingsChanged e)
        {
            // Only high contrast changes how the panel looks; sliders update themselves.
            if (_container != null && _container.gameObject.activeInHierarchy && e.settings.highContrastUI != _lastHighContrast)
                Rebuild();
        }

        void Rebuild()
        {
            var settings = Services.Settings;
            if (_container == null || settings == null) return;
            UIFactory.Clear(_container);
            var d = settings.Data;
            _lastHighContrast = d.highContrastUI;

            UIFactory.Dim(_container);
            // Landscape: two columns so everything fits on a phone in one screen. Portrait: one column.
            var panel = UIFactory.Panel(_container, _landscape ? new Vector2(1400f, 1060f) : new Vector2(980f, 1840f));
            UIFactory.Label(panel, "SETTINGS", 60, TextAnchor.MiddleCenter, 70f, title: true);
            // two pages, so each fits one phone screen
            var tabs = UIFactory.Rect("Tabs", panel);
            var tle = tabs.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            tle.preferredHeight = tle.minHeight = 80f;
            var th = tabs.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            th.spacing = 18f;
            th.childControlWidth = true; th.childControlHeight = true;
            th.childForceExpandWidth = true; th.childForceExpandHeight = true;
            var t0 = UIFactory.Button(tabs, "SOUND & CONTROLS", () => { _page = 0; Rebuild(); }, 80f);
            var t1 = UIFactory.Button(tabs, "DISPLAY & ACCESS", () => { _page = 1; Rebuild(); }, 80f);
            UIFactory.Highlight(_page == 0 ? t0 : t1, new Color32(255, 214, 90, 255));

            RectTransform colA = panel, colB = panel;
            if (_landscape)
            {
                var cols = UIFactory.Rect("Columns", panel);
                cols.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 6 * UIFactory.RowHeight + 5 * 18f;   // six rows a column on either page
                var h = cols.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                h.spacing = 48f;
                h.childControlWidth = true; h.childControlHeight = true;
                h.childForceExpandWidth = true; h.childForceExpandHeight = true;
                colA = Column(cols);
                colB = Column(cols);
            }

            if (_page == 1) { DisplayPage(settings, d, colA, colB); return; }

            UIFactory.Slider(colA, "Music", d.musicVolume, 0f, 1f, settings.SetMusicVolume);
            UIFactory.Slider(colA, "Sound FX", d.sfxVolume, 0f, 1f, settings.SetSfxVolume);
            UIFactory.Toggle(colA, "Haptics", d.hapticsEnabled, settings.SetHaptics);
            UIFactory.Toggle(colA, "D-pad (8-way)", d.dpadMode, settings.SetDpadMode);
            UIFactory.Toggle(colA, "Strong aim assist", d.strongAimAssist, settings.SetStrongAimAssist);
            UIFactory.Toggle(colB, "Floating stick", d.floatingJoystick, settings.SetFloatingJoystick);
            UIFactory.Toggle(colA, "Assist mode", d.assistMode, settings.SetAssistMode);
            UIFactory.Slider(colB, "Control opacity", d.controlOpacity, 0.2f, 1f, settings.SetControlOpacity,
                             v => Mathf.RoundToInt(v * 100f) + "%");
            UIFactory.Slider(colB, "Control size", settings.ControlScale,
                             PKR.Core.SettingsData.MinControlScale, PKR.Core.SettingsData.MaxControlScale,
                             settings.SetControlScale, v => v.ToString("0.0") + "x");
            UIFactory.Slider(colB, "Zombie HUD size", d.hudScale, PKR.Core.SettingsData.MinHudScale, PKR.Core.SettingsData.MaxHudScale,
                             settings.SetHudScale, v => Mathf.RoundToInt(v * 100f) + "%");
            // Layout buttons share one row so both columns stay six rows tall.
            var layoutRow = UIFactory.Rect("LayoutButtons", colB);
            var le = layoutRow.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredHeight = le.minHeight = UIFactory.RowHeight;
            var hl = layoutRow.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hl.spacing = 18f;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            if (_onEditLayout != null) UIFactory.Button(layoutRow, "EDIT LAYOUT", () => _onEditLayout());
            UIFactory.Button(layoutRow, "RESET LAYOUT", () => { settings.ResetControlLayout(); Rebuild(); });
            UIFactory.Button(colB, "BACK", () => _onBack?.Invoke());
        }

        /// <summary>Graphics, colour vision, motion, left-handed controls, bigger touch targets, sound cues.</summary>
        void DisplayPage(SettingsService settings, PKR.Core.SettingsData d, RectTransform colA, RectTransform colB)
        {
            var gm = (PKR.Core.GraphicsMode)d.graphicsMode;
            Cycle(colA, "Graphics: " + PKR.Core.PerfGovernor.Name(gm), () =>
            {
                settings.SetGraphicsMode((PKR.Core.GraphicsMode)(((int)gm + 1) % 3));
                Rebuild();
            });
            UIFactory.Toggle(colA, "Show FPS", d.showFps, settings.SetShowFps);
            var ca = (PKR.Core.ColorAssist)d.colorAssist;
            Cycle(colA, "Colour vision: " + PKR.Core.Accessibility.Name(ca), () =>
            {
                settings.SetColorAssist((PKR.Core.ColorAssist)(((int)ca + 1) % 3));
                Rebuild();
            });
            UIFactory.Toggle(colA, "High contrast", d.highContrastUI, settings.SetHighContrast);
            UIFactory.Toggle(colA, "Screen shake", d.screenShakeEnabled, settings.SetScreenShake);
            UIFactory.Toggle(colB, "Reduced motion", d.reducedMotion, settings.SetReducedMotion);
            UIFactory.Toggle(colB, "Left-handed controls", d.leftHanded, settings.SetLeftHanded);
            UIFactory.Toggle(colB, "Bigger touch targets", d.bigTouchTargets, settings.SetBigTouchTargets);
            UIFactory.Toggle(colB, "Sound cues on screen", d.soundCues, settings.SetSoundCues);
            var info = UIFactory.Label(colB, PKR.Core.PerfGovernor.Info(gm) + ".\n" + PKR.Core.Accessibility.Info(ca) + ".", 24, TextAnchor.MiddleCenter, 70f);
            info.color = UITheme.Current.subtle;
            UIFactory.Button(colB, "BACK", () => _onBack?.Invoke());
        }

        static void Cycle(RectTransform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var b = UIFactory.Button(parent, text, onClick);
            var t = b.GetComponentInChildren<UnityEngine.UI.Text>();
            if (t != null) { t.resizeTextForBestFit = true; t.resizeTextMinSize = 20; t.resizeTextMaxSize = 34; }
        }

        static RectTransform Column(RectTransform parent)
        {
            var c = UIFactory.Rect("Column", parent);
            var v = c.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            v.spacing = 18f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return c;
        }
    }
}
