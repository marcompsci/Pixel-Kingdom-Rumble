using UnityEngine;

namespace PKR
{
    /// <summary>Story Quest level metadata (names, scene, targets). Counts are filled in by the level builder.</summary>
    [CreateAssetMenu(menuName = "PKR/Level Definition", fileName = "NewLevel")]
    public class LevelDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used in saves. Never change after release.")]
        public string id = "new_level";
        public string displayName = "New Level";
        public string biomeName = "";
        public string sceneName = "";
        [TextArea(2, 4)] public string codexEntry = "";
        [Tooltip("Shown on the level-complete screen as the time to beat.")]
        public float parTimeSeconds = 120f;
        public int totalShards;
        public int totalSecrets;
        public Color skyColor = new Color32(255, 196, 140, 255);
    }
}
