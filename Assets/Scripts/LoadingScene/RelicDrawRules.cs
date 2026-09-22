using System;
using System.Collections.Generic;

[Serializable]
public sealed class RelicDrawRules
{
    public long goldCost = 100;
    public long duplicateFragments = 1;
    public int commonPercent = 70;
    public int epicPercent = 23;
    public int uniquePercent = 6;
    public int legendaryPercent = 1;

    public int Percent(ItemRarity rarity)
    {
        switch(rarity)
        {
            case ItemRarity.Common: return commonPercent;
            case ItemRarity.Epic: return epicPercent;
            case ItemRarity.Unique: return uniquePercent;
            case ItemRarity.Legendary: return legendaryPercent;
            default: return 0;
        }
    }

    public bool Validate(List<RelicDefinition> pool)
    {
        if(goldCost<=0 || duplicateFragments<=0 || legendaryPercent<=0 ||
            commonPercent<=epicPercent || epicPercent<=uniquePercent || uniquePercent<=legendaryPercent ||
            (long)commonPercent+epicPercent+uniquePercent+legendaryPercent!=100) return false;
        for(int tier=0;tier<4;tier++)
            if(!pool.Exists(x=>x.rarity==(ItemRarity)tier))return false;
        return true;
    }

    // 먼저 등급을 고르고 해당 등급 안에서는 종류별로 균등하게 추첨합니다.
    public RelicDefinition Pick(List<RelicDefinition> pool,double gradeRoll,double itemRoll)
    {
        if(!Validate(pool) || gradeRoll<0 || gradeRoll>=1 || itemRoll<0 || itemRoll>=1 ||
            double.IsNaN(gradeRoll) || double.IsNaN(itemRoll))return null;
        double threshold=0;
        for(int tier=0;tier<4;tier++)
        {
            threshold+=Percent((ItemRarity)tier);
            if(gradeRoll*100>=threshold && tier<3)continue;
            var candidates=pool.FindAll(x=>x.rarity==(ItemRarity)tier);
            return candidates[(int)(itemRoll*candidates.Count)];
        }
        return null;
    }
}

public sealed class ContentDrawResult
{
    public bool character;
    public string id;
    public string name;
    public ItemRarity rarity;
    public bool duplicate;
    public long fragments;
}
