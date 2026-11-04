using UnityEngine;
using UnityEngine.UI;

// A bounded, scene-owned pool. No per-hit GameObjects, tweens or material instances.
[DefaultExecutionOrder(220)]
public sealed class HitSparkPool : MonoBehaviour
{
    private readonly Transform[] items = new Transform[24];
    private readonly SpriteRenderer[] world = new SpriteRenderer[24];
    private readonly Image[] ui = new Image[24];
    private readonly float[] life = new float[24];
    private readonly float[] sizes = new float[24];
    private int next;
    private readonly Vector3[] positions = new Vector3[24];
    private readonly Quaternion[] rotations = new Quaternion[24];
    private Sprite sprite;
    private Texture2D texture;
    public static void Emit(Transform root, Vector3 position, Quaternion rotation, float size, bool canvas = false)
    {
        if (root == null) return;
        var pool = root.GetComponent<HitSparkPool>();
        if (pool == null) pool = root.gameObject.AddComponent<HitSparkPool>();
        pool.Play(position, rotation, size, canvas);
    }
    private void Play(Vector3 position, Quaternion rotation, float size, bool canvas)
    {
        if (sprite == null)
        {
            texture = new Texture2D(16,16,TextureFormat.RGBA32,false) { filterMode = FilterMode.Point };
            var pixels = new Color32[256];
            for(int y=0;y<16;y++) for(int x=0;x<16;x++)
            {
                float dx=Mathf.Abs(x-7.5f), dy=Mathf.Abs(y-7.5f);
                if(dx+dy<5 || dx<1 && dy<7 || dy<1 && dx<7)
                    pixels[y*16+x]=new Color32(255,245,190,255);
            }
            texture.SetPixels32(pixels); texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,16);
        }
        int i=next; next=(next+1)%items.Length;
        if(items[i]==null)
        {
            var obj=new GameObject("Pooled Hit Spark",canvas?typeof(RectTransform):typeof(Transform));
            obj.transform.SetParent(transform,false); items[i]=obj.transform;
            if(canvas) { ui[i]=obj.AddComponent<Image>(); ui[i].sprite=sprite; ui[i].raycastTarget=false; ((RectTransform)items[i]).sizeDelta=Vector2.one; }
            else { obj.layer=2; world[i]=obj.AddComponent<SpriteRenderer>(); world[i].sprite=sprite; world[i].sortingOrder=19000; }
        }
        items[i].position=position; items[i].rotation=rotation;
        positions[i]=position; rotations[i]=rotation;
        sizes[i]=size; life[i]=.12f; items[i].localScale=Vector3.one*size;
        items[i].gameObject.SetActive(true); items[i].SetAsLastSibling();
        if(ui[i]!=null) ui[i].color=Color.white; else world[i].color=Color.white;
    }
    private void LateUpdate()
    {
        for(int i=0;i<items.Length;i++)
        {
            if(life[i]<=0) continue;
            life[i]=Mathf.Max(0,life[i]-Time.deltaTime);
            if(life[i]==0) { items[i].gameObject.SetActive(false); continue; }
            float t=life[i]/.12f;
            if(world[i]!=null) { items[i].position=positions[i]; items[i].rotation=rotations[i]; }
            items[i].localScale=Vector3.one*sizes[i]*(1.3f-.3f*t);
            var tint=new Color(1,1,1,t);
            if(ui[i]!=null) ui[i].color=tint; else world[i].color=tint;
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < items.Length; i++)
        {
            life[i] = 0;
            if (items[i] != null) items[i].gameObject.SetActive(false);
        }
    }
    private void OnDestroy() { if(sprite!=null) Destroy(sprite); if(texture!=null) Destroy(texture); }
}
