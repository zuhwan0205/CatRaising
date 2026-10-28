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
        if (changed && giftIcon != null && giftIcon.localScale != Vector3.one * .85f)
        {
            giftIcon.localScale = Vector3.one * .85f;
            changed = true;
        }
        specialShopButton.onClick.RemoveAllListeners();
        specialShopButton.onClick.AddListener(() => Navigate("특별 상점"));
        return changed;
    }

    private SpecialShopPresenter specialShopPresenter;

    private void SpecialShop()
    {
        var template = Resources.Load<SpecialShopView>("SpecialShop/SpecialShopView");
        var popup = Resources.Load<SpecialShopPopupView>("SpecialShop/SpecialShopPopup");
        var offers = Resources.Load<SpecialShopCatalog>("SpecialShop/SpecialShopCatalog");
        if (template == null || popup == null || offers == null || !template.IsConfigured || !popup.IsConfigured)
        {
            ShowStatus("특별 상점 프리팹 연결을 확인해주세요. CatRaising > Prepare Special Shop Prefabs");
            return;
        }
        var view = Instantiate(template, page, false);
        view.name = "Special Shop Paper";
        specialShopPresenter = new SpecialShopPresenter(view, offers, popup, safeArea, RemoveUI,
            () => !saving && drawOverlay == null && detailOverlay == null);
    }

    private void ReleaseSpecialShop()
    {
        specialShopPresenter?.Dispose();
        specialShopPresenter = null;
    }
}
