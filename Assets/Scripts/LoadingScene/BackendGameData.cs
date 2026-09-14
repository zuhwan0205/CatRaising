using System;
using System.Globalization;
using BackEnd;
using LitJson;
using UnityEngine;

public class BackendGameData
{
    private const string TableName = "USER_DATA";
    private static BackendGameData instance;
    public static BackendGameData Instance => instance ?? (instance = new BackendGameData());
    public static BackendUserData userData;
    public bool NeedsMigrationSave { get; private set; }
    private string rowInDate = "";
    private string ownerInDate = "";

    private static bool Has(JsonData value, string key)
    {
        return value != null && value.IsObject && value.Keys.Contains(key);
    }

    private static Param Serialize(BackendUserData data)
    {
        Validate(data);
        var param = new Param();
        param.Add("schemaVersion", data.schemaVersion);
        param.Add("playerDataJson", JsonUtility.ToJson(data));
        return param;
    }

    private static void Validate(BackendUserData data)
    {
        if (data == null || data.schemaVersion != BackendUserData.CurrentVersion ||
            data.progress == null || data.wallet == null || data.characters == null ||
            data.equipment == null || data.relics == null || data.loadout == null ||
            data.idleReward == null || data.wallet.materials == null || data.loadout.relicIds == null)
            throw new InvalidOperationException("지원하지 않거나 손상된 유저 데이터입니다.");
        if (data.progress.accountLevel < 1 || data.progress.currentStage < 1)
            throw new InvalidOperationException("유효하지 않은 성장 데이터입니다.");
    }

    public bool GameDataInsert()
    {
        if (userData != null || !string.IsNullOrEmpty(rowInDate)) return false;
        var candidate = BackendUserData.CreateNew();
        var result = Backend.GameData.Insert(TableName, Serialize(candidate));
        if (!result.IsSuccess())
        {
            Debug.LogError("초기 데이터 저장 실패 : " + result);
            return false;
        }
        rowInDate = result.GetInDate();
        ownerInDate = Backend.UserInDate;
        userData = candidate;
        NeedsMigrationSave = false;
        return true;
    }

    public bool GameDataGet()
    {
        userData = null;
        NeedsMigrationSave = false;
        rowInDate = "";
        ownerInDate = "";
        var result = Backend.GameData.GetMyData(TableName, new Where());
        if (!result.IsSuccess())
        {
            Debug.LogError("유저 데이터 조회 실패 : " + result);
            return false;
        }
        try
        {
            var rows = result.FlattenRows();
            if (rows.Count == 0) return true;
            if (rows.Count != 1)
                throw new InvalidOperationException("유저 데이터가 여러 행입니다. 서버에서 중복 행을 확인해주세요.");
            var row = rows[0];
            BackendUserData candidate;
            bool isLegacy = !Has(row, "playerDataJson");
            if (Has(row, "playerDataJson"))
            {
                string json = row["playerDataJson"].ToString();
                var payload = JsonMapper.ToObject(json);
                foreach (string key in new[] { "schemaVersion", "progress", "wallet", "characters", "equipment", "relics", "loadout", "idleReward" })
                    if (!Has(payload, key)) throw new InvalidOperationException("세이브 필드 누락 : " + key);
                candidate = JsonUtility.FromJson<BackendUserData>(json);
            }
            else
            {
                if (Has(row, "schemaVersion") || !Has(row, "level") || !Has(row, "atk"))
                    throw new InvalidOperationException("인식할 수 없는 저장 형식입니다.");
                candidate = BackendUserData.CreateNew();
                candidate.progress.accountLevel = int.Parse(row["level"].ToString(), CultureInfo.InvariantCulture);
                candidate.legacySnapshotJson = row.ToJson();
                Debug.LogWarning("기존 테스트 데이터의 아이템과 공격력은 원본에 보관합니다. ID 변환은 아직 적용하지 않습니다.");
            }
            Validate(candidate);
            string candidateRowId = row["inDate"].ToString();
            if (string.IsNullOrEmpty(candidateRowId)) throw new InvalidOperationException("행 식별자가 없습니다.");
            rowInDate = candidateRowId;
            ownerInDate = Backend.UserInDate;
            userData = candidate;
            NeedsMigrationSave = isLegacy;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    public bool GameDataUpdate()
    {
        if (userData == null || string.IsNullOrEmpty(rowInDate) || ownerInDate != Backend.UserInDate)
        {
            Debug.LogError("현재 계정에서 조회하거나 생성한 데이터가 없습니다.");
            return false;
        }
        try
        {
            var result = Backend.GameData.UpdateV2(TableName, rowInDate, ownerInDate, Serialize(userData));
            if (!result.IsSuccess())
            {
                Debug.LogError("유저 데이터 저장 실패 : " + result);
                return false;
            }
            if (NeedsMigrationSave)
                Debug.Log("기존 유저 데이터를 새 형식으로 저장했습니다. schemaVersion : " + userData.schemaVersion);
            NeedsMigrationSave = false;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }
}
