using UnityEngine;
using UnityEngine.EventSystems;

public partial class CatGameSceneUI
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterPlayModeSelectionCleanup()
    {
        // Domain Reload를 꺼도 이벤트가 중복 등록되지 않게 합니다.
        UnityEditor.EditorApplication.playModeStateChanged -= ClearPlayModeSelection;
        UnityEditor.EditorApplication.playModeStateChanged += ClearPlayModeSelection;
    }

    private static void ClearPlayModeSelection(UnityEditor.PlayModeStateChange state)
    {
        if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
        // 플레이 종료 시 런타임 UI와 씬 오브젝트가 파괴되기 전에 Inspector 참조를 해제합니다.
        UnityEditor.Selection.objects = System.Array.Empty<UnityEngine.Object>();
    }
#endif

    private void ClearPage()
    {
        if (joystick != null) joystick.ResetInput();
        joystick=null;
        combatLabel=null;
        relicEffectLabel=null;
        playerHpLabel=null;
        lastDrawLabel=null;
        foreach (Transform child in page) RemoveUI(child.gameObject);
    }

    private void RemoveUI(GameObject target)
    {
        if (target == null) return;
        var events=EventSystem.current;
        if(events!=null && events.currentSelectedGameObject!=null &&
            events.currentSelectedGameObject.transform.IsChildOf(target.transform))
            events.SetSelectedGameObject(null);
#if UNITY_EDITOR
        // 삭제할 UI와 이미 파괴된 선택만 제거하고 나머지 선택은 유지합니다.
        var remainingSelection = new System.Collections.Generic.List<UnityEngine.Object>();
        bool selectionChanged = false;
        foreach(var selected in UnityEditor.Selection.objects)
        {
            if (selected == null) { selectionChanged = true; continue; }
            var component=selected as Component;
            var selectedObject=selected as GameObject;
            Transform selectedTransform=component!=null?component.transform:selectedObject!=null?selectedObject.transform:null;
            if(selectedTransform!=null && selectedTransform.IsChildOf(target.transform))
            {
                selectionChanged = true;
                continue;
            }
            remainingSelection.Add(selected);
        }
        if (selectionChanged) UnityEditor.Selection.objects = remainingSelection.ToArray();
#endif
        target.SetActive(false);
        Destroy(target);
    }
}
