using System.Collections.Generic;
using UnityEngine;

// 바닥 장식과 면별 명암으로 만든 2.5D 구조물. 전투 로직과 분리합니다.
public sealed class FieldScenery : MonoBehaviour
{
    public FieldNavigation Navigation { get; private set; }
    private Sprite square, oval, triangle, diamond;
    private Camera view;
    private readonly List<Sprite> ownedSprites=new List<Sprite>();

    public void Build(Sprite tile,Camera camera)
    {
        square=tile;view=camera;Navigation=new FieldNavigation();
        var circle=new Vector2[20];
        for(int i=0;i<circle.Length;i++)
        {float angle=i*Mathf.PI*2/circle.Length;circle[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.5f;}
        oval=Shape(circle);
        triangle=Shape(new[]{new Vector2(-.5f,-.5f),new Vector2(.5f,-.5f),new Vector2(0,.5f)});
        diamond=Shape(new[]{new Vector2(-.5f,0),new Vector2(0,-.5f),new Vector2(.5f,0),new Vector2(0,.5f)});
        Floor("East West Path",Vector3.zero,new Vector2(36,2.4f),new Color(.68f,.61f,.43f),square,-9900);
        Floor("North South Path",Vector3.zero,new Vector2(2.4f,36),new Color(.68f,.61f,.43f),square,-9900);
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

    private Sprite Shape(Vector2[] points)
    {
        var sprite=Sprite.Create(square.texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        var indices=new ushort[(points.Length-2)*3];
        for(int i=0;i<points.Length-2;i++){indices[i*3]=0;indices[i*3+1]=(ushort)(i+1);indices[i*3+2]=(ushort)(i+2);}
        // OverrideGeometry는 피벗 기준 월드 좌표가 아니라 Sprite Rect의 픽셀 좌표를 받습니다.
        var vertices=new Vector2[points.Length];
        for(int i=0;i<points.Length;i++)
            vertices[i]=new Vector2(Mathf.Clamp01(points[i].x+.5f)*sprite.rect.width,
                Mathf.Clamp01(points[i].y+.5f)*sprite.rect.height);
        sprite.OverrideGeometry(vertices,indices);ownedSprites.Add(sprite);return sprite;
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
    }

    private void Tree(Vector3 p)
    {
        var art=Structure("Tree",p,.65f);
        Part(art,square,new Vector2(0,.65f),new Vector2(.38f,1.3f),new Color(.43f,.31f,.21f),0);
        Part(art,oval,new Vector2(.15f,1.85f),new Vector2(2,2),new Color(.20f,.37f,.25f),1);
        Part(art,oval,new Vector2(-.15f,2.02f),new Vector2(1.9f,1.8f),new Color(.32f,.53f,.30f),2);
        Part(art,oval,new Vector2(-.42f,2.45f),new Vector2(.95f,.7f),new Color(.48f,.65f,.35f),3);
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
        item.transform.localPosition=position;item.transform.localScale=new Vector3(size.x,size.y,1);
        var renderer=item.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;return renderer;
    }
    private void Floor(string name,Vector3 position,Vector2 size,Color color,Sprite sprite,int order)
    {
        var renderer=Part(transform,sprite,Vector2.zero,size,color,order);renderer.name=name;
        renderer.transform.localPosition=new Vector3(position.x,.015f,position.z);renderer.transform.localRotation=Quaternion.Euler(90,0,0);
    }
    private void OnDestroy() {foreach(var sprite in ownedSprites)if(sprite!=null)Destroy(sprite);}
}
