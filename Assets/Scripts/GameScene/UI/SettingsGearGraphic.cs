using UnityEngine;
using UnityEngine.UI;

// 해상도에 관계없이 선명한 6톱니 설정 아이콘. 중앙은 실제로 비어 있습니다.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SettingsGearGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        float size = Mathf.Min(rect.width, rect.height);
        const int segments = 192;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            float tooth = Mathf.InverseLerp(-.5f, .5f, Mathf.Cos(6 * (angle - Mathf.PI / 2)));
            tooth = tooth * tooth * (3 - 2 * tooth);
            float radius = Mathf.Lerp(.34f, .49f, tooth) * size;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(rect.center + direction * radius, color, Vector2.zero);
            mesh.AddVert(rect.center + direction * (.18f * size), color, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            mesh.AddTriangle(i * 2, next * 2, i * 2 + 1);
            mesh.AddTriangle(next * 2, next * 2 + 1, i * 2 + 1);
        }
    }
}
