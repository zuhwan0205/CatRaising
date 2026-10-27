using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 오브젝트를 보관하지 않고, 유저 데이터와 저장 작업만 유지합니다.
public sealed class CatGameRuntime : MonoBehaviour
{
    public const string MainScene = "GameScene";
    public const string DungeonScene = "DungeonScene";
    public const string RaidScene = "RaidScene";
    public static CatGameRuntime Current { get; private set; }
    public BackendUserData Data { get; private set; }
    public CatGameCatalog Catalog { get; private set; }
    public CatGameSession Session { get; private set; }
    public bool IsPreview { get; private set; }
    public bool IsTransitioning { get; private set; }
    public EncounterRequest Encounter { get; private set; }
    private bool ownsCatalog;

    public sealed class EncounterRequest
    {
        public bool IsDungeon { get; }
        public int Index { get; }
        public bool ResultHandled { get; internal set; }
        private readonly string[] characterIds;

        internal EncounterRequest(bool dungeon, int index, IList<OwnedCharacter> team)
        {
            IsDungeon = dungeon;
            Index = index;
            characterIds = new string[team.Count];
            for (int i = 0; i < team.Count; i++) characterIds[i] = team[i].characterId;
        }

        public List<OwnedCharacter> ResolveTeam(BackendUserData data)
        {
            var result = new List<OwnedCharacter>(characterIds.Length);
            foreach (string id in characterIds)
                result.Add(data.characters.Find(cat => cat.characterId == id));
            return result;
        }
    }

    private void Awake()
    {
        if (Current != null && Current != this) { Destroy(gameObject); return; }
        Current = this;
        DontDestroyOnLoad(gameObject);
    }

    public static CatGameRuntime Initialize(BackendUserData data, CatGameCatalog catalog, bool preview)
    {
        if (Current == null) new GameObject("Cat Game Runtime").AddComponent<CatGameRuntime>();
        var runtime = Current;
        if (ReferenceEquals(runtime.Data, data) && runtime.Session != null) return runtime;
        if (runtime.Session != null && (runtime.Session.IsSaving || runtime.IsTransitioning))
            throw new InvalidOperationException("저장 또는 씬 전환을 마친 후 계정을 변경해주세요.");
        if (runtime.ownsCatalog && runtime.Catalog != null) Destroy(runtime.Catalog);
        runtime.Data = data ?? throw new ArgumentNullException(nameof(data));
        runtime.IsPreview = preview;
        runtime.Catalog = catalog != null ? catalog : Resources.Load<CatGameCatalog>("DefaultCatCatalog");
        runtime.ownsCatalog = runtime.Catalog == null;
        if (runtime.ownsCatalog) runtime.Catalog = ScriptableObject.CreateInstance<CatGameCatalog>();
        runtime.Session?.Events.Dispose();
        runtime.Session = new CatGameSession(data, preview, () => BackendGameData.Instance.GameDataUpdateAsync(),
            runtime.Catalog, preview ? null : BackendGameData.Instance.RewardJournal);
        runtime.Session.NicknameUpdateRequested = BackendLogin.Instance.UpdateNicknameAsync;
        runtime.Encounter = null;
        return runtime;
    }

    public static void ValidateDungeon(int stage, IList<OwnedCharacter> team, BackendUserData data, CatGameCatalog catalog)
    {
        if (stage < 1 || stage > 15 || stage > data.progress.dungeonClearedStage + 1 || team == null || team.Count != 3)
            throw new ArgumentException("열린 던전과 서로 다른 고양이 3종을 선택해주세요.");
        var ids = new HashSet<string>();
        foreach (var cat in team)
            if (cat == null || !data.characters.Contains(cat) || !ids.Add(cat.characterId) || catalog.FindCharacter(cat.characterId) == null)
                throw new ArgumentException("보유 고양이 3종을 선택해주세요.");
    }

    public async Task EnterEncounterAsync(bool dungeon, int index, IList<OwnedCharacter> team)
    {
        if (IsTransitioning || Encounter != null || Session.IsSaving || Session.HasPendingReward)
            throw new InvalidOperationException("진행 저장을 먼저 완료해주세요. 상점에서 저장을 재시도할 수 있습니다.");
        if (dungeon) ValidateDungeon(index, team, Data, Catalog);
        else RaidBattle.ValidateTeam(index, Data.progress.accountLevel, team, Data, Catalog);
        string scene = dungeon ? DungeonScene : RaidScene;
        EnsureSceneAvailable(scene);
        var request = new EncounterRequest(dungeon, index, team);
        IsTransitioning = true;
        try
        {
            Session.CheckpointRewards();
            if (!await Session.SaveAsync()) throw new InvalidOperationException("입장 전 저장에 실패했습니다. 다시 시도해주세요.");
            Encounter = request;
            await LoadSceneAsync(scene);
        }
        catch { Encounter = null; throw; }
        finally { IsTransitioning = false; }
    }

    public async Task<bool> FinishEncounterAsync(bool won)
    {
        var request = Encounter;
        if (request == null || request.ResultHandled) return false;
        // 저장 응답을 기다리는 동안 들어오는 종료 콜백도 중복 지급하지 않습니다.
        request.ResultHandled = true;
        return won && await Session.CompleteEncounterAsync(request.IsDungeon ? request.Index : 0);
    }

    public async Task ReturnToMainAsync()
    {
        if (IsTransitioning || Session.IsSaving) return;
        EnsureSceneAvailable(MainScene);
        IsTransitioning = true;
        try
        {
            await LoadSceneAsync(MainScene);
            Encounter = null;
        }
        finally { IsTransitioning = false; }
    }

    private static void EnsureSceneAvailable(string scene)
    {
        if (!Application.CanStreamedLevelBeLoaded(scene))
            throw new InvalidOperationException(scene + "을 Build Profiles의 활성 씬 목록에 추가해주세요.");
    }

    public async Task LogoutAsync()
    {
        if (IsTransitioning || Encounter != null || Session.IsSaving)
            throw new InvalidOperationException("진행 중인 저장 또는 전투를 마친 뒤 다시 시도해주세요.");
        EnsureSceneAvailable("LoadingScene");
        IsTransitioning = true;
        try
        {
            Session.CheckpointRewards();
            if (!await Session.SaveAsync()) throw new InvalidOperationException("저장에 실패해 로그아웃하지 않았습니다. 다시 시도해주세요.");
            if (!IsPreview && !await BackendLogin.Instance.LogoutAsync())
                throw new InvalidOperationException("로그아웃에 실패했습니다. 네트워크 연결을 확인해주세요.");
            BackendLogin.ManualLoginRequired = true;
            BackendGameData.Instance.ClearLoggedOutSession();
            await LoadSceneAsync("LoadingScene");
            Session.Events.Dispose();
            Current = null;
            if (ownsCatalog && Catalog != null) Destroy(Catalog);
            Destroy(gameObject);
        }
        finally { IsTransitioning = false; }
    }

    private static Task LoadSceneAsync(string scene)
    {
        var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        if (operation == null) throw new InvalidOperationException("씬을 열 수 없습니다: " + scene);
        var completion = new TaskCompletionSource<bool>();
        operation.completed += _ => completion.TrySetResult(true);
        return completion.Task;
    }

    private void OnDestroy()
    {
        if (Current != this) return;
        Session?.Events.Dispose();
        Current = null;
        if (ownsCatalog && Catalog != null) Destroy(Catalog);
    }
}
