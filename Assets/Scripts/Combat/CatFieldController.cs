using System.Collections.Generic;
using UnityEngine;

// XZ 필드와 카메라 출력 구성. 캐릭터는 2D 빌보드입니다.
public class CatFieldController : MonoBehaviour
{
    public RenderTexture Output { get; private set; }
    public bool Automatic => movement.Automatic;
    public FieldCombatController Combat { get; private set; }
    private CatMovementController movement;
    private GameObject field;
    private Camera fieldCamera;
    private Texture2D pixels;
    private Sprite square;
    private bool visible;
    private bool paused;
    private bool focused = true;
    private bool gameplayBlocked;
    private bool playerDead;
    private Transform playerHealthFill;

    public void Build(BackendUserData data, CatGameCatalog catalog)
    {
        field = new GameObject("2.5D Cat Field");
        field.transform.SetParent(transform,false);
        pixels=new Texture2D(1,1); pixels.SetPixel(0,0,Color.white); pixels.Apply();
        square=Sprite.Create(pixels,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);

        var cameraObject=new GameObject("Field Camera"); cameraObject.transform.SetParent(transform,false);
        fieldCamera=cameraObject.AddComponent<Camera>(); fieldCamera.orthographic=true; fieldCamera.orthographicSize=6;
        fieldCamera.clearFlags=CameraClearFlags.SolidColor; fieldCamera.backgroundColor=new Color(.30f,.42f,.34f);
        fieldCamera.cullingMask=1<<2;
        fieldCamera.nearClipPlane=.1f; fieldCamera.farClipPlane=100;
        Output=new RenderTexture(720,1000,16); Output.Create(); fieldCamera.targetTexture=Output;

        Ground("Grass",field.transform,Vector3.zero,new Vector2(40,40),new Color(.76f,.84f,.62f),-10000);
        var scenery=field.AddComponent<FieldScenery>();
        scenery.Build(square,fieldCamera);
        // 바닥에 놓인 경계선으로 이동 가능한 영역을 표시합니다.
        for(int sign=-1;sign<=1;sign+=2)
        {
            Ground("Boundary X",field.transform,new Vector3(sign*18,.01f,0),new Vector2(.1f,36),new Color(.42f,.54f,.34f),-9998);
            Ground("Boundary Z",field.transform,new Vector3(0,.01f,sign*18),new Vector2(36,.1f),new Color(.42f,.54f,.34f),-9998);
        }
        var cat = new GameObject("Player Cat"); cat.transform.SetParent(field.transform,false);
        var catArt=Billboard("Cat Artwork",cat.transform);
        var fallbackArt=new GameObject("Legacy Cat Artwork").transform;
        fallbackArt.SetParent(catArt,false);
        Paint("Body",fallbackArt,Vector2.zero,new Vector2(.8f,.7f),new Color(1,.85f,.58f),0);
        Paint("Left Ear",fallbackArt,new Vector2(-.27f,.42f),new Vector2(.22f,.28f),new Color(1,.85f,.58f),0);
        Paint("Right Ear",fallbackArt,new Vector2(.27f,.42f),new Vector2(.22f,.28f),new Color(1,.85f,.58f),0);
        Paint("Left Eye",fallbackArt,new Vector2(-.18f,.08f),new Vector2(.07f,.12f),Color.black,1);
        Paint("Right Eye",fallbackArt,new Vector2(.18f,.08f),new Vector2(.07f,.12f),Color.black,1);
        var catVisual=catArt.gameObject.AddComponent<CatCharacterVisual>();
        catVisual.Build(cat.transform,fieldCamera,data,square,fallbackArt);
        Paint("Cat HP Background",catArt,new Vector2(0,1.35f),new Vector2(1,.09f),new Color(.2f,.2f,.2f),3);
        playerHealthFill=new GameObject("Cat HP Fill Origin").transform;
        playerHealthFill.SetParent(catArt,false);playerHealthFill.localPosition=new Vector3(-.5f,1.35f,0);
        Paint("Cat HP Fill",playerHealthFill,new Vector2(.5f,0),new Vector2(1,.09f),new Color(.25f,.9f,.4f),4);
        movement=cat.AddComponent<CatMovementController>();
        movement.Navigation=scenery.Navigation;
        var targets=new List<Transform>();
        var enemies=new List<FieldEnemy>();
        foreach(var position in new[] {new Vector3(4,0,3),new Vector3(-6,0,4),new Vector3(7,0,-5),new Vector3(-5,0,-7)})
        {
            var target=new GameObject("Slime Enemy"); target.transform.SetParent(field.transform,false); target.transform.localPosition=position;
            Ground("Target Shadow",target.transform,new Vector3(0,.02f,0),new Vector2(.9f,.5f),new Color(.15f,.22f,.13f,.35f),-9000);
            var billboard=Billboard("Enemy Billboard",target.transform);
            var art=Paint("Slime",billboard,Vector2.zero,new Vector2(.9f,.7f),new Color(.42f,.58f,.90f),0);
            Paint("Health Background",billboard,new Vector2(0,.65f),new Vector2(.9f,.08f),new Color(.2f,.2f,.2f),1);
            var barOrigin=new GameObject("Health Fill Origin").transform;
            barOrigin.SetParent(billboard,false);barOrigin.localPosition=new Vector3(-.45f,.65f,0);
            Paint("Health Fill",barOrigin,new Vector2(.45f,0),new Vector2(.9f,.08f),new Color(.9f,.25f,.3f),2);
            var enemy=target.AddComponent<FieldEnemy>();enemy.Initialize(art,barOrigin,14);
            enemies.Add(enemy);
            targets.Add(target.transform);
        }
        movement.Targets=targets.ToArray();
        Combat=field.AddComponent<FieldCombatController>();
        Combat.Navigation=scenery.Navigation;
        Combat.Initialize(cat.transform,enemies.ToArray(),data,catalog);
        Combat.AttackStarted+=catVisual.Attack;
        Combat.HealthChanged+=OnPlayerHealthChanged;
        var effects=field.AddComponent<FieldCombatEffects>();
        effects.Initialize(fieldCamera,square,catVisual.ImpactRoot);
        Combat.AttackPerformed+=effects.Attack;
        Combat.PlayerHit+=effects.Hurt;
        var follow=cameraObject.AddComponent<CatCameraFollow>(); follow.Target=cat.transform; follow.Snap();
        SetVisible(true);
    }

    private Transform Billboard(string name,Transform feet)
    {
        var item=new GameObject(name); item.transform.SetParent(feet,false);
        var billboard=item.AddComponent<FieldBillboard>(); billboard.Feet=feet; billboard.ViewCamera=fieldCamera;
        return item.transform;
    }

    private SpriteRenderer Paint(string name,Transform parent,Vector2 position,Vector2 size,Color color,int order)
    {
        var item=new GameObject(name); item.layer=2; item.transform.SetParent(parent,false);
        item.transform.localPosition=position; item.transform.localScale=new Vector3(size.x,size.y,1);
        var renderer=item.AddComponent<SpriteRenderer>(); renderer.sprite=square; renderer.color=color; renderer.sortingOrder=order;
        return renderer;
    }

    private void Ground(string name,Transform parent,Vector3 position,Vector2 size,Color color,int order)
    {
        var renderer=Paint(name,parent,Vector2.zero,size,color,order);
        renderer.transform.localPosition=position;
        renderer.transform.localRotation=Quaternion.Euler(90,0,0);
    }

    public void SetGameplayBlocked(bool blocked)
    {
        gameplayBlocked=blocked;
        movement.enabled=!blocked&&!playerDead;
        Combat.Blocked=blocked;
    }
    private void OnPlayerHealthChanged(double hp,double maximum)
    {
        playerDead=hp<=0;
        movement.enabled=!gameplayBlocked&&!playerDead;
        playerHealthFill.localScale=new Vector3((float)(hp/maximum),1,1);
    }
    public void SetAutomatic(bool value) { movement.SetAutomatic(value); }
    public void SetStick(Vector2 value) { if(movement!=null) movement.StickInput=visible?value:Vector2.zero; }
    public void SetVisible(bool value)
    {
        visible=value;
        if(movement!=null)
        {
            movement.ManualInputEnabled=value;
            movement.StickInput=Vector2.zero;
        }
        ApplyActive();
    }
    private void ApplyActive()
    {
        if(field==null)return;
        // 탭은 화면 출력만 제어합니다. 필드의 전투/자동 이동은 계속 실행합니다.
        bool active=!paused&&focused;
        field.SetActive(active);
        fieldCamera.enabled=active&&visible;
    }
    private void OnApplicationPause(bool value) { paused=value; ApplyActive(); }
    private void OnApplicationFocus(bool value) { focused=value; ApplyActive(); }
    private void OnDestroy()
    {
        if(fieldCamera!=null) fieldCamera.targetTexture=null;
        if(Output!=null){Output.Release();Destroy(Output);}
        if(square!=null)Destroy(square); if(pixels!=null)Destroy(pixels);
    }
}
