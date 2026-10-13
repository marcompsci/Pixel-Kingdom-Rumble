using UnityEngine;

namespace PKR
{
    /// <summary>
    /// A soft, pulsing light halo behind a sprite (pickups, portals, crystals). Uses the bilinear-filtered
    /// Resources/PKR/fx_glow sprite so it reads as light over the crisp pixel art, and the bloom post-effect
    /// picks up its bright centre.
    /// </summary>
    public class GlowHalo : MonoBehaviour
    {
        public Color color = new Color(1f, 0.85f, 0.4f, 0.55f);
        [Tooltip("Diameter in world units.")]
        public float size = 1.6f;
        public float pulse = 0.12f;
        public float pulseSpeed = 3f;

        SpriteRenderer _sr;
        float _t;
        static Sprite _sprite;

        public static Sprite Sprite => _sprite != null ? _sprite : (_sprite = Resources.Load<Sprite>("PKR/fx_glow"));

        /// <summary>Adds (or reuses) a halo child under parent, drawn just behind sortingOrder.</summary>
        public static GlowHalo Attach(Transform parent, Color color, float size, int sortingOrder, Material material = null)
        {
            var existing = parent.GetComponentInChildren<GlowHalo>(true);
            var h = existing != null ? existing : new GameObject("Glow").AddComponent<GlowHalo>();
            if (existing == null) h.transform.SetParent(parent, false);
            h.color = color;
            h.size = size;
            h.Setup(sortingOrder, material);
            return h;
        }

        void Setup(int order, Material material)
        {
            if (_sr == null && !TryGetComponent(out _sr)) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = Sprite;
            if (material != null) _sr.sharedMaterial = material;
            _sr.sortingOrder = order - 1;
            _sr.color = color;
            Apply(1f);
        }

        void Awake()
        {
            _t = Random.value * 6f;
            if (_sr == null) Setup(0, null);
        }

        void Apply(float k)
        {
            if (_sr == null || _sr.sprite == null) return;
            float native = Mathf.Max(0.01f, _sr.sprite.bounds.size.x);
            float s = size / native * k;
            transform.localScale = new Vector3(s, s, 1f);
            var c = color;
            c.a = color.a * Mathf.Lerp(0.8f, 1.1f, (k - (1f - pulse)) / Mathf.Max(0.001f, pulse * 2f));
            _sr.color = c;
        }

        void Update()
        {
            _t += Time.deltaTime * pulseSpeed;
            Apply(1f + Mathf.Sin(_t) * pulse);
        }
    }
}
