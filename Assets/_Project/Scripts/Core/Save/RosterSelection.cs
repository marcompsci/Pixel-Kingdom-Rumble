using System;

namespace PKR.Core
{
    /// <summary>Navigation rules for the character select carousel. Pure, so it is unit-tested.</summary>
    public static class RosterSelection
    {
        /// <summary>Wrap any index into 0..count-1 (handles negatives). count &lt;= 0 returns 0.</summary>
        public static int Wrap(int index, int count)
        {
            if (count <= 0) return 0;
            int m = index % count;
            return m < 0 ? m + count : m;
        }

        /// <summary>
        /// Index to highlight when the screen opens: the preferred index if it is selectable,
        /// otherwise the first selectable one, otherwise 0.
        /// </summary>
        public static int Initial(bool[] selectable, int preferred)
        {
            if (selectable == null || selectable.Length == 0) return 0;
            if (preferred >= 0 && preferred < selectable.Length && selectable[preferred]) return preferred;
            int first = Array.IndexOf(selectable, true);
            return first >= 0 ? first : 0;
        }

        /// <summary>Step left/right (dir -1/+1) through ALL heroes, wrapping. Locked heroes can be viewed, not picked.</summary>
        public static int Step(int current, int dir, int count) => Wrap(current + (dir >= 0 ? 1 : -1), count);

        /// <summary>A random selectable index (CPU fighters' hero), or -1 if none is selectable.</summary>
        public static int PickRandom(bool[] selectable, Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (selectable == null) return -1;
            int count = 0;
            foreach (bool b in selectable) if (b) count++;
            if (count == 0) return -1;
            int pick = rng.Next(count);
            for (int i = 0; i < selectable.Length; i++)
            {
                if (!selectable[i]) continue;
                if (pick == 0) return i;
                pick--;
            }
            return -1;
        }

        /// <summary>Index of the hero with this id, or -1.</summary>
        public static int IndexOf(string[] ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id)) return -1;
            return Array.IndexOf(ids, id);
        }
    }
}
