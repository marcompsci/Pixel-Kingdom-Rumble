using PKR.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PKR
{
    /// <summary>
    /// Leaves or snow drifting over a menu or the world map while a season is live. Never takes touches. Fewer and
    /// calmer flakes with Reduced motion; none when seasons are off.
    /// </summary>
    public class SeasonWeatherUI : MonoBehaviour
    {
        WeatherSim _sim;
        RectTransform _rt;
        RectTransform[] _flakes;
        const float PixelScale = 3f; // UI px per art pixel

        /// <summary>Adds the overlay as a child filling <paramref name="parent"/>. Returns null out of season.</summary>
        public static SeasonWeatherUI Attach(RectTransform parent, float density = 1f, bool behind = false)
        {
            var s = SettingsService.Season;
            var kind = SeasonWeather.KindFor(s.Look);
            if (parent == null || kind == WeatherKind.None) return null;
            var rt = UIFactory.Rect("SeasonWeather", parent);
            UIFactory.Stretch(rt);
            if (behind) rt.SetAsFirstSibling(); else rt.SetAsLastSibling();
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var w = rt.gameObject.AddComponent<SeasonWeatherUI>();
            w._rt = rt;
            int count = Mathf.RoundToInt(WeatherSim.CountFor(kind, (int)PerfMonitor.Tier, s.Intensity, SettingsService.ReducedMotion) * 0.45f * density);
            w.Build(kind, Mathf.Max(6, count));
            return w;
        }

        void Build(WeatherKind kind, int count)
        {
            var size = _rt.rect.size;
            float width = size.x > 1f ? size.x : 1000f, height = size.y > 1f ? size.y : 1600f;
            // the sim works in "art pixels" so flakes move at a sensible speed on screen
            _sim = new WeatherSim(kind, count, width / 40f, height / 40f, 99, SettingsService.ReducedMotion);
            _flakes = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                var f = _sim.Flakes[i];
                var r = UIFactory.Rect("Flake", _rt);
                r.anchorMin = r.anchorMax = Vector2.zero;
                var img = r.gameObject.AddComponent<Image>();
                img.sprite = kind == WeatherKind.Snow ? SeasonArt.Flake(f.variant % 3) : SeasonArt.Leaf(f.variant);
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, kind == WeatherKind.Snow ? 0.85f : 0.9f);
                var px = img.sprite.rect.size * PixelScale * f.size;
                r.sizeDelta = px;
                _flakes[i] = r;
            }
        }

        void Update()
        {
            if (_sim == null) return;
            var size = _rt.rect.size;
            if (size.x > 1f && size.y > 1f) _sim.Resize(size.x / 40f, size.y / 40f);
            _sim.Step(Time.unscaledDeltaTime);
            float sx = size.x / _sim.Width, sy = size.y / _sim.Height;
            for (int i = 0; i < _flakes.Length; i++)
            {
                var f = _sim.Flakes[i];
                _flakes[i].anchoredPosition = new Vector2(f.x * sx, f.y * sy);
                if (f.spin != 0f) _flakes[i].localRotation = Quaternion.Euler(0f, 0f, f.angle * Mathf.Rad2Deg);
            }
        }
    }
}
