using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>
    /// Parses a Story level drawn as text (one character = one 1x1 world cell; the bottom row is y = 0,
    /// x grows to the right). The level builder turns the result into ground, planks, spikes, pickups, enemies
    /// and props. Pure so the layouts themselves can be unit-tested (start, goal, reachable shards...).
    ///
    /// Legend
    ///   #  ground        X  stone wall      =  one-way plank   ^  spikes        ~  water/pit decor (no collider)
    ///   P  hero start    G  goal gate       C  checkpoint      *  Star Shard    +  health crystal
    ///   ?  shard crate (hit from below)     J  spring          L  climbable vine
    ///   H  hay / hiding spot                O  stealth objective (the item to steal)
    ///   m  moving platform (horizontal)     u  lift (vertical)
    ///   B  Cog Beetle    T  Spring Tick     M  Gyro Moth       K  Bolt Knight   g  Patrol Guard (faces left)
    ///   s  secret room cell (the bounding box of all 's' becomes one secret room behind a fake wall)
    ///   .  or space: empty
    /// </summary>
    public class AsciiLevel
    {
        public struct Run { public char tile; public int x, y, length; }
        public struct Point { public char tile; public int x, y; }
        /// <summary>A block of cells: x, y is the bottom-left cell, w x h cells.</summary>
        public struct Rect { public char tile; public int x, y, w, h; }

        public int Width { get; }
        public int Height { get; }
        public readonly List<Run> Runs = new List<Run>();       // horizontal runs of # X = ^ ~ L H
        public readonly List<Point> Points = new List<Point>(); // everything else
        public bool HasSecret { get; private set; }
        public int SecretMinX { get; private set; }
        public int SecretMinY { get; private set; }
        public int SecretMaxX { get; private set; }
        public int SecretMaxY { get; private set; }

        public const string RunTiles = "#X=^~LH";
        public const string PointTiles = "PGC*+?JOmuBTMKg";

        readonly char[,] _grid;

        public AsciiLevel(IList<string> rows)
        {
            if (rows == null || rows.Count == 0) throw new ArgumentException("A level needs at least one row.");
            Height = rows.Count;
            int w = 0;
            foreach (var r in rows) w = Math.Max(w, r?.Length ?? 0);
            Width = w;
            _grid = new char[Width, Height];
            for (int row = 0; row < Height; row++)
            {
                string line = rows[row] ?? "";
                int y = Height - 1 - row;
                for (int x = 0; x < Width; x++) _grid[x, y] = x < line.Length ? line[x] : '.';
            }
            Parse();
        }

        public char At(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height ? _grid[x, y] : '.';

        public bool IsSolid(int x, int y)
        {
            char c = At(x, y);
            return c == '#' || c == 'X' || c == '?';
        }

        public int Count(char tile)
        {
            int n = 0;
            foreach (var p in Points) if (p.tile == tile) n++;
            return n;
        }

        public Point? First(char tile)
        {
            foreach (var p in Points) if (p.tile == tile) return p;
            return null;
        }

        void Parse()
        {
            for (int y = 0; y < Height; y++)
            {
                int x = 0;
                while (x < Width)
                {
                    char c = _grid[x, y];
                    if (RunTiles.IndexOf(c) >= 0)
                    {
                        int start = x;
                        while (x < Width && _grid[x, y] == c) x++;
                        Runs.Add(new Run { tile = c, x = start, y = y, length = x - start });
                        continue;
                    }
                    if (PointTiles.IndexOf(c) >= 0) Points.Add(new Point { tile = c, x = x, y = y });
                    else if (c == 's')
                    {
                        if (!HasSecret) { HasSecret = true; SecretMinX = SecretMaxX = x; SecretMinY = SecretMaxY = y; }
                        SecretMinX = Math.Min(SecretMinX, x); SecretMaxX = Math.Max(SecretMaxX, x);
                        SecretMinY = Math.Min(SecretMinY, y); SecretMaxY = Math.Max(SecretMaxY, y);
                    }
                    x++;
                }
            }
            // Checkpoints are numbered left to right.
            Points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        }

        /// <summary>Basic sanity rules every playable layout must pass (used by tests and the builder).</summary>
        public List<string> Validate()
        {
            var e = new List<string>();
            if (Count('P') != 1) e.Add($"needs exactly one start P (has {Count('P')})");
            if (Count('G') != 1) e.Add($"needs exactly one goal G (has {Count('G')})");
            var start = First('P');
            if (start.HasValue && !HasFloorBelow(start.Value.x, start.Value.y)) e.Add("start P has no floor under it");
            var goal = First('G');
            if (goal.HasValue && !IsSolid(goal.Value.x, goal.Value.y - 1)) e.Add("goal G must stand directly on ground");
            foreach (var p in Points)
                if (p.tile == 'C' && !IsSolid(p.x, p.y - 1)) e.Add($"checkpoint at {p.x},{p.y} must stand directly on ground");
            return e;
        }

        /// <summary>
        /// Greedy merge of one tile type into as few rectangles as possible (horizontal runs first, then rows of
        /// identical runs stacked upward), so ground becomes a handful of colliders instead of one per cell.
        /// </summary>
        public List<Rect> MergedRects(char tile)
        {
            var result = new List<Rect>();
            var used = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (used[x, y] || _grid[x, y] != tile) continue;
                int w = 0;
                while (x + w < Width && _grid[x + w, y] == tile && !used[x + w, y]) w++;
                int h = 1;
                while (y + h < Height && RowMatches(tile, x, y + h, w, used)) h++;
                for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++) used[xx, yy] = true;
                result.Add(new Rect { tile = tile, x = x, y = y, w = w, h = h });
            }
            return result;
        }

        bool RowMatches(char tile, int x, int y, int w, bool[,] used)
        {
            for (int xx = x; xx < x + w; xx++)
                if (_grid[xx, y] != tile || used[xx, y]) return false;
            // The run must end exactly here too, or the merge would leave a ragged sliver beside it.
            return (x + w >= Width || _grid[x + w, y] != tile || used[x + w, y]) && (x == 0 || _grid[x - 1, y] != tile || used[x - 1, y]);
        }

        /// <summary>
        /// For a moving platform at cell (x, y): how far it can travel right before its 3-wide deck would touch the
        /// next solid cell in rows y-2..y (it parks one cell short). Capped at maxTravel.
        /// </summary>
        public int MoverTravel(int x, int y, int maxTravel = 20)
        {
            for (int xx = x + 2; xx <= x + maxTravel + 3 && xx < Width; xx++)
                for (int yy = y - 2; yy <= y; yy++)
                    if (IsSolid(xx, yy)) return Math.Max(2, Math.Min(maxTravel, xx - x - 3));
            return maxTravel;
        }

        bool HasFloorBelow(int x, int y)
        {
            for (int yy = y - 1; yy >= 0; yy--)
            {
                char c = At(x, yy);
                if (IsSolid(x, yy) || c == '=') return true;
            }
            return false;
        }
    }
}
