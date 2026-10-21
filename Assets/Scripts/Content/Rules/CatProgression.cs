using System;

public static class CatProgression
{
    public const int MaxLevel = 100;
    private static readonly long[] StarCosts = { 10, 30, 80, 200, 500 };
    private static readonly int[] MonsterExperience = { 5, 8, 15, 20 };
    public static bool TryStarCost(int stars, out long cost)
    {
        cost = stars >= 0 && stars < StarCosts.Length ? StarCosts[stars] : 0;
        return cost > 0;
    }
    public static long RequiredExperience(int level) => 20L + 10L * (Math.Max(1, level) - 1);
    public static double StarMultiplier(int stars) => 1 + Math.Max(0, Math.Min(5, stars)) * .2;
    public static int DropExperience(int kind) => MonsterExperience[Math.Max(0, Math.Min(3, kind))];
    public static void AddExperience(OwnedCharacter cat, long amount)
    {
        if (amount <= 0 || cat.level >= MaxLevel) return;
        cat.experience = Math.Min(long.MaxValue - amount, Math.Max(0, cat.experience)) + amount;
        while (cat.level < MaxLevel && cat.experience >= RequiredExperience(cat.level))
        {
            cat.experience -= RequiredExperience(cat.level);
            cat.level++;
        }
        if (cat.level >= MaxLevel) cat.experience = 0;
    }
}
