#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// Used only to create missing templates. Runtime never rebuilds these layouts.
public partial class CatGameSceneUI
{
    private SpecialShopCatalog authoringOffers;
    public void CreateSpecialShopTemplates(string folder, SpecialShopCatalog offers)
    {
        authoringOffers = offers;
        uiFont = Resources.Load<Font>("Fonts/Mona12TextKR");
        page = Rect(transform, "Authoring Page", Vector2.zero, Vector2.one);
        safeArea = Rect(transform, "Authoring Safe Area", Vector2.zero, Vector2.one);
        if (!System.IO.File.Exists(folder + "/SpecialShopView.prefab") || !System.IO.File.Exists(folder + "/SpecialShopCard.prefab"))
            BuildShopTemplate(folder);
        if (!System.IO.File.Exists(folder + "/SpecialShopPopup.prefab")) BuildPopupTemplate(0, folder);
    }
    private static GameObject SaveShopPrefab(GameObject root, string path)
    {
        // Procedural rounded sprites must be assets before serializing a prefab.
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            var sprite = image.sprite;
            if (sprite == null || EditorUtility.IsPersistent(sprite)) continue;
            const string shapePath = "Assets/Scripts/Resources/SpecialShop/RoundedShape.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Texture2D>(shapePath);
            if (saved == null)
            {
                AssetDatabase.CreateAsset(sprite.texture, shapePath);
                AssetDatabase.AddObjectToAsset(sprite, shapePath);
            }
            else
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(shapePath))
                    if (asset is Sprite existing) { image.sprite = existing; break; }
            }
        }
        // Never overwrite a designer-edited prefab.
        if (System.IO.File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return PrefabUtility.SaveAsPrefabAsset(root, path);
    }
    private RectTransform packagePopup;

    private void BuildShopTemplate(string folder)
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
        for (int i = 0; i < 1; i++)
        {
            int index = i;
            var offer = authoringOffers.offers[i];
            var card = Panel(content, offer.title, new Vector2(i%2*.5f+.015f,1), new Vector2(i%2*.5f+.485f,1), new Color(.16f,.18f,.24f));
            card.pivot = new Vector2(.5f,1);
            card.anchoredPosition = new Vector2(0,-(i/2)*410-5);
            card.sizeDelta = new Vector2(0,395);
            RoundCollectionPanel(card);
            var face = Panel(card, "Package Face", new Vector2(.018f,.015f), new Vector2(.982f,.985f), offer.background);
            RoundCollectionPanel(face);
            face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            PackageArtwork(face, index, Vector2.zero, Vector2.one, true);
            var titleBand = Panel(face, "Package Title Band", new Vector2(0,.82f), Vector2.one, new Color(.12f,.09f,.07f,.72f));
            var title = Label(titleBand, offer.title, 26, new Vector2(.03f,0), new Vector2(.97f,1));
            title.color = Color.white;
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 18; title.resizeTextMaxSize = 26;
            var price = Panel(face, "Preview Price", new Vector2(.025f,.025f), new Vector2(.975f,.17f), new Color(.12f,.09f,.07f,.82f));
            RoundCollectionPanel(price);
            Label(price, offer.previewPrice, 28, Vector2.zero, Vector2.one).color = Color.white;
            // 장식은 입력을 받지 않고 카드 전체가 상세 보기 버튼입니다.
            foreach (var graphic in card.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = graphic.gameObject == card.gameObject;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            var cardView = card.gameObject.AddComponent<SpecialShopCardView>();
            cardView.Configure(button, title, price.GetComponentInChildren<Text>(), card.GetComponentInChildren<RawImage>());
            card.gameObject.AddComponent<ButtonTweenEffect>();
            var cardAsset = SaveShopPrefab(card.gameObject, folder + "/SpecialShopCard.prefab").GetComponent<SpecialShopCardView>();
            DestroyImmediate(card.gameObject);
            var grid = content.gameObject.AddComponent<SpecialShopGrid>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(300, 395);
            grid.spacing = new Vector2(16, 15);
            grid.padding = new RectOffset(8, 8, 5, 5);
            var size = content.gameObject.AddComponent<ContentSizeFitter>();
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            paper.gameObject.AddComponent<SpecialShopView>().Configure(content, cardAsset);
        }
        SaveShopPrefab(paper.gameObject, folder + "/SpecialShopView.prefab");
    }

    private static Texture2D packageArtwork;

    private void PackageArtwork(Transform parent, int index, Vector2 min, Vector2 max, bool fill = false)
    {
        if (packageArtwork == null) packageArtwork = Resources.Load<Texture2D>("PackageShopArt");
        if (packageArtwork == null) return;
        var slot = Rect(parent, "Package Illustration", min, max);
        var art = Rect(slot, "Artwork", Vector2.zero, Vector2.one);
        var image = art.gameObject.AddComponent<RawImage>();
        image.texture = packageArtwork;
        image.uvRect = new Rect(index % 2 * .5f, index < 2 ? .5f : 0, .5f, .5f);
        image.raycastTarget = false;
        var fit = art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = fill ? AspectRatioFitter.AspectMode.EnvelopeParent : AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = (float)packageArtwork.width / packageArtwork.height;
    }

    private void PackageRewards(Transform parent, SpecialShopOffer offer, Vector2 min, Vector2 max)
    {
        var row = Rect(parent, "Package Contents", min, max);
        for (int i = 0; i < 2; i++)
        {
            bool diamond = i == 0;
            var tile = Panel(row, diamond ? "Diamonds" : "Gold", new Vector2(i*.51f,0), new Vector2(i*.51f+.49f,1), new Color(.28f,.23f,.20f));
            RoundCollectionPanel(tile);
            var name = Label(tile, diamond ? "다이아" : "골드", 22, new Vector2(.03f,.76f), new Vector2(.97f,.98f));
            name.color = diamond ? new Color(.65f,.93f,1) : new Color(1,.85f,.45f);
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 16;
            name.resizeTextMaxSize = 22;
            var amount = CurrencyCell(tile, "Reward", 0, diamond);
            var icon = (RectTransform)tile.Find("Reward/Reward Icon");
            icon.anchorMin = new Vector2(.20f,.33f);
            icon.anchorMax = new Vector2(.80f,.74f);
            icon.offsetMin = icon.offsetMax = Vector2.zero;
            var fit = icon.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            // FitInParent은 전용 슬롯 안에서만 적용해 이름/수량 영역을 침범하지 않습니다.
            var slot = Rect(tile, "Reward Icon Slot", new Vector2(.20f,.33f), new Vector2(.80f,.74f));
            icon.SetParent(slot, false);
            fit.aspectRatio = 1;
            var amountRect = amount.rectTransform;
            amountRect.anchorMin = new Vector2(.03f,.03f);
            amountRect.anchorMax = new Vector2(.97f,.31f);
            amountRect.offsetMin = amountRect.offsetMax = Vector2.zero;
            amount.text = (diamond ? offer.diamonds : offer.gold).ToString("N0");
            amount.alignment = TextAnchor.MiddleCenter;
            amount.fontStyle = FontStyle.Bold;
            amount.fontSize = 26;
            amount.resizeTextMaxSize = 26;
            amount.resizeTextMinSize = 16;
        }
    }

    private void BuildPopupTemplate(int index, string folder)
    {
        if (packagePopup != null) DestroyImmediate(packagePopup.gameObject);
        var offer = authoringOffers.offers[index];
        packagePopup = Panel(safeArea, "Package Preview Popup", Vector2.zero, Vector2.one, new Color(.06f,.04f,.02f,.78f));
        packagePopup.SetAsLastSibling();
        var body = PopupContent(packagePopup, "Package Popup Frame", "Package Popup Paper", new Vector2(.045f,.18f), new Vector2(.955f,.85f), new Color(.44f,.28f,.15f), ClosePackagePreview, out var frame);
        DecorateCatPopup(body);
        var hero = Panel(body, "Package Hero", new Vector2(.025f,.55f), new Vector2(.975f,.975f), Color.Lerp(offer.background,Color.white,.22f));
        RoundCollectionPanel(hero);
        PackageArtwork(hero, index, Vector2.zero, Vector2.one);
        var titleRibbon = Panel(hero, "Title Ribbon", new Vector2(0,.82f), Vector2.one, new Color(.16f,.10f,.07f,.82f));
        var heroTitle = Label(titleRibbon, offer.title, 28, new Vector2(.03f,0), new Vector2(.86f,1));
        heroTitle.color = cream;
        heroTitle.fontStyle = FontStyle.Bold;
        heroTitle.resizeTextForBestFit = true;
        heroTitle.resizeTextMinSize = 18;
        heroTitle.resizeTextMaxSize = 28;

        var closeButton = Button(body, "×", new Vector2(.88f,.90f), new Vector2(.98f,.98f), ClosePackagePreview);
        Label(body, "패키지 구성", 24, new Vector2(.08f,.47f), new Vector2(.92f,.54f));
        PackageRewards(body, offer, new Vector2(.07f,.27f), new Vector2(.93f,.47f));
        var price = Button(body, offer.previewPrice + " · 시안", new Vector2(.15f,.12f), new Vector2(.85f,.24f), () => { });
        CozyButtonStyle.SetColor(price, new Color(1,.75f,.27f));
        var colors = price.colors; colors.disabledColor = new Color(.87f,.72f,.43f); price.colors = colors;
        price.interactable = false;
        Label(body, "예시 상품입니다 · 현재 구매할 수 없습니다", 18, new Vector2(.04f,.025f), new Vector2(.96f,.10f));
        var contents = body.Find("Package Contents");
        packagePopup.gameObject.AddComponent<SpecialShopPopupView>().Configure(heroTitle, price.GetComponentInChildren<Text>(),
            contents.Find("Diamonds/Reward").GetComponentInChildren<Text>(),
            contents.Find("Gold/Reward").GetComponentsInChildren<Text>()[1], hero.GetComponentInChildren<RawImage>(),
            closeButton, packagePopup.GetComponent<Button>(), price, frame);
        // Select exact amount labels, not the gold icon's decorative G.
        var popupView = packagePopup.GetComponent<SpecialShopPopupView>();
        popupView.Configure(heroTitle, price.GetComponentInChildren<Text>(),
            contents.Find("Diamonds/Reward/Label").GetComponent<Text>(),
            contents.Find("Gold/Reward/Label").GetComponent<Text>(), hero.GetComponentInChildren<RawImage>(),
            closeButton, packagePopup.GetComponent<Button>(), price, frame);
        SaveShopPrefab(packagePopup.gameObject, folder + "/SpecialShopPopup.prefab");
    }

    private void ClosePackagePreview()
    {
        if (packagePopup != null) DestroyImmediate(packagePopup.gameObject);
        packagePopup = null;
    }
}

#endif
