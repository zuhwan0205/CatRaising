using System;
using System.Collections.Generic;

[Serializable]
public class BackendUserData
{
    public const int CurrentVersion = 1;
    public int schemaVersion = CurrentVersion;
    public PlayerProgress progress = new PlayerProgress();
    public PlayerWallet wallet = new PlayerWallet();
    public List<OwnedCharacter> characters = new List<OwnedCharacter>();
    public List<OwnedEquipment> equipment = new List<OwnedEquipment>();
    public List<OwnedRelic> relics = new List<OwnedRelic>();
    public PlayerLoadout loadout = new PlayerLoadout();
    public IdleRewardState idleReward = new IdleRewardState();
    // 기존 장비명은 ID 대응이 미확정이므로 원본으로 보관합니다.
    public string legacySnapshotJson = "";

    public static BackendUserData CreateNew()
    {
        var data = new BackendUserData();
        data.characters.Add(new OwnedCharacter { characterId = CatGameCatalog.StarterCharacterId });
        data.loadout.characterId = CatGameCatalog.StarterCharacterId;
        return data;
    }

    public override string ToString()
    {
        return $"세이브 v{schemaVersion}, 계정 레벨 {progress.accountLevel}, 캐릭터 {characters.Count}, 장비 {equipment.Count}, 유물 {relics.Count}";
    }
}

[Serializable]
public class PlayerProgress
{
    public int accountLevel = 1;
    public long accountExperience;
    public int currentStage = 1;
    public int highestClearedStage;
    public int permanentAttackLevel;
    public int permanentDefenseLevel;
    public int permanentAttackSpeedLevel;
}

[Serializable]
public class ItemAmount { public string itemId; public long amount; }

[Serializable]
public class PlayerWallet
{
    public long gold;
    public long gems;
    public List<ItemAmount> materials = new List<ItemAmount>();
}

// 캐릭터와 유물은 종류당 하나를 육성하고 중복은 조각으로 관리하는 초기 모델입니다.
[Serializable]
public class OwnedCharacter
{
    public string characterId;
    public int level = 1;
    public long experience;
    public int ascension;
    public long fragments;
}

[Serializable]
public class OwnedEquipment
{
    public string instanceId;
    public string equipmentId;
    public int enhancement;
    public bool isLocked;
    public StatValues randomBonuses = new StatValues();

    public static OwnedEquipment Create(string definitionId)
    {
        return new OwnedEquipment { instanceId = Guid.NewGuid().ToString("N"), equipmentId = definitionId };
    }
}

[Serializable]
public class OwnedRelic
{
    public string relicId;
    public int level = 1;
    public long fragments;
}

[Serializable]
public class PlayerLoadout
{
    public string characterId;
    public string weaponInstanceId = "";
    public string armorInstanceId = "";
    public string accessoryInstanceId = "";
    public List<string> relicIds = new List<string>();
}

[Serializable]
public class IdleRewardState
{
    // 0은 서버 기준 시각 미설정. 클라이언트 시각으로 보상을 지급하지 않습니다.
    public long lastSettledUnixSeconds;
    public int rewardStage;
}
