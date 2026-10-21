using System;
using System.Collections.Generic;

[Serializable]
public sealed class CharacterDrawRules
{
    public long goldCost = 200;
    public long duplicateFragments = 1;
    public int commonPercent = 70;
    public int epicPercent = 23;
    public int uniquePercent = 6;
    public int legendaryPercent = 1;

    public int Percent(ItemRarity rarity) =>
        ContentDrawSelection.Percent(rarity, commonPercent, epicPercent, uniquePercent, legendaryPercent);

    public bool Validate(List<CharacterDefinition> pool) =>
        ContentDrawSelection.Validate(pool, item => item.rarity, goldCost, duplicateFragments,
            commonPercent, epicPercent, uniquePercent, legendaryPercent);

    public CharacterDefinition Pick(List<CharacterDefinition> pool, double gradeRoll, double itemRoll) =>
        ContentDrawSelection.Pick(pool, item => item.rarity, Percent, Validate(pool), gradeRoll, itemRoll);
}

