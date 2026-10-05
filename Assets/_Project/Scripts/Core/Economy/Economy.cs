using System;

namespace PKR.Core
{
    public enum PurchaseResult { Success, AlreadyOwned, NotEnoughShards, InvalidItem }

    /// <summary>
    /// Star Shard rules. Cosmetics only: nothing purchasable changes stats, by design.
    /// Reward numbers live here so they can be tuned in one place and unit-tested.
    /// </summary>
    public static class Economy
    {
        public const int ShardsPerPickup = 1;
        public const int LevelClearBonus = 25;
        public const int SecretBonus = 10;
        public const int FirstClearBonus = 50;
        public static readonly int[] ArenaPlacementRewards = { 20, 12, 8, 5 }; // 1st..4th
        public const int MaxShards = 999_999;

        public static int LevelReward(int shardsCollected, int secretsFound, bool firstClear)
        {
            int total = Math.Max(0, shardsCollected) * ShardsPerPickup
                      + LevelClearBonus
                      + Math.Max(0, secretsFound) * SecretBonus
                      + (firstClear ? FirstClearBonus : 0);
            return total;
        }

        /// <summary>placement is 1-based. Training mode should not call this.</summary>
        public static int ArenaReward(int placement)
        {
            if (placement < 1 || placement > ArenaPlacementRewards.Length) return 0;
            return ArenaPlacementRewards[placement - 1];
        }

        public static void Grant(SaveData save, int amount)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (amount <= 0) return;
            long next = (long)save.starShards + amount;
            save.starShards = (int)Math.Min(MaxShards, next);
        }

        public static PurchaseResult TryBuyCosmetic(SaveData save, string cosmeticId, int cost)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (string.IsNullOrEmpty(cosmeticId) || cost < 0) return PurchaseResult.InvalidItem;
            if (save.ownedCosmetics.Contains(cosmeticId)) return PurchaseResult.AlreadyOwned;
            if (save.starShards < cost) return PurchaseResult.NotEnoughShards;
            save.starShards -= cost;
            save.ownedCosmetics.Add(cosmeticId);
            return PurchaseResult.Success;
        }
    }
}
