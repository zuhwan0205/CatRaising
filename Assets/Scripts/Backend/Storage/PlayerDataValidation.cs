using System;

public static class PlayerDataValidation
{
    public static void Validate(BackendUserData data)
    {
        if (data == null || data.schemaVersion != BackendUserData.CurrentVersion ||
            data.progress == null || data.wallet == null || data.characters == null ||
            data.equipment == null || data.relics == null || data.loadout == null ||
            data.idleReward == null || data.wallet.materials == null || data.loadout.relicIds == null)
            throw new InvalidOperationException("지원하지 않거나 손상된 유저 데이터입니다.");
        if (data.progress.accountLevel < 1 || data.progress.currentStage < 1)
            throw new InvalidOperationException("유효하지 않은 성장 데이터입니다.");
    }
}
