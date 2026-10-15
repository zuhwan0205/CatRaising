using System;

public static class CatSkillTree
{
    public const int Count = 6, MaxRank = 20;
    public static readonly string[] Names = { "날카로운 발톱", "민첩한 사냥꾼", "튼튼한 몸", "단단한 털", "집중력", "사냥의 달인" };
    public static readonly string[] Effects = { "공격력 +2%", "공격 속도 +1%", "최대 HP +3%", "방어력 +0.5", "공격 범위 +1%", "공격력 +3%" };
    public static int Rank(OwnedCharacter cat, int node) => cat?.skillRanks != null && node >= 0 && node < cat.skillRanks.Length ? Math.Max(0, Math.Min(MaxRank, cat.skillRanks[node])) : 0;
    public static int Available(OwnedCharacter cat)
    {
        if (cat == null) return 0;
        int used = 0;
        for (int i = 0; i < Count; i++) used += Rank(cat, i);
        return Math.Max(0, cat.level - used);
    }
    public static bool CanLearn(OwnedCharacter cat, int node) => cat != null && node >= 0 && node < Count && Rank(cat, node) < MaxRank && Available(cat) > 0 && (node % 2 == 0 || Rank(cat, node - 1) >= 5);
    public static double Attack(OwnedCharacter cat) => 1 + Rank(cat, 0) * .02 + Rank(cat, 5) * .03;
    public static double Health(OwnedCharacter cat) => 1 + Rank(cat, 2) * .03;
    public static double Defense(OwnedCharacter cat) => Rank(cat, 3) * .5;
    public static double Speed(OwnedCharacter cat) => 1 + Rank(cat, 1) * .01;
    public static float Range(OwnedCharacter cat) => 1 + Rank(cat, 4) * .01f;
    public static int Signature(OwnedCharacter cat)
    {
        int signature = 0;
        for (int i = 0; i < Count; i++) signature = signature * 21 + Rank(cat, i);
        return signature;
    }
}
