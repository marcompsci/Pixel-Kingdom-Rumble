using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// Generates ORIGINAL placeholder pixel sprites as PNGs (16 px per unit, point filtered).
    /// Every file is prefixed "ph_" and lives in Art/Placeholder so it is obvious what to replace.
    /// Existing files are left alone, so you can hand-edit or swap them freely.
    /// </summary>
    public static class PlaceholderArt
    {
        public const string Folder = "Assets/_Project/Art/Placeholder/Sprites";
        public const int PPU = 16;

        // Palette: warm, high-contrast for small screens.
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 GrassLight = new Color32(126, 214, 84, 255);
        static readonly Color32 GrassDark = new Color32(72, 160, 64, 255);
        static readonly Color32 Soil = new Color32(176, 112, 64, 255);
        static readonly Color32 SoilDark = new Color32(132, 78, 46, 255);
        static readonly Color32 Stone = new Color32(120, 128, 168, 255);
        static readonly Color32 StoneLight = new Color32(168, 178, 214, 255);
        static readonly Color32 StoneDark = new Color32(78, 84, 120, 255);
        static readonly Color32 Plank = new Color32(214, 150, 82, 255);
        static readonly Color32 PlankDark = new Color32(150, 96, 52, 255);
        static readonly Color32 Brass = new Color32(236, 186, 74, 255);
        // Nova: sky courier. Teal coat, coral scarf, pale face, comet-gold trim.
        static readonly Color32 NovaCoat = new Color32(46, 168, 178, 255);
        static readonly Color32 NovaCoatDark = new Color32(28, 112, 130, 255);
        static readonly Color32 NovaScarf = new Color32(255, 112, 96, 255);
        static readonly Color32 NovaSkin = new Color32(255, 214, 170, 255);
        static readonly Color32 NovaHair = new Color32(64, 40, 96, 255);
        static readonly Color32 Ink = new Color32(30, 24, 40, 255);

        public static Sprite Ground() => GetOrCreate("ph_ground", 16, 16, false, (x, y) =>
        {
            if (y >= 12) return ((x + y) % 5 == 0) ? GrassDark : GrassLight;       // grass cap
            if (y == 11) return (x % 3 == 0) ? GrassDark : SoilDark;                // grass roots
            return ((x * 7 + y * 3) % 11 == 0) ? SoilDark : Soil;                   // speckled soil
        });

        public static Sprite StoneBlock() => GetOrCreate("ph_stone", 16, 16, false, (x, y) =>
        {
            if (y == 15 || x == 0) return StoneLight;
            if (y == 0 || x == 15) return StoneDark;
            if (y == 8 || (y > 8 ? x == 7 : x == 11)) return StoneDark;              // brick seams
            return Stone;
        });

        public static Sprite OneWay() => GetOrCreate("ph_oneway", 16, 16, false, (x, y) =>
        {
            if (y < 11) return Clear;
            if (y == 15) return Brass;                                              // brass edge = "you can stand here"
            return (x == 0 || x == 8 || y == 11) ? PlankDark : Plank;
        });

        public static Sprite Nova() => GetOrCreate("ph_nova", 16, 24, true, NovaPixel);

        static readonly Color32 Straw = new Color32(232, 196, 112, 255);
        static readonly Color32 StrawDark = new Color32(190, 150, 74, 255);
        static readonly Color32 Ring = new Color32(214, 64, 64, 255);
        static readonly Color32 Post = new Color32(140, 92, 54, 255);

        /// <summary>Training dummy: a wooden post with a round straw target. 16x24, bottom pivot.</summary>
        public static Sprite Dummy() => GetOrCreate("ph_dummy", 16, 24, true, (x, y) =>
        {
            // Base plank
            if (y <= 1) return (x >= 3 && x <= 12) ? PlankDark : Clear;
            // Post
            if (y <= 9) return (x >= 7 && x <= 8) ? Post : Clear;
            // Round straw target centered at (7.5, 16), radius 6.5, with red rings
            float dx = x - 7.5f, dy = y - 16f;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d > 6.5f) return (x >= 7 && x <= 8 && y <= 11) ? Post : Clear;
            if (d > 5.6f) return StrawDark;
            if ((d > 3.4f && d <= 4.4f) || d <= 1.3f) return Ring;
            return ((x + y) % 4 == 0) ? StrawDark : Straw;
        });

        static Color32 NovaPixel(int x, int y)
        {
            // 16x24, facing right. Built from simple shapes; replace with production art later.
            // Boots
            if (y <= 1) return (x >= 4 && x <= 6) || (x >= 9 && x <= 11) ? Ink : Clear;
            // Legs
            if (y <= 5) return (x >= 5 && x <= 6) || (x >= 9 && x <= 10) ? NovaCoatDark : Clear;
            // Coat body with flared hem
            if (y <= 13)
            {
                int half = y <= 7 ? 6 : 5;
                if (x < 8 - half || x > 7 + half) return Clear;
                if (y == 13 && x >= 4 && x <= 11) return Brass;                       // belt
                return (x == 8 - half || x == 7 + half) ? NovaCoatDark : NovaCoat;
            }
            // Scarf, trailing behind (left) like a comet tail
            if (y <= 15)
            {
                if (x >= 4 && x <= 11) return NovaScarf;
                if (x >= 1 && x <= 3 && y == 15 - (3 - x) / 2) return NovaScarf;
                return Clear;
            }
            // Head
            if (y <= 21)
            {
                if (x < 5 || x > 11) return Clear;
                if (y >= 20) return NovaHair;
                if (x == 10 && y == 18) return Ink;                                   // eye
                if (x == 5) return NovaHair;
                return NovaSkin;
            }
            // Hair tuft + star clip
            if (y <= 23)
            {
                if (x == 11 && y == 22) return Brass;
                return (x >= 5 && x <= 10) ? NovaHair : Clear;
            }
            return Clear;
        }

        /// <summary>Creates (once) a PNG sprite from a pixel function and imports it as a point-filtered sprite.</summary>
        public static Sprite GetOrCreate(string name, int w, int h, bool bottomPivot, Func<int, int, Color32> pixel)
        {
            EditorUtil.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x, y);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = PPU;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // required for SpriteDrawMode.Tiled
                settings.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError($"[PKR] Failed to load generated sprite {path}");
            return sprite;
        }
    }
}
