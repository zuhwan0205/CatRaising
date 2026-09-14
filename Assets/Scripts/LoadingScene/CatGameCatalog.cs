using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity { Normal, Epic, Unique, Legendary }
public enum EquipmentSlot { Weapon, Armor, Accessory }
public enum EffectTrigger { OnAttack, OnHit, OnDamaged }

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
