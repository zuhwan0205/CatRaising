using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    [SerializeField] private int headerVersion;
    [SerializeField] private Text profileLevel;
    [SerializeField] private RectTransform profilePortrait;
    private string profileCharacterId;

    public bool UpgradeCurrencyHeader()
    {
        if (safeArea == null || headerVersion >= 3) return false;
        if (headerVersion == 2)
        {
            var existing = safeArea.Find("Currency Bar") as RectTransform;
            if (existing != null)
            {
                existing.anchorMin = new Vector2(.025f, .9095f);
                existing.anchorMax = new Vector2(.975f, .995f);
                if (page != null) page.anchorMax = new Vector2(page.anchorMax.x, .9015f);
                headerVersion = 3;
                return true;
            }
        }
        var old = safeArea.Find("Currency Bar");
        if (old != null)
        {
            old.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Mona12TextKR");
        BuildCurrencyHeader();
        if (page != null) page.anchorMax = new Vector2(page.anchorMax.x, .9015f);
        return true;
    }

    private void BuildCurrencyHeader()
    {
        var frame = Panel(safeArea, "Currency Bar", new Vector2(.025f, .9095f), new Vector2(.975f, .995f), new Color(.18f, .25f, .23f));
        Panel(frame, "Bottom Trim", Vector2.zero, new Vector2(1, .035f), new Color(.80f, .64f, .33f));
        var portraitFrame = Panel(frame, "Profile Frame", new Vector2(.015f, .09f), new Vector2(.18f, .91f), new Color(.88f, .69f, .35f));
        profilePortrait = Panel(portraitFrame, "Profile Portrait", new Vector2(.045f, .045f), new Vector2(.955f, .955f), new Color(.31f, .40f, .32f));
        Label(frame, "고양이 집사", 23, new Vector2(.20f, .48f), new Vector2(.57f, .90f)).color = new Color(1, .94f, .77f);
        profileLevel = Label(frame, "Lv.1", 20, new Vector2(.20f, .10f), new Vector2(.57f, .49f));
        profileLevel.color = Color.white;
        var diamond = Panel(frame, "Diamond Row", new Vector2(.61f, .53f), new Vector2(.98f, .94f), new Color(.10f, .17f, .17f));
        var gold = Panel(frame, "Gold Row", new Vector2(.61f, .06f), new Vector2(.98f, .47f), new Color(.10f, .17f, .17f));
        gemAmount = CurrencyCell(diamond, "Diamond", 0, true);
        goldAmount = CurrencyCell(gold, "Gold", 0, false);
        headerVersion = 3;
        profileCharacterId = null;
        RefreshProfile();
        foreach (var graphic in frame.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
    }

    private void RefreshProfile()
    {
        if (profileLevel == null || profilePortrait == null) return;
        profileLevel.text = $"Lv.{(data == null ? 1 : data.progress.accountLevel)}";
        string id = data == null ? CatGameCatalog.StarterCharacterId : data.loadout.characterId;
        if (profileCharacterId == id) return;
        for (int i = profilePortrait.childCount - 1; i >= 0; i--)
        {
            var child = profilePortrait.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        var definitions = catalog != null ? catalog : Resources.Load<CatGameCatalog>("DefaultCatCatalog");
        CharacterPortrait(profilePortrait, definitions == null ? null : definitions.FindCharacter(id));
        profileCharacterId = id;
    }
    private Text CurrencyCell(RectTransform parent, string name, float left, bool diamond)
    {
        var cell = Rect(parent, name, new Vector2(left, 0), new Vector2(1, 1));
        var icon = Rect(cell, name + " Icon", new Vector2(.035f, -.12f), new Vector2(.24f, 1.12f));
        // 숫자 길이에 따라 아이콘이 늘어나지 않도록 고정 크기로 표시합니다.
        icon.anchorMin = icon.anchorMax = new Vector2(.13f, .5f);
        icon.sizeDelta = new Vector2(25, 25);
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
        var value = Label(cell, "0", 21, new Vector2(.27f, 0), new Vector2(.94f, 1));
        value.alignment = TextAnchor.MiddleRight;
        value.color = new Color(1f, .97f, .83f);
        value.resizeTextForBestFit = true;
        value.resizeTextMinSize = 12;
        value.resizeTextMaxSize = 29;
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
