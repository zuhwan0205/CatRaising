using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    [SerializeField] private Button specialShopButton;

    private bool UpgradeFiveButtonLayout()
    {
        var navigation = safeArea.Find("Navigation") as RectTransform;
        if (navigation == null) return false;
        bool changed = navigationSizeVersion < 4;
        if (changed)
        {
            navigation.anchorMin = new Vector2(.025f, .015f);
            navigation.anchorMax = new Vector2(.975f, .12f);
            // 기존 4개 탭 배열과 이벤트 연결을 유지하고 표시 위치만 재배치합니다.
            // 모험은 중앙 28%, 나머지는 15% 너비로 간격을 확보합니다.
            float[] lefts = { .36f, .015f, .665f, .835f };
            float[] rights = { .64f, .165f, .815f, .985f };
            for (int i = 0; i < tabs.Length; i++)
            {
                var rect = (RectTransform)tabs[i].transform;
                rect.anchorMin = new Vector2(lefts[i], 0);
                rect.anchorMax = new Vector2(rights[i], i == 0 ? 1 : .76f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            navigationSizeVersion = 4;
        }
        if (specialShopButton == null)
        {
            specialShopButton = Button(navigation, "", new Vector2(.185f, 0), new Vector2(.335f, .76f), () => Navigate("특별 상점"));
            specialShopButton.name = "Special Shop";
            CozyButtonStyle.SetColor(specialShopButton, CozyButtonStyle.Peach);
            var slot = Rect(specialShopButton.transform, "Gift Icon Slot", new Vector2(.15f,.12f), new Vector2(.85f,.88f));
            var icon = Rect(slot, "Gift Icon", Vector2.zero, Vector2.one);
            var fit = icon.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1;
            Panel(icon, "Gift Shadow", new Vector2(.12f,.08f), new Vector2(.88f,.77f), new Color(.40f,.20f,.14f));
            Panel(icon, "Gift Box", new Vector2(.16f,.14f), new Vector2(.84f,.76f), new Color(.91f,.39f,.35f));
            Panel(icon, "Gift Lid", new Vector2(.08f,.65f), new Vector2(.92f,.82f), new Color(1,.58f,.43f));
            Panel(icon, "Gold Ribbon", new Vector2(.43f,.14f), new Vector2(.57f,.84f), new Color(1,.85f,.40f));
            Panel(icon, "Ribbon Left", new Vector2(.23f,.83f), new Vector2(.46f,.95f), new Color(1,.85f,.40f));
            Panel(icon, "Ribbon Right", new Vector2(.54f,.83f), new Vector2(.77f,.95f), new Color(1,.85f,.40f));
            foreach (var graphic in slot.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            changed = true;
        }
        if (changed)
        {
            var specialRect = (RectTransform)specialShopButton.transform;
            specialRect.anchorMin = new Vector2(.185f, 0);
            specialRect.anchorMax = new Vector2(.335f, .76f);
            specialRect.offsetMin = specialRect.offsetMax = Vector2.zero;
        }
        var giftIcon = specialShopButton.transform.Find("Gift Icon Slot/Gift Icon");
        if (giftIcon != null && giftIcon.localScale != Vector3.one * .85f)
        {
            giftIcon.localScale = Vector3.one * .85f;
            changed = true;
        }
        specialShopButton.onClick.RemoveAllListeners();
        specialShopButton.onClick.AddListener(() => Navigate("특별 상점"));
        return changed;
    }

    // 시안 전용 데이터: 스토어 상품이나 실제 지급 처리와 연결하지 않습니다.
    private sealed class PackagePreview
    {
        public string title, price;
        public long gems, gold;
        public Color color;
        public PackagePreview(string title, string price, long gems, long gold, Color color)
        { this.title = title; this.price = price; this.gems = gems; this.gold = gold; this.color = color; }
    }
    private static readonly PackagePreview[] packagePreviews =
    {
        new PackagePreview("첫 만남 패키지", "₩4,900", 200, 30000, new Color(.20f,.59f,.65f)),
        new PackagePreview("반짝 다이아 패키지", "₩9,900", 500, 60000, new Color(.43f,.39f,.69f)),
        new PackagePreview("쑥쑥 성장 패키지", "₩19,000", 1200, 200000, new Color(.70f,.39f,.29f)),
        new PackagePreview("집사의 보물 패키지", "₩29,000", 2000, 500000, new Color(.62f,.48f,.23f))
    };
    private RectTransform packagePopup;

    private void SpecialShop()
    {
        var paper = Panel(page, "Special Shop Paper", Vector2.zero, Vector2.one, cream);
        DecorateCatPopup(paper);
        var banner = Panel(paper, "Special Shop Banner", new Vector2(.03f,.89f), new Vector2(.97f,.99f), new Color(.58f,.31f,.24f));
        RoundCollectionPanel(banner);
        Label(banner, "특별 상점", 32, Vector2.zero, Vector2.one).color = cream;
        Label(paper, "미리보기 · 구성과 가격은 예시입니다", 19, new Vector2(.03f,.83f), new Vector2(.97f,.89f));
        var viewport = Panel(paper, "Package Viewport", new Vector2(.02f,.02f), new Vector2(.98f,.82f), Color.clear);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Package Cards", new Vector2(0,1), Vector2.one);
        content.pivot = new Vector2(.5f,1);
        content.sizeDelta = new Vector2(0,840);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45;
        for (int i = 0; i < packagePreviews.Length; i++)
        {
            int index = i;
            var offer = packagePreviews[i];
            var card = Panel(content, offer.title, new Vector2(i%2*.5f+.015f,1), new Vector2(i%2*.5f+.485f,1), new Color(.16f,.18f,.24f));
            card.pivot = new Vector2(.5f,1);
            card.anchoredPosition = new Vector2(0,-(i/2)*410-5);
            card.sizeDelta = new Vector2(0,395);
            RoundCollectionPanel(card);
            var face = Panel(card, "Package Face", new Vector2(.018f,.015f), new Vector2(.982f,.985f), offer.color);
            RoundCollectionPanel(face);
            var shine = Panel(face, "Package Highlight", new Vector2(.035f,.34f), new Vector2(.965f,.96f), Color.Lerp(offer.color,Color.white,.26f));
            RoundCollectionPanel(shine);
            Label(face, "PACKAGE", 17, new Vector2(.06f,.87f), new Vector2(.94f,.97f)).color = cream;
            PackageGift(face, new Vector2(.20f,.53f), new Vector2(.80f,.85f));
            var title = Label(face, offer.title, 26, new Vector2(.025f,.40f), new Vector2(.975f,.54f));
            title.color = Color.white;
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 18; title.resizeTextMaxSize = 26;
            PackageRewards(face, offer, new Vector2(.04f,.20f), new Vector2(.96f,.38f));
            var price = Panel(face, "Preview Price", new Vector2(.025f,.025f), new Vector2(.975f,.17f), Color.Lerp(offer.color,Color.black,.38f));
            RoundCollectionPanel(price);
            Label(price, offer.price, 28, Vector2.zero, Vector2.one).color = Color.white;
            // 장식은 입력을 받지 않고 카드 전체가 상세 보기 버튼입니다.
            foreach (var graphic in card.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = graphic.gameObject == card.gameObject;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ShowPackagePreview(index));
            card.gameObject.AddComponent<ButtonTweenEffect>();
        }
    }

    private void PackageGift(Transform parent, Vector2 min, Vector2 max)
    {
        var slot = Rect(parent, "Package Gift Slot", min, max);
        var gift = Rect(slot, "Package Gift", Vector2.zero, Vector2.one);
        var aspect = gift.gameObject.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = 1;
        Panel(gift, "Shadow", new Vector2(.10f,.06f), new Vector2(.90f,.75f), new Color(.30f,.18f,.15f));
        Panel(gift, "Box", new Vector2(.15f,.12f), new Vector2(.85f,.74f), new Color(.98f,.68f,.35f));
        Panel(gift, "Lid", new Vector2(.08f,.66f), new Vector2(.92f,.81f), new Color(1,.86f,.52f));
        Panel(gift, "Ribbon", new Vector2(.43f,.12f), new Vector2(.57f,.82f), new Color(.80f,.28f,.30f));
        Panel(gift, "Bow Left", new Vector2(.22f,.82f), new Vector2(.45f,.96f), new Color(.96f,.40f,.41f));
        Panel(gift, "Bow Right", new Vector2(.55f,.82f), new Vector2(.78f,.96f), new Color(.96f,.40f,.41f));
        foreach (var graphic in gift.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
    }

    private void PackageRewards(Transform parent, PackagePreview offer, Vector2 min, Vector2 max)
    {
        var row = Rect(parent, "Package Contents", min, max);
        for (int i = 0; i < 2; i++)
        {
            var tile = Panel(row, i == 0 ? "Diamonds" : "Gold", new Vector2(i*.51f,0), new Vector2(i*.51f+.49f,1), new Color(.28f,.23f,.20f));
            RoundCollectionPanel(tile);
            var amount = CurrencyCell(tile, "Reward", 0, i == 0);
            amount.text = (i == 0 ? offer.gems : offer.gold).ToString("N0");
            amount.fontSize = 22;
            amount.resizeTextMaxSize = 22;
            amount.resizeTextMinSize = 15;
        }
    }

    private void ShowPackagePreview(int index)
    {
        if (packagePopup != null) RemoveUI(packagePopup.gameObject);
        var offer = packagePreviews[index];
        packagePopup = Panel(safeArea, "Package Preview Popup", Vector2.zero, Vector2.one, new Color(.06f,.04f,.02f,.78f));
        packagePopup.SetAsLastSibling();
        var body = PopupContent(packagePopup, "Package Popup Frame", "Package Popup Paper", new Vector2(.045f,.18f), new Vector2(.955f,.85f), new Color(.44f,.28f,.15f), ClosePackagePreview, out var frame);
        DecorateCatPopup(body);
        var hero = Panel(body, "Package Hero", new Vector2(.025f,.48f), new Vector2(.975f,.975f), Color.Lerp(offer.color,Color.white,.22f));
        RoundCollectionPanel(hero);
        Label(hero, offer.title, 30, new Vector2(.04f,.68f), new Vector2(.88f,.96f)).color = Color.white;
        PackageGift(hero, new Vector2(.24f,.08f), new Vector2(.76f,.67f));
        Button(body, "×", new Vector2(.88f,.90f), new Vector2(.98f,.98f), ClosePackagePreview);
        Label(body, "패키지 구성", 24, new Vector2(.08f,.40f), new Vector2(.92f,.48f));
        PackageRewards(body, offer, new Vector2(.07f,.28f), new Vector2(.93f,.40f));
        var price = Button(body, offer.price + " · 시안", new Vector2(.15f,.12f), new Vector2(.85f,.24f), () => { });
        CozyButtonStyle.SetColor(price, new Color(1,.75f,.27f));
        var colors = price.colors; colors.disabledColor = new Color(.87f,.72f,.43f); price.colors = colors;
        price.interactable = false;
        Label(body, "예시 상품입니다 · 현재 구매할 수 없습니다", 18, new Vector2(.04f,.025f), new Vector2(.96f,.10f));
        UISelectionMotion.Play(frame, true);
    }

    private void ClosePackagePreview()
    {
        if (packagePopup != null) RemoveUI(packagePopup.gameObject);
        packagePopup = null;
    }
}
