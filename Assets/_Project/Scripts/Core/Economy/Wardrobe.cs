using System;
using System.Collections.Generic;

namespace PKR.Core
{
    /// <summary>One cosmetic as the rules see it: which hero it is for and what it costs.</summary>
    public struct CosmeticItem
    {
        public string id;
        public string heroId;
        public int cost;
    }

    /// <summary>
    /// Buying and equipping cosmetics (Star Shard shop). Each hero wears at most one palette; equipping one
    /// takes off any other palette for the same hero. Cosmetic only: nothing here touches stats.
    /// </summary>
    public static class Wardrobe
    {
        public static PurchaseResult Buy(SaveData save, CosmeticItem item) => Economy.TryBuyCosmetic(save, item.id, item.cost);

        public static bool Owns(SaveData save, string cosmeticId) =>
            save != null && !string.IsNullOrEmpty(cosmeticId) && save.ownedCosmetics.Contains(cosmeticId);

        /// <summary>Wear an owned cosmetic. Returns false if it isn't owned or isn't in the catalog.</summary>
        public static bool Equip(SaveData save, string cosmeticId, IList<CosmeticItem> catalog)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (!Owns(save, cosmeticId) || !TryFind(catalog, cosmeticId, out var item)) return false;
            UnequipHero(save, item.heroId, catalog);
            save.equippedCosmetics.Add(cosmeticId);
            return true;
        }

        /// <summary>Back to the hero's default look.</summary>
        public static void UnequipHero(SaveData save, string heroId, IList<CosmeticItem> catalog)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (catalog == null) return;
            foreach (var c in catalog)
                if (c.heroId == heroId) save.equippedCosmetics.Remove(c.id);
        }

        /// <summary>The cosmetic this hero is wearing, or null for the default look.</summary>
        public static string EquippedFor(SaveData save, string heroId, IList<CosmeticItem> catalog)
        {
            if (save == null || catalog == null) return null;
            foreach (var c in catalog)
                if (c.heroId == heroId && save.equippedCosmetics.Contains(c.id)) return c.id;
            return null;
        }

        static bool TryFind(IList<CosmeticItem> catalog, string id, out CosmeticItem item)
        {
            item = default;
            if (catalog == null) return false;
            foreach (var c in catalog)
                if (c.id == id) { item = c; return true; }
            return false;
        }
    }
}
