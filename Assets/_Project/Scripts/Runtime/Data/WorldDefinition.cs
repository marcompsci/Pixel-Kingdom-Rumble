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

        public string[] LevelIds()
        {
            var ids = new string[levels.Count];
            for (int i = 0; i < levels.Count; i++) ids[i] = levels[i] != null ? levels[i].id : "";
            return ids;
        }
    }
}
