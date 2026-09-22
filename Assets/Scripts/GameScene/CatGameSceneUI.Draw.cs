using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 뽑기 연출은 저장 결과를 기다린 뒤 공개합니다. 이 파일에서는 추첨/지급하지 않습니다.
public partial class CatGameSceneUI
{
    private RectTransform drawOverlay;
    private bool drawCompleted;
    private bool drawingCharacter;
    private ContentDrawResult revealedResult;
    private string drawFailure;

    public bool BeginRelicDraw(bool character = false)
    {
        if(saving || drawOverlay!=null)return false;
        drawingCharacter=character;
        drawCompleted=false;revealedResult=null;drawFailure="";
        drawOverlay=Panel(safeArea,"Relic Draw Overlay",Vector2.zero,Vector2.one,new Color(.06f,.04f,.12f,.96f));
        drawOverlay.SetAsLastSibling();
        StartCoroutine(AnimateRelicDraw());
        return true;
    }

    public void CompleteRelicDraw(ContentDrawResult result)
    {
        if(drawOverlay==null)return;
        revealedResult=result;
        drawFailure=status.text;
        drawCompleted=true;
        RefreshWallet();
    }

    private IEnumerator AnimateRelicDraw()
    {
        var title=Label(drawOverlay,drawingCharacter?"캐릭터 상자를 여는 중…":"유물 상자를 여는 중…",32,new Vector2(.06f,.79f),new Vector2(.94f,.91f));
        title.color=Color.white;
        var hint=Label(drawOverlay,"잠시만 기다려 주세요",21,new Vector2(.06f,.15f),new Vector2(.94f,.29f));
        hint.color=new Color(.85f,.83f,.94f);
        var chest=Rect(drawOverlay,"Treasure Chest",new Vector2(.26f,.36f),new Vector2(.74f,.60f));
        Panel(chest,"Body Outline",new Vector2(0,0),new Vector2(1,.65f),new Color(.18f,.10f,.06f));
        Panel(chest,"Wood",new Vector2(.025f,.025f),new Vector2(.975f,.61f),new Color(.58f,.29f,.12f));
        var gold=new Color(.96f,.71f,.22f);
        Panel(chest,"Left Band",new Vector2(.12f,.025f),new Vector2(.22f,.62f),gold);
        Panel(chest,"Right Band",new Vector2(.78f,.025f),new Vector2(.88f,.62f),gold);
        Panel(chest,"Rim",new Vector2(0,.57f),new Vector2(1,.65f),gold);
        var lid=Panel(chest,"Lid",new Vector2(-.025f,.63f),new Vector2(1.025f,.94f),new Color(.79f,.44f,.18f));
        lid.pivot=new Vector2(.5f,0);
        Panel(lid,"Lid Rim",new Vector2(0,0),new Vector2(1,.15f),gold);
        Panel(lid,"Lid Band Left",new Vector2(.14f,0),new Vector2(.23f,1),gold);
        Panel(lid,"Lid Band Right",new Vector2(.77f,0),new Vector2(.86f,1),gold);
        var lockPlate=Panel(chest,"Lock",new Vector2(.40f,.39f),new Vector2(.60f,.73f),gold);
        Panel(lockPlate,"Keyhole",new Vector2(.40f,.30f),new Vector2(.60f,.70f),new Color(.18f,.10f,.06f));
        float elapsed=0;
        while(!drawCompleted || (revealedResult!=null && elapsed<1.2f))
        {
            elapsed+=Mathf.Min(Time.unscaledDeltaTime,.1f);
            chest.localRotation=Quaternion.Euler(0,0,Mathf.Sin(elapsed*28)*5);
            chest.localScale=Vector3.one*(1+Mathf.Abs(Mathf.Sin(elapsed*14))*.04f);
            if(elapsed>3)hint.text="저장 결과를 기다리고 있어요…";
            yield return null;
        }
        chest.localRotation=Quaternion.identity;chest.localScale=Vector3.one;
        if(revealedResult==null)
        {
            title.text="뽑기를 완료하지 못했어요";
            hint.text=string.IsNullOrEmpty(drawFailure)?"저장 상태와 골드를 확인하고 다시 시도해 주세요.":drawFailure;
            AddDrawCloseButton();
            yield break;
        }

        var result=revealedResult;
        SetDrawResult(result);
        Color rarityColor=new[]{new Color(.8f,.84f,.88f),new Color(.64f,.40f,1),new Color(1,.44f,.23f),new Color(1,.84f,.20f)}[Mathf.Clamp((int)result.rarity,0,3)];
        title.text="상자가 열립니다!";
        lockPlate.gameObject.SetActive(false);
        var reward=Panel(drawOverlay,"Relic Reward",new Vector2(.32f,.44f),new Vector2(.68f,.64f),rarityColor);
        var inner=Panel(reward,"Reward Face",new Vector2(.045f,.045f),new Vector2(.955f,.955f),new Color(.17f,.12f,.26f));
        var iconSprite=result.character?catalog.FindCharacter(result.id)?.icon:catalog.FindRelic(result.id)?.icon;
        if(iconSprite!=null)
        {
            var icon=Rect(inner,"Relic Icon",new Vector2(.15f,.15f),new Vector2(.85f,.85f)).gameObject.AddComponent<Image>();
            icon.sprite=iconSprite;icon.preserveAspect=true;icon.raycastTarget=false;
        }
        else
        {
            var emblem=Panel(inner,"Relic Emblem",new Vector2(.34f,.27f),new Vector2(.66f,.73f),rarityColor);
            emblem.localRotation=Quaternion.Euler(0,0,45);
        }
        var group=reward.gameObject.AddComponent<CanvasGroup>();group.alpha=0;
        var start=reward.anchoredPosition;
        for(float time=0;time<.65f;time+=Mathf.Min(Time.unscaledDeltaTime,.1f))
        {
            float t=Mathf.Clamp01(time/.65f),ease=1-Mathf.Pow(1-t,3);
            lid.localRotation=Quaternion.Euler(-110*ease,0,-12*ease);
            reward.anchoredPosition=start+Vector2.up*130*ease;
            reward.localScale=Vector3.one*Mathf.Lerp(.25f,1,ease);group.alpha=ease;
            yield return null;
        }
        group.alpha=1;reward.localScale=Vector3.one;reward.anchoredPosition=start+Vector2.up*130;
        title.text=$"{Rarity(result.rarity)} · {result.name}";title.color=rarityColor;
        string kind=result.character?"캐릭터":"유물";
        hint.text=result.duplicate?$"중복 {kind} · 조각 +{result.fragments}\n보유 {kind}에 조각을 더했습니다.":$"새 {kind}를 획득했어요!\n{kind} 메뉴에서 확인하세요.";
        AddDrawCloseButton();
    }

    private void AddDrawCloseButton()
    {
        // 전투 보상 저장이 진행 중이어도 결과 창 닫기는 가능합니다.
        var rect=Panel(drawOverlay,"Close Draw Result",new Vector2(.25f,.06f),new Vector2(.75f,.13f),accent);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
        Label(rect,"확인",25,Vector2.zero,Vector2.one).color=Color.white;
        button.onClick.AddListener(()=>
        {
            if(drawOverlay==null)return;
            drawOverlay.gameObject.SetActive(false);Destroy(drawOverlay.gameObject);drawOverlay=null;
            RefreshWallet();
        });
    }
}
