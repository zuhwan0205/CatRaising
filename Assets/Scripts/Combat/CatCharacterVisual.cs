using UnityEngine;

// 몸의 연출만 담당합니다. 이동/피해/HP 바와 별도의 계층에서 움직입니다.
[DefaultExecutionOrder(205)]
public sealed class CatCharacterVisual : MonoBehaviour
{
    public Transform ImpactRoot { get; private set; }
    private Transform feet, animated, shadow;
    private Camera view;
    private BackendUserData data;
    private GameObject fallback;
    private SpriteRenderer catSprite;
    private Sprite generatedSprite, shadowSprite;
    private Vector3 previous;
    private float phase, motion;
    private string selectedId;

    public void Build(Transform player,Camera camera,BackendUserData playerData,Sprite square,Transform fallbackArt)
    {
        feet=player;view=camera;data=playerData;previous=feet.position;
        ImpactRoot=new GameObject("Cat Impact Visual").transform;ImpactRoot.SetParent(transform,false);
        fallback=fallbackArt.gameObject;fallbackArt.SetParent(ImpactRoot,false);
        animated=new GameObject("Shared Cat Artwork").transform;animated.SetParent(ImpactRoot,false);
        animated.localPosition=new Vector3(0,.35f,0);
        var texture=Resources.Load<Texture2D>("CatStarterIsometric");
        if(texture!=null)
        {
            generatedSprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.height/1.65f);
            catSprite=animated.gameObject.AddComponent<SpriteRenderer>();catSprite.sprite=generatedSprite;catSprite.sortingOrder=1;
            animated.gameObject.layer=2;
        }
        else Debug.LogWarning("CatStarterIsometric 이미지가 없어 기존 고양이 그림을 사용합니다.");

        shadowSprite=Sprite.Create(square.texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        var vertices=new Vector2[24];var indices=new ushort[66];
        for(int i=0;i<vertices.Length;i++)
        {
            float angle=i*Mathf.PI*2/vertices.Length;
            vertices[i]=new Vector2(.5f+Mathf.Cos(angle)*.49f,.5f+Mathf.Sin(angle)*.49f);
        }
        for(int i=0;i<22;i++){indices[i*3]=0;indices[i*3+1]=(ushort)(i+1);indices[i*3+2]=(ushort)(i+2);}
        shadowSprite.OverrideGeometry(vertices,indices);
        shadow=new GameObject("Soft Foot Shadow").transform;shadow.SetParent(feet,false);shadow.gameObject.layer=2;
        shadow.localPosition=new Vector3(0,.025f,0);shadow.localRotation=Quaternion.Euler(90,0,0);shadow.localScale=new Vector3(1,.65f,1);
        var renderer=shadow.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=shadowSprite;
        renderer.color=new Color(.12f,.18f,.10f,.3f);renderer.sortingOrder=-9000;
        ApplySelection();
    }

    private void ApplySelection()
    {
        selectedId=data.loadout.characterId;
        // 개별 캐릭터 아트가 준비될 때까지 선택 ID와 무관하게 새 그림을 공통 사용합니다.
        bool detailed=catSprite!=null;
        animated.gameObject.SetActive(detailed);fallback.SetActive(!detailed);
        phase=motion=0;previous=feet.position;
    }
    private void LateUpdate()
    {
        if(feet==null)return;
        if(selectedId!=data.loadout.characterId)ApplySelection();
        Vector3 delta=feet.position-previous;previous=feet.position;
        float distance=delta.magnitude;
        // 재등장 순간의 순간이동을 걷기 애니메이션으로 처리하지 않습니다.
        bool walking=distance>.0001f && distance<.75f;
        float dt=Mathf.Min(Time.deltaTime,.1f);
        motion=Mathf.MoveTowards(motion,walking?1:0,dt*10);
        if(walking)
        {
            phase+=distance*13;
            float horizontal=Vector3.Dot(delta,view.transform.right);
            if(catSprite!=null && Mathf.Abs(horizontal)>.001f)catSprite.flipX=horizontal<0;
        }
        animated.localPosition=new Vector3(0,.35f+Mathf.Abs(Mathf.Sin(phase))*.055f*motion,0);
        animated.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase)*2.5f*motion);
        animated.localScale=new Vector3(1+Mathf.Sin(phase*2)*.018f*motion,1-Mathf.Sin(phase*2)*.018f*motion,1);
        shadow.localScale=new Vector3(1-Mathf.Abs(Mathf.Sin(phase))*.06f*motion,.65f,1);
    }
    private void OnDestroy()
    {
        if(generatedSprite!=null)Destroy(generatedSprite);
        if(shadowSprite!=null)Destroy(shadowSprite);
    }
}
