using UnityEngine;

namespace PKR
{
    /// <summary>
    /// Per-level puzzle state shared by keys, locked doors and switch blocks. Reset by LevelFlowController when a
    /// level loads. Keys you pick up stay with you after a fall; switch colour persists until you hit a switch again.
    /// </summary>
    public static class LevelPuzzleState
    {
        public static int Keys { get; private set; }
        /// <summary>True: red (1) blocks are solid, blue (2) are ghosts. False: the other way round.</summary>
        public static bool RedActive { get; private set; } = true;
        public static event System.Action Changed;

        public static void Reset()
        {
            Keys = 0;
            RedActive = true;
            Changed?.Invoke();
        }

        public static void AddKey() { Keys++; Changed?.Invoke(); }

        public static bool UseKey()
        {
            if (Keys <= 0) return false;
            Keys--;
            Changed?.Invoke();
            return true;
        }

        public static void Toggle()
        {
            RedActive = !RedActive;
            Changed?.Invoke();
        }
    }
}
