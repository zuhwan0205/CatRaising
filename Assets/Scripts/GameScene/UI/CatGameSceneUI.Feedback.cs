using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private RectTransform upgradePortrait;
    private Text upgradeDetails;
    private Tween goldFlash;

    public void PlayGoldReward()
    {
        if(goldAmount == null)return;
        goldFlash?.Kill();
        goldAmount.color = new Color(1f, .76f, .15f);
        goldFlash=DOTween.To(()=>goldAmount.color, c=>goldAmount.color=c,new Color(1f,.97f,.83f),.55f)
            .SetUpdate(true).SetLink(goldAmount.gameObject,LinkBehaviour.KillOnDisable);
    }

    public void PlayCharacterUpgrade()
    {
        if(upgradePortrait==null || upgradeDetails==null)return;
        var portrait=upgradePortrait;
        var details=upgradeDetails;
        var sequence=DOTween.Sequence().SetUpdate(true).SetLink(portrait.gameObject,LinkBehaviour.KillOnDisable);
        sequence.Append(DOTween.To(()=>portrait.localScale,v=>portrait.localScale=v,Vector3.one*1.06f,.16f).SetEase(Ease.OutQuad));
        sequence.Append(DOTween.To(()=>portrait.localScale,v=>portrait.localScale=v,Vector3.one,.22f).SetEase(Ease.OutBack));
        details.color=new Color(.8f,.39f,.06f);
        sequence.Join(DOTween.To(()=>details.color,c=>details.color=c,ink,.65f));
    }

    private void AddRareDrawGlow(RectTransform reward,Color color)
    {
        var glow=Panel(reward,"Rare Glow",new Vector2(-.12f,-.12f),new Vector2(1.12f,1.12f),new Color(color.r,color.g,color.b,.25f));
        glow.SetAsFirstSibling();glow.GetComponent<Image>().raycastTarget=false;
        var fade=glow.gameObject.AddComponent<CanvasGroup>();
        DOTween.To(()=>fade.alpha,v=>fade.alpha=v,.25f,.65f).SetLoops(-1,LoopType.Yoyo)
            .SetUpdate(true).SetLink(reward.gameObject,LinkBehaviour.KillOnDisable);
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI/4;
            var point=new Vector2(.5f+Mathf.Cos(angle)*.68f,.5f+Mathf.Sin(angle)*.68f);
            var sparkle=Panel(reward,"Rarity Sparkle",point,point,new Color(1f,.96f,.75f));
            sparkle.sizeDelta=new Vector2(i%2==0?12:7,i%2==0?12:7);
            sparkle.localRotation=Quaternion.Euler(0,0,45);
            sparkle.GetComponent<Image>().raycastTarget=false;
            DOTween.To(()=>sparkle.localScale,v=>sparkle.localScale=v,Vector3.one*.15f,.45f+i*.04f)
                .SetLoops(-1,LoopType.Yoyo).SetUpdate(true).SetLink(reward.gameObject,LinkBehaviour.KillOnDisable);
        }
    }
}
