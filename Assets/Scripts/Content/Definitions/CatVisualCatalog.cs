using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CatSpriteFrame
{
    public Rect normalizedRect;
    public Vector2 pivot;
    public float pixelsPerUnitPerSheetHeight;
}
[Serializable]
public sealed class CatVisualDefinition
{
    public string characterId;
    [Tooltip("Resources 기준 경로. 텍스처를 직접 참조하지 않아 선택한 고양이만 로드합니다.")]
    public string sheetResource;
    public CatSpriteFrame[] frames;
    [Min(.01f)] public float mainBattleScale = 1;
}
[CreateAssetMenu(menuName = "Cat Raising/Visual Catalog")]
public sealed class CatVisualCatalog : ScriptableObject
{
    public CatVisualDefinition[] characters = Array.Empty<CatVisualDefinition>();
    private Dictionary<string, CatVisualDefinition> lookup;
    private static CatVisualCatalog current;
    public static CatVisualDefinition Find(string id)
    {
        if (current == null) current = Resources.Load<CatVisualCatalog>("CatVisualCatalog");
        return current == null ? null : current.Lookup(id);
    }
    public CatVisualDefinition Lookup(string id)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<string, CatVisualDefinition>();
            foreach (var value in characters)
                if (value != null && !string.IsNullOrEmpty(value.characterId)) lookup[value.characterId] = value;
        }
        return id != null && lookup.TryGetValue(id, out var result) ? result : null;
    }
    private void OnValidate() => lookup = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { if (current != null) current.lookup = null; current = null; }
}
