using UnityEngine;
using System.Collections.Generic;

// A shared frame cache: metadata is data-driven, only the requested sheet is loaded.
public static class CatAnimationFrames
{
    private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
    public static System.Collections.IEnumerator Prepare(string id)
    {
        if (id == null || cache.ContainsKey(id)) yield break;
        var definition = CatVisualCatalog.Find(id);
        if (definition == null || string.IsNullOrEmpty(definition.sheetResource)) yield break;
        yield return Resources.LoadAsync<Texture2D>(definition.sheetResource);
        Get(id);
    }
    public static Sprite[] Get(string id)
    {
        id = id ?? "";
        if (cache.TryGetValue(id, out var cached)) return cached;
        var definition = CatVisualCatalog.Find(id);
        var sheet = definition == null ? null : Resources.Load<Texture2D>(definition.sheetResource);
        if (sheet == null || definition.frames == null) return cache[id] = new Sprite[12];
        var result = new Sprite[definition.frames.Length];
        for (int i = 0; i < result.Length; i++)
        {
            var frame = definition.frames[i];
            float left = frame.normalizedRect.x * sheet.width, bottom = frame.normalizedRect.y * sheet.height;
            var rect = new Rect(left, bottom, Mathf.Min(frame.normalizedRect.width * sheet.width, sheet.width - left),
                Mathf.Min(frame.normalizedRect.height * sheet.height, sheet.height - bottom));
            result[i] = Sprite.Create(sheet, rect, frame.pivot, sheet.height * frame.pixelsPerUnitPerSheetHeight, 0, SpriteMeshType.FullRect);
        }
        return cache[id] = result;
    }
    public static Sprite[] GetFallback()
    {
        const string key = "__fallback";
        if (cache.TryGetValue(key, out var existing)) return existing;
        var frames = new Sprite[5];
        var sheet = Resources.Load<Texture2D>("CatCombatFrames");
        var texture = sheet != null ? sheet : Resources.Load<Texture2D>("CatStarterIsometric");
        if (texture != null)
        {
            if (sheet != null)
            {
                float width = sheet.width / 2f, height = sheet.height / 2f;
                // 시트의 위쪽 행: 정면 대기/공격, 아래쪽 행: 후면 대기/공격.
                for (int i = 0; i < 4; i++)
                    frames[i] = Sprite.Create(sheet, new Rect((i % 2) * width, (i < 2 ? 1 : 0) * height, width, height), new Vector2(.5f, .5f), height / 1.65f, 0, SpriteMeshType.FullRect);
            }
            else
                frames[4] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), texture.height / 1.65f);

        }
        cache[key] = frames;
        return frames;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Clear();
        Application.quitting -= Clear;
        Application.quitting += Clear;
    }
    private static void Clear()
    {
        foreach (var frames in cache.Values)
            foreach (var frame in frames)
                if (frame != null) Object.Destroy(frame);
        cache.Clear();
    }
}
