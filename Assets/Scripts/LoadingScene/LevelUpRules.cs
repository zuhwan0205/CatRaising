using System;

[Serializable]
public sealed class LevelUpRules
{
    public int maxLevel = 100;
    public long baseGoldCost = 20;
    public long goldCostPerLevel = 10;

    public bool TryGetCost(int level, out long cost)
    {
        cost = 0;
        if (level < 1 || level >= maxLevel || baseGoldCost <= 0 || goldCostPerLevel < 0) return false;
        try { cost = checked(baseGoldCost + goldCostPerLevel * (level - 1L)); }
        catch (OverflowException) { return false; }
        return true;
    }
}
