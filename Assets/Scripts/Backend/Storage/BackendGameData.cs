using System;
using System.Threading.Tasks;
using System.Threading;
using BackEnd;
using UnityEngine;
#if BACKND_SDK_INSTALLED
using System.Collections.Generic;
using System.Text;
using BACKND.Database;
using Newtonsoft.Json;
#endif

// 신규 게임 데이터 읽기/쓰기는 BACKND.Database로만 처리합니다.
public class BackendGameData
{
    private static BackendGameData instance;
    public static BackendGameData Instance => instance ?? (instance = new BackendGameData());
    public static BackendUserData userData;
    public string LastError
    {
        get; private set;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        var previous = instance;
        instance = null;
        userData = null;
#if BACKND_SDK_INSTALLED
        previous?.client?.Dispose();
#endif
    }

    // A timeout only abandons waiting; it does not prove a server write was cancelled.
    private async Task<T> WaitForRequest<T>(Task<T> request, string stage, CancellationToken cancellation = default)
    {
        using var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        var deadline = Task.Delay(45000, timer.Token);
        if (await Task.WhenAny(request, deadline) != request)
        {
            _ = request.ContinueWith(t => { var observed = t.Exception; },
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            cancellation.ThrowIfCancellationRequested();
            throw new TimeoutException(stage + " 응답이 45초 이상 지연되었습니다. 잠시 후 다시 로그인해주세요.");
        }
        timer.Cancel();
        cancellation.ThrowIfCancellationRequested();
        if (instance != this || !Application.isPlaying)
            throw new OperationCanceledException("이전 실행의 데이터 요청을 무시합니다.");
        return await request;
    }

    private bool busy;
    private bool requiresReload;
    private ItemAuditSnapshot auditSnapshot;
    public CombatRewardJournal RewardJournal { get; private set; }

    public void ClearLoggedOutSession()
    {
        if (busy) throw new InvalidOperationException("저장 작업이 아직 진행 중입니다.");
#if BACKND_SDK_INSTALLED
        client?.Dispose();
        client = null;
        ownerInDate = null;
        saveId = null;
        revision = 0;
#endif
        userData = null;
        auditSnapshot = null;
        RewardJournal = null;
        requiresReload = false;
        LastError = "";
    }

    // 개발자가 한 번 실행하는 스키마 설정. 기존 테이블은 수정/삭제하지 않습니다.
    public static async Task CreateTableAsync(string databaseUuid)
    {
#if BACKND_SDK_INSTALLED
        if (!Guid.TryParse(databaseUuid, out _)) throw new InvalidOperationException("DB UUID가 올바르지 않습니다.");
        using (var setupClient = new Client(databaseUuid))
        {
            await setupClient.Initialize();
            if (string.IsNullOrEmpty(setupClient.UserUUID)) throw new InvalidOperationException("먼저 로그인해주세요.");
            await setupClient.CreateTable<PlayerSaveRow>();
        }
#else
        await Task.CompletedTask;
        throw new InvalidOperationException("데이터베이스 SDK 설치/컴파일을 완료해주세요.");
#endif
    }
#if BACKND_SDK_INSTALLED
    private Client client;
    private string ownerInDate;
    private string saveId;
    private long revision;
#endif

    public async Task<bool> InitializeAndLoadAsync(string databaseUuid, Action<string> progress = null, CancellationToken cancellation = default)
    {
        if (busy)
        {
            LastError = "이전 데이터 요청이 진행 중입니다. 잠시 후 다시 시도해주세요.";
            return false;
        }
        busy = true;
        userData = null;
        RewardJournal = null;
        requiresReload = false;
        LastError = "";
        try
        {
#if BACKND_SDK_INSTALLED
            client?.Dispose();
            client = null;
            ownerInDate = null;
            if (!Guid.TryParse(databaseUuid, out _))
                throw new InvalidOperationException("BackendManager에 데이터베이스 관리의 DB UUID를 입력해주세요.");
            string owner = Backend.UserInDate;
            if (string.IsNullOrEmpty(owner)) throw new InvalidOperationException("먼저 로그인해주세요.");
            client = new Client(databaseUuid);
            progress?.Invoke("DB 인증 서버에 연결하는 중…");
            await WaitForRequest(InitializeClientAsync(client), "DB 인증", cancellation);
            if (string.IsNullOrEmpty(client.UserUUID)) throw new InvalidOperationException("데이터베이스 인증 초기화에 실패했습니다.");
            string key = client.UserUUID;
            progress?.Invoke("저장된 유저 데이터를 불러오는 중…");
            var row = await WaitForRequest(ReadRowAsync(client, key), "유저 데이터 조회", cancellation);
            if (row == null)
            {
                // 조회 오류는 예외로 중단되며, 조회 성공 후 없음일 때만 이전/신규 생성합니다.
                progress?.Invoke("기존 데이터를 확인하고 초기 데이터를 준비하는 중…");
                var candidate = await WaitForRequest(LegacyGameDataReader.LoadAsync(), "기존 데이터 조회", cancellation) ?? BackendUserData.CreateNew();
                row = ToRow(candidate, key, 1);
                EnsureOwner(owner);
                var inserted = await WaitForRequest(InsertRowAsync(client, row), "초기 데이터 저장", cancellation);
                if (inserted != 1) throw new InvalidOperationException("초기 데이터 이전/삽입 결과를 확인할 수 없습니다. 다시 로그인해주세요.");
                Debug.Log("데이터베이스 player_save 초기 저장 완료. 기존 USER_DATA는 보존합니다.");
            }
            EnsureOwner(owner);
            progress?.Invoke("미저장 보상을 확인하고 복구하는 중…");
            var loaded = FromRow(row);
            // 닉네임은 계정 서버가 기준입니다. 프로필 JSON 저장 실패 후에도 복원됩니다.
            if (!string.IsNullOrEmpty(Backend.UserNickName)) loaded.progress.profileName = Backend.UserNickName;
            var journal = new CombatRewardJournal(
                System.IO.Path.Combine(Application.persistentDataPath, "CombatRewards"),
                databaseUuid + ":" + key, loaded.progress.combatSaveReceipt);
            if (!journal.Reconcile(loaded.progress.combatSaveReceipt))
                throw new InvalidOperationException("다른 기기 등의 저장과 로컬 미저장 기록이 충돌했습니다. 중복 지급을 방지하기 위해 자동 복구를 중단했고 기록은 보존했습니다.");
            journal.Restore(loaded);
            ownerInDate = owner;
            saveId = key;
            revision = row.Revision;
            userData = loaded;
            RewardJournal = journal;
            auditSnapshot = new ItemAuditSnapshot(loaded);
            if (loaded.progress.itemAuditPending == null)
                loaded.progress.itemAuditPending = new System.Collections.Generic.List<ItemAuditEvent>();
            _ = BackendItemAudit.FlushAsync(loaded, owner, key);
            return true;
#else
            await Task.CompletedTask;
            throw new InvalidOperationException("데이터베이스 SDK 패키지 설치/컴파일을 완료해주세요. BACKND_SDK_INSTALLED 설정도 확인해주세요.");
#endif
        }
        catch (Exception exception)
        {
#if BACKND_SDK_INSTALLED
            client?.Dispose();
            client = null;
#endif
            LastError = exception.Message;
            Debug.LogException(exception);
            return false;
        }
        finally { busy = false; }
    }

    public async Task<bool> GameDataUpdateAsync()
    {
        if (busy)
            return false;
        busy = true;
        LastError = "";
        var pendingAudits = new System.Collections.Generic.List<ItemAuditEvent>();
        bool auditWasInitialized = userData?.progress?.itemAuditInitialized ?? false;
        bool committed = false;
        try
        {
#if BACKND_SDK_INSTALLED
            if (client == null || userData == null || string.IsNullOrEmpty(ownerInDate))
                throw new InvalidOperationException("현재 계정의 데이터가 준비되지 않았습니다.");
            EnsureOwner(ownerInDate);
            if (requiresReload) throw new InvalidOperationException("저장 결과를 확인하려면 다시 로그인해주세요. 로컬 전투 보상은 보존됩니다.");
            // 이전 요청의 응답이 끊겼다면 먼저 서버가 실제로 저장했는지 확인합니다.
            if (RewardJournal != null && RewardJournal.HasAttempts)
            {
                string lookupKey = saveId;
                var current = await WaitForRequest(ReadRowAsync(client, lookupKey), "이전 저장 확인");
                if (current == null || current.Revision != revision)
                {
                    requiresReload = true;
                    throw new InvalidOperationException("이전 저장 결과가 서버에 반영되었거나 다른 기기에서 변경되었습니다. 다시 로그인해주세요.");
                }
                if (!RewardJournal.Reconcile(current.Progress.combatSaveReceipt))
                    throw new InvalidOperationException("저장 상태 확인이 필요합니다. 로컬 보상 기록을 보존하고 다시 로그인해주세요.");
                revision = current.Revision;
            }
            long expectedRevision = revision;
            var nextAudit = new ItemAuditSnapshot(userData);
            var previousAudit = auditSnapshot ?? nextAudit;
            if (!auditWasInitialized)
            {
                var baseline = previousAudit.Compare(previousAudit, "inventory_baseline",
                    checked(expectedRevision + 1), Application.version, true);
                if (baseline != null) pendingAudits.Add(baseline);
            }
            var changeAudit = previousAudit.Compare(nextAudit, userData.auditSource,
                checked(expectedRevision + 1), Application.version);
            if (changeAudit != null) pendingAudits.Add(changeAudit);
            if (pendingAudits.Count > 0)
            {
                if (userData.progress.itemAuditPending.Count + pendingAudits.Count > 24)
                {
                    _ = BackendItemAudit.FlushAsync(userData, ownerInDate, saveId);
                    throw new InvalidOperationException("아이템 로그 전송이 지연되고 있습니다. 잠시 후 다시 저장해주세요.");
                }
                userData.progress.itemAuditPending.AddRange(pendingAudits);
            }
            userData.progress.itemAuditInitialized = true;
            if (RewardJournal != null) userData.progress.combatSaveReceipt = RewardJournal.BeginUpload();
            var row = ToRow(userData, saveId, checked(expectedRevision + 1));
            string key = saveId;
            var affected = await WaitForRequest(UpdateRowAsync(client, key, expectedRevision, row), "유저 데이터 저장");
            if (affected != 1)
                throw new InvalidOperationException("저장 버전이 변경됐거나 행이 없습니다. 다시 로그인해 최신 데이터를 불러와주세요.");
            revision = row.Revision;
            committed = true;
            auditSnapshot = nextAudit;
            // 서버 저장 성공 후 로컬 디스크 오류가 나도 구매 등을 롤백하면 안 됩니다.
            try { RewardJournal?.Reconcile(row.Progress.combatSaveReceipt); }
            catch (Exception exception) { Debug.LogException(exception); }
            _ = BackendItemAudit.FlushAsync(userData, ownerInDate, saveId);
            return true;
#else
            await Task.CompletedTask;
            throw new InvalidOperationException("데이터베이스 SDK 설치가 필요합니다.");
#endif
        }
        catch (Exception exception)
        {
            if (exception is TimeoutException) requiresReload = true;
            LastError = exception.Message;
            Debug.LogException(exception);
            return false;
        }
        finally
        {
            if (instance == this && !committed && userData != null)
            {
                foreach (var entry in pendingAudits) userData.progress.itemAuditPending.Remove(entry);
                userData.progress.itemAuditInitialized = auditWasInitialized;
#if BACKND_SDK_INSTALLED
                // 용량 제한으로 저장이 거절돼도 이미 확정된 대기 로그는 다시 전송합니다.
                if (!string.IsNullOrEmpty(ownerInDate) && !string.IsNullOrEmpty(saveId))
                    _ = BackendItemAudit.FlushAsync(userData, ownerInDate, saveId);
#endif
            }
            busy = false;
        }
    }

#if BACKND_SDK_INSTALLED
    private static async Task<bool> InitializeClientAsync(Client requestClient)
    {
        await requestClient.Initialize();
        return true;
    }

    private static async Task<PlayerSaveRow> ReadRowAsync(Client requestClient, string key) =>
        await requestClient.From<PlayerSaveRow>().OfCurrentUser().Where(x => x.SaveId == key).FirstOrDefault();

    private static async Task<int> InsertRowAsync(Client requestClient, PlayerSaveRow row) =>
        (await requestClient.From<PlayerSaveRow>().Insert(row)).AffectedRows;

    private static async Task<int> UpdateRowAsync(Client requestClient, string key, long expectedRevision, PlayerSaveRow row) =>
        (await requestClient.From<PlayerSaveRow>().OfCurrentUser()
            .Where(x => x.SaveId == key && x.Revision == expectedRevision).Update(row)).AffectedRows;

    private static void EnsureOwner(string owner)
    {
        if (owner != Backend.UserInDate) throw new InvalidOperationException("로그인 계정이 변경되었습니다. 다시 로그인해주세요.");
    }

    private static PlayerSaveRow ToRow(BackendUserData source, string key, long revision)
    {
        PlayerDataValidation.Validate(source);
        // 네트워크 대기 중 원본이 변경되어도 요청은 같은 스냅샷을 사용합니다.
        var data = JsonConvert.DeserializeObject<BackendUserData>(JsonConvert.SerializeObject(source));
        var archive = string.IsNullOrEmpty(data.legacySnapshotJson)
            ? new Dictionary<string, object>()
            : JsonConvert.DeserializeObject<Dictionary<string, object>>(data.legacySnapshotJson);
        var row = new PlayerSaveRow
        {
            SaveId = key, SchemaVersion = data.schemaVersion, Revision = revision,
            Progress = data.progress, Wallet = data.wallet, Characters = data.characters,
            Equipment = data.equipment, Relics = data.relics, Loadout = data.loadout,
            IdleReward = data.idleReward, LegacyArchive = archive ?? new Dictionary<string, object>()
        };
        CheckJsonSize(row.Progress); CheckJsonSize(row.Wallet); CheckJsonSize(row.Characters);
        CheckJsonSize(row.Equipment); CheckJsonSize(row.Relics); CheckJsonSize(row.Loadout);
        CheckJsonSize(row.IdleReward); CheckJsonSize(row.LegacyArchive);
        return row;
    }

    private static void CheckJsonSize(object value)
    {
        if (Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(value)) > 12 * 1024)
            throw new InvalidOperationException("JSON 컬럼의 12KB 제한을 초과했습니다. 보유 목록 분리가 필요합니다.");
    }

    private static BackendUserData FromRow(PlayerSaveRow row)
    {
        if (row.SchemaVersion != BackendUserData.CurrentVersion || row.Revision < 1)
            throw new InvalidOperationException("지원하지 않는 세이브 버전입니다.");
        var data = new BackendUserData
        {
            schemaVersion = row.SchemaVersion, progress = row.Progress, wallet = row.Wallet,
            characters = row.Characters, equipment = row.Equipment, relics = row.Relics,
            loadout = row.Loadout, idleReward = row.IdleReward,
            legacySnapshotJson = row.LegacyArchive == null || row.LegacyArchive.Count == 0 ? "" : JsonConvert.SerializeObject(row.LegacyArchive)
        };
        PlayerDataValidation.Validate(data);
        return data;
    }
#endif
}
