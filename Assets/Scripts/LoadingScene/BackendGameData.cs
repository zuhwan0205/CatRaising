using System;
using System.Threading.Tasks;
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
    public string LastError { get; private set; }
    private bool busy;

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

    public async Task<bool> InitializeAndLoadAsync(string databaseUuid)
    {
        if (busy) return false;
        busy = true;
        userData = null;
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
            await client.Initialize();
            if (string.IsNullOrEmpty(client.UserUUID)) throw new InvalidOperationException("데이터베이스 인증 초기화에 실패했습니다.");
            string key = client.UserUUID;
            var row = await client.From<PlayerSaveRow>().OfCurrentUser().Where(x => x.SaveId == key).FirstOrDefault();
            if (row == null)
            {
                // 조회 오류는 예외로 중단되며, 조회 성공 후 없음일 때만 이전/신규 생성합니다.
                var candidate = await LegacyGameDataReader.LoadAsync() ?? BackendUserData.CreateNew();
                row = ToRow(candidate, key, 1);
                EnsureOwner(owner);
                var inserted = await client.From<PlayerSaveRow>().Insert(row);
                if (inserted.AffectedRows != 1) throw new InvalidOperationException("초기 데이터 이전/삽입 결과를 확인할 수 없습니다. 다시 로그인해주세요.");
                Debug.Log("데이터베이스 player_save 초기 저장 완료. 기존 USER_DATA는 보존합니다.");
            }
            EnsureOwner(owner);
            var loaded = FromRow(row);
            ownerInDate = owner;
            saveId = key;
            revision = row.Revision;
            userData = loaded;
            return true;
#else
            await Task.CompletedTask;
            throw new InvalidOperationException("데이터베이스 SDK 패키지 설치/컴파일을 완료해주세요. BACKND_SDK_INSTALLED 설정도 확인해주세요.");
#endif
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogException(exception);
            return false;
        }
        finally { busy = false; }
    }

    public async Task<bool> GameDataUpdateAsync()
    {
        if (busy) return false;
        busy = true;
        LastError = "";
        try
        {
#if BACKND_SDK_INSTALLED
            if (client == null || userData == null || string.IsNullOrEmpty(ownerInDate))
                throw new InvalidOperationException("현재 계정의 데이터가 준비되지 않았습니다.");
            EnsureOwner(ownerInDate);
            long expectedRevision = revision;
            var row = ToRow(userData, saveId, checked(expectedRevision + 1));
            string key = saveId;
            var result = await client.From<PlayerSaveRow>().OfCurrentUser()
                .Where(x => x.SaveId == key && x.Revision == expectedRevision).Update(row);
            if (result.AffectedRows != 1)
                throw new InvalidOperationException("저장 버전이 변경됐거나 행이 없습니다. 다시 로그인해 최신 데이터를 불러와주세요.");
            revision = row.Revision;
            return true;
#else
            await Task.CompletedTask;
            throw new InvalidOperationException("데이터베이스 SDK 설치가 필요합니다.");
#endif
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogException(exception);
            return false;
        }
        finally { busy = false; }
    }

#if BACKND_SDK_INSTALLED
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
