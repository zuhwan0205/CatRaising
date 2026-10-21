using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private RectTransform detailOverlay;
    private string detailTab;
    private OwnedRelic popupRelic;
    private CanvasGroup listInput;

    private RectTransform OpenDetailPopup(string tab)
    {
        bool opening = detailOverlay == null;
        if (detailOverlay != null) RemoveUI(detailOverlay.gameObject);
        detailTab = tab;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        listInput = page.GetComponent<CanvasGroup>();
        if (listInput == null) listInput = page.gameObject.AddComponent<CanvasGroup>();
        listInput.interactable = false;
        listInput.blocksRaycasts = false;
        foreach (var tabButton in tabs) tabButton.interactable = false;
        detailOverlay = Panel(safeArea, "Detail Popup Overlay", Vector2.zero, Vector2.one, new Color(.05f, .08f, .05f, .72f));
        detailOverlay.SetAsLastSibling();
        var content = PopupContent(detailOverlay, "Popup Wood Frame", "Popup Content",
            new Vector2(.035f, .15f), new Vector2(.965f, .90f), new Color(.44f, .28f, .12f), CloseDetailPopup, out var frame);
        if (opening)
        {
            frame.localScale = Vector3.one * .9f;
            DOTween.To(() => frame.localScale, value => frame.localScale = value, Vector3.one, .22f)
                .SetEase(Ease.OutBack).SetUpdate(true).SetLink(frame.gameObject, LinkBehaviour.KillOnDisable);
        }
        return content;
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
        RemoveUI(detailOverlay.gameObject);
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
