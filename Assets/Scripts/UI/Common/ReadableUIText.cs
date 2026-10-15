using UnityEngine;
using UnityEngine.UI;

public static class ReadableUIText
{
    public static void Configure(Text text, int size)
    {
        text.fontSize = Mathf.Max(18, size);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = text.fontSize;
    }

    public static void Upgrade(Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            Configure(text, Mathf.RoundToInt(text.fontSize * (1.3f / 1.1f)));
    }
}
