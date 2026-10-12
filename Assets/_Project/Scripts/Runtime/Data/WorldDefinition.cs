using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>A Story Quest world: its name and levels in play order (each unlocks after the previous is cleared).</summary>
    [CreateAssetMenu(menuName = "PKR/World Definition", fileName = "NewWorld")]
    public class WorldDefinition : ScriptableObject
    {
        public string displayName = "Sunspire Isles";
        [TextArea(2, 4)] public string description = "";
        public List<LevelDefinition> levels = new List<LevelDefinition>();
        [Tooltip("Level select draws the first four levels as one map split into four realm sections (Shattered Realms).")]
        public bool sectioned;
        [Tooltip("Sectioned worlds: color and one-line rule for each of the first four levels.")]
        public List<Color> sectionColors = new List<Color>();
        public List<string> sectionNotes = new List<string>();

        public string[] LevelIds()
        {
            var ids = new string[levels.Count];
            for (int i = 0; i < levels.Count; i++) ids[i] = levels[i] != null ? levels[i].id : "";
            return ids;
        }
    }
}
