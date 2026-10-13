using UnityEngine;

namespace PKR
{
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
