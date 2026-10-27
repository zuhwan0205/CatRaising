using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 월드 SpriteRenderer와 던전 Image가 같은 프레임을 공유합니다.
public static class CatSpellSprites
{
    public const float FlightSeconds = .25f;
    public const float ImpactSeconds = .22f;
    public const float BreathSeconds = .38f;
    private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
    public static bool Supports(string id) => Resource(id) != null;
    private static string Resource(string id) => id == "cat_lion" ? "DragonBreathVfx" :
        id == "cat_shadow" ? "SnowflakeVfx" : id == "cat_moon" ? "MoonlightVfx" : null;

    public static IEnumerator Prepare(string id)
    {
        string resource = Resource(id);
        if (resource == null || cache.ContainsKey(id)) yield break;
        yield return Resources.LoadAsync<Texture2D>(resource);
        Get(id);
    }

    public static Sprite[] Get(string id)
    {
        string resource = Resource(id);
        if (resource == null) return null;
        if (cache.TryGetValue(id, out var existing)) return existing;
        var texture = Resources.Load<Texture2D>(resource);
        if (texture == null) return null;
        // 1254px 원본의 여백을 제외한 셀. 픽셀은 변경하지 않고 스프라이트 영역만 지정합니다.
        Rect[] regions = id == "cat_lion" ? new[] {
            new Rect(150,250,360,260), new Rect(735,220,410,340),
            new Rect(108,775,472,344), new Rect(745,829,402,254) } :
            id == "cat_shadow" ? new[] {
            new Rect(74,212,506,228), new Rect(703,208,496,234),
            new Rect(120,741,388,390), new Rect(663,675,552,530) } : new[] {
            new Rect(36,196,552,314), new Rect(681,196,534,306),
            new Rect(90,683,484,462), new Rect(659,645,550,530) };
        var frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            Rect r = regions[i];
            Vector2 pivot = Vector2.one * .5f;
            if (id == "cat_lion")
            {
                float tipY = i < 2 ? 395 : 950;
                pivot = new Vector2(12f / r.width, 1 - (tipY - r.y) / r.height);
            }
            else if (i < 2) pivot = new Vector2(.78f, .5f);
            float sx = texture.width / 1254f, sy = texture.height / 1254f;
            frames[i] = Sprite.Create(texture, new Rect(r.x * sx, (1254 - r.yMax) * sy, r.width * sx, r.height * sy),
                pivot, r.width * sx, 0, SpriteMeshType.FullRect);
        }
        cache[id] = frames;
        return frames;
    }

    public static Sprite Frame(Sprite[] frames, bool fire, float age, bool impact)
    {
        if (frames == null) return null;
        int frame = fire ? Mathf.Clamp((int)(age / BreathSeconds * 4), 0, 3) :
            impact ? 2 + Mathf.Clamp((int)((age - FlightSeconds) / ImpactSeconds * 2), 0, 1) : (int)(age * 14) % 2;
        return frames[frame];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        Reset();
        Application.quitting -= Reset;
        Application.quitting += Reset;
    }

    private static void Reset()
    {
        foreach (var frames in cache.Values)
            foreach (var frame in frames) if (frame != null) Object.Destroy(frame);
        cache.Clear();
    }
}
