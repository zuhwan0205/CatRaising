using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity { Normal, Epic, Unique, Legendary }
public enum EquipmentSlot { Weapon, Armor, Accessory }
public enum EffectTrigger { OnAttack, OnHit, OnDamaged, OnKill }

[Serializable]
public class StatValues
{
    public float attack;
    public float defense;
    public float attackSpeed;
    public float maxHealth;
}

[Serializable]
public class CharacterDefinition
{
    [TextArea] public string description;
    public Sprite icon;
    public float attackRange = 1.7f;
    public string id;
    public string displayName;
    public ItemRarity rarity;
    public string attackId;
    public StatValues baseStats = new StatValues();
    public StatValues statsPerLevel = new StatValues();
}

[Serializable]
public class EquipmentDefinition
{
    public string id;
    public string displayName;
    public ItemRarity rarity;
    public EquipmentSlot slot;
    public StatValues baseStats = new StatValues();
    public StatValues statsPerEnhancement = new StatValues();
}

[Serializable]
public class RelicDefinition
{
    [TextArea] public string description;
    public Sprite icon;
    public float effectPerLevel;
    public string id;
    public string displayName;
    public ItemRarity rarity;
    public string effectId;
    public EffectTrigger trigger;
    [Range(0, 1)] public float chance = 1;
    public float effectValue;
    public float durationSeconds;
    public float cooldownSeconds;
    public int maxStacks = 1;
}

// 공통 설정은 유저 세이브에 복제하지 않습니다.
[CreateAssetMenu(menuName = "Cat Raising/Game Catalog")]
public class CatGameCatalog : ScriptableObject
{
    public List<CatCharacterAsset> characterAssets = new List<CatCharacterAsset>();
    public List<CatRelicAsset> relicAssets = new List<CatRelicAsset>();

    private void OnValidate()
    {
        var ids = new HashSet<string>();
        foreach (var asset in characterAssets)
            if (asset != null && asset.definition != null && !ids.Add(asset.definition.id))
                Debug.LogError("캐릭터 에셋 ID가 중복됩니다: " + asset.definition.id, this);
        ids.Clear();
        foreach (var asset in relicAssets)
            if (asset != null && asset.definition != null && !ids.Add(asset.definition.id))
                Debug.LogError("유물 에셋 ID가 중복됩니다: " + asset.definition.id, this);
    }

    // 이전 카탈로그의 인라인 데이터는 유지하되, 개별 에셋을 우선 조회합니다.
    public CharacterDefinition FindCharacter(string id)
    {
        var asset = characterAssets.Find(x => x != null && x.definition != null && x.definition.id == id);
        return asset != null ? asset.definition : characters.Find(x => x != null && x.id == id);
    }

    public RelicDefinition FindRelic(string id)
    {
        var asset = relicAssets.Find(x => x != null && x.definition != null && x.definition.id == id);
        return asset != null ? asset.definition : relics.Find(x => x != null && x.id == id);
    }
    public const string StarterCharacterId = "cat_starter";
    public List<CharacterDefinition> characters = new List<CharacterDefinition>
    {
        new CharacterDefinition
        {
            id = StarterCharacterId, displayName = "기본 고양이",
            rarity = ItemRarity.Normal, attackId = "claw_melee",
            baseStats = new StatValues { attack = 3.5f, attackSpeed = 1, maxHealth = 100 },
            statsPerLevel = new StatValues { attack = 1, maxHealth = 5 }
        }
    };
    public List<EquipmentDefinition> equipment = new List<EquipmentDefinition>();
    public List<RelicDefinition> relics = new List<RelicDefinition>();
}
