using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIDesignEditor
{
    private const string AssetPath = "Assets/Scripts/Resources/UIDesignOverrides.asset";

    [MenuItem("CatRaising/UI 디자인/선택 UI 디자인 저장 (하위 포함)")]
    private static void Save()
    {
        var selected = Selection.activeTransform as RectTransform;
        var canvas = selected == null ? null : selected.GetComponentInParent<Canvas>(true)?.rootCanvas;
        if (canvas == null) { Debug.LogWarning("Hierarchy에서 Canvas 내부의 UI를 선택해주세요."); return; }
        var settings = AssetDatabase.LoadAssetAtPath<UIDesignOverrides>(AssetPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<UIDesignOverrides>();
            AssetDatabase.CreateAsset(settings, AssetPath);
        }
        Undo.RecordObject(settings, "Save UI design");
        int count = 0;
        foreach (var rect in selected.GetComponentsInChildren<RectTransform>(true))
        {
            // Screen and safe-area rectangles are controlled by the device, not the design.
            if (rect == canvas.transform || rect.name == "Safe Area") continue;
            var entry = Capture(canvas, rect);
            var previous = settings.entries.Find(e => e.canvasKey == entry.canvasKey && e.path == entry.path);
            if (previous != null) { entry.fixedCaption = previous.fixedCaption; entry.caption = previous.caption; }
            settings.entries.RemoveAll(e => e.canvasKey == entry.canvasKey && e.path == entry.path);
            settings.entries.Add(entry);
            count++;
        }
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"UI 디자인 {count}개 저장 완료. Play 종료 후에도 유지됩니다. 글자 내용·재화·버튼 동작은 저장하지 않습니다.", settings);
    }

    [MenuItem("CatRaising/UI 디자인/선택 Text 고정 문구 저장")]
    private static void SaveCaption()
    {
        var text = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<Text>();
        if (text == null || text.GetComponentInParent<InputField>() != null)
        { Debug.LogWarning("입력칸이 아닌 제목·버튼 등의 고정 Text를 선택해주세요."); return; }
        Save();
        var settings = AssetDatabase.LoadAssetAtPath<UIDesignOverrides>(AssetPath);
        var canvas = text.GetComponentInParent<Canvas>(true)?.rootCanvas;
        if (settings == null || canvas == null) return;
        var entry = settings.entries.Find(e => e.canvasKey == UIDesignOverrides.Key(canvas) &&
            e.path == UIDesignOverrides.PathOf(canvas.transform, text.transform));
        if (entry == null) return;
        entry.fixedCaption = true; entry.caption = text.text;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("고정 문구를 저장했습니다. 재화·레벨·HP 등 실시간 값에는 사용하지 마세요.");
    }

    private static UIDesignOverrides.Entry Capture(Canvas canvas, RectTransform rect)
    {
        var e = new UIDesignOverrides.Entry
        {
            canvasKey = UIDesignOverrides.Key(canvas), path = UIDesignOverrides.PathOf(canvas.transform, rect),
            anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, pivot = rect.pivot,
            size = rect.sizeDelta, position = rect.anchoredPosition, scale = rect.localScale
        };
        if (rect.TryGetComponent<Graphic>(out var g)) { e.graphic = true; e.color = g.color; }
        if (rect.TryGetComponent<Text>(out var t))
        {
            e.text = true; e.font = EditorUtility.IsPersistent(t.font) ? t.font : null;
            e.fontSize = t.fontSize; e.fontStyle = t.fontStyle; e.alignment = t.alignment;
            e.bestFit = t.resizeTextForBestFit; e.minSize = t.resizeTextMinSize; e.maxSize = t.resizeTextMaxSize;
        }
        if (rect.TryGetComponent<Image>(out var i) && (i.sprite == null || EditorUtility.IsPersistent(i.sprite)))
        { e.image = true; e.sprite = i.sprite; e.preserveAspect = i.preserveAspect; }
        if (rect.TryGetComponent<RawImage>(out var r))
        { e.rawImage = true; e.texture = EditorUtility.IsPersistent(r.texture) ? r.texture : null; e.uv = r.uvRect; }
        if (rect.TryGetComponent<AspectRatioFitter>(out var a))
        { e.aspect = true; e.aspectEnabled = a.enabled; e.aspectMode = a.aspectMode; e.aspectRatio = a.aspectRatio; }
        return e;
    }

    [MenuItem("CatRaising/UI 디자인/선택 UI 저장 디자인 삭제 (하위 포함)")]
    private static void Remove()
    {
        var selected = Selection.activeTransform;
        var canvas = selected == null ? null : selected.GetComponentInParent<Canvas>(true)?.rootCanvas;
        var settings = AssetDatabase.LoadAssetAtPath<UIDesignOverrides>(AssetPath);
        if (canvas == null || settings == null) return;
        Undo.RecordObject(settings, "Remove UI design");
        string path = UIDesignOverrides.PathOf(canvas.transform, selected);
        settings.entries.RemoveAll(e => e.canvasKey == UIDesignOverrides.Key(canvas) &&
            (path.Length == 0 || e.path == path || e.path.StartsWith(path + "/")));
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("저장한 디자인을 삭제했습니다. 화면을 다시 열거나 Play를 다시 시작하면 기본 디자인을 사용합니다.");
    }
}
