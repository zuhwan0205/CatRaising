using UnityEngine;

public sealed class SpecialShopView : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private SpecialShopCardView cardPrefab;
    public bool IsConfigured => content != null && cardPrefab != null && cardPrefab.IsConfigured;

    public SpecialShopCardView AddCard(SpecialShopOffer offer)
    {
        var card = Instantiate(cardPrefab, content, false);
        card.name = offer.id;
        card.Bind(offer);
        return card;
    }
#if UNITY_EDITOR
    public void Configure(RectTransform parent, SpecialShopCardView template)
    { content = parent; cardPrefab = template; }
#endif
}
