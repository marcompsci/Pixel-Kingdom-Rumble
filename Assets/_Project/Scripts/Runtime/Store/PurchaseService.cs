using System;
using UnityEngine;

namespace PKR
{
    /// <summary>What a store backend must do (Unity IAP on device; a stand-in in the Editor and tests).</summary>
    public interface IStoreBackend
    {
        bool IsReady { get; }
        /// <summary>Localized price from the App Store ("$3.99"), or null before the store answers.</summary>
        string PriceOf(string productId);
        /// <summary>True if the store says this non-consumable is owned (receipt present).</summary>
        bool Owns(string productId);
        void Buy(string productId, Action<bool, string> done);
        void Restore(Action<bool, string> done);
    }

    /// <summary>
    /// The one paid item: "Shadow Contracts" (non-consumable, $3.99), which unlocks every contract except the free
    /// teaser. Ownership is stored in the save (contractsUnlocked) and re-checked from the store receipt on start, so
    /// a reinstall can get it back with RESTORE PURCHASE (required by Apple for non-consumables).
    /// The real backend lives outside the PKR assemblies (Assets/_Project/IAP) and registers itself at startup.
    /// </summary>
    public static class PurchaseService
    {
        public const string ContractsProductId = "com.marcompsci.pixelkingdomrumble.shadowcontracts";
        public const string FallbackPrice = "$3.99";

        public static IStoreBackend Backend { get; private set; }
        public static event Action EntitlementsChanged;

        public static void Register(IStoreBackend backend) => Backend = backend;

        public static bool ContractsOwned => Services.Save != null && Services.Save.Data.contractsUnlocked;

        public static string ContractsPrice
        {
            get
            {
                var p = Backend != null ? Backend.PriceOf(ContractsProductId) : null;
                return string.IsNullOrEmpty(p) ? FallbackPrice : p;
            }
        }

        public static bool StoreReady => Backend != null && Backend.IsReady;

        /// <summary>Called by the backend once it knows what the receipt contains.</summary>
        public static void SyncFromStore()
        {
            if (Backend != null && Backend.Owns(ContractsProductId)) Grant();
        }

        public static void BuyContracts(Action<bool, string> done)
        {
            if (Backend == null) { done?.Invoke(false, "The store isn't available on this device."); return; }
            Backend.Buy(ContractsProductId, (ok, msg) =>
            {
                if (ok) Grant();
                done?.Invoke(ok, msg);
            });
        }

        public static void Restore(Action<bool, string> done)
        {
            if (Backend == null) { done?.Invoke(false, "The store isn't available on this device."); return; }
            Backend.Restore((ok, msg) =>
            {
                SyncFromStore();
                done?.Invoke(ok && ContractsOwned, ContractsOwned ? "Shadow Contracts restored." : (msg ?? "Nothing to restore."));
            });
        }

        /// <summary>Unlock (purchase, restore, or a tester build's unlock button).</summary>
        public static void Grant()
        {
            var save = Services.Save;
            if (save == null || save.Data.contractsUnlocked) return;
            save.Data.contractsUnlocked = true;
            save.SaveNow();
            Debug.Log("[PKR] Shadow Contracts unlocked.");
            EntitlementsChanged?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Backend = null;
            EntitlementsChanged = null;
        }
    }
}
