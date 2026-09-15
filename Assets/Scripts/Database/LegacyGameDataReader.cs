using System;
using System.Globalization;
using System.Threading.Tasks;
using BackEnd;
using LitJson;
using UnityEngine;

// 이전 전용 읽기 어댑터. 기존 USER_DATA에 대한 쓰기/삭제는 하지 않습니다.
public static class LegacyGameDataReader
{
    public static async Task<BackendUserData> LoadAsync()
    {
        var completion = new TaskCompletionSource<BackendReturnObject>();
        Backend.GameData.GetMyData("USER_DATA", new Where(), result => completion.TrySetResult(result));
        var response = await completion.Task;
        if (!response.IsSuccess())
            throw new InvalidOperationException("기존 USER_DATA 조회 실패. 데이터 유실 방지를 위해 신규 생성을 중단합니다. " + response.GetStatusCode());
        var rows = response.FlattenRows();
        if (rows.Count == 0) return null;
        if (rows.Count != 1) throw new InvalidOperationException("기존 USER_DATA가 여러 행입니다. 중복 행을 확인해주세요.");
        var row = rows[0];
        BackendUserData data;
        if (Has(row, "playerDataJson"))
        {
            string json = row["playerDataJson"].ToString();
            var payload = JsonMapper.ToObject(json);
            foreach (string key in new[] { "schemaVersion", "progress", "wallet", "characters", "equipment", "relics", "loadout", "idleReward" })
                if (!Has(payload,key)) throw new InvalidOperationException("기존 데이터 필드 누락: " + key);
            data = JsonUtility.FromJson<BackendUserData>(json);
        }
        else
        {
            if (Has(row,"schemaVersion") || !Has(row,"level") || !Has(row,"atk"))
                throw new InvalidOperationException("인식할 수 없는 기존 데이터 형식입니다.");
            data = BackendUserData.CreateNew();
            data.progress.accountLevel = int.Parse(row["level"].ToString(), CultureInfo.InvariantCulture);
            data.legacySnapshotJson = row.ToJson();
        }
        PlayerDataValidation.Validate(data);
        return data;
    }

    private static bool Has(JsonData value, string key)
    { return value != null && value.IsObject && value.Keys.Contains(key); }
}
