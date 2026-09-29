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
    private readonly Sprite[] pixelFrames=new Sprite[12];
    private readonly Sprite[] mackerelFrames=new Sprite[12];
    private readonly Sprite[] trioFrames=new Sprite[12];
    private readonly Sprite[] legendaryFrames=new Sprite[12];
    private readonly Sprite[] mageFrames=new Sprite[12];
    private readonly Sprite[] dragonFrames=new Sprite[12];
    private readonly Sprite[] archerFrames=new Sprite[12];
    private readonly Sprite[] snowFrames=new Sprite[12];
    private readonly Sprite[] hammerFrames=new Sprite[12];
    public bool UsesHammer => selectedId=="cat_tiger";
    public bool UsesIce => selectedId=="cat_shadow";
    public bool UsesBow => selectedId=="cat_ninja";
    public bool UsesFireBreath => selectedId=="cat_lion";
    public bool UsesMagic => selectedId=="cat_moon";
    private Sprite[] activePixelFrames;
    private bool usePixelCat;
    private FieldBillboard billboard;
    private float originalCenterHeight;
    private float idleTime;
    private float attackRemaining, facingRemaining;
    private bool facingBack;
    private Vector2 attackDirection;
    private float AttackDuration => UsesHammer?.52f:.32f;

    public void Build(Transform player,Camera camera,BackendUserData playerData,Sprite square,Transform fallbackArt)
    {
        feet=player;view=camera;data=playerData;previous=feet.position;
        billboard=GetComponent<FieldBillboard>();
        if(billboard!=null)originalCenterHeight=billboard.CenterHeight;
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

        var pixelSheet=Resources.Load<Texture2D>("CatStarterBipedPixel");
        if(pixelSheet!=null)
        {
            float cellWidth=pixelSheet.width/4f,cellHeight=pixelSheet.height/3f;
            // 위에서부터 대기/걷기/공격. 행마다 발바닥 기준점을 맞춥니다.
            float[] footPivots={.045f,.075f,.04f};
            for(int i=0;i<pixelFrames.Length;i++)
            {
                int row=i/4;
                pixelFrames[i]=Sprite.Create(pixelSheet,new Rect(i%4*cellWidth,(2-row)*cellHeight,cellWidth,cellHeight),
                    new Vector2(.52f,footPivots[row]),cellHeight/1.45f,0,SpriteMeshType.FullRect);
            }
            if(catSprite==null)
            {
                catSprite=animated.gameObject.AddComponent<SpriteRenderer>();
                catSprite.sortingOrder=1;animated.gameObject.layer=2;
            }
        }

        var fishSheet=Resources.Load<Texture2D>("CatMackerelBipedPixel");
        if(fishSheet!=null)
        {
            float w=fishSheet.width/4f,h=fishSheet.height/3f;
            for(int i=0;i<12;i++)
            {
                int row=i/4;
                float leftInset=i==11?w*.22f:0;
                float width=i==10?w*1.14f:w-leftInset;
                // 공격의 고등어 끝이 기본 셀을 넘으므로 세 번째 공격 프레임만 넓게 읽습니다.
                var rect=new Rect(i%4*w+leftInset,(2-row)*h,width,h);
                mackerelFrames[i]=Sprite.Create(fishSheet,rect,new Vector2((.60f*w-leftInset)/width,row==0?.04f:row==1?.075f:.12f),h/1.45f,0,SpriteMeshType.FullRect);
            }
        }

        var trioSheet=Resources.Load<Texture2D>("CatKittenTrioPixel");
        if(trioSheet!=null)
        {
            float w=trioSheet.width/4f,h=trioSheet.height/3f;
            for(int i=0;i<12;i++)
                trioFrames[i]=Sprite.Create(trioSheet,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(.52f,i<4?.105f:i<8?.12f:.14f),h/1.65f,0,SpriteMeshType.FullRect);
        }
        var legendarySheet=Resources.Load<Texture2D>("CatLegendaryBootsPixel");
        if(legendarySheet!=null)
        {
            float w=legendarySheet.width/4f,h=legendarySheet.height/3f;
            for(int i=0;i<12;i++)
                legendaryFrames[i]=Sprite.Create(legendarySheet,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(.5f,i<4?.02f:i<8?.08f:.16f),h/1.65f,0,SpriteMeshType.FullRect);
        }
        var mageSheet=Resources.Load<Texture2D>("CatStarMagePixel");
        if(mageSheet!=null)
        {
            float w=mageSheet.width/4f,h=mageSheet.height/3f;
            for(int i=0;i<12;i++)
                mageFrames[i]=Sprite.Create(mageSheet,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(.55f,i>=4 && i<8?.04f:.02f),h/1.45f,0,SpriteMeshType.FullRect);
        }
        var dragonSheet=Resources.Load<Texture2D>("CatDragonPixel");
        if(dragonSheet!=null)
        {
            float w=dragonSheet.width/4f,h=dragonSheet.height/3f;
            for(int i=0;i<12;i++)
                dragonFrames[i]=Sprite.Create(dragonSheet,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(.5f,.08f),h/1.55f,0,SpriteMeshType.FullRect);
        }
        var archerSheet=Resources.Load<Texture2D>("CatSiameseArcherPixel");
        if(archerSheet!=null)
        {
            float w=archerSheet.width/4f,h=archerSheet.height/3f;
            for(int i=0;i<12;i++)
                archerFrames[i]=Sprite.Create(archerSheet,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(.5f,.08f),h/1.45f,0,SpriteMeshType.FullRect);
        }
        var snowSheet=Resources.Load<Texture2D>("CatSnowflakePixel");
        if(snowSheet!=null)
        {
            float w=snowSheet.width/4f,h=snowSheet.height/3f;
            for(int i=0;i<12;i++)
                snowFrames[i]=Sprite.Create(snowSheet,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(.52f,.02f),h/1.45f,0,SpriteMeshType.FullRect);
        }
        var hammerSheet=Resources.Load<Texture2D>("CatHammerKnightPixel");
        if(hammerSheet!=null)
        {
            float w=hammerSheet.width/4f,h=hammerSheet.height/3f;
            for(int i=0;i<12;i++)
                hammerFrames[i]=Sprite.Create(hammerSheet,new Rect(i%4*w,(2-i/4)*h,w,h),new Vector2(i==9?.38f:i==10?.32f:i==8?.48f:.64f,i<4?.07f:i<8?.12f:.16f),h/1.65f,0,SpriteMeshType.FullRect);
        }
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
        activePixelFrames=selectedId=="cat_tiger"?hammerFrames:selectedId=="cat_shadow"?snowFrames:selectedId=="cat_ninja"?archerFrames:selectedId=="cat_lion"?dragonFrames:selectedId=="cat_moon"?mageFrames:selectedId=="cat_celestial"?legendaryFrames:selectedId=="cat_kitten_trio"?trioFrames:selectedId=="cat_knight"?mackerelFrames:selectedId==CatGameCatalog.StarterCharacterId?pixelFrames:null;
        usePixelCat=activePixelFrames!=null && activePixelFrames[0]!=null;
        // 픽셀 시트는 발바닥 pivot이므로 중앙 pivot용 높이 보정을 적용하지 않습니다.
        if(billboard!=null)billboard.CenterHeight=usePixelCat?0:originalCenterHeight;
        bool detailed=catSprite!=null && (usePixelCat || combatFrames[0]!=null || generatedSprite!=null);
        if(detailed)catSprite.sprite=usePixelCat?activePixelFrames[0]:combatFrames[0]!=null?combatFrames[0]:generatedSprite;
        animated.gameObject.SetActive(detailed);fallback.SetActive(!detailed);
        phase=motion=idleTime=0;previous=feet.position;
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
        idleTime+=dt;
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
        float lunge=attackRemaining>0 && !UsesMagic && !UsesFireBreath && !UsesBow && !UsesIce?Mathf.Sin(progress*Mathf.PI)*.12f:0;
        float walk=attackRemaining>0?0:motion;
        if(usePixelCat)
        {
            int frame=attackRemaining>0 ? 8+Mathf.Min(3,Mathf.FloorToInt(progress*4)) :
                walking ? 4+Mathf.FloorToInt(phase/1.5f)%4 : Mathf.FloorToInt(idleTime*4)%4;
            catSprite.sprite=activePixelFrames[frame];
            animated.localPosition=new Vector3(attackDirection.x*lunge,attackDirection.y*lunge,0);
            animated.localRotation=Quaternion.identity;
            animated.localScale=Vector3.one;
            shadow.localScale=new Vector3(.75f,.45f,1);
            return;
        }
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
        foreach(var frame in pixelFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in mackerelFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in trioFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in legendaryFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in mageFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in dragonFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in archerFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in snowFrames)if(frame!=null)Destroy(frame);
        foreach(var frame in hammerFrames)if(frame!=null)Destroy(frame);
    }
}
