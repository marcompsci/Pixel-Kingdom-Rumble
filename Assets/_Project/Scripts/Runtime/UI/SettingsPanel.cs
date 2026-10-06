using System;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Settings contents, built into any container (pause menu now, main menu in increment 6):
    /// music/SFX volume, haptics, screen shake, high contrast, floating joystick, control size,
    /// edit control layout, reset layout. Rebuilds itself when high contrast changes so colors update.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        RectTransform _container;
        Action _onBack;
        Action _onEditLayout;
        bool _landscape;

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
            var panel = UIFactory.Panel(_container, _landscape ? new Vector2(1400f, 820f) : new Vector2(980f, 1500f));
            UIFactory.Label(panel, "SETTINGS", 60, TextAnchor.MiddleCenter, 80f, title: true);

            RectTransform colA = panel, colB = panel;
            if (_landscape)
            {
                var cols = UIFactory.Rect("Columns", panel);
                cols.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 5 * UIFactory.RowHeight + 4 * 18f;
                var h = cols.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                h.spacing = 48f;
                h.childControlWidth = true; h.childControlHeight = true;
                h.childForceExpandWidth = true; h.childForceExpandHeight = true;
                colA = Column(cols);
                colB = Column(cols);
            }

            UIFactory.Slider(colA, "Music", d.musicVolume, 0f, 1f, settings.SetMusicVolume);
            UIFactory.Slider(colA, "Sound FX", d.sfxVolume, 0f, 1f, settings.SetSfxVolume);
            UIFactory.Toggle(colA, "Haptics", d.hapticsEnabled, settings.SetHaptics);
            UIFactory.Toggle(colA, "Screen shake", d.screenShakeEnabled, settings.SetScreenShake);
            UIFactory.Toggle(colA, "High contrast", d.highContrastUI, settings.SetHighContrast);
            UIFactory.Toggle(colB, "Floating stick", d.floatingJoystick, settings.SetFloatingJoystick);
            UIFactory.Slider(colB, "Control size", settings.ControlScale,
                             PKR.Core.SettingsData.MinControlScale, PKR.Core.SettingsData.MaxControlScale,
                             settings.SetControlScale, v => v.ToString("0.0") + "x");
            if (_onEditLayout != null) UIFactory.Button(colB, "EDIT CONTROL LAYOUT", () => _onEditLayout());
            UIFactory.Button(colB, "RESET CONTROL LAYOUT", () => { settings.ResetControlLayout(); Rebuild(); });
            UIFactory.Button(colB, "BACK", () => _onBack?.Invoke());
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
