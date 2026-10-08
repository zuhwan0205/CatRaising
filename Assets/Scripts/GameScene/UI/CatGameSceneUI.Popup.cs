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
        bool opening=detailOverlay==null;
        if(detailOverlay!=null)RemoveUI(detailOverlay.gameObject);
        detailTab=tab;
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        listInput=page.GetComponent<CanvasGroup>();
        if(listInput==null)listInput=page.gameObject.AddComponent<CanvasGroup>();
        listInput.interactable=false;listInput.blocksRaycasts=false;
        foreach(var tabButton in tabs)tabButton.interactable=false;
        detailOverlay=Panel(safeArea,"Detail Popup Overlay",Vector2.zero,Vector2.one,new Color(.05f,.08f,.05f,.72f));
        detailOverlay.SetAsLastSibling();
        var dismiss=detailOverlay.gameObject.AddComponent<Button>();
        dismiss.transition=Selectable.Transition.None;
        dismiss.onClick.AddListener(CloseDetailPopup);
        var frame=Panel(detailOverlay,"Popup Wood Frame",new Vector2(.035f,.15f),new Vector2(.965f,.90f),new Color(.44f,.28f,.12f));
        // 창 안쪽의 빈 공간을 눌러도 뒤의 닫기 버튼까지 전달되지 않습니다.
        var intercept=frame.gameObject.AddComponent<Button>();intercept.transition=Selectable.Transition.None;
        var content=Panel(frame,"Popup Content",new Vector2(.025f,.025f),new Vector2(.975f,.975f),cream);
        if(opening)
        {
            frame.localScale=Vector3.one*.9f;
            DOTween.To(()=>frame.localScale,v=>frame.localScale=v,Vector3.one,.22f)
                .SetEase(Ease.OutBack).SetUpdate(true).SetLink(frame.gameObject,LinkBehaviour.KillOnDisable);
        }
        return content;
    }

    private void CloseDetailPopup()
    {
        if(saving || detailOverlay==null)return;
        var scroll=page.GetComponentInChildren<ScrollRect>();
        float position=scroll!=null?scroll.verticalNormalizedPosition:1;
        RemoveUI(detailOverlay.gameObject);detailOverlay=null;popupRelic=null;
        listInput.interactable=true;listInput.blocksRaycasts=true;
        foreach(var tabButton in tabs)tabButton.interactable=true;
        // 장착/강화 후 카드 정보를 갱신하면서 원래 스크롤 위치를 복구합니다.
        string message=status.text;
        Show(detailTab);
        Canvas.ForceUpdateCanvases();
        scroll=page.GetComponentInChildren<ScrollRect>();
        if(scroll!=null)scroll.verticalNormalizedPosition=position;
        status.text=message;
    }
}
