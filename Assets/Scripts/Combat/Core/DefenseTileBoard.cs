using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class DefenseTileBoard : MonoBehaviour
{
    public const int Width=10, Height=14;
    public readonly HashSet<Vector2Int> Road=new HashSet<Vector2Int>();
    public Vector2[] Route { get; private set; }
    private readonly HashSet<Vector2Int> decorations = new HashSet<Vector2Int>();
    public bool CanPlace(Vector2Int cell) => !Road.Contains(cell) && !decorations.Contains(cell);
    private GameObject world;
    private RenderTexture output;
    private Camera mapCamera;
    private readonly List<Object> assets=new List<Object>();
    public void Build(RawImage display)
    {
        var corners=new[] {new Vector2Int(-1,10),new Vector2Int(4,10),new Vector2Int(4,6),new Vector2Int(1,6),new Vector2Int(1,1),new Vector2Int(8,1),new Vector2Int(8,5),new Vector2Int(6,5),new Vector2Int(6,8),new Vector2Int(8,8),new Vector2Int(8,12),new Vector2Int(5,12),new Vector2Int(5,14)};
        var route=new List<Vector2>();
        for(int i=0;i<corners.Length-1;i++)
        {
            var at=corners[i]; var end=corners[i+1];
            var step=new Vector2Int(System.Math.Sign(end.x-at.x),System.Math.Sign(end.y-at.y));
            while(at!=end) { Road.Add(at); route.Add(Center(at)); at+=step; }
        }
        Road.Add(corners[corners.Length-1]); route.Add(Center(corners[corners.Length-1])); Route=route.ToArray();
        world=new GameObject("Defense Tilemap World",typeof(Grid)); world.transform.position=new Vector3(10000,10000,0);
        var layer=new GameObject("Terrain",typeof(Tilemap),typeof(TilemapRenderer)); layer.transform.SetParent(world.transform,false); layer.layer=30;
        var map=layer.GetComponent<Tilemap>();
        var grasses=new Tile[4]; var roads=new Tile[16];
        for(int i=0;i<4;i++) grasses[i]=MakeTile(false,i);
        for(int i=0;i<16;i++) roads[i]=MakeTile(true,i);
        for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
        {
            var cell=new Vector2Int(x,y);
            int edges=0;
            if(!Road.Contains(cell+Vector2Int.left)) edges|=1;
            if(!Road.Contains(cell+Vector2Int.right)) edges|=2;
            if(!Road.Contains(cell+Vector2Int.down)) edges|=4;
            if(!Road.Contains(cell+Vector2Int.up)) edges|=8;
            map.SetTile(new Vector3Int(x,y,0),Road.Contains(cell)?roads[edges]:grasses[(x*7+y*11)%4]);
        }
        BuildDecorations();
        var camObject=new GameObject("Defense Tile Camera",typeof(Camera)); camObject.transform.SetParent(world.transform,false);
        camObject.transform.localPosition=new Vector3(Width*.5f,Height*.5f,-10);
        var cam=camObject.GetComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=Height*.5f; cam.cullingMask=1<<30;
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.28f,.49f,.18f);
        output=new RenderTexture(600,840,16); output.Create(); cam.targetTexture=output; cam.aspect=(float)Width/Height;
        display.texture=output;
        // 맵과 장식은 정적입니다. 몬스터/고양이는 UI에서 따로 움직입니다.
        map.RefreshAllTiles();
        mapCamera=cam;
        Camera.onPostRender+=SnapshotRendered;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=PipelineSnapshotRendered;
    }
    public static Vector2 Center(Vector2Int cell) => new Vector2((cell.x+.5f)/Width,(cell.y+.5f)/Height);
    private Tile MakeTile(bool road,int variant)
    {
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false); texture.filterMode=FilterMode.Point;
        for(int y=0;y<32;y++) for(int x=0;x<32;x++)
        {
            float noise=((x*17+y*31+x*y)%19)/420f;
            Color color=road?new Color(.66f+noise,.55f+noise,.35f+noise):new Color(.30f+noise+variant*.012f,.49f+noise+variant*.016f,.20f+noise);
            if(road)
            {
                bool rim=((variant&1)!=0 && x<2)||((variant&2)!=0 && x>29)||((variant&4)!=0 && y<2)||((variant&8)!=0 && y>29);
                if(rim) color=new Color(.39f,.35f,.20f);
                else if((x*13+y*23)%113==0) color=new Color(.77f,.67f,.46f);
            }
            else
            {
                // 작은 풀잎과 꽃은 타일 안에 구워 별도 오브젝트를 늘리지 않습니다.
                if((x*7+y*11+variant*17)%67==0) color=new Color(.48f,.63f,.28f);
                if(variant==2 && ((x-12)*(x-12)+(y-18)*(y-18)<=3 || (x-24)*(x-24)+(y-7)*(y-7)<=2)) color=new Color(.98f,.83f,.43f);
                if(variant==3 && ((x-9)*(x-9)+(y-12)*(y-12)<=2)) color=new Color(.95f,.92f,.73f);
            }
            texture.SetPixel(x,y,color);
        }
        texture.Apply(); var sprite=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,32);
        var tile=ScriptableObject.CreateInstance<Tile>(); tile.sprite=sprite; assets.Add(tile); assets.Add(sprite); assets.Add(texture); return tile;
    }
    private void BuildDecorations()
    {
        var texture=Resources.Load<Texture2D>("DungeonForestProps");
        if(texture==null) return;
        var regions=new[] {new Rect(38,38,566,580),new Rect(674,175,545,439),new Rect(82,648,509,555),new Rect(674,733,548,456)};
        var sprites=new Sprite[4];
        for(int i=0;i<sprites.Length;i++)
        {
            var r=regions[i]; float sx=texture.width/1254f,sy=texture.height/1254f;
            sprites[i]=Sprite.Create(texture,new Rect(r.x*sx,(1254-r.yMax)*sy,r.width*sx,r.height*sy),new Vector2(.5f,0),r.width*sx,0,SpriteMeshType.FullRect);
            assets.Add(sprites[i]);
        }
        PlaceProp(sprites[0],0,12,2,2,"Old Forest Tree");
        PlaceProp(sprites[0],2,2,2,2,"Garden Tree");
        PlaceProp(sprites[0],8,6,2,2,"East Tree");
        PlaceProp(sprites[2],6,10,1,2,"Cat Guardian Statue");
        PlaceProp(sprites[3],0,0,1,1,"Supply Crate");
        PlaceProp(sprites[3],9,3,1,1,"Lantern Supplies");
        PlaceProp(sprites[1],3,11,1,1,"Mushroom Grove");
        PlaceProp(sprites[1],7,4,1,1,"Mushroom Grove");
        PlaceProp(sprites[1],0,8,1,1,"Mushroom Grove");
        PlaceProp(sprites[1],9,10,1,1,"Mushroom Grove");
    }
    private void PlaceProp(Sprite sprite,int x,int y,int width,int height,string label)
    {
        for(int j=y;j<y+height;j++) for(int i=x;i<x+width;i++)
            if(i<0 || i>=Width || j<0 || j>=Height || !CanPlace(new Vector2Int(i,j))) return;
        for(int j=y;j<y+height;j++) for(int i=x;i<x+width;i++) decorations.Add(new Vector2Int(i,j));
        var obj=new GameObject(label); obj.layer=30; obj.transform.SetParent(world.transform,false);
        obj.transform.localPosition=new Vector3(x+width*.5f,y+.06f,0);
        float scale=Mathf.Min(width-.12f,(height-.12f)/sprite.bounds.size.y);
        obj.transform.localScale=Vector3.one*scale;
        var renderer=obj.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;
        renderer.sortingOrder=1000-y;
    }
    private void SnapshotRendered(Camera camera)
    {
        if(camera==mapCamera) camera.enabled=false;
    }
    private void PipelineSnapshotRendered(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera) => SnapshotRendered(camera);
    private void LateUpdate()
    {
        // 모바일 복귀 등으로 렌더 텍스처가 사라졌을 때만 다시 그립니다.
        if(output!=null && !output.IsCreated() && mapCamera!=null)
        {
            output.Create();
            mapCamera.enabled=true;
        }
    }
    private void OnDestroy()
    {
        Camera.onPostRender-=SnapshotRendered;
        UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=PipelineSnapshotRendered;
        if(world!=null) Destroy(world);
        if(output!=null) {output.Release();Destroy(output);}
        foreach(var asset in assets) if(asset!=null) Destroy(asset);
    }
}
