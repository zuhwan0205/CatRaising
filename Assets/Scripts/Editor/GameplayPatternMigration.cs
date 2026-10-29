using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameplayPatternMigration
{
    static GameplayPatternMigration() => EditorApplication.delayCall += Prepare;
    [MenuItem("CatRaising/Prepare Gameplay Pattern Data")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            BuildPipeline.isBuildingPlayer || AssetDatabase.IsAssetImportWorkerProcess()) return;
        const string path = "Assets/Scripts/Resources/CatVisualCatalog.asset";
        if (AssetDatabase.LoadAssetAtPath<CatVisualCatalog>(path) == null)
        {
            var catalog = ScriptableObject.CreateInstance<CatVisualCatalog>();
            string[] ids = { "cat_starter", "cat_spinner", "cat_dancer", "cat_guardian", "cat_knight", "cat_kitten_trio",
                "cat_celestial", "cat_moon", "cat_lion", "cat_ninja", "cat_shadow", "cat_tiger" };
            float[] heights = { 0, 0, 0, 0, 336f/362*1.45f, 228f/362*1.65f, 294f/362*1.65f,
                325f/362*1.45f, 317f/362*1.55f, 321f/362*1.45f, 334f/362*1.45f, 296f/362*1.65f };
            catalog.characters = new CatVisualDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var sprites = LegacyCatFrameMigration.Get(ids[i]);
                if (sprites.Any(s => s == null)) throw new InvalidOperationException("Missing source frames: " + ids[i]);
                var sheet = sprites[0].texture;
                string source = AssetDatabase.GetAssetPath(sheet);
                int start = source.LastIndexOf("/Resources/", StringComparison.Ordinal) + "/Resources/".Length;
                string resource = source.Substring(start, source.LastIndexOf('.') - start);
                catalog.characters[i] = new CatVisualDefinition { characterId = ids[i], sheetResource = resource,
                    mainBattleScale = heights[i] == 0 ? 1 : (262f / (1254f / 4f) * 1.65f) / heights[i],
                    frames = sprites.Select(s => new CatSpriteFrame { normalizedRect = new Rect(s.rect.x / sheet.width,
                        s.rect.y / sheet.height, s.rect.width / sheet.width, s.rect.height / sheet.height),
                        pivot = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height),
                        pixelsPerUnitPerSheetHeight = s.pixelsPerUnit / sheet.height }).ToArray() };
            }
            AssetDatabase.CreateAsset(catalog, path);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:CatCharacterAsset", new[] { "Assets/Scripts" }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<CatCharacterAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (Migrate(asset.definition)) EditorUtility.SetDirty(asset);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:CatGameCatalog", new[] { "Assets/Scripts" }))
        {
            var asset = AssetDatabase.LoadAssetAtPath<CatGameCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var definition in asset.characters) if (Migrate(definition)) EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
    }
    private static bool Migrate(CharacterDefinition value)
    {
        if (value == null || value.combatConfigurationVersion >= 1) return false;
        value.delivery = value.id == "cat_ninja" || value.id == "cat_shadow" || value.id == "cat_moon"
            ? AttackDelivery.Projectile : AttackDelivery.Immediate;
        value.hitEffect = value.id == "cat_shadow" ? CharacterHitEffect.Slow : value.id == "cat_moon" ? CharacterHitEffect.ArmorBreak : CharacterHitEffect.None;
        value.grantsAttackAura = value.id == "cat_dancer";
        value.combatConfigurationVersion = 1;
        return true;
    }
}
