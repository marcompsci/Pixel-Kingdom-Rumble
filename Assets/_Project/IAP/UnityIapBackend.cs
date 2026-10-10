using System;
using System.Collections.Generic;
using PKR;
using UnityEngine;
using UnityEngine.Purchasing;
using PKRPurchases = PKR.PurchaseService;

/// <summary>
/// Unity IAP 5 (com.unity.purchasing 5.x, StoreController API) behind PKR.PurchaseService. Lives in Assembly-CSharp
/// (no asmdef) so it can see the IAP package without the PKR assemblies referencing it. In the Editor Unity IAP uses
/// its fake store, so purchases succeed without Apple. On a device the product must exist in App Store Connect
/// (see Docs/SHADOW_CONTRACTS.md).
/// </summary>
public class UnityIapBackend : IStoreBackend
{
    StoreController _store;
    bool _ready;
    bool _owned;
    Action<bool, string> _pendingBuy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static async void Boot()
    {
        if (PKRPurchases.Backend != null) return;
        var backend = new UnityIapBackend();
        PKRPurchases.Register(backend);
        try { await backend.Start(); }
        catch (Exception e) { Debug.LogWarning("[PKR] Store unavailable: " + e.Message); }
    }

    async System.Threading.Tasks.Task Start()
    {
        _store = UnityIAPServices.StoreController();
        _store.OnStoreDisconnected += d => Debug.LogWarning("[PKR] Store disconnected: " + d.message);
        _store.OnProductsFetched += products =>
        {
            _ready = true;
            _store.FetchPurchases(); // what this Apple ID already owns
        };
        _store.OnProductsFetchFailed += f => Debug.LogWarning("[PKR] Products unavailable: " + f.FailureReason);
        _store.OnPurchasesFetched += orders =>
        {
            foreach (var o in orders.ConfirmedOrders) if (Contains(o)) Own();
            foreach (var p in orders.PendingOrders)
            {
                if (Contains(p)) Own();
                _store.ConfirmPurchase(p);
            }
        };
        _store.OnPurchasePending += order =>
        {
            bool ours = Contains(order);
            if (ours) Own();
            _store.ConfirmPurchase(order); // non-consumable: nothing to deliver server-side
            if (ours) Finish(true, null);
        };
        _store.OnPurchaseConfirmed += order => { if (Contains(order)) Own(); };
        _store.OnPurchaseFailed += failed =>
        {
            string reason = failed.FailureReason.ToString();
            Finish(false, reason == "UserCancelled" ? "Purchase cancelled." : $"Purchase failed ({reason}).");
        };
        _store.OnPurchaseDeferred += deferred => Finish(false, "Purchase is waiting for approval (Ask to Buy).");

        await _store.Connect();
        _store.FetchProducts(new List<ProductDefinition>
        {
            new ProductDefinition(PKRPurchases.ContractsProductId, PKRPurchases.ContractsProductId, ProductType.NonConsumable)
        });
    }

    static bool Contains(Order order)
    {
        if (order?.CartOrdered == null) return false;
        foreach (var item in order.CartOrdered.Items())
            if (item?.Product?.definition != null && item.Product.definition.id == PKRPurchases.ContractsProductId) return true;
        return false;
    }

    void Own()
    {
        _owned = true;
        PKRPurchases.Grant();
    }

    void Finish(bool ok, string msg)
    {
        var done = _pendingBuy;
        _pendingBuy = null;
        done?.Invoke(ok, msg);
    }

    // ---- IStoreBackend --------------------------------------------------------------------------

    public bool IsReady => _ready;

    public string PriceOf(string productId)
    {
        var p = _store != null ? _store.GetProductById(productId) : null;
        return p != null && p.availableToPurchase && p.metadata != null ? p.metadata.localizedPriceString : null;
    }

    public bool Owns(string productId) => _owned && productId == PKRPurchases.ContractsProductId;

    public void Buy(string productId, Action<bool, string> done)
    {
        var p = _store != null ? _store.GetProductById(productId) : null;
        if (!_ready || p == null || !p.availableToPurchase)
        {
            done?.Invoke(false, "The App Store isn't ready yet. Try again in a moment.");
            return;
        }
        _pendingBuy = done;
        _store.PurchaseProduct(p);
    }

    public void Restore(Action<bool, string> done)
    {
        if (_store == null) { done?.Invoke(false, "The App Store isn't ready yet."); return; }
        _store.RestoreTransactions((ok, msg) =>
        {
            _store.FetchPurchases();
            done?.Invoke(ok, ok ? null : msg);
        });
    }
}
