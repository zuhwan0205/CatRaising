using System;

public static class CatSkillTree
{
    public const int Count = 17, MaxRank = 5;
    public const long ResetCost = 10;
    public static bool HasLearnedSkills(OwnedCharacter cat)
    {
        for (int i = 0; i < Count; i++)
            if (Rank(cat, i) > 0) return true;
        return false;
    }
    public const int Vitality = 0, Daggers = 1, Vampire = 2, Poison = 3, Greatsword = 4,
        Lightning = 5, Mace = 6, Haste = 7, SteelArrow = 8, Explosion = 9,
        LargeArrow = 10, DoubleArrow = 11, Burn = 12, ArrowStacks = 13,
        Magnet = 14, Boots = 15, Potion = 16;
    public static readonly string[] Names = {
        "생명력", "쌍단검", "흡혈", "독 방울", "대검", "번개", "둔기",
        "신속", "강철 화살", "폭발", "거대 화살", "쌍화살", "화상", "집중 사격",
        "자석", "신발", "회복 물약"
    };
    public static readonly string[] Effects = {
        "단계당 최대 HP +10%", "단계당 공격력 -5% · 공격 속도 +20%",
        "단계당 실제 피해량의 2% 회복",
        "명중 시 독 +1 · 단계당 중첩 한도 +1\n중첩당 최대 HP 0.5%/초 · 5초 유지\n레이드 보스에게는 효과 40%",
        "단계당 공격 속도 -5% · 공격력 +30%",
        "명중 시 주변 반경 2에 번개\n단계당 공격력의 20% 피해",
        "기본 공격이 대상 주변 범위 공격으로 변경\n단계당 범위 반경 0.4 증가",
        "단계당 공격 속도 +5%",
        "관통 화살 발사 · 사거리 끝에서 소멸\n피해량 68/76/84/92/100%",
        "사거리 끝에서 폭발 · 단계당 피해 20%\n반경 1.2/1.4/1.6/1.8/2",
        "화살 크기·충돌 폭 단계당 +10%\n최대 1.5배",
        "한 번에 화살 2개 발사\n두 번째 화살 피해 52/64/76/88/100%",
        "명중 시 화상 +1 · 단계당 중첩 한도 +1\n중첩당 최대 HP 0.5%/초 · 5초 유지\n레이드 보스에게는 효과 40%",
        "같은 대상에게 공격할 때 중첩 +1\n중첩당 공속 +10% · 단계당 한도 +1",
        "필드의 모든 경험치 결정을 자동 수집\n수집 간격 5/4/3/2/1초 (골드는 기존 자동 획득)",
        "단계당 이동 속도 +5%",
        "HP 50% 미만일 때 자동 사용 · 재사용 60초\n단계당 최대 HP의 10% 회복"
    };
    public static readonly int[] Parents = { -1, 0, 1, 1, 0, 4, 4, -1, 7, 8, 8, 7, 11, 11, -1, -1, -1 };
    public static readonly int[] Siblings = { -1, 4, 3, 2, 1, 6, 5, -1, 11, 10, 9, 8, 13, 12, -1, -1, -1 };
    public static bool IsRanged(OwnedCharacter cat) => cat != null &&
        (cat.characterId == "cat_ninja" || cat.characterId == "cat_moon" ||
         cat.characterId == "cat_lion" || cat.characterId == "cat_shadow");
    public static bool IsAllowed(OwnedCharacter cat, int node) =>
        cat != null && node >= 0 && node < Count &&
        (node >= Magnet || (IsRanged(cat) ? node >= Haste : node < Haste));
    // 구형 6칸 트리는 효과를 재해석하지 않고 사용 포인트를 환급합니다.
    public static int Rank(OwnedCharacter cat, int node) =>
        IsAllowed(cat, node) && cat.skillRanks != null && cat.skillRanks.Length == Count
            ? Math.Max(0, Math.Min(MaxRank, cat.skillRanks[node])) : 0;
    public static int Available(OwnedCharacter cat)
    {
        if (cat == null) return 0;
        int used = 0;
        for (int i = 0; i < Count; i++) used += Rank(cat, i);
        return Math.Max(0, cat.level - used);
    }
    public static bool BranchLocked(OwnedCharacter cat, int node)
    {
        for (int current = node; current >= 0 && current < Count; current = Parents[current])
            if (Siblings[current] >= 0 && Rank(cat, Siblings[current]) > 0) return true;
        return false;
    }
    public static bool CanLearn(OwnedCharacter cat, int node) =>
        IsAllowed(cat, node) && !BranchLocked(cat, node) && Rank(cat, node) < MaxRank &&
        Available(cat) > 0 && (Parents[node] < 0 || Rank(cat, Parents[node]) == MaxRank);
    public static double Attack(OwnedCharacter cat) => (1 - Rank(cat, Daggers) * .05) * (1 + Rank(cat, Greatsword) * .30);
    public static double Health(OwnedCharacter cat) => 1 + Rank(cat, Vitality) * .10;
    public static double Defense(OwnedCharacter cat) => 0;
    public static double Speed(OwnedCharacter cat) =>
        (1 + Rank(cat, Daggers) * .20 + Rank(cat, Haste) * .05) * (1 - Rank(cat, Greatsword) * .05);
    public static float Range(OwnedCharacter cat) => 1;
    public static float Movement(OwnedCharacter cat) => 1 + Rank(cat, Boots) * .05f;
    public static int Signature(OwnedCharacter cat)
    {
        unchecked
        {
            int signature = 17;
            for (int i = 0; i < Count; i++) signature = signature * 31 + Rank(cat, i);
            return signature;
        }
    }
}
