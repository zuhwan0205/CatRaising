using UnityEngine;
using UnityEngine.UI;

// Layout settings stay editable on the shop prefab. Width adapts to the viewport.
public sealed class SpecialShopGrid : GridLayoutGroup
{
    public override void SetLayoutHorizontal()
    {
        int columns = Mathf.Max(1, constraintCount);
        float width = (rectTransform.rect.width - padding.horizontal - spacing.x * (columns - 1)) / columns;
        cellSize = new Vector2(Mathf.Max(1, width), cellSize.y);
        base.SetLayoutHorizontal();
    }
}
