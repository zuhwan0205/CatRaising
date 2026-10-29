using UnityEngine;
using UnityEngine.UI;

// 월드맵의 길과 원형 스테이지를 텍스처 없이 그립니다.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DungeonMapOval : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        mesh.AddVert(center, color, Vector2.zero);
        const int sides = 24;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * rect.width * .5f, Mathf.Sin(angle) * rect.height * .5f), color, Vector2.zero);
        }
        for (int i = 0; i < sides; i++) mesh.AddTriangle(0, i + 1, (i + 1) % sides + 1);
    }
}
