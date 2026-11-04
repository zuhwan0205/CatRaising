using System;
using System.Collections.Generic;
using UnityEngine;

// 숫자는 기존 에셋과의 호환을 위해 유지합니다.
public enum ItemRarity
{
    Common = 0, Epic = 1, Unique = 2, Legendary = 3
}
public enum EquipmentSlot
{
    Weapon, Armor, Accessory
}
public enum EffectTrigger
{
    OnAttack, OnHit, OnDamaged, OnKill
}

[Serializable]
public class StatValues
{
    public float attack;
    public float defense;
    public float attackSpeed;
    public float maxHealth;
}

public enum AttackDelivery { Immediate, Projectile }
public enum CharacterHitEffect { None, Slow, ArmorBreak }

[Serializable]
public class CharacterDefinition
{
    public bool inCharacterDraw = true;
    public CharacterGrowthRules fragmentGrowth = new CharacterGrowthRules();
    public ShopOffer shop = new ShopOffer();
    public LevelUpRules growth = new LevelUpRules();
    [TextArea] public string description;
    public Sprite icon;
    public float attackRange = 1.7f;
    public string id;
    public string displayName;
    public ItemRarity rarity;
    public string attackId;
    [HideInInspector] public int combatConfigurationVersion;
    public AttackDelivery delivery;
    public CharacterHitEffect hitEffect;
    public bool grantsAttackAura;
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
    public bool inRelicDraw = true;
    public ShopOffer shop = new ShopOffer();
    public LevelUpRules growth = new LevelUpRules();
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

    public double EffectAtLevel(int level, int ascension = 0)
    {
        return (effectValue + (double)effectPerLevel * Math.Max(0, level - 1)) * CatProgression.StarMultiplier(ascension);
    }

    public string DescriptionAtLevel(int level, int ascension = 0)
    {
        if (effectId != "heal" && effectId != "bonus_damage") return description;
        string timing;
        switch (trigger)
        {
            case EffectTrigger.OnHit: timing = "기본 공격 명중 시"; break;
            case EffectTrigger.OnDamaged: timing = "살아남은 피격 후"; break;
            case EffectTrigger.OnKill: timing = "적 처치 시"; break;
            default: timing = "공격 시"; break;
        }
        string probability = chance < 1 ? $" {Mathf.Clamp01(chance):0.##%} 확률로" : "";
        string value = EffectAtLevel(level, ascension).ToString("0.##");
        string effect = effectId == "heal" ? $"HP를 {value} 회복합니다." :
            trigger == EffectTrigger.OnAttack ? $"해당 공격의 각 대상에게 {value}의 추가 피해를 줍니다." :
            $"{value}의 추가 피해를 줍니다.";
        string result = $"{timing}{probability} {effect}";
        if (cooldownSeconds > 0) result += $" 재사용 대기시간은 {cooldownSeconds:0.##}초입니다.";
        if (effectId == "heal" && trigger == EffectTrigger.OnDamaged)
            result += " 치명적인 피해는 막지 못합니다.";
        return result;
    }
}

// 공통 설정은 유저 세이브에 복제하지 않습니다.
[CreateAssetMenu(menuName = "Cat Raising/Game Catalog")]
public class CatGameCatalog : ScriptableObject
{
    public CharacterDrawRules characterDraw = new CharacterDrawRules();
    public List<CharacterDefinition> CharacterDrawPool()
    {
        var pool = new List<CharacterDefinition>();
        foreach (var character in AllCharacters())
            if (character.inCharacterDraw)
                pool.Add(character);
        return pool;
    }
    public RelicDrawRules relicDraw = new RelicDrawRules();
    public List<RelicDefinition> RelicDrawPool()
    {
        var pool = new List<RelicDefinition>();
        foreach (var relic in AllRelics())
            if (relic.inRelicDraw)
                pool.Add(relic);
        return pool;
    }
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
    public IEnumerable<CharacterDefinition> AllCharacters()
    {
        var ids = new HashSet<string>();
        foreach (var asset in characterAssets)
            if (asset != null && asset.definition != null && !string.IsNullOrWhiteSpace(asset.definition.id) && ids.Add(asset.definition.id))
                yield return asset.definition;
        foreach (var definition in characters)
            if (definition != null && !string.IsNullOrWhiteSpace(definition.id) && ids.Add(definition.id))
                yield return definition;
    }

    public IEnumerable<RelicDefinition> AllRelics()
    {
        var ids = new HashSet<string>();
        foreach (var asset in relicAssets)
            if (asset != null && asset.definition != null && !string.IsNullOrWhiteSpace(asset.definition.id) && ids.Add(asset.definition.id))
                yield return asset.definition;
        foreach (var definition in relics)
            if (definition != null && !string.IsNullOrWhiteSpace(definition.id) && ids.Add(definition.id))
                yield return definition;
    }

    public CharacterDefinition FindCharacter(string id)
    {
        var asset = characterAssets.Find(x => x != null && x.definition != null && x.definition.id == id);
        return asset != null ? asset.definition : characters.Find(x => x != null && x.id == id);
    }

    public RelicDefinition FindRelic(string id)
    {
        foreach (var asset in relicAssets)
            if (asset != null && asset.definition != null && asset.definition.id == id) return asset.definition;
        foreach (var definition in relics)
            if (definition != null && definition.id == id) return definition;
        return null;
    }
    public const string StarterCharacterId = "cat_starter";
    public List<CharacterDefinition> characters = new List<CharacterDefinition>
    {
        new CharacterDefinition
        {
            id = StarterCharacterId, displayName = "기본 고양이",
            rarity = ItemRarity.Common, attackId = "claw_melee",
            attackRange = 1.2f,
            baseStats = new StatValues { attack = 1, attackSpeed = 1, maxHealth = 100 },
            statsPerLevel = new StatValues { attack = 1, maxHealth = 5 }
        }
    };
    public List<EquipmentDefinition> equipment = new List<EquipmentDefinition>();
    public List<RelicDefinition> relics = new List<RelicDefinition>();
}
