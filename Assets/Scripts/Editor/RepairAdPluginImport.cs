#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 광고 네이티브 플러그인의 디스크/AssetDatabase 수정 시각 불일치를 복구합니다.
public static class RepairAdPluginImport
{
    private const string RepairVersion = "CatRaising.AdPluginTimestampRepair.v1.";
    private static string RepairKey => RepairVersion + Application.dataPath;
    private static readonly string[] Paths =
    {
        "Assets/Plugins/iOS/unity-plugin-library.xcframework",
        "Assets/Plugins/Android/GoogleMobileAdsPlugin.androidlib"
    };

    [InitializeOnLoadMethod]
    private static void ScheduleOnce()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess() || EditorPrefs.GetBool(RepairKey, false)) return;
        EditorApplication.update -= WaitForEditor;
        EditorApplication.update += WaitForEditor;
    }

    private static void WaitForEditor()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= WaitForEditor;
        Repair();
    }

    [MenuItem("Cat Raising/Repair Ad Plugin Import")]
    private static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("플레이를 종료한 후 광고 플러그인 재임포트를 실행해주세요.");
            return;
        }
        // 재임포트 중 도메인이 다시 로드되어도 반복 실행하지 않습니다.
        EditorPrefs.SetBool(RepairKey, true);
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in Paths)
            {
                if (!Directory.Exists(path))
                {
                    Debug.LogWarning("광고 플러그인 경로가 없습니다: " + path);
                    continue;
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate |
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ImportRecursive);
            }
            Debug.Log("광고 네이티브 플러그인 재임포트 요청 완료. Console을 비운 뒤 오류 재발 여부를 확인해주세요.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogWarning("Unity를 재시작한 뒤 Cat Raising > Repair Ad Plugin Import를 실행해주세요.");
        }
    }
}
#endif
