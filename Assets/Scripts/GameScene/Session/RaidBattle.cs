using System;
using System.Collections.Generic;

// 레이드 입장 조건만 담당합니다. 실제 전투는 CatFieldController에서 진행합니다.
public static class RaidBattle
{
    public static readonly int[] RequiredLevels = { 5, 10, 20 };
    public static readonly string[] Names = { "거대 먼지 뭉치", "도둑 생쥐 대장", "거대 털실 슬라임" };
    public static readonly string[] ArtworkNames = { "RaidDustBoss", "RaidMouseBoss", "RaidYarnBoss" };
    public static readonly ItemRarity[] Rarities = { ItemRarity.Epic, ItemRarity.Unique, ItemRarity.Legendary };
    // 테스트 종료 후 false로 바꾸면 원래 레벨 제한을 복구합니다.
    public static bool IgnoreLevelRequirementForTesting { get; set; } = true;

    public static bool CanEnter(int index, int accountLevel)
    {
        return index >= 0 && index < RequiredLevels.Length &&
            (IgnoreLevelRequirementForTesting || accountLevel >= RequiredLevels[index]);
    }

    public static void ValidateTeam(int index, int accountLevel, IList<OwnedCharacter> team, BackendUserData data, CatGameCatalog catalog)
    {
        if (!CanEnter(index, accountLevel) || team == null || team.Count != 4)
            throw new ArgumentException("레이드 입장 조건을 확인해주세요.");
        var ids = new HashSet<string>();
        foreach (var owned in team)
        {
            if (owned == null || !data.characters.Contains(owned) || !ids.Add(owned.characterId))
                throw new ArgumentException("서로 다른 보유 고양이 4마리가 필요합니다.");
            if (catalog.FindCharacter(owned.characterId) == null)
                throw new ArgumentException("캐릭터 설정이 없습니다.");
        }
    }
}
