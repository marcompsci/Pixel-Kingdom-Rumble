using System;
using PKR;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

/// <summary>
/// Unity IAP (com.unity.purchasing 4.x) behind PurchaseService. Lives in Assembly-CSharp (no asmdef) so it can see the
/// IAP package without the PKR assemblies having to reference it. In the Editor Unity IAP uses its fake store, so
/// purchases succeed without Apple. On a device the product must exist in App Store Connect
/// (see Docs/SHADOW_CONTRACTS.md).
/// </summary>
public class UnityIapBackend : IStoreBackend, IDetailedStoreListener
{
    IStoreController _controller;
    IExtensionProvider _extensions;
    Action<bool, string> _pendingBuy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (PurchaseService.Backend != null) return;
        var backend = new UnityIapBackend();
        PurchaseService.Register(backend);
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(PurchaseService.ContractsProductId, ProductType.NonConsumable);
        UnityPurchasing.Initialize(backend, builder);
    }

    public bool IsReady => _controller != null;

    public string PriceOf(string productId)
    {
        var p = _controller != null ? _controller.products.WithID(productId) : null;
        return p != null && p.availableToPurchase ? p.metadata.localizedPriceString : null;
    }

    public bool Owns(string productId)
    {
        var p = _controller != null ? _controller.products.WithID(productId) : null;
        return p != null && p.hasReceipt;
    }

    public void Buy(string productId, Action<bool, string> done)
    {
        if (_controller == null) { done?.Invoke(false, "The App Store isn't ready yet. Try again in a moment."); return; }
        var p = _controller.products.WithID(productId);
        if (p == null || !p.availableToPurchase) { done?.Invoke(false, "This item isn't available right now."); return; }
        _pendingBuy = done;
        _controller.InitiatePurchase(p);
    }

    public void Restore(Action<bool, string> done)
    {
        if (_extensions == null) { done?.Invoke(false, "The App Store isn't ready yet."); return; }
        _extensions.GetExtension<IAppleExtensions>().RestoreTransactions((ok, msg) => done?.Invoke(ok, ok ? null : msg));
    }

    // ---- IStoreListener ------------------------------------------------------------------------

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _controller = controller;
        _extensions = extensions;
        PurchaseService.SyncFromStore();
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message) =>
        Debug.LogWarning($"[PKR] Store unavailable: {error} {message}");

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (args.purchasedProduct.definition.id == PurchaseService.ContractsProductId) PurchaseService.Grant();
        var done = _pendingBuy;
        _pendingBuy = null;
        done?.Invoke(true, null);
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => Fail(reason.ToString());

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription description) =>
        Fail(description != null ? description.reason.ToString() : "Unknown");

    void Fail(string reason)
    {
        var done = _pendingBuy;
        _pendingBuy = null;
        done?.Invoke(false, reason == "UserCancelled" ? "Purchase cancelled." : $"Purchase failed ({reason}).");
    }
}
