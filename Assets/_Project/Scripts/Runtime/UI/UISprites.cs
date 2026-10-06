using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Runtime-generated placeholder UI shapes (filled circle, ring) and the built-in font,
    /// so touch controls work without any imported art. Clearly placeholder; swap for real sprites later.
    /// </summary>
    public static class UISprites
    {
        const int Size = 128;
        static Sprite _circle, _ring;
        static Font _font;

        static Sprite _rounded, _check;

        public static Sprite Circle => _circle != null ? _circle : (_circle = Make(false));

        /// <summary>White rounded rectangle with a 12 px 9-slice border, for panels, buttons and slider tracks.</summary>
        public static Sprite RoundedRect => _rounded != null ? _rounded : (_rounded = MakeRounded());

        /// <summary>White check mark for toggles.</summary>
        public static Sprite Check => _check != null ? _check : (_check = MakeCheck());
        public static Sprite Ring => _ring != null ? _ring : (_ring = Make(true));

        /// <summary>Unity 2022.2+ ships "LegacyRuntime.ttf" as the built-in runtime font.</summary>
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        static Sprite MakeRounded()
        {
            const int n = 32, r = 10;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "PKR_UI_Rounded", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // Distance outside the inner rectangle [r, n-1-r] gives an anti-aliased rounded corner.
                float dx = Mathf.Max(r - x, 0, x - (n - 1 - r));
                float dy = Mathf.Max(r - y, 0, y - (n - 1 - r));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(255f * Mathf.Clamp01(r - d + 0.5f));
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        }

        static Sprite MakeCheck()
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "PKR_UI_Check", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[n * n];
            Vector2 a = new Vector2(6, 16), b = new Vector2(13, 8), c = new Vector2(26, 24);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2(x, y);
                float d = Mathf.Min(DistToSegment(p, a, b), DistToSegment(p, b, c));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(3.2f - d)));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static Sprite Make(bool ring)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = ring ? "PKR_UI_Ring" : "PKR_UI_Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[Size * Size];
            float r = Size * 0.5f - 1f;
            float inner = ring ? r - 7f : -1f;
            Vector2 c = new Vector2(Size * 0.5f - 0.5f, Size * 0.5f - 0.5f);
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float outerA = Mathf.Clamp01(r - d + 0.5f);          // anti-aliased outer edge
                float innerA = ring ? Mathf.Clamp01(d - inner + 0.5f) : 1f;
                byte a = (byte)(255f * outerA * innerA);
                px[y * Size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
