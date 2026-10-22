using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GameSceneUISetup
{
    static GameSceneUISetup()
    {
        EditorApplication.delayCall += Prepare;
        EditorSceneManager.sceneOpened += (_, __) => Prepare();
    }

    [MenuItem("CatRaising/Prepare Game Scene UI")]
    private static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            BuildPipeline.isBuildingPlayer || AssetDatabase.IsAssetImportWorkerProcess()) return;
        foreach (var entry in Object.FindObjectsByType<CatGameScene>(FindObjectsInactive.Include))
        {
            var scene = entry.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
            var previewCamera = entry.transform.Find("Editor Map Preview Camera")?.GetComponent<Camera>();
            if (previewCamera != null && Mathf.Approximately(previewCamera.orthographicSize, 9))
            {
                previewCamera.orthographicSize = 10.5f;
                EditorUtility.SetDirty(previewCamera);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            var existingMap = entry.transform.Find("Editable Battle Map");
            if (existingMap != null && FieldMapSize.RemovePebbles(existingMap.gameObject))
                EditorSceneManager.MarkSceneDirty(scene);
            if (existingMap != null && (existingMap.GetComponent<FieldMapSize>() == null || existingMap.GetComponent<FieldMapSize>().NeedsExpansion))
            {
                FieldMapSize.Expand(existingMap.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            if (existingMap != null)
            {
                foreach (var facing in existingMap.GetComponentsInChildren<FieldBillboard>(true))
                    if (previewCamera != null) facing.ViewCamera = previewCamera;
                if (FieldPropGrounding.Apply(existingMap.gameObject))
                    EditorSceneManager.MarkSceneDirty(scene);
            }
            var ui = entry.GetComponent<CatGameSceneUI>();
            if (ui != null && ui.HasSceneUI)
            {
                if (ui.UpgradeTextSize() | ui.UpgradeCurrencyHeader() | ui.UpgradeBackground() | ui.UpgradeFullScreenBattle() | ui.UpgradeNavigationIcons())
                {
                    EditorUtility.SetDirty(ui);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                continue;
            }
            var prefab = Resources.Load<GameObject>("BattleMap");
            if (prefab == null)
            {
                Debug.LogWarning("BattleMap 프리팹을 찾을 수 없어 게임 씬 배치를 중단했습니다.", entry);
                continue;
            }
            var map = (GameObject)PrefabUtility.InstantiatePrefab(prefab, entry.transform);
            map.name = "Editable Battle Map";
            FieldMapSize.Expand(map);
            var cameraObject = new GameObject("Editor Map Preview Camera", typeof(Camera));
            cameraObject.transform.SetParent(entry.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.30f, .42f, .34f);
            camera.cullingMask = 1 << 2;
            camera.transform.rotation = Quaternion.Euler(50, 0, 0);
            camera.transform.position = -camera.transform.forward * 16;
            const string path = "Assets/Scripts/GameScene/GameScenePreview.renderTexture";
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (texture == null)
            {
                texture = new RenderTexture(720, 1000, 16);
                AssetDatabase.CreateAsset(texture, path);
            }
            camera.targetTexture = texture;
            foreach (var billboard in map.GetComponentsInChildren<FieldBillboard>(true))
            {
                billboard.ViewCamera = camera;
                billboard.transform.rotation = camera.transform.rotation;
            }
            FieldPropGrounding.Apply(map);
            entry.PrepareSceneUI(map, camera);
            EditorUtility.SetDirty(entry);
            EditorUtility.SetDirty(entry.GetComponent<CatGameSceneUI>());
            EditorSceneManager.MarkSceneDirty(scene);
            camera.Render();
            Debug.Log("게임 씬 UI와 맵을 배치했습니다. Game Canvas와 Editable Battle Map을 편집한 뒤 씬을 저장해주세요.", entry);
        }
    }
}
