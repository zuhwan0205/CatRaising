using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpecialShopCardView : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Text titleLabel, priceLabel;
    [SerializeField] private RawImage artwork;
    private SpecialShopOffer offer;
    public event Action<SpecialShopOffer> Selected;
    public bool IsConfigured => selectButton != null && titleLabel != null && priceLabel != null && artwork != null;

    public void Bind(SpecialShopOffer value)
    {
        offer = value;
        // Only dynamic values: authored layout, typography and colors stay in the prefab.
        if (titleLabel != null) titleLabel.text = value.title;
        if (priceLabel != null) priceLabel.text = value.previewPrice;
        SpecialShopPopupView.SetArtwork(artwork, value);
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelected);
            selectButton.onClick.AddListener(OnSelected);
        }
    }
    private void OnSelected() { if (offer != null) Selected?.Invoke(offer); }
    private void OnDestroy()
    {
        if (selectButton != null) selectButton.onClick.RemoveListener(OnSelected);
        Selected = null;
    }
#if UNITY_EDITOR
    public void Configure(Button button, Text title, Text price, RawImage art)
    { selectButton = button; titleLabel = title; priceLabel = price; artwork = art; }
#endif
}
