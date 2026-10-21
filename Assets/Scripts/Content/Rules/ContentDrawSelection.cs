using System;
using System.Collections.Generic;

// 설정 필드는 기존 직렬화 클래스에 두고 추첨 알고리즘만 공유합니다.
public static class ContentDrawSelection
{
    public static int Percent(ItemRarity rarity, int common, int epic, int unique, int legendary)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return common;
            case ItemRarity.Epic: return epic;
            case ItemRarity.Unique: return unique;
            case ItemRarity.Legendary: return legendary;
            default: return 0;
        }
    }

    public static bool Validate<T>(List<T> pool, Func<T, ItemRarity> rarity,
        long cost, long fragments, int common, int epic, int unique, int legendary)
    {
        if (cost <= 0 || fragments <= 0 || legendary <= 0 ||
            common <= epic || epic <= unique || unique <= legendary ||
            (long)common + epic + unique + legendary != 100)
            return false;
        for (int tier = 0; tier < 4; tier++)
            if (!pool.Exists(item => rarity(item) == (ItemRarity)tier))
                return false;
        return true;
    }

    // 먼저 등급을 추첨하고 해당 등급 안에서 균등 추첨합니다.
    // 후보 목록을 할당하지 않고 원래 순서로 두 번 순회합니다.
    public static T Pick<T>(List<T> pool, Func<T, ItemRarity> rarity, Func<ItemRarity, int> percent,
        bool valid, double gradeRoll, double itemRoll) where T : class
    {
        if (!valid || gradeRoll < 0 || gradeRoll >= 1 || itemRoll < 0 || itemRoll >= 1 ||
            double.IsNaN(gradeRoll) || double.IsNaN(itemRoll))
            return null;
        double threshold = 0;
        for (int tier = 0; tier < 4; tier++)
        {
            threshold += percent((ItemRarity)tier);
            if (gradeRoll * 100 >= threshold && tier < 3) continue;
            int count = 0;
            foreach (var item in pool)
                if (rarity(item) == (ItemRarity)tier) count++;
            int index = (int)(itemRoll * count);
            foreach (var item in pool)
                if (rarity(item) == (ItemRarity)tier && index-- == 0) return item;
        }
        return null;
    }
}
