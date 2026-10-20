using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 에디터에서 한 번 생성한 UI를 일반 씬 오브젝트로 저장합니다.
[InitializeOnLoad]
public static class LoadingSceneUISetup
{
    static LoadingSceneUISetup()
    {
        EditorApplication.delayCall += PrepareLoadedScenes;
        EditorSceneManager.sceneOpened += (_, __) => PrepareLoadedScenes();
    }

    [MenuItem("CatRaising/Prepare Loading Scene UI")]
    private static void PrepareLoadedScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            BuildPipeline.isBuildingPlayer || AssetDatabase.IsAssetImportWorkerProcess()) return;
        foreach (var manager in Object.FindObjectsByType<BackendManager>(FindObjectsInactive.Include))
        {
            var scene = manager.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
            var ui = manager.GetComponent<LoadingLoginUI>();
            if (ui != null && ui.HasSceneUI)
            {
                if (ui.UpgradeTextSize() | ui.UpgradeBackgroundSize())
                {
                    EditorUtility.SetDirty(ui);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                continue;
            }
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Prepare Loading Scene UI");
            Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Prepare Loading Scene UI");
            if (ui == null) ui = Undo.AddComponent<LoadingLoginUI>(manager.gameObject);
            ui.Build(manager);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("로딩 UI를 씬에 생성했습니다. Login Canvas를 편집하고 씬을 저장해주세요.", ui);
        }
    }
}
