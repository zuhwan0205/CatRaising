using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private static Sprite collectionCorner;

    private static Sprite CollectionCorner()
    {
        if (collectionCorner != null) return collectionCorner;
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Max(8 - (x + .5f), (x + .5f) - 24, 0);
                float dy = Mathf.Max(8 - (y + .5f), (y + .5f) - 24, 0);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(8.5f - Mathf.Sqrt(dx * dx + dy * dy))));
            }
        texture.Apply();
        collectionCorner = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(9, 9, 9, 9));
        return collectionCorner;
    }

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
        image.sprite = CollectionCorner();
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
