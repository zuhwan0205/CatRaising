using System.Collections.Generic;
using UnityEngine;

// 바닥 장식과 면별 명암으로 만든 2.5D 구조물. 전투 로직과 분리합니다.
public sealed class FieldScenery : MonoBehaviour
{
    public FieldNavigation Navigation { get; private set; }
    private Sprite square, oval, triangle, diamond;
    private Camera view;
    private readonly List<Sprite> ownedSprites=new List<Sprite>();
    private readonly List<Texture2D> ownedTextures=new List<Texture2D>();

    public void Build(Sprite tile,Camera camera)
    {
        square=tile;view=camera;Navigation=new FieldNavigation();
        var circle=new Vector2[20];
        for(int i=0;i<circle.Length;i++)
        {float angle=i*Mathf.PI*2/circle.Length;circle[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.5f;}
        oval=Shape(circle);
        triangle=Shape(new[]{new Vector2(-.5f,-.5f),new Vector2(.5f,-.5f),new Vector2(0,.5f)});
        diamond=Shape(new[]{new Vector2(-.5f,0),new Vector2(0,-.5f),new Vector2(.5f,0),new Vector2(0,.5f)});
        if(BuildImportedGarden()) { Navigation.Bake(); return; }
        Floor("East West Path",Vector3.zero,new Vector2(36,2.4f),Color.white,PixelSurface(512,32,false),-9900);
        Floor("North South Path",Vector3.zero,new Vector2(2.4f,36),Color.white,PixelSurface(32,512,false),-9900);
        Floor("Pixel Grass",Vector3.zero,new Vector2(40,40),Color.white,PixelSurface(512,512,true),-9950);
        var random=new System.Random(731);
        for(int i=0;i<180;i++)
        {
            float x=(float)random.NextDouble()*35-17.5f,z=(float)random.NextDouble()*35-17.5f;
            if(Mathf.Abs(x)<1.5f||Mathf.Abs(z)<1.5f)continue;
            Floor("Grass Patch",new Vector3(x,0,z),new Vector2(.5f+(float)random.NextDouble(),.35f),
                i%3==0?new Color(.56f,.68f,.39f):new Color(.66f,.76f,.47f),oval,-9890);
        }
        for(int i=-16;i<=16;i+=2)
        {
            Floor("Path Stone",new Vector3(i,0,.12f),new Vector2(.8f,.7f),new Color(.76f,.73f,.60f),diamond,-9880);
            if(Mathf.Abs(i)>1)Floor("Path Stone",new Vector3(.1f,0,i),new Vector2(.8f,.7f),new Color(.76f,.73f,.60f),diamond,-9880);
        }
        House(new Vector3(-4,0,3)); House(new Vector3(10,0,-8));
        Tower(new Vector3(4,0,5)); Tower(new Vector3(-9,0,-7));
        Crate(new Vector3(4,0,-3)); Crate(new Vector3(-6,0,-2));
        foreach(var p in new[]{new Vector3(-4,0,8),new Vector3(7,0,2),new Vector3(-8,0,5),new Vector3(9,0,-4),new Vector3(-4,0,-9),new Vector3(6,0,11)})Tree(p);
        for(int i=-16;i<=16;i+=8) {Tree(new Vector3(i,0,16));Tree(new Vector3(i,0,-16));}
        Navigation.Bake();
    }

    // Resources에 복사한 배경 에셋을 에디터와 기기 빌드에서 동일하게 사용합니다.
    private bool BuildImportedGarden()
    {
        var grass=Resources.Load<Texture2D>("FieldArt/TX Tileset Grass");
        var stone=Resources.Load<Texture2D>("FieldArt/TX Tileset Stone Ground");
        var plants=Resources.Load<Texture2D>("FieldArt/TX Plant");
        var props=Resources.Load<Texture2D>("FieldArt/TX Props");
        if(grass==null || stone==null || plants==null || props==null)
        {
            Debug.LogWarning("FieldArt 배경 에셋이 없어 기본 배경을 사용합니다.");
            return false;
        }
        var grassTiles=new Sprite[16];
        for(int i=0;i<16;i++)grassTiles[i]=Atlas(grass,(i%4)*32,(i/4)*32,32,32);
        var paving=Atlas(stone,32,32,32,32);
        var random=new System.Random(731);
        // 바닥은 잔디로 연결하고 석조 구역은 따로 배치합니다.
        for(int z=-10;z<10;z++)for(int x=-10;x<10;x++)
        {
            Floor("Garden Grass",new Vector3(x*2+1,0,z*2+1),
                new Vector2(2,2),Color.white,grassTiles[random.Next(16)],-9950);
        }
        var slab=Atlas(stone,0,0,96,96);
        GardenTerrace(new Vector3(-5,0,7),3,2,slab);
        GardenTerrace(new Vector3(9,0,-8),2,2,slab);
        // 끊어진 징검돌로 구역을 연결하고 중앙 전투 공간은 비워둡니다.
        for(int i=0;i<18;i++)
        {
            float z=-13+i*1.5f;
            float x=3.5f+Mathf.Sin(i*.45f)*2.5f;
            if(i%5==0)continue;
            Floor("Scattered Paving",new Vector3(x,0,z),new Vector2(.85f,1.1f),Color.white,paving,-9910);
        }
        var tree=Atlas(plants,16,14,128,140);
        var bush=Atlas(plants,208,184,64,48);
        var crate=Atlas(props,160,16,32,48);
        var pillar=Atlas(props,224,8,32,56);
        var rock=Atlas(props,0,424,64,56);
        foreach(var p in new[]{new Vector3(-6,0,5),new Vector3(7,0,5),new Vector3(-8,0,-6),
            new Vector3(8,0,-7),new Vector3(-11,0,10),new Vector3(11,0,11)})
            GardenProp("Garden Tree",p,tree,3.8f,.7f);
        for(int i=-16;i<=16;i+=4)
        {
            GardenProp("Border Tree",new Vector3(i,0,17),tree,3.8f,.7f);
            GardenProp("Border Tree",new Vector3(i,0,-17),tree,3.8f,.7f);
            if(Mathf.Abs(i)<15)
            {
                GardenProp("Border Tree",new Vector3(-17,0,i),tree,3.8f,.7f);
                GardenProp("Border Tree",new Vector3(17,0,i),tree,3.8f,.7f);
            }
        }
        foreach(var p in new[]{new Vector3(-4,0,6),new Vector3(4,0,6),new Vector3(-10,0,-3),new Vector3(10,0,-3)})
            GardenProp("Ruined Pillar",p,pillar,2.1f,.55f);
        GardenProp("Supply Crate",new Vector3(5,0,-4),crate,1.2f,.55f);
        GardenProp("Supply Crate",new Vector3(6.3f,0,-4.5f),crate,1.2f,.55f);
        GardenProp("Garden Rock",new Vector3(-6,0,-3),rock,1.3f,.75f);
        GardenProp("Garden Rock",new Vector3(10,0,8),rock,1.3f,.75f);
        foreach(var p in new[]{new Vector3(-8,0,7),new Vector3(9,0,6),new Vector3(-10,0,-8),new Vector3(10,0,-9)})
            GardenProp("Garden Bush",p,bush,1.2f,.45f);
        var bench=Atlas(props,288,16,64,48);
        var pot=Atlas(props,160,216,32,40);
        var barrel=Atlas(props,160,152,32,40);
        var well=Atlas(props,416,352,64,64);
        var monument=Atlas(props,440,16,48,80);
        var rubble=Atlas(props,128,480,32,32);
        GardenProp("Old Well",new Vector3(-5,0,-7),well,1.9f,.85f);
        GardenProp("Shrine Monument",new Vector3(-5,0,9),monument,2.5f,.65f);
        GardenProp("Stone Bench",new Vector3(-8,0,8.5f),bench,1.1f,.65f);
        GardenProp("Stone Bench",new Vector3(-2,0,8.5f),bench,1.1f,.65f);
        GardenProp("Terrace Barrel",new Vector3(10,0,-7),barrel,1.2f,.45f);
        GardenProp("Terrace Pot",new Vector3(8.5f,0,-7.5f),pot,.95f,.35f);
        GardenProp("Well Pot",new Vector3(-6.5f,0,-7),pot,.85f,.3f);
        GardenProp("Supply Barrel",new Vector3(5.5f,0,-5.6f),barrel,1.1f,.4f);
        var tuft=Atlas(plants,0,392,32,24);
        for(int i=0;i<100;i++)
        {
            var p=new Vector3((float)random.NextDouble()*32-16,0,(float)random.NextDouble()*32-16);
            if(p.sqrMagnitude<9 || (p.x>-10&&p.x<0&&p.z>3&&p.z<11)
                || (p.x>5&&p.x<13&&p.z>-13&&p.z<-4))continue;
            if(i%7==0)GardenProp("Loose Stone",p,rubble,.45f,.18f);
            else
            {
                var art=new GameObject("Grass Detail").transform;art.SetParent(transform,false);art.localPosition=p;
                var facing=art.gameObject.AddComponent<FieldBillboard>();facing.Feet=art;facing.ViewCamera=view;facing.CenterHeight=0;
                Part(art,tuft,new Vector2(0,.2f),new Vector2(.55f,.4f),Color.white,0);
            }
        }
        return true;
    }

    private void GardenTerrace(Vector3 center,int columns,int rows,Sprite slab)
    {
        for(int z=0;z<rows;z++)for(int x=0;x<columns;x++)
            Floor("Ruined Terrace",center+new Vector3((x-(columns-1)*.5f)*2.4f,0,(z-(rows-1)*.5f)*2.4f),
                new Vector2(2.4f,2.4f),Color.white,slab,-9920);
    }

    // 좌상단 픽셀 좌표를 Unity 스프라이트 좌표로 변환합니다.
    private Sprite Atlas(Texture2D texture,int x,int top,int width,int height)
    {
        texture.filterMode=FilterMode.Point;
        var sprite=Sprite.Create(texture,new Rect(x,texture.height-top-height,width,height),
            new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
        ownedSprites.Add(sprite);
        return sprite;
    }

    private void GardenProp(string name,Vector3 position,Sprite sprite,float height,float radius)
    {
        var art=Structure(name,position,radius);
        float width=height*sprite.rect.width/sprite.rect.height;
        Part(art,sprite,new Vector2(0,height*.5f),new Vector2(width,height),Color.white,0);
    }

    private Sprite PixelSprite(Texture2D texture)
    {
        texture.filterMode=FilterMode.Point;
        texture.wrapMode=TextureWrapMode.Clamp;
        texture.Apply(false,true);
        ownedTextures.Add(texture);
        var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect);
        ownedSprites.Add(sprite);
        return sprite;
    }

    private Sprite PixelSurface(int width,int height,bool grass)
    {
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
        var colors=new Color[width*height];
        var random=new System.Random(grass?417:892);
        Color ground=grass?new Color(.38f,.51f,.28f):new Color(.62f,.51f,.34f);
        for(int i=0;i<colors.Length;i++)colors[i]=ground;
        // 픽셀을 작은 묶음으로 배치해 풀과 자갈 무늬를 만듭니다.
        for(int i=0;i<width*height/24;i++)
        {
            int x=random.Next(1,width-2),y=random.Next(1,height-2);
            Color detail=grass?(i%2==0?new Color(.46f,.59f,.32f):new Color(.31f,.44f,.24f)):
                (i%2==0?new Color(.72f,.62f,.43f):new Color(.53f,.43f,.29f));
            colors[y*width+x]=detail;colors[y*width+x+1]=detail;
            if(grass)colors[(y+1)*width+x]=detail;
        }
        texture.SetPixels(colors);
        return PixelSprite(texture);
    }

    private Sprite Shape(Vector2[] points)
    {
        const int size=24;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        var colors=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            var p=new Vector2((x+.5f)/size-.5f,(y+.5f)/size-.5f);
            bool inside=false;
            for(int i=0,j=points.Length-1;i<points.Length;j=i++)
            {
                var a=points[i];var b=points[j];
                if((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            colors[y*size+x]=inside?Color.white:Color.clear;
        }
        texture.SetPixels(colors);
        return PixelSprite(texture);
    }
    private Transform Structure(string name,Vector3 position,float radius)
    {
        Navigation.Add(position,radius);
        Floor(name+" Shadow",position+new Vector3(.45f,0,-.2f),new Vector2(radius*3.2f,radius*1.9f),new Color(.16f,.22f,.14f,.30f),oval,-9100);
        var feet=new GameObject(name).transform;feet.SetParent(transform,false);feet.localPosition=position;
        var art=new GameObject("Artwork").transform;art.SetParent(feet,false);
        var billboard=art.gameObject.AddComponent<FieldBillboard>();billboard.Feet=feet;billboard.ViewCamera=view;billboard.CenterHeight=0;
        return art;
    }

    private void House(Vector3 p)
    {
        var art=Structure("Cat Cottage",p,1.25f);
        Part(art,square,new Vector2(0,1),new Vector2(2.1f,2),new Color(.86f,.72f,.48f),0);
        Part(art,square,new Vector2(.85f,1),new Vector2(.4f,2),new Color(.61f,.46f,.32f),1);
        Part(art,triangle,new Vector2(0,2.25f),new Vector2(2.6f,1.4f),new Color(.59f,.28f,.23f),2);
        Part(art,triangle,new Vector2(-.16f,2.32f),new Vector2(2.1f,1.12f),new Color(.79f,.41f,.30f),3);
        Part(art,square,new Vector2(0,.5f),new Vector2(.6f,1),new Color(.24f,.22f,.22f),4);
        Part(art,oval,new Vector2(-.6f,1.35f),new Vector2(.42f,.48f),new Color(1,.87f,.48f),4);
        Part(art,triangle,new Vector2(-.88f,2.93f),new Vector2(.45f,.55f),new Color(.59f,.28f,.23f),4);
        Part(art,triangle,new Vector2(.88f,2.93f),new Vector2(.45f,.55f),new Color(.59f,.28f,.23f),4);
        for(int row=0;row<4;row++)
        {
            float y=1.86f+row*.22f;
            Part(art,square,new Vector2(-.16f,y),new Vector2(1.7f-row*.38f,.065f),new Color(.64f,.32f,.24f),5);
        }
        for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            Part(art,square,new Vector2(-.75f+col*.7f,.30f+row*.55f),new Vector2(.14f,.07f),new Color(.76f,.60f,.39f),3);
    }

    private void Tree(Vector3 p)
    {
        var art=Structure("Tree",p,.65f);
        Part(art,square,new Vector2(0,.65f),new Vector2(.38f,1.3f),new Color(.43f,.31f,.21f),0);
        Part(art,oval,new Vector2(.15f,1.85f),new Vector2(2,2),new Color(.20f,.37f,.25f),1);
        Part(art,oval,new Vector2(-.15f,2.02f),new Vector2(1.9f,1.8f),new Color(.32f,.53f,.30f),2);
        Part(art,oval,new Vector2(-.42f,2.45f),new Vector2(.95f,.7f),new Color(.48f,.65f,.35f),3);
        for(int i=0;i<5;i++)
            Part(art,square,new Vector2(-.55f+(i%3)*.35f,1.7f+(i/3)*.35f),new Vector2(.18f,.12f),new Color(.43f,.60f,.32f),4);
    }

    private void Tower(Vector3 p)
    {
        var art=Structure("Stone Pillar",p,.8f);
        Part(art,square,new Vector2(0,.18f),new Vector2(1.7f,.36f),new Color(.38f,.42f,.42f),0);
        Part(art,square,new Vector2(0,1.05f),new Vector2(1.05f,1.8f),new Color(.63f,.66f,.62f),1);
        Part(art,square,new Vector2(.38f,1.05f),new Vector2(.3f,1.8f),new Color(.43f,.49f,.48f),2);
        for(int i=1;i<=3;i++)Part(art,square,new Vector2(0,i*.5f),new Vector2(1.05f,.04f),new Color(.46f,.51f,.49f),3);
        Part(art,diamond,new Vector2(0,2),new Vector2(1.65f,.7f),new Color(.81f,.82f,.71f),4);
    }

    private void Crate(Vector3 p)
    {
        var art=Structure("Wooden Crate",p,.65f);
        Part(art,square,new Vector2(0,.48f),new Vector2(1.1f,.96f),new Color(.57f,.36f,.19f),0);
        Part(art,diamond,new Vector2(0,1),new Vector2(1.1f,.45f),new Color(.85f,.64f,.36f),1);
        for(int i=-1;i<=1;i++)Part(art,square,new Vector2(i*.4f,.48f),new Vector2(.07f,.96f),new Color(.32f,.23f,.15f),2);
    }

    private SpriteRenderer Part(Transform parent,Sprite sprite,Vector2 position,Vector2 size,Color color,int order)
    {
        var item=new GameObject("Detail");item.layer=2;item.transform.SetParent(parent,false);
        item.transform.localPosition=position;item.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
        var renderer=item.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;return renderer;
    }
    private void Floor(string name,Vector3 position,Vector2 size,Color color,Sprite sprite,int order)
    {
        var renderer=Part(transform,sprite,Vector2.zero,size,color,order);renderer.name=name;
        renderer.transform.localPosition=new Vector3(position.x,.015f,position.z);renderer.transform.localRotation=Quaternion.Euler(90,0,0);
    }
    private void OnDestroy() {foreach(var sprite in ownedSprites)if(sprite!=null)Destroy(sprite);foreach(var texture in ownedTextures)if(texture!=null)Destroy(texture);}
}
