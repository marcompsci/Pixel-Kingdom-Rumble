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

        public static Sprite Circle => _circle != null ? _circle : (_circle = Make(false));
        public static Sprite Ring => _ring != null ? _ring : (_ring = Make(true));

        /// <summary>Unity 2022.2+ ships "LegacyRuntime.ttf" as the built-in runtime font.</summary>
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

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
