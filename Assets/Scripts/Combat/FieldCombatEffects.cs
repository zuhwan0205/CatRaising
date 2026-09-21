using UnityEngine;
using UnityEngine.Rendering;

// 재사용하는 할퀴기 3줄과 피격 색상. 피해 판정에는 관여하지 않습니다.
[DefaultExecutionOrder(210)]
public class FieldCombatEffects : MonoBehaviour
{
    private Camera view;
    private Transform slash;
    private SortingGroup group;
    private SpriteRenderer[] lines;
    private Transform catArt;
    private SpriteRenderer[] catSprites;
    private Color[] originalColors;
    private Vector3 target;
    private float slashTime;
    private float hurtTime;
    private float attackTime;
    private float facing=1;
    private const float Duration=.25f;

    public void Initialize(Camera camera,Sprite sprite,Transform artwork)
    {
        view=camera;catArt=artwork;
        catSprites=artwork.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors=new Color[catSprites.Length];
        for(int i=0;i<catSprites.Length;i++)originalColors[i]=catSprites[i].color;
        slash=new GameObject("Claw Slash Effect").transform;slash.SetParent(transform,false);
        group=slash.gameObject.AddComponent<SortingGroup>();
        lines=new SpriteRenderer[3];
        for(int i=0;i<3;i++)
        {
            var line=new GameObject("Claw "+i);line.layer=2;line.transform.SetParent(slash,false);
            line.transform.localPosition=new Vector3((i-1)*.23f,0,0);
            line.transform.localRotation=Quaternion.Euler(0,0,-35);
            line.transform.localScale=new Vector3(.07f,.85f,1);
            lines[i]=line.AddComponent<SpriteRenderer>();lines[i].sprite=sprite;
        }
        slash.gameObject.SetActive(false);
    }
    public void Attack(Vector3 source,Vector3 destination)
    {
        target=destination;slashTime=Duration;attackTime=.15f;
        facing=Vector3.Dot(destination-source,view.transform.right)<0?-1:1;
        slash.gameObject.SetActive(true);
        RenderSlash();
    }
    public void Hurt() { hurtTime=.2f; }
    private void Update()
    {
        float delta=Mathf.Min(Time.deltaTime,.1f);
        slashTime=Mathf.Max(0,slashTime-delta);hurtTime=Mathf.Max(0,hurtTime-delta);attackTime=Mathf.Max(0,attackTime-delta);
        if(slashTime<=0)slash.gameObject.SetActive(false);
        catArt.localScale=Vector3.one*(1+attackTime);
        for(int i=0;i<catSprites.Length;i++)catSprites[i].color=hurtTime>0?new Color(1,.25f,.25f,originalColors[i].a):originalColors[i];
    }
    private void LateUpdate() { if(slashTime>0)RenderSlash(); }
    private void RenderSlash()
    {
        slash.position=target+view.transform.up*.55f;
        slash.rotation=view.transform.rotation;
        float progress=1-slashTime/Duration;
        slash.localScale=new Vector3(facing*(.65f+progress*.6f),.65f+progress*.6f,1);
        group.sortingOrder=10002-Mathf.RoundToInt(Vector3.Dot(target-view.transform.position,view.transform.forward)*100);
        foreach(var line in lines)line.color=new Color(1,.97f,.65f,slashTime/Duration);
    }
    private void OnDisable()
    {
        slashTime=hurtTime=attackTime=0;
        if(slash!=null)slash.gameObject.SetActive(false);
        if(catArt!=null)catArt.localScale=Vector3.one;
        if(catSprites!=null)for(int i=0;i<catSprites.Length;i++)catSprites[i].color=originalColors[i];
    }
}
