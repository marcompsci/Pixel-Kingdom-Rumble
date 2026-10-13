using UnityEngine;

namespace PKR
{
    /// <summary>
    /// One background layer that scrolls slower than the camera (factor 0 = fixed in the world, 1 = glued to the
    /// camera). Four copies of the art sit side by side and leapfrog, so a 20-unit-wide strip covers any level
    /// width and any zoom. A solid strip in the art's bottom colour fills everything below it.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public Sprite sprite;
        public Material material;
        [Range(0f, 1f)] public float factorX = 0.3f;
        [Range(0f, 1f)] public float factorY = 0.2f;
        public int sortingOrder = -80;
        [Tooltip("Colour of the art's bottom row: fills the area below the strip.")]
        public Color baseColor = Color.clear;
        public Color tint = Color.white;

        Transform _cam;
        Vector3 _camStart, _start;
        readonly SpriteRenderer[] _tiles = new SpriteRenderer[4];
        SpriteRenderer _base;
        float _tileW;

        void Start()
        {
            var cam = Camera.main;
            if (cam == null || sprite == null) { enabled = false; return; }
            _cam = cam.transform;
            _camStart = _cam.position;
            _start = transform.position;
            _tileW = sprite.bounds.size.x;
            for (int i = 0; i < _tiles.Length; i++)
            {
                var go = new GameObject("Tile" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tint;
                sr.sortingOrder = sortingOrder;
                if (material != null) sr.sharedMaterial = material;
                _tiles[i] = sr;
            }
            if (baseColor.a > 0f)
            {
                var go = new GameObject("Base");
                go.transform.SetParent(transform, false);
                _base = go.AddComponent<SpriteRenderer>();
                _base.sprite = SolidSprite();
                _base.color = baseColor;
                _base.sortingOrder = sortingOrder;
                if (material != null) _base.sharedMaterial = material;
            }
            LateUpdate();
        }

        static Sprite _solid;
        static Sprite SolidSprite()
        {
            if (_solid != null) return _solid;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            _solid = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 1f), 4f); // 1x1 unit, top pivot
            return _solid;
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            Vector3 c = _cam.position;
            // Where the pattern's origin is now (moves with the camera by factorX), then the copy nearest the camera.
            float origin = _start.x + (c.x - _camStart.x) * factorX;
            float k = Mathf.Floor((c.x - origin) / _tileW);
            float baseX = origin + k * _tileW;
            float y = _start.y + (c.y - _camStart.y) * factorY;
            for (int i = 0; i < _tiles.Length; i++)
                _tiles[i].transform.position = new Vector3(baseX + (i - 1) * _tileW, y, transform.position.z);
            if (_base != null)
            {
                _base.transform.position = new Vector3(c.x, y + 0.02f, transform.position.z);
                _base.transform.localScale = new Vector3(_tileW * 4f, 60f, 1f);
            }
        }
    }

    /// <summary>
    /// Full-screen vertical sky gradient that follows the camera (drawn behind every parallax layer). Built at
    /// runtime from two colours so each level keeps its own sky (sunset, midday, night...).
    /// </summary>
    public class SkyGradient : MonoBehaviour
    {
        public Color top = new Color(0.3f, 0.45f, 0.8f);
        public Color bottom = new Color(0.75f, 0.88f, 1f);
        public int sortingOrder = -100;
        public Material material;
        [Tooltip("Pixel bands in the gradient (retro look). 0 = smooth.")]
        public int bands = 24;

        Camera _cam;
        SpriteRenderer _sr;

        void Start()
        {
            _cam = Camera.main;
            if (_cam == null) { enabled = false; return; }
            int h = 64;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++)
            {
                float t = y / (h - 1f);
                if (bands > 0) t = Mathf.Floor(t * bands) / bands;
                tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            tex.Apply();
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 1f);
            _sr.sortingOrder = sortingOrder;
            if (material != null) _sr.sharedMaterial = material;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (_cam == null || _sr == null) return;
            float hgt = _cam.orthographicSize * 2f + 1f;
            float wid = hgt * Mathf.Max(1f, _cam.aspect) + 1f;
            var p = _cam.transform.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            transform.localScale = new Vector3(wid / 1f, hgt / 64f, 1f);
        }
    }
}
