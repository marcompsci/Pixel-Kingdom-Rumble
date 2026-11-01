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

    /// <summary>New Year: small firework bursts in the sky behind the level now and then.</summary>
    public class SeasonFireworks : MonoBehaviour
    {
        Camera _cam;
        Material _mat;
        float _next = 1.5f;
        readonly SpriteRenderer[] _sparks = new SpriteRenderer[18];
        readonly Vector2[] _vel = new Vector2[18];
        float _age = 99f;
        Vector2 _origin;

        public void Init(Material mat)
        {
            _cam = GetComponent<Camera>();
            _mat = mat;
            var root = new GameObject("Fireworks").transform;
            for (int i = 0; i < _sparks.Length; i++)
            {
                var sr = new GameObject("Spark").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root, false);
                sr.sprite = SeasonArt.Dot();
                if (mat != null) sr.sharedMaterial = mat;
                sr.sortingOrder = -65;
                sr.transform.localScale = Vector3.one * 1.6f;
                sr.enabled = false;
                _sparks[i] = sr;
            }
        }

        void Update()
        {
            if (_cam == null) return;
            float dt = Time.deltaTime;
            _next -= dt;
            if (_next <= 0f)
            {
                _next = 2.6f + Random.value * 2.2f;
                var c = _cam.transform.position;
                float h = _cam.orthographicSize;
                _origin = new Vector2(c.x + Random.Range(-h, h) * _cam.aspect * 0.8f, c.y + h * Random.Range(0.35f, 0.75f));
                var col = (Color)SeasonArt.BulbColours[Random.Range(0, SeasonArt.BulbColours.Length)];
                for (int i = 0; i < _sparks.Length; i++)
                {
                    float a = i * Mathf.PI * 2f / _sparks.Length;
                    _vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.2f, 2.8f);
                    _sparks[i].color = col;
                    _sparks[i].enabled = true;
                    _sparks[i].transform.position = _origin;
                }
                _age = 0f;
            }
            if (_age > 1.3f) return;
            _age += dt;
            float alpha = 1f - Ease.Out(_age / 1.3f);
            for (int i = 0; i < _sparks.Length; i++)
            {
                _vel[i] += new Vector2(0f, -2.2f) * dt;
                _vel[i] *= 1f - 1.6f * dt;
                var t = _sparks[i].transform;
                t.position += (Vector3)(_vel[i] * dt);
                var c = _sparks[i].color; c.a = alpha; _sparks[i].color = c;
                if (_age > 1.3f) _sparks[i].enabled = false;
            }
        }
    }
}
