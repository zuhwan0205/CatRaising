using System;

[Serializable]
public sealed class ShopOffer
{
    public bool available;
    public long goldPrice = 100;
    public bool CanSell => available && goldPrice > 0;
}
