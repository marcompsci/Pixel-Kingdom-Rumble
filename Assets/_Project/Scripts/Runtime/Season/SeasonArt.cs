using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Original seasonal pixel art drawn in code at the game's density (32 px per tile), with the same dark outline
    /// the rest of the art uses: leaves, snow, pumpkins, hay, snowmen, a lit tree, gifts, a feast table, the season
    /// tokens and the hats. Built once on first use and cached.
    /// </summary>
    public static class SeasonArt
    {
        public const float PPU = 32f;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Ink = new Color32(34, 24, 38, 255);

        // ---- a tiny raster -----------------------------------------------------------------------------------------

        sealed class Px
        {
            public readonly int W, H;
            public readonly Color32[] C;
            public Px(int w, int h) { W = w; H = h; C = new Color32[w * h]; }
            public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
            public void Set(int x, int y, Color32 c) { if (In(x, y)) C[y * W + x] = c; }
            public Color32 Get(int x, int y) => In(x, y) ? C[y * W + x] : Clear;
            public void Rect(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Set(x, y, c);
            }
            public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                    }
            }
            /// <summary>Pixels of the ellipse on its lower-right side get the shade colour (light from the top left).</summary>
            public void ShadeEllipse(float cx, float cy, float rx, float ry, Color32 shade, float amount = 0.45f)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                        if (dx * dx + dy * dy > 1f || Get(x, y).a == 0) continue;
                        if (dx * 0.6f - dy * 0.8f > 1f - amount * 2f) Set(x, y, shade);
                    }
            }
            public void Tri(int baseY, int topY, float cx, float halfBase, Color32 c)
            {
                for (int y = baseY; y <= topY; y++)
                {
                    float k = (topY - y) / (float)Mathf.Max(1, topY - baseY);
                    float hw = halfBase * k;
                    for (int x = Mathf.FloorToInt(cx - hw); x <= Mathf.CeilToInt(cx + hw) - 1; x++) Set(x, y, c);
                }
            }
            /// <summary>A 1 px dark outline around every opaque pixel (the art style's ink line).</summary>
            public void Outline(Color32 ink)
            {
                var copy = (Color32[])C.Clone();
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        if (copy[y * W + x].a != 0) continue;
                        bool edge = false;
                        for (int k = 0; k < 4 && !edge; k++)
                        {
                            int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                            if (nx >= 0 && ny >= 0 && nx < W && ny < H && copy[ny * W + nx].a > 128) edge = true;
                        }
                        if (edge) C[y * W + x] = ink;
                    }
            }
        }

        static Sprite Make(string key, Px p, Vector2 pivot, bool tiled = false, FilterMode filter = FilterMode.Point)
        {
            var tex = new Texture2D(p.W, p.H, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                name = "season_" + key,
            };
            tex.SetPixels32(p.C);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, p.W, p.H), pivot, PPU, 0, SpriteMeshType.FullRect);
            s.name = "season_" + key;
            Cache[key] = s;
            return s;
        }

        static bool Cached(string key, out Sprite s) => Cache.TryGetValue(key, out s) && s != null;

        static Color32 Rgb(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        // ---- palettes ----------------------------------------------------------------------------------------------

        public static readonly Color32[] LeafColours =
        {
            Rgb(214, 74, 42), Rgb(236, 142, 46), Rgb(242, 196, 70), Rgb(170, 58, 48),
        };

        public static readonly Color32[] BulbColours =
        {
            Rgb(255, 86, 86), Rgb(255, 214, 80), Rgb(96, 220, 120), Rgb(90, 170, 255), Rgb(255, 140, 220),
        };

        // ---- weather -----------------------------------------------------------------------------------------------

        public static Sprite Leaf(int variant)
        {
            variant = Mathf.Abs(variant) % LeafColours.Length;
            string key = "leaf" + variant;
            if (Cached(key, out var s)) return s;
            var c = LeafColours[variant];
            var dark = Rgb(c.r * 3 / 4, c.g * 3 / 4, c.b * 3 / 4);
            string[] rows =
            {
                "...x...",
                "..xxx..",
                ".xxvxx.",
                "xxxvxxx",
                ".xxvxx.",
                "..xvx..",
                "...s...",
            };
            var p = new Px(9, 9);
            for (int r = 0; r < rows.Length; r++)
                for (int x = 0; x < rows[r].Length; x++)
                {
                    char ch = rows[r][x];
                    int y = 8 - 1 - r;
                    if (ch == 'x') p.Set(x + 1, y, c);
                    else if (ch == 'v') p.Set(x + 1, y, dark);
                    else if (ch == 's') p.Set(x + 1, y, Rgb(110, 70, 40));
                }
            return Make(key, p, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Flake(int variant)
        {
            variant = Mathf.Abs(variant) % 3;
            string key = "flake" + variant;
            if (Cached(key, out var s)) return s;
            var w = Rgb(255, 255, 255);
            var soft = Rgb(220, 236, 255, 200);
            Px p;
            if (variant == 0) { p = new Px(2, 2); p.Rect(0, 0, 1, 1, w); }
            else if (variant == 1) { p = new Px(3, 3); p.Set(1, 0, soft); p.Set(0, 1, soft); p.Set(1, 1, w); p.Set(2, 1, soft); p.Set(1, 2, soft); }
            else
            {
                p = new Px(5, 5);
                for (int i = 0; i < 5; i++) { p.Set(2, i, soft); p.Set(i, 2, soft); }
                p.Set(0, 0, soft); p.Set(4, 4, soft); p.Set(0, 4, soft); p.Set(4, 0, soft);
                p.Set(2, 2, w);
            }
            return Make(key, p, new Vector2(0.5f, 0.5f));
        }

        // ---- ground dressing ---------------------------------------------------------------------------------------

        /// <summary>Snow lying on a ledge: a bumpy white top, blue shade underneath. Tiles sideways.</summary>
        public static Sprite SnowCap()
        {
            if (Cached("snowcap", out var s)) return s;
            var p = new Px(32, 12);
            var white = Rgb(246, 250, 255);
            var shade = Rgb(196, 214, 240);
            var deep = Rgb(160, 182, 222);
            for (int x = 0; x < 32; x++)
            {
                int top = 8 + Mathf.RoundToInt(1.4f * Mathf.Sin(x * Mathf.PI * 2f / 32f) + 0.8f * Mathf.Sin(x * Mathf.PI * 6f / 32f + 1f));
                int bottom = 2 + ((x * 7) % 5 == 0 ? -1 : 0) + ((x * 3) % 7 == 0 ? -1 : 0);
                for (int y = bottom; y <= top; y++) p.Set(x, y, y <= bottom + 1 ? deep : y <= bottom + 3 ? shade : white);
                p.Set(x, top + 1, Rgb(255, 255, 255, 140));
            }
            return Make("snowcap", p, new Vector2(0.5f, 0.5f), tiled: true);
        }

        /// <summary>Fallen leaves along a ledge (mostly transparent). Tiles sideways.</summary>
        public static Sprite LeafLitter()
        {
            if (Cached("litter", out var s)) return s;
            var p = new Px(32, 8);
            var rng = new System.Random(11);
            for (int i = 0; i < 26; i++)
            {
                int x = rng.Next(0, 32), y = rng.Next(1, 4);
                var c = LeafColours[rng.Next(0, LeafColours.Length)];
                p.Set(x, y, c); p.Set((x + 1) % 32, y, c);
                if (rng.Next(0, 2) == 0) p.Set(x, y + 1, c);
            }
            return Make("litter", p, new Vector2(0.5f, 0.5f), tiled: true);
        }

        public static Sprite Pumpkin(bool lantern)
        {
            string key = lantern ? "lantern" : "pumpkin";
            if (Cached(key, out var s)) return s;
            var p = new Px(24, 20);
            var orange = Rgb(236, 126, 40);
            var rib = Rgb(200, 92, 30);
            p.Ellipse(12f, 8f, 10.5f, 7.5f, orange);
            p.ShadeEllipse(12f, 8f, 10.5f, 7.5f, rib, 0.3f);
            for (int y = 2; y < 15; y++) { if (p.Get(8, y).a > 0) p.Set(8, y, rib); if (p.Get(15, y).a > 0) p.Set(15, y, rib); }
            p.Set(9, 12, Rgb(255, 170, 80)); p.Set(10, 13, Rgb(255, 170, 80));
            p.Rect(11, 15, 12, 18, Rgb(96, 120, 50));
            p.Set(13, 18, Rgb(96, 120, 50)); p.Set(14, 18, Rgb(120, 150, 60));
            if (lantern)
            {
                var glow = Rgb(255, 228, 110);
                p.Tri(9, 11, 7.5f, 2f, glow); p.Tri(9, 11, 16.5f, 2f, glow);
                p.Rect(6, 4, 17, 5, glow); p.Set(8, 6, glow); p.Set(15, 6, glow); p.Set(12, 3, glow);
                p.Set(9, 5, orange); p.Set(14, 5, orange);
            }
            p.Outline(Ink);
            return Make(key, p, new Vector2(0.5f, 0f));
        }

        public static Sprite HayBale()
        {
            if (Cached("hay", out var s)) return s;
            var p = new Px(30, 20);
            var straw = Rgb(222, 184, 92);
            var dark = Rgb(184, 142, 64);
            p.Rect(1, 1, 28, 17, straw);
            for (int y = 3; y < 17; y += 3) for (int x = 2; x < 28; x++) if ((x + y) % 5 != 0) p.Set(x, y, dark);
            p.Rect(8, 1, 9, 17, Rgb(150, 90, 50)); p.Rect(20, 1, 21, 17, Rgb(150, 90, 50));
            p.Rect(1, 17, 28, 17, Rgb(240, 210, 130));
            p.Outline(Ink);
            return Make("hay", p, new Vector2(0.5f, 0f));
        }

        public static Sprite CornBundle()
        {
            if (Cached("corn", out var s)) return s;
            var p = new Px(20, 44);
            var stalk = Rgb(214, 182, 104);
            var dry = Rgb(176, 140, 70);
            for (int i = 0; i < 5; i++)
            {
                int x = 5 + i * 2;
                int top = 34 + (i % 2) * 4;
                for (int y = 1; y < top; y++) p.Set(x + (y > 24 ? (i - 2) * (y - 24) / 6 : 0), y, i % 2 == 0 ? stalk : dry);
            }
            for (int y = 18; y < 32; y += 5) { p.Set(3, y, stalk); p.Set(2, y + 1, stalk); p.Set(16, y + 2, dry); p.Set(17, y + 3, dry); }
            p.Rect(4, 14, 15, 15, Rgb(150, 90, 50));
            p.Outline(Ink);
            return Make("corn", p, new Vector2(0.5f, 0f));
        }

        public static Sprite Snowman()
        {
            if (Cached("snowman", out var s)) return s;
            var p = new Px(26, 38);
            var snow = Rgb(244, 248, 255);
            var shade = Rgb(196, 214, 240);
            p.Ellipse(13f, 9f, 9f, 8.5f, snow); p.ShadeEllipse(13f, 9f, 9f, 8.5f, shade, 0.35f);
            p.Ellipse(13f, 23f, 6.5f, 6.5f, snow); p.ShadeEllipse(13f, 23f, 6.5f, 6.5f, shade, 0.35f);
            p.Rect(8, 17, 18, 18, Rgb(214, 60, 60)); p.Rect(16, 12, 17, 17, Rgb(214, 60, 60)); // scarf
            p.Set(11, 25, Ink); p.Set(15, 25, Ink);
            p.Rect(13, 22, 16, 22, Rgb(240, 130, 40)); // carrot nose
            p.Set(13, 11, Ink); p.Set(13, 8, Ink); p.Set(13, 5, Ink);
            p.Rect(8, 29, 18, 30, Rgb(60, 50, 70)); p.Rect(10, 31, 16, 35, Rgb(60, 50, 70)); // hat
            p.Rect(10, 31, 16, 31, Rgb(90, 160, 110));
            for (int i = 0; i < 4; i++) { p.Set(3 - i / 2, 20 + i, Rgb(110, 70, 40)); p.Set(22 + i / 2, 20 + i, Rgb(110, 70, 40)); }
            p.Outline(Ink);
            return Make("snowman", p, new Vector2(0.5f, 0f));
        }

        /// <summary>A pine tree for the goal. Bulb spots are listed by PineBulbs so they can twinkle as separate sprites.</summary>
        public static Sprite Pine()
        {
            if (Cached("pine", out var s)) return s;
            var p = new Px(44, 64);
            var g1 = Rgb(46, 120, 76); var g2 = Rgb(34, 92, 62); var snow = Rgb(240, 246, 255);
            p.Rect(19, 1, 24, 8, Rgb(110, 70, 40));
            p.Tri(8, 30, 21.5f, 20f, g2); p.Tri(10, 30, 21.5f, 17f, g1);
            p.Tri(24, 46, 21.5f, 16f, g2); p.Tri(26, 46, 21.5f, 13f, g1);
            p.Tri(38, 58, 21.5f, 11f, g2); p.Tri(40, 58, 21.5f, 8f, g1);
            for (int x = 4; x < 40; x += 3) { if (p.Get(x, 9).a > 0) p.Set(x, 9, snow); }
            for (int x = 8; x < 36; x += 3) { if (p.Get(x, 25).a > 0) p.Set(x, 25, snow); }
            p.Outline(Ink);
            return Make("pine", p, new Vector2(0.5f, 0f));
        }

        /// <summary>Where the lights hang on the pine (pixels from its bottom-left).</summary>
        public static readonly Vector2Int[] PineBulbs =
        {
            new Vector2Int(10, 14), new Vector2Int(18, 18), new Vector2Int(27, 13), new Vector2Int(34, 16),
            new Vector2Int(14, 31), new Vector2Int(23, 28), new Vector2Int(30, 33),
            new Vector2Int(17, 44), new Vector2Int(26, 42), new Vector2Int(21, 52),
        };

        public static Sprite Star()
        {
            if (Cached("star", out var s)) return s;
            var p = new Px(11, 11);
            var y = Rgb(255, 224, 90);
            for (int i = 0; i < 11; i++) { p.Set(5, i, y); p.Set(i, 5, y); }
            p.Rect(3, 3, 7, 7, y);
            p.Set(5, 5, Rgb(255, 255, 230));
            p.Outline(Ink);
            return Make("star", p, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Bulb()
        {
            if (Cached("bulb", out var s)) return s;
            var p = new Px(5, 6);
            var w = Rgb(255, 255, 255);
            p.Rect(1, 0, 3, 3, w); p.Set(2, 4, Rgb(80, 80, 90)); p.Set(1, 2, Rgb(255, 255, 255));
            return Make("bulb", p, new Vector2(0.5f, 0.8f));
        }

        public static Sprite Gift(int variant)
        {
            variant = Mathf.Abs(variant) % 3;
            string key = "gift" + variant;
            if (Cached(key, out var s)) return s;
            Color32[] box = { Rgb(214, 60, 70), Rgb(70, 150, 220), Rgb(90, 180, 110) };
            Color32[] ribbon = { Rgb(255, 214, 80), Rgb(255, 255, 255), Rgb(214, 60, 70) };
            var p = new Px(16, 15);
            p.Rect(1, 1, 14, 10, box[variant]);
            p.Rect(0, 9, 15, 11, box[variant]);
            p.Rect(7, 1, 8, 11, ribbon[variant]); p.Rect(0, 10, 15, 10, ribbon[variant]);
            p.Set(5, 12, ribbon[variant]); p.Set(6, 13, ribbon[variant]); p.Set(9, 13, ribbon[variant]); p.Set(10, 12, ribbon[variant]);
            p.Outline(Ink);
            return Make(key, p, new Vector2(0.5f, 0f));
        }

        public static Sprite Wreath()
        {
            if (Cached("wreath", out var s)) return s;
            var p = new Px(22, 22);
            p.Ellipse(11f, 11f, 9.5f, 9.5f, Rgb(46, 120, 76));
            p.Ellipse(11f, 11f, 5f, 5f, Clear);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                p.Set(11 + Mathf.RoundToInt(Mathf.Cos(a) * 7f), 11 + Mathf.RoundToInt(Mathf.Sin(a) * 7f), Rgb(220, 50, 60));
            }
            p.Rect(8, 2, 13, 4, Rgb(220, 50, 60)); p.Set(7, 1, Rgb(220, 50, 60)); p.Set(14, 1, Rgb(220, 50, 60));
            p.Outline(Ink);
            return Make("wreath", p, new Vector2(0.5f, 0.5f));
        }

        public static Sprite FeastTable()
        {
            if (Cached("feast", out var s)) return s;
            var p = new Px(46, 30);
            var wood = Rgb(140, 90, 52);
            p.Rect(4, 1, 6, 12, wood); p.Rect(39, 1, 41, 12, wood);
            p.Rect(1, 12, 44, 15, Rgb(236, 226, 200)); // cloth
            for (int x = 1; x <= 44; x += 4) p.Set(x, 11, Rgb(236, 226, 200));
            p.Ellipse(14f, 19f, 7f, 3.6f, Rgb(214, 150, 80)); // pie
            p.Ellipse(14f, 19.5f, 5.5f, 2.4f, Rgb(198, 110, 50));
            p.Rect(25, 16, 29, 22, Rgb(196, 132, 70)); p.Rect(26, 23, 28, 24, Rgb(236, 226, 200)); // bread loaf
            p.Ellipse(36f, 19.5f, 4.5f, 3.5f, Rgb(236, 126, 40)); p.Set(36, 23, Rgb(96, 120, 50));
            p.Outline(Ink);
            return Make("feast", p, new Vector2(0.5f, 0f));
        }

        // ---- tokens ------------------------------------------------------------------------------------------------

        public static Sprite Token(PKR.Core.SeasonEvent ev)
        {
            string key = "token" + (int)ev;
            if (Cached(key, out var s)) return s;
            var p = new Px(22, 22);
            switch (ev)
            {
                case PKR.Core.SeasonEvent.Harvest:
                    p.Ellipse(11f, 8.5f, 6.5f, 7.5f, Rgb(244, 188, 60));
                    p.ShadeEllipse(11f, 8.5f, 6.5f, 7.5f, Rgb(206, 140, 40), 0.35f);
                    p.Ellipse(11f, 15f, 8.5f, 4f, Rgb(140, 92, 52));
                    for (int x = 4; x < 19; x += 2) p.Set(x, 15, Rgb(110, 70, 40));
                    p.Rect(10, 18, 11, 20, Rgb(110, 70, 40));
                    p.Set(8, 9, Rgb(255, 240, 170)); p.Set(8, 10, Rgb(255, 240, 170));
                    break;
                case PKR.Core.SeasonEvent.Winterfest:
                    p.Rect(3, 2, 18, 13, Rgb(214, 50, 64));
                    p.Rect(2, 12, 19, 15, Rgb(214, 50, 64));
                    p.Rect(10, 2, 11, 15, Rgb(255, 214, 80)); p.Rect(2, 13, 19, 13, Rgb(255, 214, 80));
                    p.Set(7, 16, Rgb(255, 214, 80)); p.Set(8, 17, Rgb(255, 214, 80)); p.Set(13, 17, Rgb(255, 214, 80)); p.Set(14, 16, Rgb(255, 214, 80));
                    p.Set(9, 16, Rgb(255, 214, 80)); p.Set(12, 16, Rgb(255, 214, 80));
                    break;
                case PKR.Core.SeasonEvent.Frost:
                    var ice = Rgb(170, 230, 255); var bright = Rgb(240, 252, 255);
                    for (int i = 2; i < 20; i++) { p.Set(11, i, ice); p.Set(i, 11, ice); }
                    for (int i = 4; i < 18; i++) { p.Set(i, i, ice); p.Set(i, 22 - i, ice); }
                    for (int k = -1; k <= 1; k += 2) { p.Set(11 + k, 4, ice); p.Set(11 + k, 18, ice); p.Set(4, 11 + k, ice); p.Set(18, 11 + k, ice); }
                    p.Rect(10, 10, 12, 12, bright);
                    break;
                default: // Autumn: a big maple leaf
                    var red = Rgb(226, 74, 40); var gold = Rgb(246, 170, 50);
                    p.Tri(6, 20, 11f, 6f, red); p.Tri(6, 15, 5f, 4f, red); p.Tri(6, 15, 17f, 4f, red);
                    p.Rect(4, 6, 18, 9, red);
                    for (int y = 4; y < 19; y++) p.Set(11, y, gold);
                    p.Rect(11, 1, 11, 5, Rgb(110, 70, 40));
                    break;
            }
            p.Outline(Ink);
            return Make(key, p, new Vector2(0.5f, 0.5f));
        }

        // ---- hats --------------------------------------------------------------------------------------------------

        public enum Hat { LeafCrown, Beanie, HarvestScarf, Earmuffs }

        public static Sprite HatSprite(Hat hat)
        {
            string key = "hat" + (int)hat;
            if (Cached(key, out var s)) return s;
            Px p;
            switch (hat)
            {
                case Hat.Beanie:
                    p = new Px(20, 16);
                    p.Ellipse(10f, 2f, 8.5f, 9f, Rgb(214, 50, 64));
                    p.Rect(0, 0, 19, 0, Clear);
                    p.Rect(1, 1, 18, 3, Rgb(246, 246, 246));
                    for (int x = 3; x < 17; x += 3) p.Set(x, 6, Rgb(180, 36, 50));
                    p.Ellipse(10f, 11.5f, 2.5f, 2.2f, Rgb(250, 250, 250));
                    p.Set(14, 3, Rgb(46, 120, 76)); p.Set(15, 4, Rgb(46, 120, 76)); p.Set(16, 3, Rgb(220, 50, 60)); // holly
                    break;
                case Hat.HarvestScarf:
                {
                    // a wrap around the neck with one knitted tail and a fringe
                    p = new Px(24, 20);
                    var o = Rgb(236, 126, 40); var o2 = Rgb(196, 88, 30); var cream = Rgb(250, 222, 160);
                    p.Ellipse(12f, 14f, 10.5f, 4.5f, o);
                    p.Ellipse(12f, 15f, 7f, 2.2f, Clear);
                    for (int x = 3; x < 22; x += 4) for (int y = 10; y < 19; y++) if (p.Get(x, y).a > 0) p.Set(x, y, o2);
                    p.Rect(14, 3, 18, 11, o);
                    p.Rect(14, 5, 18, 5, cream); p.Rect(14, 8, 18, 8, cream);
                    for (int x = 14; x <= 18; x += 2) { p.Set(x, 2, o2); p.Set(x, 1, o2); }
                    break;
                }
                case Hat.Earmuffs:
                    p = new Px(24, 12);
                    var band = Rgb(150, 170, 200); var fluff = Rgb(200, 236, 255);
                    for (int x = 4; x < 20; x++) p.Set(x, 10 - (int)(Mathf.Abs(x - 11.5f) > 5 ? 1 : 0), band);
                    p.Ellipse(3.5f, 5f, 3.5f, 4.5f, fluff); p.Ellipse(20.5f, 5f, 3.5f, 4.5f, fluff);
                    break;
                default: // leaf crown
                    p = new Px(20, 9);
                    for (int i = 0; i < 5; i++)
                    {
                        var c = LeafColours[i % LeafColours.Length];
                        int x = 2 + i * 4;
                        p.Rect(x, 1, x + 2, 4, c); p.Set(x + 1, 5, c); p.Set(x + 1, 6, c);
                    }
                    p.Rect(1, 1, 18, 1, Rgb(110, 70, 40));
                    break;
            }
            p.Outline(Ink);
            return Make(key, p, new Vector2(0.5f, 0f));
        }

        public static Hat? KeepsakeHat(string keepsakeId) => keepsakeId switch
        {
            "keepsake_leaf_crown" => Hat.LeafCrown,
            "keepsake_harvest_scarf" => Hat.HarvestScarf,
            "keepsake_holly_beanie" => Hat.Beanie,
            "keepsake_frost_earmuffs" => Hat.Earmuffs,
            _ => (Hat?)null,
        };

        /// <summary>A 1x1 white pixel (wires between bulbs, firework sparks).</summary>
        public static Sprite Dot()
        {
            if (Cached("dot", out var s)) return s;
            var p = new Px(2, 2);
            p.Rect(0, 0, 1, 1, Rgb(255, 255, 255));
            return Make("dot", p, new Vector2(0.5f, 0.5f));
        }
    }
}
