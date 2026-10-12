using System;

// 준비 화면과 실제 스폰이 같은 구성을 사용합니다.
public static class DungeonStageRules
{
    public const int WaveCount = 3;
    public static ItemRarity MonsterRarity(int kind) => ItemRarity.Common;
    public static readonly string[] MonsterNames = { "먼지 뭉치", "도둑 생쥐", "털실 슬라임", "깡통 게" };
    public static readonly string[] MonsterArt = { "DustBunnyMonster", "CheeseMouseMonster", "YarnSlimeMonster", "TinCanCrabMonster" };
    public static int KindCount(int stage) => Math.Min(4, 2 + (stage - 1) / 5);
    public static int EnemyCount(int stage, int wave) => 3 + wave + (stage - 1) / 5;
    public static int Kind(int stage, int wave, int slot) => (wave - 1 + slot) % KindCount(stage);
    public static bool IsBoss(int stage, int wave, int slot) => stage % 5 == 0 && wave == WaveCount && slot == 0;
    public static double Health(int stage, int wave, int slot) =>
        (6 + stage * 2 + wave * 2) * (IsBoss(stage, wave, slot) ? 6 : 1);
}
