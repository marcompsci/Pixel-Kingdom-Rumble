using System;
using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>The city layout (Resources/PKR/ZombieLand/city.json, made by Tools/maps/zombieland_city.py).</summary>
    [Serializable]
    public class ZLCityData
    {
        [Serializable] public class P { public float x, y; }
        [Serializable] public class District { public string id; public int x0, y0, x1, y1; }
        [Serializable] public class Building { public int x, y, w, h; public string style, name; }
        [Serializable] public class Prop { public string k; public float x, y; public int solid, r, w, h; }
        [Serializable] public class Safehouse { public string id, name, district; public float x, y; }
        [Serializable] public class Vehicle { public string k; public float x, y, a; }
        [Serializable] public class Pickup { public string id, k; public float x, y; }
        [Serializable] public class Nest { public string id, district; public float x, y; }
        [Serializable] public class RelicCase { public string relic; public float x, y; }
        [Serializable] public class Lair { public string id; public float x, y; }
        [Serializable] public class Mission { public string id; public float x, y; public List<P> points = new List<P>(); public P dest; public int hasDest; }
        [Serializable] public class Site { public string key; public float x, y; }
        // Last Stand (block.json)
        [Serializable] public class Zone { public string id, name; }
        [Serializable] public class Door { public string id, name, opens; public int x, y, w, h, cost; }
        [Serializable] public class Station { public string k, id, zone; public float x, y; public int cost; }
        [Serializable] public class Spawn { public float x, y; public string zone; }
        [Serializable] public class Spot { public float x, y; public string zone; }

        public int w, h;
        public string[] rows;
        public P start;
        public List<District> districts = new List<District>();
        public List<Building> buildings = new List<Building>();
        public List<Prop> props = new List<Prop>();
        public List<Safehouse> safehouses = new List<Safehouse>();
        public List<Vehicle> vehicles = new List<Vehicle>();
        public List<Pickup> pickups = new List<Pickup>();
        public List<Nest> nests = new List<Nest>();
        public List<RelicCase> relics = new List<RelicCase>();
        public List<Lair> lairs = new List<Lair>();
        public List<Mission> missions = new List<Mission>();
        public List<Site> sites = new List<Site>();
        public List<Zone> zones = new List<Zone>();
        public List<Door> doors = new List<Door>();
        public List<Station> stations = new List<Station>();
        public List<Spawn> spawns = new List<Spawn>();
        public List<Spot> boxSpots = new List<Spot>();
        public List<Spot> eggs = new List<Spot>();

        [NonSerialized] bool[,] _blocked;

        public static ZLCityData Load() => Load("city");

        /// <summary>Loads a map from Resources/PKR/ZombieLand/{name}.json ("city" or "block").</summary>
        public static ZLCityData Load(string name)
        {
            var ta = Resources.Load<TextAsset>("PKR/ZombieLand/" + name);
            if (ta == null) return null;
            var c = JsonUtility.FromJson<ZLCityData>(ta.text);
            c.BuildGrid();
            return c;
        }

        public char Tile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h || rows == null || y >= rows.Length) return 'f';
            return rows[y][x];
        }

        public static bool SolidTile(char c) => c == 'B' || c == 'w' || c == 'f';

        void BuildGrid()
        {
            _blocked = new bool[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _blocked[x, y] = SolidTile(Tile(x, y)) || Tile(x, y) == 'D';
            foreach (var p in props)
            {
                if (p.solid == 0) continue;
                int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
                if (x >= 0 && y >= 0 && x < w && y < h) _blocked[x, y] = true;
            }
        }

        /// <summary>Last Stand: a debris barricade is cleared (its tiles become walkable).</summary>
        public void Open(Door d)
        {
            for (int y = d.y; y < d.y + d.h; y++)
                for (int x = d.x; x < d.x + d.w; x++)
                    if (x >= 0 && y >= 0 && x < w && y < h) _blocked[x, y] = false;
        }

        public bool Blocked(int x, int y) => x < 0 || y < 0 || x >= w || y >= h || _blocked[x, y];
        public bool Walkable(Vector2 p) => !Blocked(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        public string DistrictAt(Vector2 p)
        {
            if (p.x >= 200) return "isle";      // chapter 2: Blackwater Isle, across the causeway
            if (p.x >= 177) return "harbor";
            string best = "suburbs";
            foreach (var d in districts)
                if (p.x >= d.x0 && p.x <= d.x1 + 1 && p.y >= d.y0 && p.y <= d.y1 + 1) best = d.id;
            return best;
        }

        public Mission MissionSite(string id)
        {
            foreach (var m in missions) if (m.id == id) return m;
            return null;
        }

        public Vector2 SiteOf(string key)
        {
            foreach (var s in sites) if (s.key == key) return new Vector2(s.x, s.y);
            return new Vector2(start.x, start.y);
        }

        /// <summary>Line of sight across tiles (buildings and fences block it; water doesn't).</summary>
        public bool Sees(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int n = Mathf.CeilToInt(d * 2f);
            for (int i = 1; i < n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                char c = Tile(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
                if (c == 'B' || c == 'f') return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Shortest walking distance to the hero over the city grid (a flow field), recomputed a few times a second in
    /// a window around the hero. Zombies far from the hero walk down the gradient so they go around buildings.
    /// </summary>
    public class ZLFlowField
    {
        public const int Radius = 40;
        readonly ZLCityData _city;
        readonly int[,] _dist;
        readonly Queue<Vector2Int> _q = new Queue<Vector2Int>(4096);
        Vector2Int _origin;
        int _stamp;
        readonly int[,] _seen;
        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        /// <summary>How far (tiles) the field reaches. The city uses a window around the hero; Last Stand maps are
        /// small enough to cover whole (a short window left the dead stuck on the far side of the map).</summary>
        readonly int _radius;

        public ZLFlowField(ZLCityData city) : this(city, Radius) { }

        public ZLFlowField(ZLCityData city, int radius)
        {
            _radius = Mathf.Max(8, radius);
            _city = city;
            _dist = new int[city.w, city.h];
            _seen = new int[city.w, city.h];
        }

        public void Build(Vector2 target)
        {
            _stamp++;
            _origin = new Vector2Int(Mathf.FloorToInt(target.x), Mathf.FloorToInt(target.y));
            _q.Clear();
            if (!Inside(_origin)) return;
            _dist[_origin.x, _origin.y] = 0;
            _seen[_origin.x, _origin.y] = _stamp;
            _q.Enqueue(_origin);
            while (_q.Count > 0)
            {
                var c = _q.Dequeue();
                int d = _dist[c.x, c.y];
                if (d >= _radius * 10) continue;
                for (int i = 0; i < Dirs.Length; i++)
                {
                    var n = c + Dirs[i];
                    if (!Inside(n) || _seen[n.x, n.y] == _stamp || _city.Blocked(n.x, n.y)) continue;
                    if (Mathf.Abs(n.x - _origin.x) > _radius || Mathf.Abs(n.y - _origin.y) > _radius) continue;
                    // no corner cutting
                    if (i >= 4 && (_city.Blocked(c.x + Dirs[i].x, c.y) || _city.Blocked(c.x, c.y + Dirs[i].y))) continue;
                    _seen[n.x, n.y] = _stamp;
                    _dist[n.x, n.y] = d + (i >= 4 ? 14 : 10);
                    _q.Enqueue(n);
                }
            }
        }

        /// <summary>Co-op: distances to the nearest of several heroes (no window limit; Last Stand's block is small).</summary>
        public void Build(List<Vector2> targets)
        {
            _stamp++;
            _q.Clear();
            if (targets.Count == 0) return;
            _origin = new Vector2Int(Mathf.FloorToInt(targets[0].x), Mathf.FloorToInt(targets[0].y));
            foreach (var t in targets)
            {
                var c = new Vector2Int(Mathf.FloorToInt(t.x), Mathf.FloorToInt(t.y));
                if (!Inside(c) || _seen[c.x, c.y] == _stamp) continue;
                _dist[c.x, c.y] = 0;
                _seen[c.x, c.y] = _stamp;
                _q.Enqueue(c);
            }
            while (_q.Count > 0)
            {
                var c = _q.Dequeue();
                int d = _dist[c.x, c.y];
                if (d >= _radius * 20) continue;
                for (int i = 0; i < Dirs.Length; i++)
                {
                    var n = c + Dirs[i];
                    if (!Inside(n) || _city.Blocked(n.x, n.y)) continue;
                    if (i >= 4 && (_city.Blocked(c.x + Dirs[i].x, c.y) || _city.Blocked(c.x, c.y + Dirs[i].y))) continue;
                    int nd = d + (i >= 4 ? 14 : 10);
                    if (_seen[n.x, n.y] == _stamp && _dist[n.x, n.y] <= nd) continue;
                    _seen[n.x, n.y] = _stamp;
                    _dist[n.x, n.y] = nd;
                    _q.Enqueue(n);
                }
            }
        }

        bool Inside(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < _city.w && c.y < _city.h;

        /// <summary>Direction toward the target from p (zero if p is outside the field).</summary>
        public Vector2 Direction(Vector2 p)
        {
            var c = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            if (!Inside(c) || _seen[c.x, c.y] != _stamp) return Vector2.zero;
            int best = _dist[c.x, c.y];
            Vector2Int bestDir = Vector2Int.zero;
            for (int i = 0; i < Dirs.Length; i++)
            {
                var n = c + Dirs[i];
                if (!Inside(n) || _seen[n.x, n.y] != _stamp) continue;
                if (i >= 4 && (_city.Blocked(c.x + Dirs[i].x, c.y) || _city.Blocked(c.x, c.y + Dirs[i].y))) continue;
                if (_dist[n.x, n.y] < best) { best = _dist[n.x, n.y]; bestDir = Dirs[i]; }
            }
            if (bestDir == Vector2Int.zero) return Vector2.zero;
            // aim at the next cell's centre so bodies don't hug corners
            var next = new Vector2(c.x + bestDir.x + 0.5f, c.y + bestDir.y + 0.5f);
            return (next - p).normalized;
        }

        public bool Reaches(Vector2 p)
        {
            var c = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            return Inside(c) && _seen[c.x, c.y] == _stamp;
        }
    }
}
