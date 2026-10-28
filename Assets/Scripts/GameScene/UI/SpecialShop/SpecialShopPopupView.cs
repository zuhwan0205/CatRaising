using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpecialShopPopupView : MonoBehaviour
{
    [SerializeField] private Text titleLabel, priceLabel, diamondLabel, goldLabel;
    [SerializeField] private RawImage artwork;
    [SerializeField] private Button closeButton, backdropButton, priceButton;
    [SerializeField] private RectTransform frame;
    public event Action CloseRequested;
    public bool IsConfigured => titleLabel != null && priceLabel != null && diamondLabel != null &&
        goldLabel != null && artwork != null && closeButton != null && backdropButton != null && priceButton != null && frame != null;

    public void Bind(SpecialShopOffer value)
    {
        if (titleLabel != null) titleLabel.text = value.title;
        if (priceLabel != null) priceLabel.text = value.previewPrice + " · 시안";
        if (diamondLabel != null) diamondLabel.text = value.diamonds.ToString("N0");
        if (goldLabel != null) goldLabel.text = value.gold.ToString("N0");
        SetArtwork(artwork, value);
        // Remains a preview even if interactability is accidentally enabled in the prefab.
        if (priceButton != null) priceButton.interactable = false;
        BindClose(closeButton); BindClose(backdropButton);
        UISelectionMotion.Play(frame, true);
    }
    private void BindClose(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveListener(RequestClose);
        button.onClick.AddListener(RequestClose);
    }
    private void RequestClose() => CloseRequested?.Invoke();
    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(RequestClose);
        if (backdropButton != null) backdropButton.onClick.RemoveListener(RequestClose);
        CloseRequested = null;
    }
    public static void SetArtwork(RawImage image, SpecialShopOffer value)
    {
        if (image == null) return;
        image.texture = value.artwork;
        image.uvRect = value.artworkUV;
        var fit = image.GetComponent<AspectRatioFitter>();
        if (fit != null && value.artwork != null && value.artworkUV.height > 0)
            fit.aspectRatio = value.artwork.width * value.artworkUV.width / (value.artwork.height * value.artworkUV.height);
    }
#if UNITY_EDITOR
    public void Configure(Text title, Text price, Text diamonds, Text gold, RawImage art,
        Button close, Button backdrop, Button purchase, RectTransform popupFrame)
    {
        titleLabel = title; priceLabel = price; diamondLabel = diamonds; goldLabel = gold;
        artwork = art; closeButton = close; backdropButton = backdrop; priceButton = purchase; frame = popupFrame;
    }
#endif
}
