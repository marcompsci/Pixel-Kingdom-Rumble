using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Falling leaves or snow around the camera (WeatherSim does the maths). Two layers: most flakes behind the level,
    /// a few small ones in front for depth. The count follows the graphics tier and the season's intensity, and
    /// Reduced motion makes it sparse, slow and straight.
    /// </summary>
    public class SeasonWeather : MonoBehaviour
    {
        Camera _cam;
        WeatherSim _sim;
        SpriteRenderer[] _srs;
        Vector3 _lastCam;
        int _tier = -1;
        SeasonState _state;
        Material _mat;
        float _gust;

        public static SeasonWeather Attach(Camera cam, SeasonState state, Material mat)
        {
            var w = cam.gameObject.GetComponent<SeasonWeather>();
            if (w == null) w = cam.gameObject.AddComponent<SeasonWeather>();
            w._cam = cam;
            w._state = state;
            w._mat = mat;
            w.Build();
            return w;
        }

        public static WeatherKind KindFor(SeasonLook look) =>
            look == SeasonLook.Winter ? WeatherKind.Snow : look == SeasonLook.Autumn ? WeatherKind.Leaves : WeatherKind.None;

        void Build()
        {
            if (_srs != null) foreach (var sr in _srs) if (sr != null) Destroy(sr.gameObject);
            var kind = KindFor(_state.Look);
            _tier = (int)PerfMonitor.Tier;
            float intensity = _state.Intensity * (_state.Event == SeasonEvent.Frost ? 0.8f : 1f);
            int count = WeatherSim.CountFor(kind, _tier, intensity, SettingsService.ReducedMotion);
            float h = _cam.orthographicSize * 2f + 2f, w = h * Mathf.Max(1f, _cam.aspect) + 2f;
            _sim = new WeatherSim(kind, count, w, h, 1234, SettingsService.ReducedMotion);
            _srs = new SpriteRenderer[count];
            var root = new GameObject("Weather").transform;
            for (int i = 0; i < count; i++)
            {
                var f = _sim.Flakes[i];
                var go = new GameObject("Flake");
                go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = kind == WeatherKind.Snow ? SeasonArt.Flake(f.variant % 3) : SeasonArt.Leaf(f.variant);
                if (_mat != null) sr.sharedMaterial = _mat;
                bool front = i % 6 == 0;
                sr.sortingOrder = front ? 40 : -60;
                float s = f.size * (front ? 1.25f : 0.9f) * (kind == WeatherKind.Snow ? 1f : 1.1f);
                go.transform.localScale = new Vector3(s, s, 1f);
                sr.color = new Color(1f, 1f, 1f, front ? 0.85f : kind == WeatherKind.Snow ? 0.9f : 0.95f);
                _srs[i] = sr;
            }
            _lastCam = _cam.transform.position;
        }

        void LateUpdate()
        {
            if (_cam == null || _sim == null) return;
            if ((int)PerfMonitor.Tier != _tier) { Build(); return; } // the game stepped its quality up or down
            var c = _cam.transform.position;
            var d = c - _lastCam;
            _lastCam = c;
            float h = _cam.orthographicSize * 2f + 2f, w = h * Mathf.Max(1f, _cam.aspect) + 2f;
            _sim.Resize(w, h);
            if (!SettingsService.ReducedMotion)
            {
                _gust += Time.deltaTime;
                _sim.Wind = (_sim.Kind == WeatherKind.Leaves ? 0.5f : 0.15f) * Mathf.Sin(_gust * 0.35f);
            }
            // a big camera jump (respawn) shouldn't smear every flake across the screen
            if (d.sqrMagnitude > 25f) d = Vector3.zero;
            _sim.Step(Time.deltaTime, d.x, d.y);
            float left = c.x - _sim.Width * 0.5f, bottom = c.y - _sim.Height * 0.5f;
            for (int i = 0; i < _srs.Length; i++)
            {
                var f = _sim.Flakes[i];
                var t = _srs[i].transform;
                t.position = new Vector3(left + f.x, bottom + f.y, 0f);
                if (f.spin != 0f) t.rotation = Quaternion.Euler(0f, 0f, f.angle * Mathf.Rad2Deg);
            }
        }

        void OnDestroy()
        {
            if (_srs == null) return;
            foreach (var sr in _srs) if (sr != null) Destroy(sr.transform.parent != null ? sr.transform.parent.gameObject : sr.gameObject);
            _srs = null;
        }
    }
}
