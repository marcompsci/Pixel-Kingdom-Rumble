using System.Collections.Generic;
using UnityEngine;

namespace PKR
{
    /// <summary>All heroes in display order (character select, codex). Add a hero by adding its definition here.</summary>
    [CreateAssetMenu(menuName = "PKR/Character Roster", fileName = "Roster")]
    public class CharacterRoster : ScriptableObject
    {
        public List<CharacterDefinition> heroes = new List<CharacterDefinition>();
        [Tooltip("Star Shard shop items (palettes). Optional.")]
        public CosmeticCatalog cosmetics;

        /// <summary>The palette this hero wears in the current save (white = default look).</summary>
        public Color TintFor(string heroId)
        {
            var save = Services.Save;
            return cosmetics != null && save != null ? cosmetics.TintFor(save.Data, heroId) : Color.white;
        }

        public CharacterDefinition Find(string id)
        {
            foreach (var h in heroes) if (h != null && h.id == id) return h;
            return null;
        }

        /// <summary>Unlock rules for heroes that are playable but not unlocked by default.</summary>
        public System.Collections.Generic.List<PKR.Core.UnlockRule> GetUnlockRules()
        {
            var rules = new System.Collections.Generic.List<PKR.Core.UnlockRule>();
            foreach (var h in heroes)
                if (h != null && h.playableInThisBuild && !h.unlockedByDefault && !string.IsNullOrEmpty(h.unlockByClearingLevelId))
                    rules.Add(new PKR.Core.UnlockRule { characterId = h.id, clearLevelId = h.unlockByClearingLevelId, minSecrets = h.unlockMinSecrets });
            return rules;
        }

        /// <summary>Selectable = playable in this build and unlocked in the save (or unlocked by default).</summary>
        public bool IsSelectable(CharacterDefinition h)
        {
            if (h == null || !h.playableInThisBuild) return false;
            if (h.unlockedByDefault) return true;
            var save = Services.Save;
            return save != null && save.Data.IsCharacterUnlocked(h.id);
        }
    }
}
