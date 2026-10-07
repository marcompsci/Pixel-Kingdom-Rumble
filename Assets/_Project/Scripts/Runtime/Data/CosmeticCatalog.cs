using System.Collections.Generic;
using PKR.Core;
using UnityEngine;

namespace PKR
{
    /// <summary>Everything the Star Shard shop sells. Referenced from the CharacterRoster.</summary>
    [CreateAssetMenu(menuName = "PKR/Cosmetic Catalog", fileName = "Cosmetics")]
    public class CosmeticCatalog : ScriptableObject
    {
        public List<CosmeticDefinition> items = new List<CosmeticDefinition>();

        /// <summary>The catalog as Core sees it (for Wardrobe rules).</summary>
        public List<CosmeticItem> AsItems()
        {
            var list = new List<CosmeticItem>(items.Count);
            foreach (var c in items)
                if (c != null) list.Add(new CosmeticItem { id = c.id, heroId = c.heroId, cost = c.cost });
            return list;
        }

        public CosmeticDefinition Find(string id)
        {
            foreach (var c in items) if (c != null && c.id == id) return c;
            return null;
        }

        public List<CosmeticDefinition> ForHero(string heroId)
        {
            var list = new List<CosmeticDefinition>();
            foreach (var c in items) if (c != null && c.heroId == heroId) list.Add(c);
            return list;
        }

        /// <summary>The palette tint this hero wears in the given save (white = default look).</summary>
        public Color TintFor(SaveData save, string heroId)
        {
            var id = Wardrobe.EquippedFor(save, heroId, AsItems());
            var c = id != null ? Find(id) : null;
            return c != null ? c.tint : Color.white;
        }
    }
}
