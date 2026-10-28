using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private RectTransform CollectionFrame(Transform parent, string name, Color rarity)
    {
        var card = Panel(parent, name, Vector2.zero, Vector2.one, new Color(.10f, .07f, .14f));
        RoundCollectionPanel(card);
        var shadow = card.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(.08f, .04f, .10f, .55f);
        shadow.effectDistance = new Vector2(2, -5);
        var rim = Panel(card, "Rarity Frame", Vector2.zero, Vector2.one, rarity);
        rim.offsetMin = new Vector2(2, 3);
        rim.offsetMax = new Vector2(-2, -2);
        RoundCollectionPanel(rim);
        var light = Panel(rim, "Frame Highlight", Vector2.zero, Vector2.one, Color.Lerp(rarity, Color.white, .30f));
        light.offsetMin = new Vector2(2, 4);
        light.offsetMax = new Vector2(-2, -2);
        RoundCollectionPanel(light);
        var face = Panel(light, "Dark Portrait Well", Vector2.zero, Vector2.one, Color.Lerp(new Color(.08f, .07f, .12f), rarity, .20f));
        face.offsetMin = new Vector2(2, 2);
        face.offsetMax = new Vector2(-2, -3);
        RoundCollectionPanel(face);
        foreach (var panel in new[] { rim, light, face }) panel.GetComponent<Image>().raycastTarget = false;
        return card;
    }

    private void RoundCollectionPanel(RectTransform panel)
    {
        var image = panel.GetComponent<Image>();
        image.sprite = RoundedUIShape.GetSprite();
        image.type = Image.Type.Sliced;
    }

    private void CollectionBadge(Transform card, string text, Vector2 min, Vector2 max, Color color)
    {
        var badge = Panel(card, "Card Badge", min, max, Color.Lerp(color, Color.black, .35f));
        RoundCollectionPanel(badge);
        badge.GetComponent<Image>().raycastTarget = false;
        Label(badge, text, 13, Vector2.zero, Vector2.one).color = Color.white;
    }
}
