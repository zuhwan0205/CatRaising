#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 외부에서 추가된 이미지의 최초 임포트를 확정합니다. 프로젝트 전체 설정은 변경하지 않습니다.
public static class CatArtworkImport
{


    [InitializeOnLoadMethod]
    private static void ScheduleImport()
    {
        EditorApplication.delayCall += EnsureImported;
    }

    [MenuItem("Cat Raising/Repair Cat Artwork Import")]
    private static void EnsureImported()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ImportTexture("Assets/Scripts/Content/Resources/CatStarterIsometric.png");
        ImportTexture("Assets/Scripts/Content/Resources/CatCombatFrames.png");
    }

    private static void ImportTexture(string Path)
    {
        if (!System.IO.File.Exists(Path)) return;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Path) == null)
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(Path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("고양이 이미지 임포트 실패: " + Path);
            return;
        }
        bool changed = importer.textureType != TextureImporterType.Default ||
            importer.npotScale != TextureImporterNPOTScale.None || !importer.alphaIsTransparency ||
            importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (changed)
        {
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Path) == null)
            Debug.LogError("고양이 이미지를 Texture2D로 읽을 수 없습니다: " + Path);
    }
}
#endif
