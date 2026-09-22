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
    private readonly Sprite[] combatFrames=new Sprite[4];
    private float attackRemaining, facingRemaining;
    private bool facingBack;
    private Vector2 attackDirection;
    private const float AttackDuration=.32f;

    public void Build(Transform player,Camera camera,BackendUserData playerData,Sprite square,Transform fallbackArt)
    {
        feet=player;view=camera;data=playerData;previous=feet.position;
        ImpactRoot=new GameObject("Cat Impact Visual").transform;ImpactRoot.SetParent(transform,false);
        fallback=fallbackArt.gameObject;fallbackArt.SetParent(ImpactRoot,false);
        animated=new GameObject("Shared Cat Artwork").transform;animated.SetParent(ImpactRoot,false);
        animated.localPosition=new Vector3(0,.35f,0);
        var sheet=Resources.Load<Texture2D>("CatCombatFrames");
        var texture=sheet!=null?sheet:Resources.Load<Texture2D>("CatStarterIsometric");
        if(texture!=null)
        {
            if(sheet!=null)
            {
                float width=sheet.width/2f,height=sheet.height/2f;
                // 시트의 위쪽 행: 정면 대기/공격, 아래쪽 행: 후면 대기/공격.
                for(int i=0;i<4;i++)
                    combatFrames[i]=Sprite.Create(sheet,new Rect((i%2)*width,(i<2?1:0)*height,width,height),new Vector2(.5f,.5f),height/1.65f,0,SpriteMeshType.FullRect);
            }
            else generatedSprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.height/1.65f);
            catSprite=animated.gameObject.AddComponent<SpriteRenderer>();catSprite.sprite=combatFrames[0]!=null?combatFrames[0]:generatedSprite;catSprite.sortingOrder=1;
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
        attackRemaining=facingRemaining=0;
    }

    public void Attack(Vector3 source,Vector3 target)
    {
        if(selectedId!=data.loadout.characterId)ApplySelection();
        Face(target-source);
        attackRemaining=AttackDuration;
        facingRemaining=.45f;
    }

    private void Face(Vector3 direction)
    {
        Vector2 screen=new Vector2(Vector3.Dot(direction,view.transform.right),Vector3.Dot(direction,view.transform.up));
        if(screen.sqrMagnitude<.00001f)return;
        screen=screen.normalized;
        attackDirection=screen;
        if(catSprite!=null && Mathf.Abs(screen.x)>.01f)catSprite.flipX=screen.x<0;
        if(Mathf.Abs(screen.y)>.03f)facingBack=screen.y>0;
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
        attackRemaining=Mathf.Max(0,attackRemaining-dt);
        facingRemaining=Mathf.Max(0,facingRemaining-dt);
        motion=Mathf.MoveTowards(motion,walking?1:0,dt*10);
        if(walking)
        {
            phase+=distance*13;
            if(facingRemaining<=0)Face(delta);
        }
        float progress=1-attackRemaining/AttackDuration;
        bool striking=attackRemaining>0 && progress>=.15f && progress<.8f;
        float lunge=attackRemaining>0?Mathf.Sin(progress*Mathf.PI)*.12f:0;
        float walk=attackRemaining>0?0:motion;
        if(catSprite!=null && combatFrames[0]!=null)catSprite.sprite=combatFrames[(facingBack?2:0)+(striking?1:0)];
        animated.localPosition=new Vector3(attackDirection.x*lunge,.35f+attackDirection.y*lunge+Mathf.Abs(Mathf.Sin(phase))*.055f*walk,0);
        animated.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase)*2.5f*walk);
        animated.localScale=new Vector3(1+Mathf.Sin(phase*2)*.018f*walk,1-Mathf.Sin(phase*2)*.018f*walk,1);
        shadow.localScale=new Vector3(1-Mathf.Abs(Mathf.Sin(phase))*.06f*motion,.65f,1);
    }
    private void OnDestroy()
    {
        if(generatedSprite!=null)Destroy(generatedSprite);
        if(shadowSprite!=null)Destroy(shadowSprite);
        foreach(var frame in combatFrames)if(frame!=null)Destroy(frame);
    }
}
