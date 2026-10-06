using UnityEngine;

// 같은 RenderTexture 객체를 유지해 UI와 카메라 참조를 함께 갱신합니다.
[RequireComponent(typeof(RectTransform))]
public sealed class FieldViewportSize : MonoBehaviour
{
    public RenderTexture Target;
    private RectTransform viewport;
    private Canvas canvas;

    private void Awake()
    {
        viewport = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void LateUpdate()
    {
        if (Target == null || canvas == null)
            return;
        Vector2 size = viewport.rect.size * canvas.scaleFactor;
        if (size.x < 1 || size.y < 1)
            return;
        // 큰 창에서도 해상도를 제한하면서 표시 영역과 같은 종횡비를 유지합니다.
        float scale = Mathf.Min(1f, 1600f / Mathf.Max(size.x, size.y));
        int width = Mathf.Max(1, Mathf.RoundToInt(size.x * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(size.y * scale));
        if (Target.width == width && Target.height == height)
            return;
        Target.Release();
        Target.width = width;
        Target.height = height;
        Target.Create();
    }
}
