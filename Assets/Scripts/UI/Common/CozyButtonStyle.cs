using UnityEngine;
using UnityEngine.UI;

// 시안의 나무 테두리를 UI 요소로 구성하여 버튼 폭이 달라도 선 두께를 유지합니다.
public static class CozyButtonStyle
{
    public static readonly Color Green = new Color(.46f, .58f, .32f);
    public static readonly Color Cream = new Color(.96f, .86f, .65f);
    public static readonly Color Peach = new Color(.95f, .60f, .37f);

    public static void Apply(Button button, Color faceColor)
    {
        var root = button.GetComponent<RectTransform>();
        var frame = button.GetComponent<Image>();
        frame.color = new Color(.27f, .16f, .08f);
        var rim = Layer(root, "Wood Rim", 3, new Color(.69f, .43f, .20f));
        var highlight = Layer(rim.rectTransform, "Wood Highlight", 3, new Color(.96f, .75f, .39f));
        var inset = Layer(highlight.rectTransform, "Inner Wood", 3, new Color(.43f, .28f, .13f));
        var face = Layer(inset.rectTransform, "Button Face", 2, Color.white);
        rim.transform.SetAsFirstSibling();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.ColorTint;
        SetColor(button, faceColor);
        if (button.GetComponent<ButtonTweenEffect>() == null)
            button.gameObject.AddComponent<ButtonTweenEffect>();
    }

    public static void SetColor(Button button, Color color)
    {
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, .20f);
        colors.selectedColor = Color.Lerp(color, Color.white, .12f);
        colors.pressedColor = Color.Lerp(color, Color.black, .24f);
        colors.disabledColor = new Color(.52f, .51f, .46f);
        colors.colorMultiplier = 1;
        colors.fadeDuration = .08f;
        button.colors = colors;
    }

    private static Image Layer(RectTransform parent, string name, float inset, Color color)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
