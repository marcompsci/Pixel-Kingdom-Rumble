using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Original pixel art for the Kingdom Plaza and the party quests, drawn in code at 32 px per tile with the house
    /// ink outline: quest buildings, item boxes, coins, balloons, the shield bubble, the slick puddle and the crown.
    /// </summary>
    public static class PlazaArt
    {
        public const float PPU = 32f;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        public static readonly Color32 Ink = new Color32(34, 24, 38, 255);

        sealed class Px
        {
            public readonly int W, H;
            public readonly Color32[] C;
            public Px(int w, int h) { W = w; H = h; C = new Color32[w * h]; }
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < W && y < H) C[y * W + x] = c; }
            public Color32 Get(int x, int y) => x >= 0 && y >= 0 && x < W && y < H ? C[y * W + x] : Clear;
            public void Rect(int x0, int y0, int x1, int y1, Color32 c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Set(x, y, c); }
            public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                    }
            }
            public void Tri(int baseY, int topY, float cx, float halfBase, Color32 c)
            {
                for (int y = baseY; y <= topY; y++)
                {
                    float hw = halfBase * (topY - y) / Mathf.Max(1f, topY - baseY);
                    for (int x = Mathf.FloorToInt(cx - hw); x <= Mathf.CeilToInt(cx + hw) - 1; x++) Set(x, y, c);
                }
            }
            public void Outline(Color32 ink)
            {
                var copy = (Color32[])C.Clone();
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        if (copy[y * W + x].a != 0) continue;
                        bool edge = false;
                        if (x + 1 < W && copy[y * W + x + 1].a > 128) edge = true;
                        if (x - 1 >= 0 && copy[y * W + x - 1].a > 128) edge = true;
                        if (y + 1 < H && copy[(y + 1) * W + x].a > 128) edge = true;
                        if (y - 1 >= 0 && copy[(y - 1) * W + x].a > 128) edge = true;
                        if (edge) C[y * W + x] = ink;
                    }
            }
        }

        static Color32 Rgb(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        static Color32 Shade(Color32 c, float k) => new Color32((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), c.a);

        static Sprite Make(string key, Px p, Vector2 pivot)
        {
            var tex = new Texture2D(p.W, p.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "plaza_" + key };
            tex.SetPixels32(p.C);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, p.W, p.H), pivot, PPU, 0, SpriteMeshType.FullRect);
            s.name = "plaza_" + key;
            Cache[key] = s;
            return s;
        }

        static bool Cached(string key, out Sprite s) => Cache.TryGetValue(key, out s) && s != null;

        /// <summary>World colours for the Story world buildings (Sunspire, Frostgear, Shattered Realms, Starfall).</summary>
        public static readonly Color32[] WorldColours = { Rgb(236, 186, 90), Rgb(140, 196, 236), Rgb(176, 120, 210), Rgb(80, 96, 180) };
        /// <summary>Party quest colours (Item Race, Balloon, Ring-Out, Coin Rush, King of the Hill).</summary>
        public static readonly Color32[] PartyColours = { Rgb(230, 80, 70), Rgb(240, 120, 190), Rgb(240, 150, 50), Rgb(250, 210, 70), Rgb(120, 200, 110) };

        /// <summary>A quest building 4 tiles wide and 5 tall: walls, roof, a door, windows and a pennant or an emblem.</summary>
        public static Sprite Building(string key, Color32 wall, bool party)
        {
            if (Cached("bld_" + key, out var s)) return s;
            var p = new Px(132, 168);
            var dark = Shade(wall, 0.72f);
            var roof = party ? Rgb(250, 246, 236) : Shade(wall, 0.5f);
            p.Rect(10, 1, 121, 112, wall);
            for (int y = 8; y < 112; y += 16) for (int x = 10; x <= 121; x++) if ((x / 8 + y / 16) % 2 == 0) p.Set(x, y, dark); // brick rows
            if (party)
            {
                // striped awning roof and pennants
                for (int x = 4; x <= 127; x++)
                    for (int y = 113; y <= 128; y++)
                        p.Set(x, y, (x / 12) % 2 == 0 ? wall : roof);
                p.Rect(4, 113, 127, 114, Shade(wall, 0.55f));
                for (int i = 0; i < 5; i++)
                {
                    int x = 14 + i * 26;
                    p.Rect(x, 129, x + 1, 160, Rgb(120, 90, 60));
                    p.Tri(150, 160, x + 8f, 6f, i % 2 == 0 ? wall : roof);
                }
            }
            else
            {
                p.Tri(113, 160, 66f, 66f, roof); // pointed roof
                p.Ellipse(66f, 128f, 9f, 9f, Rgb(255, 230, 120)); // world emblem
                p.Ellipse(66f, 128f, 5f, 5f, wall);
            }
            // door (arched) and windows
            p.Rect(50, 1, 81, 44, Rgb(70, 44, 36));
            p.Ellipse(66f, 44f, 16f, 10f, Rgb(70, 44, 36));
            p.Rect(54, 1, 77, 40, Rgb(110, 70, 50));
            p.Set(73, 20, Rgb(255, 214, 90)); p.Set(74, 20, Rgb(255, 214, 90));
            foreach (int x in new[] { 20, 94 })
            {
                p.Rect(x, 58, x + 17, 82, Rgb(255, 226, 140));
                p.Rect(x + 8, 58, x + 9, 82, dark);
                p.Rect(x, 69, x + 17, 70, dark);
            }
            p.Outline(Ink);
            return Make("bld_" + key, p, new Vector2(0.5f, 0f));
        }

        public static Sprite ItemBox()
        {
            if (Cached("itembox", out var s)) return s;
            var p = new Px(26, 26);
            var face = Rgb(120, 200, 255, 230);
            p.Rect(2, 2, 23, 23, face);
            p.Rect(2, 2, 23, 3, Rgb(80, 150, 230, 230));
            p.Rect(2, 22, 23, 23, Rgb(190, 235, 255, 230));
            // a star in the middle
            var star = Rgb(255, 230, 90);
            for (int i = 6; i <= 19; i++) { p.Set(12, i, star); p.Set(13, i, star); p.Set(i, 12, star); p.Set(i, 13, star); }
            p.Rect(9, 9, 16, 16, star);
            p.Outline(Ink);
            return Make("itembox", p, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Coin()
        {
            if (Cached("coin", out var s)) return s;
            var p = new Px(16, 16);
            p.Ellipse(8f, 8f, 6.5f, 7f, Rgb(255, 204, 64));
            p.Ellipse(8f, 8f, 4f, 5f, Rgb(240, 170, 40));
            p.Rect(7, 5, 8, 11, Rgb(255, 236, 140));
            p.Outline(Ink);
            return Make("coin", p, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Balloon()
        {
            if (Cached("balloon", out var s)) return s;
            var p = new Px(14, 22);
            p.Ellipse(7f, 14f, 6f, 7f, Rgb(255, 255, 255));
            p.Set(5, 17, Rgb(255, 255, 255, 255));
            p.Set(7, 6, Rgb(255, 255, 255)); p.Set(6, 6, Rgb(255, 255, 255)); p.Set(8, 6, Rgb(255, 255, 255));
            for (int y = 0; y < 6; y++) p.Set(7 + (y % 2), y, Rgb(80, 70, 90));
            p.Outline(Ink);
            p.Set(4, 17, Rgb(255, 255, 255, 255));
            return Make("balloon", p, new Vector2(0.5f, 0f));
        }

        public static Sprite Bubble()
        {
            if (Cached("bubble", out var s)) return s;
            var p = new Px(44, 44);
            for (int y = 0; y < 44; y++)
                for (int x = 0; x < 44; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - 22f) * (x + 0.5f - 22f) + (y + 0.5f - 22f) * (y + 0.5f - 22f));
                    if (d <= 21f && d >= 18.5f) p.Set(x, y, Rgb(150, 230, 255, 220));
                    else if (d < 18.5f) p.Set(x, y, Rgb(150, 230, 255, 46));
                }
            p.Rect(12, 28, 15, 31, Rgb(255, 255, 255, 200));
            return Make("bubble", p, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Puddle()
        {
            if (Cached("puddle", out var s)) return s;
            var p = new Px(40, 10);
            p.Ellipse(20f, 5f, 19f, 4.5f, Rgb(140, 90, 200, 220));
            p.Ellipse(14f, 6f, 6f, 1.5f, Rgb(210, 170, 255, 230));
            p.Outline(Ink);
            return Make("puddle", p, new Vector2(0.5f, 0f));
        }

        public static Sprite Crown()
        {
            if (Cached("crown", out var s)) return s;
            var p = new Px(20, 14);
            var gold = Rgb(255, 204, 64);
            p.Rect(2, 1, 17, 6, gold);
            p.Tri(6, 12, 3f, 2.5f, gold); p.Tri(6, 13, 10f, 2.5f, gold); p.Tri(6, 12, 17f, 2.5f, gold);
            p.Set(10, 3, Rgb(230, 60, 80)); p.Set(5, 3, Rgb(90, 180, 255)); p.Set(15, 3, Rgb(90, 180, 255));
            p.Outline(Ink);
            return Make("crown", p, new Vector2(0.5f, 0f));
        }

        /// <summary>A plain white square (zones, bars, sparks).</summary>
        public static Sprite Solid()
        {
            if (Cached("solid", out var s)) return s;
            var p = new Px(4, 4);
            p.Rect(0, 0, 3, 3, Rgb(255, 255, 255));
            return Make("solid", p, new Vector2(0.5f, 0.5f));
        }
    }
}
