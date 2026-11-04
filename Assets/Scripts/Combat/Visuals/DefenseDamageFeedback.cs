using UnityEngine;
using UnityEngine.UI;

public sealed class DefenseDamageFeedback : MonoBehaviour
{
    private readonly Text[] labels = new Text[24];
    private readonly Vector2[] origins = new Vector2[24];
    private readonly float[] remaining = new float[24];
    private int next;
    private float shake, nextShake;
    private Vector3 offset;
    private Font font;
    public void Show(Vector2 position, double damage, bool critical)
    {
        int i=next; next=(next+1)%labels.Length;
        if(labels[i]==null)
        {
            if(font==null) font=Resources.Load<Font>("Fonts/Mona12TextKR") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var obj=new GameObject("Pooled Defense Damage",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            obj.transform.SetParent(transform,false);
            labels[i]=obj.GetComponent<Text>(); labels[i].font=font; labels[i].alignment=TextAnchor.MiddleCenter; labels[i].raycastTarget=false;
            var rect=labels[i].rectTransform; rect.anchorMin=rect.anchorMax=Vector2.zero; rect.sizeDelta=new Vector2(180,64);
            var outline=obj.AddComponent<Outline>(); outline.effectColor=new Color(.12f,.06f,.03f); outline.effectDistance=new Vector2(1,-1);
        }
        labels[i].text=damage.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+(critical?"!":"");
        labels[i].fontSize=critical?32:24;
        labels[i].color=critical?new Color(1,.38f,.22f):Color.white;
        origins[i]=position+Vector2.up*25; remaining[i]=.5f;
        labels[i].rectTransform.anchoredPosition=origins[i];
        labels[i].gameObject.SetActive(true); labels[i].transform.SetAsLastSibling();
        if(critical && Time.time>=nextShake) { shake=.12f; nextShake=Time.time+.25f; }
    }
    private void LateUpdate()
    {
        transform.localPosition-=offset; offset=Vector3.zero;
        shake=Mathf.Max(0,shake-Time.deltaTime);
        if(shake>0) offset=new Vector3(Mathf.Sin(shake*200),Mathf.Sin(shake*290)*.5f,0)*(2*shake/.12f);
        transform.localPosition+=offset;
        for(int i=0;i<labels.Length;i++)
        {
            if(remaining[i]<=0) continue;
            remaining[i]=Mathf.Max(0,remaining[i]-Time.deltaTime);
            if(remaining[i]==0) { labels[i].gameObject.SetActive(false); continue; }
            float t=1-remaining[i]/.5f;
            labels[i].rectTransform.anchoredPosition=origins[i]+Vector2.up*(32*t);
            var color=labels[i].color; color.a=Mathf.Min(1,remaining[i]/.2f); labels[i].color=color;
        }
    }
    private void OnDisable()
    {
        transform.localPosition-=offset; offset=Vector3.zero; shake=nextShake=0;
        for(int i=0;i<labels.Length;i++) { remaining[i]=0; if(labels[i]!=null) labels[i].gameObject.SetActive(false); }
    }
}
