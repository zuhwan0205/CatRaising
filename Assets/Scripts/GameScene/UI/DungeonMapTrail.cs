using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class DungeonMapTrail : MaskableGraphic
{
    private Vector2[] points;

    public void Initialize(Vector2[] route)
    {
        points = (Vector2[])route.Clone();
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (points == null || points.Length < 2) return;
        var rect = rectTransform.rect;
        for (int segment = 0; segment < points.Length - 1; segment++)
            for (int step = 0; step < 20; step++)
                AddOval(mesh, rect, Sample(segment, step / 20f), new Vector2(.007f, .012f), new Color(.94f, .82f, .51f, .85f));
        for (int segment = 0; segment < points.Length - 1; segment++)
            for (int step = 0; step < 10; step++)
                AddOval(mesh, rect, Sample(segment, step / 10f), new Vector2(.0017f, .0026f), new Color(.75f, .53f, .27f));
    }

    private Vector2 Sample(int segment, float t)
    {
        var start = points[segment];
        var end = points[segment + 1];
        return new Vector2(Mathf.Lerp(start.x, end.x, t), Mathf.Lerp(start.y, end.y, Mathf.SmoothStep(0, 1, t)));
    }

    private static void AddOval(VertexHelper mesh, Rect rect, Vector2 point, Vector2 radius, Color tint)
    {
        int first = mesh.currentVertCount;
        var center = rect.min + Vector2.Scale(point, rect.size);
        var size = Vector2.Scale(radius, rect.size);
        mesh.AddVert(center, tint, Vector2.zero);
        const int sides = 24;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * size.x, Mathf.Sin(angle) * size.y), tint, Vector2.zero);
        }
        for (int i = 0; i < sides; i++) mesh.AddTriangle(first, first + i + 1, first + (i + 1) % sides + 1);
    }
}
