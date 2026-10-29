using System;
using UnityEngine;

[Serializable]
public sealed class SpecialShopOffer
{
    public string id;
    public string title;
    public string previewPrice;
    [Min(0)] public long diamonds, gold;
    public Texture2D artwork;
    public Rect artworkUV = new Rect(0, 0, 1, 1);
    public Color background = Color.white;
}

[CreateAssetMenu(menuName = "Cat Raising/Special Shop Catalog")]
public sealed class SpecialShopCatalog : ScriptableObject
{
    [Tooltip("시안 전용 상품입니다. 실제 결제·지급과 연결하지 않습니다.")]
    public SpecialShopOffer[] offers = Array.Empty<SpecialShopOffer>();
}
