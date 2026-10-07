using UnityEngine;

namespace PKR
{
    /// <summary>A cosmetic palette for one hero, bought with Star Shards. Cosmetic only: no stats.</summary>
    [CreateAssetMenu(menuName = "PKR/Cosmetic", fileName = "NewCosmetic")]
    public class CosmeticDefinition : ScriptableObject
    {
        [Tooltip("Stable ID used in saves. Never change after release.")]
        public string id = "new_cosmetic";
        public string displayName = "New Palette";
        [Tooltip("CharacterDefinition.id of the hero who can wear it.")]
        public string heroId = "nova";
        [Min(0)] public int cost = 60;
        [Tooltip("Multiplied over the hero's sprite colors.")]
        public Color tint = Color.white;
    }
}
