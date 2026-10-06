using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private void BuildCurrencyHeader()
    {
        var frame = Panel(safeArea, "Currency Bar", new Vector2(.025f, .944f), new Vector2(.975f, .989f), new Color(.20f, .12f, .05f));
        var gold = Panel(frame, "Gold Rim", new Vector2(.005f, .08f), new Vector2(.995f, .94f), new Color(.87f, .65f, .20f));
        var inner = Panel(gold, "Green Inset", new Vector2(.008f, .12f), new Vector2(.992f, .88f), new Color(.08f, .24f, .15f));
        Panel(inner, "Top Highlight", new Vector2(0, .90f), Vector2.one, new Color(.37f, .55f, .27f));
        Panel(inner, "Divider", new Vector2(.496f, 0), new Vector2(.504f, 1), new Color(.79f, .57f, .17f));
        gemAmount = CurrencyCell(inner, "Diamond", 0, true);
        goldAmount = CurrencyCell(inner, "Gold", .5f, false);
        foreach (var graphic in frame.GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = false;
    }

    private Text CurrencyCell(RectTransform parent, string name, float left, bool diamond)
    {
        var cell = Rect(parent, name, new Vector2(left, 0), new Vector2(left + .49f, 1));
        var icon = Rect(cell, name + " Icon", new Vector2(.035f, -.12f), new Vector2(.24f, 1.12f));
        // 숫자 길이에 따라 아이콘이 늘어나지 않도록 고정 크기로 표시합니다.
        icon.anchorMin = icon.anchorMax = new Vector2(.13f, .5f);
        icon.sizeDelta = new Vector2(46, 46);
        var outline = Panel(icon, "Icon Outline", new Vector2(.1f, .1f), new Vector2(.9f, .9f), new Color(.10f, .12f, .09f));
        if (diamond)
            outline.localRotation = Quaternion.Euler(0, 0, 45);
        var face = Panel(outline, "Icon Face", new Vector2(.12f, .12f), new Vector2(.88f, .88f),
            diamond ? new Color(.18f, .72f, .94f) : new Color(1f, .77f, .17f));
        Panel(face, "Light Facet", new Vector2(.1f, .52f), new Vector2(.56f, .9f),
            diamond ? new Color(.74f, .96f, 1f) : new Color(1f, .94f, .51f));
        if (!diamond)
        {
            var symbol = Label(face, "G", 25, Vector2.zero, Vector2.one);
            symbol.color = new Color(.47f, .29f, .04f);
            symbol.alignment = TextAnchor.MiddleCenter;
        }
        var value = Label(cell, "0", 26, new Vector2(.27f, 0), new Vector2(.94f, 1));
        value.alignment = TextAnchor.MiddleRight;
        value.color = new Color(1f, .97f, .83f);
        value.resizeTextForBestFit = true;
        value.resizeTextMinSize = 16;
        value.resizeTextMaxSize = 26;
        var shadow = value.gameObject.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);
        return value;
    }

    private static string CompactCurrency(long amount)
    {
        if (amount < 1000000)
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        if (amount < 1000000000)
            return (amount / 1000000d).ToString("0.##", CultureInfo.InvariantCulture) + "M";
        if (amount < 1000000000000)
            return (amount / 1000000000d).ToString("0.##", CultureInfo.InvariantCulture) + "B";
        return (amount / 1000000000000d).ToString("0.##", CultureInfo.InvariantCulture) + "T";
    }
}
