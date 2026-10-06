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

        /// <summary>Index of the hero with this id, or -1.</summary>
        public static int IndexOf(string[] ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id)) return -1;
            return Array.IndexOf(ids, id);
        }
    }
}
