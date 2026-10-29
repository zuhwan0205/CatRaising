using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Creates missing authoring assets once. Existing designer work is never regenerated.
[InitializeOnLoad]
public static class SpecialShopPrefabSetup
{
    public const string Folder = "Assets/Scripts/Resources/SpecialShop";
    static SpecialShopPrefabSetup() => EditorApplication.delayCall += Prepare;

    [MenuItem("CatRaising/Prepare Special Shop Prefabs")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            BuildPipeline.isBuildingPlayer || AssetDatabase.IsAssetImportWorkerProcess()) return;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Scripts/Resources", "SpecialShop");
        var catalog = AssetDatabase.LoadAssetAtPath<SpecialShopCatalog>(Folder + "/SpecialShopCatalog.asset");
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SpecialShopCatalog>();
            string[] titles = { "첫 만남 패키지", "반짝 다이아 패키지", "쑥쑥 성장 패키지", "집사의 보물 패키지" };
            string[] ids = { "welcome_preview", "diamond_preview", "growth_preview", "treasure_preview" };
            string[] prices = { "₩4,900", "₩9,900", "₩19,000", "₩29,000" };
            long[] gems = { 200, 500, 1200, 2000 }, gold = { 30000, 60000, 200000, 500000 };
            Color[] colors = { new Color(.20f,.59f,.65f), new Color(.43f,.39f,.69f), new Color(.70f,.39f,.29f), new Color(.62f,.48f,.23f) };
            var artwork = Resources.Load<Texture2D>("PackageShopArt");
            if (artwork == null) throw new InvalidOperationException("PackageShopArt 이미지가 없습니다.");
            catalog.offers = new SpecialShopOffer[4];
            for (int i = 0; i < 4; i++)
                catalog.offers[i] = new SpecialShopOffer { id = ids[i], title = titles[i], previewPrice = prices[i],
                    diamonds = gems[i], gold = gold[i], background = colors[i], artwork = artwork,
                    artworkUV = new Rect(i % 2 * .5f, i < 2 ? .5f : 0, .5f, .5f) };
            AssetDatabase.CreateAsset(catalog, Folder + "/SpecialShopCatalog.asset");
        }
        if (File.Exists(Folder + "/SpecialShopView.prefab") && File.Exists(Folder + "/SpecialShopCard.prefab") &&
            File.Exists(Folder + "/SpecialShopPopup.prefab")) return;
        if (catalog.offers == null || catalog.offers.Length == 0 || catalog.offers[0] == null)
            throw new InvalidOperationException("기본 프리팹을 만들 상품 데이터가 없습니다.");
        var host = new GameObject("Special Shop Template Authoring");
        try
        {
            host.AddComponent<CatGameSceneUI>().CreateSpecialShopTemplates(Folder, catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("특별 상점 프리팹 생성 완료: " + Folder + ". 이후 이 프리팹을 직접 편집하세요.");
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }
}
