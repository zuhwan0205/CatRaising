using System;

[Serializable]
public sealed class CharacterGrowthRules
{
    public int maxLevel = 100;
    public long baseFragments = 1;
    public long fragmentsPerLevel = 1;

    public bool TryGetCost(int level,out long cost)
    {
        cost=0;
        if(level<1 || level>=maxLevel || baseFragments<=0 || fragmentsPerLevel<0)return false;
        try {cost=checked(baseFragments+(level-1L)*fragmentsPerLevel);}
        catch(OverflowException){return false;}
        return true;
    }
}
