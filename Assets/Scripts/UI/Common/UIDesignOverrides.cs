using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: never stores listeners, interactability, visibility or player values.
public sealed class UIDesignOverrides : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string canvasKey, path;
        public Vector2 anchorMin, anchorMax, pivot, size, position;
        public Vector3 scale;
        public bool graphic, text, image, rawImage, aspect;
        public bool fixedCaption;
        [TextArea] public string caption;
        public Color color;
        public int fontSize, minSize, maxSize;
        public bool bestFit, preserveAspect, aspectEnabled;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public Font font;
        public Sprite sprite;
        public Texture texture;
        public Rect uv;
        public AspectRatioFitter.AspectMode aspectMode;
        public float aspectRatio;
        [NonSerialized] public RectTransform lastApplied;

        public void Apply(RectTransform rect)
        {
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot;
            rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = scale;
            if (rect.TryGetComponent<ButtonTweenEffect>(out var press)) press.SetDesignScale(scale);
            if (rect.TryGetComponent<UISelectionMotion>(out var selection)) selection.SetDesignScale(scale);
            if (graphic && rect.TryGetComponent<Graphic>(out var g)) g.color = color;
            if (text && rect.TryGetComponent<Text>(out var t))
            {
                if (font != null) t.font = font;
                t.fontSize = fontSize; t.fontStyle = fontStyle; t.alignment = alignment;
                t.resizeTextForBestFit = bestFit; t.resizeTextMinSize = minSize; t.resizeTextMaxSize = maxSize;
                if (fixedCaption) t.text = caption;
            }
            if (image && rect.TryGetComponent<Image>(out var i))
            { i.sprite = sprite; i.preserveAspect = preserveAspect; }
            if (rawImage && rect.TryGetComponent<RawImage>(out var r))
            { if (texture != null) r.texture = texture; r.uvRect = uv; }
            if (aspect && rect.TryGetComponent<AspectRatioFitter>(out var a))
            { a.enabled = aspectEnabled; a.aspectMode = aspectMode; a.aspectRatio = aspectRatio; }
        }
    }

    public List<Entry> entries = new List<Entry>();
    private static UIDesignOverrides settings;
    private static bool loaded;
    private static readonly HashSet<Canvas> pending = new HashSet<Canvas>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Canvas.willRenderCanvases -= ApplyPending;
        pending.Clear(); settings = null; loaded = false;
    }

    // Called by UI factories; coalesces creation into one pass before rendering.
    public static void Schedule(Transform target)
    {
        if (!Application.isPlaying || target == null) return;
        var canvas = target.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;
        pending.Add(canvas.rootCanvas);
        Canvas.willRenderCanvases -= ApplyPending;
        Canvas.willRenderCanvases += ApplyPending;
    }

    private static void ApplyPending()
    {
        Canvas.willRenderCanvases -= ApplyPending;
        if (!loaded) { settings = Resources.Load<UIDesignOverrides>("UIDesignOverrides"); loaded = true; }
        foreach (var canvas in pending)
        {
            if (canvas == null) continue;
            foreach (var button in canvas.GetComponentsInChildren<Button>(true))
                CozyButtonStyle.RoundCorners(button);
            if (settings != null) settings.ApplyTo(canvas);
        }
        pending.Clear();
    }

    public void ApplyTo(Canvas canvas)
    {
        string key = Key(canvas);
        foreach (var entry in entries)
        {
            if (entry.canvasKey != key) continue;
            var target = Find(canvas.transform, entry.path) as RectTransform;
            // Do not restart tween scales or overwrite later state updates when another popup opens.
            if (target != null && entry.lastApplied != target)
            {
                entry.Apply(target);
                entry.lastApplied = target;
            }
        }
    }

    public static string Key(Canvas canvas) => canvas.gameObject.scene.name + "/" + canvas.name;

    public static string PathOf(Transform root, Transform target)
    {
        var parts = new List<string>();
        for (var node = target; node != root && node != null; node = node.parent)
        {
            int ordinal = 0;
            for (int i = 0; i < node.GetSiblingIndex(); i++)
                if (node.parent.GetChild(i).name == node.name) ordinal++;
            parts.Add(Uri.EscapeDataString(node.name) + "#" + ordinal);
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static Transform Find(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path)) return root;
        foreach (string part in path.Split('/'))
        {
            int split = part.LastIndexOf('#');
            if (split < 0 || !int.TryParse(part.Substring(split + 1), out int ordinal)) return null;
            string name = Uri.UnescapeDataString(part.Substring(0, split));
            Transform found = null;
            foreach (Transform child in root)
                if (child.name == name && ordinal-- == 0) { found = child; break; }
            if (found == null) return null;
            root = found;
        }
        return root;
    }
}
