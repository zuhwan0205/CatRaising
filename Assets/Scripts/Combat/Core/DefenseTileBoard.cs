using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class DefenseTileBoard : MonoBehaviour
{
    public const int Width=10, Height=14;
    public readonly HashSet<Vector2Int> Road=new HashSet<Vector2Int>();
    public Vector2[] Route { get; private set; }
    private GameObject world;
    private RenderTexture output;
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
        var grass=MakeTile(false); var road=MakeTile(true);
        for(int y=0;y<Height;y++) for(int x=0;x<Width;x++) map.SetTile(new Vector3Int(x,y,0),Road.Contains(new Vector2Int(x,y))?road:grass);
        var camObject=new GameObject("Defense Tile Camera",typeof(Camera)); camObject.transform.SetParent(world.transform,false);
        camObject.transform.localPosition=new Vector3(Width*.5f,Height*.5f,-10);
        var cam=camObject.GetComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=Height*.5f; cam.cullingMask=1<<30;
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.28f,.49f,.18f);
        output=new RenderTexture(600,840,16); output.Create(); cam.targetTexture=output; cam.aspect=(float)Width/Height;
        display.texture=output;
    }
    public static Vector2 Center(Vector2Int cell) => new Vector2((cell.x+.5f)/Width,(cell.y+.5f)/Height);
    private Tile MakeTile(bool road)
    {
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false); texture.filterMode=FilterMode.Point;
        for(int y=0;y<32;y++) for(int x=0;x<32;x++)
        {
            float noise=((x*17+y*31+x*y)%19)/190f;
            Color color=road?new Color(.55f+noise,.52f+noise,.43f+noise):new Color(.29f+noise,.52f+noise,.17f+noise);
            if(road && (x==0 || y==0)) color*=.83f;
            if(!road && (x*7+y*11)%47==0) color=new Color(.59f,.71f,.31f);
            texture.SetPixel(x,y,color);
        }
        texture.Apply(); var sprite=Sprite.Create(texture,new Rect(0,0,32,32),Vector2.one*.5f,32);
        var tile=ScriptableObject.CreateInstance<Tile>(); tile.sprite=sprite; assets.Add(tile); assets.Add(sprite); assets.Add(texture); return tile;
    }
    private void OnDestroy()
    {
        if(world!=null) Destroy(world);
        if(output!=null) {output.Release();Destroy(output);}
        foreach(var asset in assets) if(asset!=null) Destroy(asset);
    }
}
