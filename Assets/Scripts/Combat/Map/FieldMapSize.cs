using UnityEngine;

public sealed class FieldMapSize : MonoBehaviour
{
    public const float HalfSize = 54;
    [SerializeField] private bool expanded;
    [SerializeField] private bool extendedArea;
    public bool NeedsExpansion => !extendedArea || expanded;

    public static bool Expand(GameObject map)
    {
        RemovePebbles(map);
        var size = map.GetComponent<FieldMapSize>();
        if (size == null) size = map.AddComponent<FieldMapSize>();
        // 이전 버전의 오브젝트 확대를 되돌리고 원래 크기의 지형을 이어 붙입니다.
        if (size.expanded)
        {
            map.transform.localScale /= 3;
            size.expanded = false;
        }
        if (size.extendedArea) return false;
        var originals = new Transform[map.transform.childCount];
        for (int i = 0; i < originals.Length; i++) originals[i] = map.transform.GetChild(i);
        for (int z = -1; z <= 1; z++)
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && z == 0) continue;
                var area = new GameObject($"Map Extension {x}, {z}").transform;
                area.SetParent(map.transform, false);
                area.localPosition = new Vector3(x * 36, 0, z * 36);
                foreach (var original in originals) Instantiate(original.gameObject, area, false);
            }
        size.extendedArea = true;
        return true;
    }

    public static bool RemovePebbles(GameObject map)
    {
        bool changed = false;
        foreach (var item in map.GetComponentsInChildren<Transform>(true))
        {
            if (!item.name.StartsWith("Loose Stone", System.StringComparison.Ordinal) || !item.gameObject.activeSelf) continue;
            // 프리팹 연결을 유지하면서 그림자와 충돌도 함께 제외합니다.
            item.gameObject.SetActive(false);
            changed = true;
        }
        return changed;
    }
}
