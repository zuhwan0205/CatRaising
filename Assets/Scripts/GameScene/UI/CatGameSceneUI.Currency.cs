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
        if (safeArea == null) return false;
        var header = safeArea.Find("Currency Bar");
        if (headerVersion >= 2 && header != null)
        {
            bool changed = headerVersion < 5;
            ConfigureProfileHeader(header);
            return changed;
        }
        if (header != null)
        {
            header.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(header.gameObject); else DestroyImmediate(header.gameObject);
        }
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Mona12TextKR");
        BuildCurrencyHeader();
        return true;
    }

    private void ConfigureProfileHeader(Transform header)
    {
        header.GetComponent<Image>().color = Color.clear;
        var trim = header.Find("Bottom Trim");
        if (trim != null) trim.gameObject.SetActive(false);
        foreach (Transform child in header)
        {
            if (child.GetComponent<Text>() != null) child.gameObject.SetActive(false);
            if (child.name == "Diamond Row" || child.name == "Gold Row")
            {
                child.GetComponent<Image>().color = new Color(.10f, .17f, .17f, .40f);
                var row = (RectTransform)child;
                row.anchorMin = new Vector2(.46f, row.anchorMin.y);
                row.anchorMax = new Vector2(.83f, row.anchorMax.y);
                row.offsetMin = row.offsetMax = Vector2.zero;
            }
        }
        var frame = header.Find("Profile Frame");
        if (frame != null)
        {
            frame.GetComponent<Image>().raycastTarget = true;
            var button = frame.GetComponent<Button>();
            if (button == null) button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame.GetComponent<Image>();
            button.onClick.RemoveListener(ShowProfilePopup);
            button.onClick.AddListener(ShowProfilePopup);
        }
        headerVersion = 5;
        ConfigureSettingsButton(header);
    }
    private void BuildCurrencyHeader()
    {
        var frame = Panel(safeArea, "Currency Bar", new Vector2(.025f, .9095f), new Vector2(.975f, .995f), Color.clear);
        var portraitFrame = Panel(frame, "Profile Frame", new Vector2(.015f, .09f), new Vector2(.18f, .91f), new Color(.88f, .69f, .35f));
        profilePortrait = Panel(portraitFrame, "Profile Portrait", new Vector2(.045f, .045f), new Vector2(.955f, .955f), new Color(.31f, .40f, .32f));
        Label(frame, "고양이 집사", 23, new Vector2(.20f, .48f), new Vector2(.57f, .90f)).color = new Color(1, .94f, .77f);
        profileLevel = Label(frame, "Lv.1", 20, new Vector2(.20f, .10f), new Vector2(.57f, .49f));
        profileLevel.color = Color.white;
        var diamond = Panel(frame, "Diamond Row", new Vector2(.61f, .53f), new Vector2(.98f, .94f), new Color(.10f, .17f, .17f));
        var gold = Panel(frame, "Gold Row", new Vector2(.61f, .06f), new Vector2(.98f, .47f), new Color(.10f, .17f, .17f));
        gemAmount = CurrencyCell(diamond, "Diamond", 0, true);
        goldAmount = CurrencyCell(gold, "Gold", 0, false);
        headerVersion = 4;
        AddProfileTextOutline(frame);
        profileCharacterId = null;
        RefreshProfile();
        foreach (var graphic in frame.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
        ConfigureProfileHeader(frame);
    }

    private void RefreshProfile()
    {
        if (profileLevel == null || profilePortrait == null) return;
        profileLevel.text = $"Lv.{(data == null ? 1 : data.progress.accountLevel)}";
        string id = ProfilePortraitId;
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

    private void AddProfileTextOutline(Transform header)
    {
        foreach (var text in header.GetComponentsInChildren<Text>(true))
        {
            if (text.transform.parent != header || text.GetComponent<Outline>() != null) continue;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.06f, .08f, .06f, .85f);
            outline.effectDistance = new Vector2(1, -1);
        }
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
        value.resizeTextMinSize = 16;
        value.resizeTextMaxSize = value.fontSize;
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
