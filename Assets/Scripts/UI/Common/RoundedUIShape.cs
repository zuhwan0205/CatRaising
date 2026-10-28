using UnityEngine;

// Shared nine-slice shape for buttons and collection frames.
public static class RoundedUIShape
{
    private static Sprite collectionCorner;

    public static Sprite GetSprite()
    {
        if (collectionCorner != null) return collectionCorner;
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Max(8 - (x + .5f), (x + .5f) - 24, 0);
                float dy = Mathf.Max(8 - (y + .5f), (y + .5f) - 24, 0);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(8.5f - Mathf.Sqrt(dx * dx + dy * dy))));
            }
        texture.Apply();
        collectionCorner = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(9, 9, 9, 9));
        return collectionCorner;
    }

}
