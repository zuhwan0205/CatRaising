using System;
using System.Threading.Tasks;

// UI/SDK에 독립적인 비동기 저장 기능입니다. 전송 중 씬이 닫혀도 완료 결과를 처리합니다.
public class CatGameSession
{
    private readonly BackendUserData data;
    private readonly bool preview;
    private readonly Func<Task<bool>> saveData;
    public bool IsSaving { get; private set; }
    public event Action<bool> BusyChanged;
    public event Action<string> StatusChanged;

    public CatGameSession(BackendUserData data, bool preview, Func<Task<bool>> saveData)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.preview = preview;
        this.saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
    }

    public async Task SelectCharacterAsync(OwnedCharacter character)
    {
        if (IsSaving || character == null || !data.characters.Contains(character)) return;
        string previous = data.loadout.characterId;
        data.loadout.characterId = character.characterId;
        await SaveAsync(() => data.loadout.characterId = previous);
    }

    public async Task SaveAsync(Action rollback = null)
    {
        if (IsSaving) return;
        if (preview)
        {
            StatusChanged?.Invoke("미리보기 변경입니다. 서버에는 저장하지 않습니다.");
            return;
        }
        IsSaving = true;
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
            IsSaving = false;
            BusyChanged?.Invoke(false);
        }
        StatusChanged?.Invoke(success ? "저장했습니다." : "저장하지 못했습니다. 반복되면 다시 로그인해주세요.");
    }
}
