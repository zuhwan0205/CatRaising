using UnityEngine;

// One shared soft oval; no per-monster Update or material instance.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MonsterGroundShadow : MonoBehaviour
{
    private static Sprite sharedSprite;
    private static Texture2D sharedTexture;
    private SpriteRenderer image;
    private float sizeScale = 1;
    private void Awake()
    {
        if (sharedSprite == null)
        {
            const int size = 32;
            sharedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            sharedTexture.name = "Shared Monster Contact Shadow";
            sharedTexture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float radius = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1).magnitude;
                    pixels[y * size + x] = new Color32(255, 255, 255,
                        (byte)(255 * (1 - Mathf.SmoothStep(.55f, 1, radius))));
                }
            sharedTexture.SetPixels32(pixels);
            sharedTexture.Apply(false, true);
            sharedSprite = Sprite.Create(sharedTexture, new Rect(0, 0, size, size), Vector2.one * .5f, size);
        }
        image = GetComponent<SpriteRenderer>();
        image.sprite = sharedSprite;
        image.color = new Color(.10f, .12f, .08f, .32f);
        image.sortingOrder = -9000;
        gameObject.layer = 2;
        transform.localPosition = new Vector3(0, .015f, 0);
        transform.localRotation = Quaternion.Euler(90, 0, 0);
        SetSize(sizeScale);
    }
    public void SetSize(float scale)
    {
        sizeScale = scale;
        transform.localScale = new Vector3(.78f * scale, .48f * scale, 1);
    }
    private void OnEnable() { CatLocalSettings.MonsterShadowsChanged += Refresh; Refresh(); }
    private void OnDisable() => CatLocalSettings.MonsterShadowsChanged -= Refresh;
    private void Refresh() { if (image != null) image.enabled = CatLocalSettings.MonsterShadows; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShared()
    {
        if (sharedSprite != null) Destroy(sharedSprite);
        if (sharedTexture != null) Destroy(sharedTexture);
        sharedSprite = null; sharedTexture = null;
    }
}
