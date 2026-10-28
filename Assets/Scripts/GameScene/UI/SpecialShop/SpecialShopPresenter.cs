using System;
using System.Collections.Generic;
using UnityEngine;

// Owns user actions and popup lifetime; no prices, drawing code or backend mutations.
public sealed class SpecialShopPresenter : IDisposable
{
    private readonly List<SpecialShopCardView> cards = new List<SpecialShopCardView>();
    private readonly SpecialShopPopupView popupPrefab;
    private readonly Transform popupParent;
    private readonly Action<GameObject> remove;
    private readonly Func<bool> canOpen;
    private SpecialShopPopupView popup;
    private bool disposed;
    public SpecialShopPresenter(SpecialShopView view, SpecialShopCatalog catalog, SpecialShopPopupView popupPrefab,
        Transform popupParent, Action<GameObject> remove, Func<bool> canOpen)
    {
        this.popupPrefab = popupPrefab; this.popupParent = popupParent; this.remove = remove; this.canOpen = canOpen;
        foreach (var offer in catalog.offers)
        {
            if (offer == null) continue;
            var card = view.AddCard(offer);
            card.Selected += Open;
            cards.Add(card);
        }
    }
    private void Open(SpecialShopOffer offer)
    {
        if (disposed || !canOpen() || popupParent == null) return;
        Close();
        popup = UnityEngine.Object.Instantiate(popupPrefab, popupParent, false);
        popup.name = "Package Preview Popup";
        popup.transform.SetAsLastSibling();
        popup.CloseRequested += Close;
        popup.Bind(offer);
    }
    private void Close()
    {
        if (popup == null) return;
        var previous = popup;
        popup = null;
        previous.CloseRequested -= Close;
        remove(previous.gameObject);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var card in cards) if (card != null) card.Selected -= Open;
        cards.Clear();
        Close();
    }
}
