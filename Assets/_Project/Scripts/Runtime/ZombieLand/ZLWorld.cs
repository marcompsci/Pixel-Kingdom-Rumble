using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PKR
{
    /// <summary>Sorting for the top-down city: ground, decals, things sorted by height on screen, roofs, canopies.</summary>
    public static class ZLSort
    {
        public const int Ground = -32000, Decal = -31000, Roof = 30000, Canopy = 31000, Overhead = 32000;
        /// <summary>Lower on screen draws in front.</summary>
        public static int ByY(float y) => 10000 - Mathf.RoundToInt(y * 20f);
    }

    /// <summary>
    /// Builds New Hollow at runtime from city.json: a Tilemap for the ground, roof sprites (9-sliced) with a wall face
    /// for every building, landmark art, props, and colliders for buildings, water, the fence and solid props.
    /// Everything is generated, so the scene file stays tiny.
    /// </summary>
    public class ZLWorld : MonoBehaviour
    {
        public ZLCityData City { get; private set; }
        public Material material;
        readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        readonly Dictionary<string, Tile> _tiles = new Dictionary<string, Tile>();
        Transform _root;

        public Sprite Sprite(string path)
        {
            if (_sprites.TryGetValue(path, out var s)) return s;
            s = Resources.Load<Sprite>("PKR/ZombieLand/" + path);
            _sprites[path] = s;
            return s;
        }

        public bool Build()
        {
            City = ZLCityData.Load();
            if (City == null) { Debug.LogError("[ZombieLand] city.json missing (run Tools/art/build_all.py zombieland)"); return false; }
            _root = new GameObject("City").transform;
            _root.SetParent(transform, false);
            Ground();
            Colliders();
            Buildings();
            Props();
            return true;
        }

        // ---- ground --------------------------------------------------------------------------------------

        Tile TileFor(char c, int x, int y)
        {
            string key;
            int v = ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;
            switch (c)
            {
                case 'r': key = "r" + (v % 7 == 0 ? 1 : v % 11 == 0 ? 2 : 0); break;
                case 'h': key = "h0"; break;
                case 'v': key = "v0"; break;
                case 'c': key = "c0"; break;
                case 'C': key = "C0"; break;
                case 's': key = "s" + (v % 2); break;
                case 'g': key = "g" + (v % 3); break;
                case 'y': key = "y" + (v % 2); break;
                case 'p': key = "p0"; break;
                case 'o': key = "o" + (v % 2); break;
                case 'i': key = "i" + (v % 5 == 0 ? 1 : 0); break;
                case 'd': key = "d0"; break;
                case 'w': key = "w" + (v % 2); break;
                case 'b': key = "b0"; break;
                case 'f': key = "f0"; break;
                case 'l': key = "l" + (x % 4 == 0 ? 1 : 0); break;
                case 'x': key = "x0"; break;
                default: key = "B0"; break;
            }
            if (_tiles.TryGetValue(key, out var t)) return t;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = Sprite("Tiles/zl_" + key);
            t.colliderType = Tile.ColliderType.None;
            _tiles[key] = t;
            return t;
        }

        void Ground()
        {
            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.transform.SetParent(_root, false);
            var tmGo = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            tmGo.transform.SetParent(gridGo.transform, false);
            var tm = tmGo.GetComponent<Tilemap>();
            var tr = tmGo.GetComponent<TilemapRenderer>();
            tr.sortingOrder = ZLSort.Ground;
            tr.mode = TilemapRenderer.Mode.Chunk;
            if (material != null) tr.sharedMaterial = material;
            var c = City;
            var positions = new Vector3Int[c.w * c.h];
            var tiles = new TileBase[c.w * c.h];
            int i = 0;
            for (int y = 0; y < c.h; y++)
                for (int x = 0; x < c.w; x++)
                {
                    positions[i] = new Vector3Int(x, y, 0);
                    tiles[i] = TileFor(c.Tile(x, y), x, y);
                    i++;
                }
            tm.SetTiles(positions, tiles);
            // the sea and the land beyond the fence
            var outside = new GameObject("Outside").AddComponent<SpriteRenderer>();
            outside.transform.SetParent(_root, false);
            outside.sprite = Sprite("Tiles/zl_w0");
            outside.drawMode = SpriteDrawMode.Tiled;
            outside.size = new Vector2(c.w + 80, c.h + 80);
            outside.transform.position = new Vector3(c.w * 0.5f, c.h * 0.5f, 0f);
            outside.sortingOrder = ZLSort.Ground - 1;
            if (material != null) outside.sharedMaterial = material;
        }

        // ---- colliders -----------------------------------------------------------------------------------

        void Colliders()
        {
            var c = City;
            var solids = new GameObject("Solids");
            solids.transform.SetParent(_root, false);
            // Merge solid tiles (water, fence, building footprints) into rectangles row by row.
            var used = new bool[c.w, c.h];
            for (int y = 0; y < c.h; y++)
                for (int x = 0; x < c.w; x++)
                {
                    char t = c.Tile(x, y);
                    if (used[x, y] || !ZLCityData.SolidTile(t)) continue;
                    int x1 = x;
                    while (x1 + 1 < c.w && !used[x1 + 1, y] && ZLCityData.SolidTile(c.Tile(x1 + 1, y))) x1++;
                    int y1 = y;
                    while (y1 + 1 < c.h)
                    {
                        bool ok = true;
                        for (int xx = x; xx <= x1 && ok; xx++) ok = !used[xx, y1 + 1] && ZLCityData.SolidTile(c.Tile(xx, y1 + 1));
                        if (!ok) break;
                        y1++;
                    }
                    for (int yy = y; yy <= y1; yy++) for (int xx = x; xx <= x1; xx++) used[xx, yy] = true;
                    var box = solids.AddComponent<BoxCollider2D>();
                    box.offset = new Vector2((x + x1 + 1) * 0.5f, (y + y1 + 1) * 0.5f);
                    box.size = new Vector2(x1 - x + 1, y1 - y + 1);
                }
            // walls around the whole map
            var edge = solids.AddComponent<EdgeCollider2D>();
            edge.points = new[] { new Vector2(0, 0), new Vector2(c.w, 0), new Vector2(c.w, c.h), new Vector2(0, c.h), new Vector2(0, 0) };
        }

        // ---- buildings -------------------------------------------------------------------------------------

        void Buildings()
        {
            var parent = new GameObject("Buildings").transform;
            parent.SetParent(_root, false);
            int seed = 0;
            foreach (var b in City.buildings)
            {
                seed++;
                var center = new Vector2(b.x + b.w * 0.5f, b.y + b.h * 0.5f);
                var lm = Sprite("Landmarks/lm_" + b.style);
                if (lm != null)
                {
                    var sr = MakeSprite("Landmark_" + b.style, lm, center, parent, ZLSort.Roof);
                    sr.drawMode = SpriteDrawMode.Simple;
                    if (!string.IsNullOrEmpty(b.name)) Label(parent, b.name, center + new Vector2(0f, b.h * 0.5f + 0.6f));
                    continue;
                }
                var roof = Sprite("Roofs/roof_" + b.style);
                var face = Sprite("Roofs/face_" + b.style);
                float faceH = b.style.StartsWith("container") ? 0.4f : 0.8f;
                if (roof != null)
                {
                    var sr = MakeSprite("Roof", roof, new Vector2(center.x, center.y + faceH * 0.5f), parent, ZLSort.Roof);
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = new Vector2(b.w, b.h - faceH);
                }
                if (face != null)
                {
                    var fr = MakeSprite("Face", face, new Vector2(center.x, b.y + faceH * 0.5f), parent, ZLSort.Roof - 1);
                    fr.drawMode = SpriteDrawMode.Tiled;
                    fr.size = new Vector2(b.w, faceH);
                }
                // rooftop clutter on the bigger roofs
                if (b.w >= 7 && b.h >= 7 && !b.style.StartsWith("house"))
                {
                    var rng = new System.Random(seed * 7919);
                    int n = 1 + (b.w * b.h) / 40;
                    string[] kinds = { "ac", "vent", "tank", "skylight" };
                    for (int k = 0; k < n; k++)
                    {
                        var p = new Vector2(b.x + 1.5f + (float)rng.NextDouble() * (b.w - 3f), b.y + 1.8f + (float)rng.NextDouble() * (b.h - 3.5f));
                        MakeSprite("RoofProp", Sprite("Roofs/rp_" + kinds[rng.Next(kinds.Length)]), p, parent, ZLSort.Roof + 1);
                    }
                }
            }
        }

        static readonly HashSet<string> Canopies = new HashSet<string> { "tree", "pine", "bush", "crane" };

        void Props()
        {
            var parent = new GameObject("Props").transform;
            parent.SetParent(_root, false);
            var solids = new GameObject("PropColliders");
            solids.transform.SetParent(_root, false);
            foreach (var p in City.props)
            {
                var s = Sprite("Props/" + p.k);
                if (s == null) continue;
                var pos = new Vector2(p.x, p.y);
                int order = p.solid == 0 ? ZLSort.Decal : Canopies.Contains(p.k) ? ZLSort.Canopy : ZLSort.ByY(p.y);
                var sr = MakeSprite(p.k, s, pos, parent, order);
                if (p.r != 0) sr.transform.rotation = Quaternion.Euler(0f, 0f, p.r);
                if (p.solid == 0) continue;
                var box = solids.AddComponent<BoxCollider2D>();
                box.offset = pos;
                bool rotated = p.r == 90 || p.r == 270;
                float w = Mathf.Max(0.6f, p.w), h = Mathf.Max(0.6f, p.h);
                if (p.k == "tree" || p.k == "pine" || p.k == "lamp") { w = 0.5f; h = 0.5f; }  // only the trunk blocks
                if (p.k.StartsWith("wreck")) { w = 1.9f; h = 0.9f; }
                box.size = rotated ? new Vector2(h, w) : new Vector2(w, h);
            }
        }

        public SpriteRenderer MakeSprite(string name, Sprite s, Vector2 pos, Transform parent, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : _root, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            return sr;
        }

        void Label(Transform parent, string text, Vector2 pos)
        {
            var go = new GameObject("Sign");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UISprites.DisplayFont;
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.92f, 0.6f, 0.95f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UISprites.DisplayFont.material;
            mr.sortingOrder = ZLSort.Overhead;
        }
    }
}
