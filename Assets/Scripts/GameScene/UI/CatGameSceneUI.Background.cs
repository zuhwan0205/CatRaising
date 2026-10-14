using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    public bool UpgradeBackground()
    {
        if (safeArea == null) return false;
        var background = safeArea.parent.Find("Background") as RectTransform;
        if (background == null || background.Find("Cozy Cat Wallpaper") != null) return false;
        var texture = Resources.Load<Texture2D>("GameUIArt/CozyCatWallpaper");
        if (texture == null) return false;
        var picture = Rect(background, "Cozy Cat Wallpaper", Vector2.zero, Vector2.one);
        var image = picture.gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = (float)texture.width / texture.height;
        if (background.GetComponent<RectMask2D>() == null) background.gameObject.AddComponent<RectMask2D>();
        background.GetComponent<Image>().raycastTarget = false;
        return true;
    }
}
