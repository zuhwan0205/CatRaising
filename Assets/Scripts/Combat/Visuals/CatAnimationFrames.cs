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
        var actionFrames = LoadActionFrames(id);
        if (actionFrames != null) return cache[id] = actionFrames;
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
        if (id == "cat_celestial") LoadPixelFrames("CatLegendaryBootsPixel", frames, 1.65f, i => new Vector2(BootsPivotX(i), i < 4 ? .02f : i < 8 ? .08f : .16f));
        if (id == "cat_moon") LoadPixelFrames("CatStarMagePixel", frames, 1.45f, i => new Vector2(.55f, i >= 4 && i < 8 ? .04f : .02f));
        if (id == "cat_lion") LoadPixelFrames("CatDragonPixel", frames, 1.55f, i => new Vector2(.5f, .08f));
        if (id == "cat_ninja") LoadPixelFrames("CatSiameseArcherPixel", frames, 1.45f, i => new Vector2(.5f, .08f));
        if (id == "cat_shadow") LoadPixelFrames("CatSnowflakePixel", frames, 1.45f, i => new Vector2(.52f, .02f));
        if (id == "cat_tiger") LoadPixelFrames("CatHammerKnightPixel", frames, 1.65f,
            i => new Vector2(i == 9 ? .38f : i == 10 ? .32f : i == 8 ? .48f : .64f, i < 4 ? .07f : i < 8 ? .12f : .16f));
        cache[id] = frames;
        return frames;
    }
    // 새 시트는 대기/걷기/공격/피격 4행입니다. 원본의 여백 차이를 잘라 발 기준을 맞춥니다.
    private static Sprite[] LoadActionFrames(string id)
    {
        string resource;
        int[] rows, attackColumns, bottoms, centers;
        switch (id)
        {
            case "cat_starter":
                resource = "CatStarterActionPixel";
                rows = new[] { 0, 313, 627, 934, 1254 };
                attackColumns = new[] { 0, 313, 627, 984, 1254 };
                bottoms = new[] { 312,312,312,313, 612,612,612,612, 902,901,901,902, 1203,1203,1203,1203 };
                centers = new[] { 190,490,790,1095, 190,500,800,1100, 180,480,785,1100, 185,490,790,1100 };
                break;
            case "cat_spinner":
                resource = "CatSpinnerActionPixel";
                rows = new[] { 0,313,627,939,1254 };
                attackColumns = new[] { 0,313,627,953,1254 };
                bottoms = new[] { 301,301,301,301, 613,613,611,613, 916,915,916,914, 1234,1233,1234,1233 };
                centers = new[] { 200,500,810,1120, 190,500,800,1120, 195,500,810,1120, 190,500,800,1120 };
                break;
            case "cat_dancer":
                resource = "CatDancerActionPixel";
                rows = new[] { 0,323,627,936,1254 };
                attackColumns = new[] { 0,318,651,984,1254 };
                bottoms = new[] { 323,323,323,323, 627,627,627,627, 919,919,919,919, 1219,1219,1219,1219 };
                // 이동 행은 리본/꼬리 외곽이 아닌 두 발의 중심(y=615)을 정렬합니다.
                // 특히 마지막 프레임의 기존 기준점은 발 중심보다 18px 왼쪽이었습니다.
                centers = new[] { 190,480,790,1090, 191,490,793,1108, 180,490,790,1130, 190,490,790,1100 };
                break;
            case "cat_guardian":
                resource = "CatGuardianActionPixel";
                rows = new[] { 0,313,627,940,1254 };
                attackColumns = new[] { 0,317,636,973,1254 };
                bottoms = new[] { 302,302,302,302, 621,622,622,623, 913,913,913,919, 1218,1216,1215,1220 };
                centers = new[] { 185,500,810,1125, 185,500,810,1125, 200,480,800,1120, 185,500,810,1125 };
                break;
            default: return null;
        }
        var sheet = Resources.Load<Texture2D>(resource);
        if (sheet == null) return null;
        var result = new Sprite[16];
        float sx = sheet.width / 1254f, sy = sheet.height / 1254f;
        int[] columns = { 0,313,627,940,1254 };
        for (int i = 0; i < result.Length; i++)
        {
            int row = i / 4, column = i % 4;
            var cuts = row == 2 ? attackColumns : columns;
            float left = cuts[column], right = cuts[column + 1];
            // 가장자리 효과를 포함하되 옆 프레임과 섞이지 않도록 경계를 보정합니다.
            if (id == "cat_starter" && row == 3) { if (column == 0) right = 315; if (column == 1) left = 315; }
            if (id == "cat_guardian" && row == 3) { if (column == 2) right = 946; if (column == 3) left = 946; }
            var rect = new Rect(left * sx, (1254 - rows[row + 1]) * sy, (right - left) * sx, (rows[row + 1] - rows[row]) * sy);
            var pivot = new Vector2((centers[i] - left) / (right - left), (rows[row + 1] - bottoms[i]) / (float)(rows[row + 1] - rows[row]));
            result[i] = Sprite.Create(sheet, rect, pivot, sheet.height / 4f / 1.65f, 0, SpriteMeshType.FullRect);
        }
        return result;
    }
    // 원본 시트의 머리/몸통이 프레임마다 옆으로 밀려 있어 셀 중앙 대신 몸 중심을 맞춥니다.
    // 칼·꼬리까지 포함한 외곽 중심은 포즈에 따라 바뀌므로 사용하지 않습니다.
    private static float BootsPivotX(int frame)
    {
        switch (frame)
        {
            case 1: return .426f;
            case 2: return .346f;
            case 3: return .419f;
            case 5: return .442f;
            case 6: return .361f;
            case 7: return .402f;
            default: return .5f;
        }
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
