using System;
using System.Threading.Tasks;

// UI/SDK에 독립적인 비동기 저장 기능입니다. 전송 중 씬이 닫혀도 완료 결과를 처리합니다.
public class CatGameSession
{
    private readonly BackendUserData data;
    private readonly bool preview;
    private readonly Func<Task<bool>> saveData;
    public bool IsSaving { get; private set; }
    public bool HasPendingReward { get; private set; }
    private bool rewardLimitReached;
    public event Action<bool> GameplayBlockedChanged;
    public event Action<bool> BusyChanged;
    public event Action<string> StatusChanged;

    public async Task AwardGoldAsync(long amount)
    {
        if (IsSaving || HasPendingReward || rewardLimitReached || amount < 0) return;
        if (data.wallet.gold > long.MaxValue - amount)
        {
            rewardLimitReached = true;
            GameplayBlockedChanged?.Invoke(true);
            StatusChanged?.Invoke("골드 한도에 도달해 전투를 중단했습니다.");
            return;
        }
        data.wallet.gold += amount;
        HasPendingReward = true;
        GameplayBlockedChanged?.Invoke(true);
        await SaveAsync();
    }

    public CatGameSession(BackendUserData data, bool preview, Func<Task<bool>> saveData)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.preview = preview;
        this.saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
    }

    public async Task SelectCharacterAsync(OwnedCharacter character)
    {
        if (IsSaving || HasPendingReward || character == null || !data.characters.Contains(character)) return;
        string previous = data.loadout.characterId;
        data.loadout.characterId = character.characterId;
        await SaveAsync(() => data.loadout.characterId = previous);
    }

    public async Task ToggleRelicAsync(OwnedRelic relic)
    {
        if (IsSaving || HasPendingReward || relic == null || !data.relics.Contains(relic)) return;
        var previous = new System.Collections.Generic.List<string>(data.loadout.relicIds);
        if (data.loadout.relicIds.Contains(relic.relicId))
            data.loadout.relicIds.RemoveAll(id => id == relic.relicId);
        else
        {
            if (data.loadout.relicIds.Count >= 3)
            {
                StatusChanged?.Invoke("유물은 최대 3개까지 장착할 수 있습니다.");
                return;
            }
            data.loadout.relicIds.Add(relic.relicId);
        }
        await SaveAsync(() => data.loadout.relicIds = previous);
    }

    public async Task SaveAsync(Action rollback = null)
    {
        if (IsSaving) return;
        if (preview)
        {
            HasPendingReward = false;
            GameplayBlockedChanged?.Invoke(rewardLimitReached);
            StatusChanged?.Invoke("미리보기 변경입니다. 서버에는 저장하지 않습니다.");
            return;
        }
        IsSaving = true;
        GameplayBlockedChanged?.Invoke(true);
        bool success = false;
        try
        {
            BusyChanged?.Invoke(true);
            StatusChanged?.Invoke("저장 중…");
            success = await saveData();
        }
        catch (Exception) { success = false; }
        finally
        {
            if (!success) rollback?.Invoke();
            if (success) HasPendingReward = false;
            IsSaving = false;
            GameplayBlockedChanged?.Invoke(HasPendingReward || rewardLimitReached);
            BusyChanged?.Invoke(false);
        }
        StatusChanged?.Invoke(success ? "저장했습니다." : "저장 실패 · 전투가 대기 중이면 저장 버튼으로 재시도해주세요. 반복되면 다시 로그인해주세요.");
    }
}
