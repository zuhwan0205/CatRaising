using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private RectTransform detailOverlay;
    private RectTransform cachedDetailOverlay, cachedDetailContent, cachedDetailFrame;
    private string detailTab;
    private OwnedRelic popupRelic;
    private CanvasGroup listInput;

    private RectTransform OpenDetailPopup(string tab)
    {
        bool opening = detailOverlay == null;
        if (cachedDetailContent != null)
            foreach (Transform child in cachedDetailContent) RemoveUI(child.gameObject);
        if (cachedDetailOverlay != null)
            foreach (Transform child in cachedDetailOverlay)
                if (child != cachedDetailFrame) RemoveUI(child.gameObject);
        detailTab = tab;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        listInput = page.GetComponent<CanvasGroup>();
        if (listInput == null) listInput = page.gameObject.AddComponent<CanvasGroup>();
        listInput.interactable = false;
        listInput.blocksRaycasts = false;
        foreach (var tabButton in tabs) tabButton.interactable = false;
        if (cachedDetailOverlay == null)
        {
            cachedDetailOverlay = Panel(safeArea, "Detail Popup Overlay", Vector2.zero, Vector2.one, new Color(.05f, .08f, .05f, .72f));
            var paper = PopupContent(cachedDetailOverlay, "Popup Wood Frame", "Popup Content",
                new Vector2(.035f, .15f), new Vector2(.965f, .90f), new Color(.44f, .28f, .12f), CloseDetailPopup, out cachedDetailFrame);
            DecorateCatPopup(paper);
            cachedDetailContent = Rect(paper, "Detail Contents", Vector2.zero, Vector2.one);
        }
        detailOverlay = cachedDetailOverlay;
        detailOverlay.gameObject.SetActive(true);
        detailOverlay.SetAsLastSibling();
        var content = cachedDetailContent;
        var frame = cachedDetailFrame;
        frame.anchorMin = new Vector2(.035f, .15f);
        frame.anchorMax = new Vector2(.965f, .90f);
        frame.localScale = Vector3.one;
        if (opening)
        {
            frame.localScale = Vector3.one * .9f;
            DOTween.To(() => frame.localScale, value => frame.localScale = value, Vector3.one, .22f)
                .SetEase(Ease.OutBack).SetUpdate(true).SetLink(frame.gameObject, LinkBehaviour.KillOnDisable);
        }
        return content;
    }

    private void DecorateCatPopup(RectTransform content)
    {
        content.GetComponent<Image>().color = new Color(1f, .95f, .87f);
        RoundCollectionPanel(content);
        // 장식을 먼저 만들어 내용 아래에 두고 입력을 가로채지 않습니다.
        var decorations = Rect(content, "Soft Paw Pattern", Vector2.zero, Vector2.one);
        for (int i = 0; i < 8; i++)
        {
            bool left = i % 2 == 0;
            var position = new Vector2(left ? .075f : .925f, .13f + (i / 2) * .235f);
            var paw = Rect(decorations, "Paw " + i, position, position);
            paw.sizeDelta = new Vector2(76, 82);
            paw.localRotation = Quaternion.Euler(0, 0, left ? -22 : 22);
            Color tint = i % 3 == 0 ? new Color(.81f, .49f, .43f, .48f) : new Color(.66f, .45f, .29f, .38f);
            MapOval(paw, "Soft Pad", new Vector2(.5f, .30f), new Vector2(.28f, .23f), tint);
            MapOval(paw, "Left Toe", new Vector2(.16f, .60f), new Vector2(.12f, .15f), tint);
            MapOval(paw, "Upper Left Toe", new Vector2(.38f, .80f), new Vector2(.12f, .16f), tint);
            MapOval(paw, "Upper Right Toe", new Vector2(.64f, .80f), new Vector2(.12f, .16f), tint);
            MapOval(paw, "Right Toe", new Vector2(.86f, .59f), new Vector2(.12f, .15f), tint);
        }
    }

    private RectTransform PopupContent(RectTransform overlay, string frameName, string contentName,
        Vector2 min, Vector2 max, Color frameColor, UnityEngine.Events.UnityAction dismiss, out RectTransform frame)
    {
        var backdrop = overlay.gameObject.AddComponent<Button>();
        backdrop.transition = Selectable.Transition.None;
        backdrop.onClick.AddListener(dismiss);
        frame = Panel(overlay, frameName, min, max, frameColor);
        // 창 안쪽 클릭은 배경의 닫기 버튼으로 전달하지 않습니다.
        frame.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        return Panel(frame, contentName, new Vector2(.025f, .025f), new Vector2(.975f, .975f), cream);
    }

    private void CloseDetailPopup()
    {
        if (saving || detailOverlay == null) return;
        var scroll = page.GetComponentInChildren<ScrollRect>();
        float position = scroll != null ? scroll.verticalNormalizedPosition : 1;
        detailOverlay.gameObject.SetActive(false);
        detailOverlay = null;
        popupRelic = null;
        listInput.interactable = true;
        listInput.blocksRaycasts = true;
        foreach (var tabButton in tabs) tabButton.interactable = true;
        // 장착/강화 후 카드 정보를 갱신하면서 원래 스크롤 위치를 복구합니다.
        string message=status.text;
        Show(detailTab);
        Canvas.ForceUpdateCanvases();
        scroll = page.GetComponentInChildren<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = position;
        status.text = message;
    }
}
