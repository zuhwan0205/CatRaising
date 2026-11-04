using UnityEngine;
using UnityEngine.UI;

// Axis values are normalized independently; labels display the unscaled stats.
public sealed class CharacterRadarGraphic : MaskableGraphic
{
    public float[] Values = new float[6];
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Vector2 center = rectTransform.rect.center;
        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .48f;
        for (int ring = 1; ring <= 4; ring++)
            for (int i = 0; i < 6; i++)
                Line(vh, center + Point(i, radius * ring / 4), center + Point(i + 1, radius * ring / 4), new Color(.42f,.32f,.22f,.35f));
        for (int i = 0; i < 6; i++)
        {
            Line(vh, center, center + Point(i, radius), new Color(.42f,.32f,.22f,.25f));
            Vector2 a = center + Point(i, radius * Mathf.Clamp01(Values[i]));
            Vector2 b = center + Point(i + 1, radius * Mathf.Clamp01(Values[(i + 1) % 6]));
            int index = vh.currentVertCount;
            Color fill = new Color(.28f,.62f,.51f,.40f);
            vh.AddVert(center, fill, Vector2.zero); vh.AddVert(a, fill, Vector2.zero); vh.AddVert(b, fill, Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);
            Line(vh,a,b,new Color(.15f,.43f,.32f));
        }
    }
    private static Vector2 Point(int i, float r) => new Vector2(Mathf.Sin(i * Mathf.PI / 3), Mathf.Cos(i * Mathf.PI / 3)) * r;
    private static void Line(VertexHelper vh, Vector2 a, Vector2 b, Color tint)
    {
        Vector2 normal = new Vector2(-(b-a).y,(b-a).x).normalized * .8f;
        int n = vh.currentVertCount;
        vh.AddVert(a-normal,tint,Vector2.zero); vh.AddVert(a+normal,tint,Vector2.zero);
        vh.AddVert(b+normal,tint,Vector2.zero); vh.AddVert(b-normal,tint,Vector2.zero);
        vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
    }
}
