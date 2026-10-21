using UnityEngine;
using System.Collections.Generic;

// 생성한 프레임만 소유합니다. 원본 Resources 텍스처는 파괴하지 않습니다.
public static class CatAnimationFrames
{
    private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
    public static Sprite[] Get(string id)
    {
        id = id ?? "";
        if (cache.TryGetValue(id, out var existing)) return existing;
        var frames = new Sprite[12];
        var pixelSheet = id == CatGameCatalog.StarterCharacterId ? Resources.Load<Texture2D>("CatStarterBipedPixel") : null;
        if (pixelSheet != null)
        {
            float cellWidth = pixelSheet.width / 4f, cellHeight = pixelSheet.height / 3f;
            // 위에서부터 대기/걷기/공격. 행마다 발바닥 기준점을 맞춥니다.
            float[] footPivots = { .045f, .075f, .04f };
            for (int i = 0; i < frames.Length; i++)
            {
                int row = i / 4;
                frames[i] = Sprite.Create(pixelSheet, new Rect(i % 4 * cellWidth, (2 - row) * cellHeight, cellWidth, cellHeight),
                    new Vector2(.52f, footPivots[row]), cellHeight / 1.45f, 0, SpriteMeshType.FullRect);
            }
        }

        var fishSheet = id == "cat_knight" ? Resources.Load<Texture2D>("CatMackerelBipedPixel") : null;
        if (fishSheet != null)
        {
            float w = fishSheet.width / 4f, h = fishSheet.height / 3f;
            for (int i = 0; i < 12; i++)
            {
                int row = i / 4;
                float leftInset = i == 11 ? w * .22f : 0;
                float width = i == 10 ? w * 1.14f : w - leftInset;
                // 공격의 고등어 끝이 기본 셀을 넘으므로 세 번째 공격 프레임만 넓게 읽습니다.
                var rect = new Rect(i % 4 * w + leftInset, (2 - row) * h, width, h);
                frames[i] = Sprite.Create(fishSheet, rect, new Vector2((.60f * w - leftInset) / width, row == 0 ? .04f : row == 1 ? .075f : .12f), h / 1.45f, 0, SpriteMeshType.FullRect);
            }
        }

        if (id == "cat_kitten_trio") LoadPixelFrames("CatKittenTrioPixel", frames, 1.65f, i => new Vector2(.52f, i < 4 ? .105f : i < 8 ? .12f : .14f));
        if (id == "cat_celestial") LoadPixelFrames("CatLegendaryBootsPixel", frames, 1.65f, i => new Vector2(.5f, i < 4 ? .02f : i < 8 ? .08f : .16f));
        if (id == "cat_moon") LoadPixelFrames("CatStarMagePixel", frames, 1.45f, i => new Vector2(.55f, i >= 4 && i < 8 ? .04f : .02f));
        if (id == "cat_lion") LoadPixelFrames("CatDragonPixel", frames, 1.55f, i => new Vector2(.5f, .08f));
        if (id == "cat_ninja") LoadPixelFrames("CatSiameseArcherPixel", frames, 1.45f, i => new Vector2(.5f, .08f));
        if (id == "cat_shadow") LoadPixelFrames("CatSnowflakePixel", frames, 1.45f, i => new Vector2(.52f, .02f));
        if (id == "cat_tiger") LoadPixelFrames("CatHammerKnightPixel", frames, 1.65f,
            i => new Vector2(i == 9 ? .38f : i == 10 ? .32f : i == 8 ? .48f : .64f, i < 4 ? .07f : i < 8 ? .12f : .16f));
        cache[id] = frames;
        return frames;
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

    private static void LoadPixelFrames(string resource, Sprite[] frames, float height, System.Func<int, Vector2> pivot)
    {
        var sheet = Resources.Load<Texture2D>(resource);
        if (sheet == null) return;
        float w = sheet.width / 4f, h = sheet.height / 3f;
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(sheet, new Rect(i % 4 * w, (2 - i / 4) * h, w, h),
                pivot(i), h / height, 0, SpriteMeshType.FullRect);
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
