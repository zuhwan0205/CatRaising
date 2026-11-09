using System;
using System.Threading.Tasks;
using BackEnd;
using UnityEngine;

// player_save의 outbox를 뒤끝 게임 로그로 전달합니다. 중복 수신은 eventId로 판별합니다.
public static class BackendItemAudit
{
    public const string LogType = "item_audit";
    private static bool sending;

    public static async Task FlushAsync(BackendUserData data, string owner, string userUuid)
    {
        if (sending || data?.progress?.itemAuditPending == null) return;
        sending = true;
        try
        {
            // 요청 중 새로 추가된 로그는 다음 저장/로그인 때 전송합니다.
            var batch = data.progress.itemAuditPending.ToArray();
            foreach (var entry in batch)
            {
                if (Backend.UserInDate != owner || BackendGameData.userData != data) return;
                var param = new Param();
                param.Add("eventId", entry.eventId);
                param.Add("userUuid", userUuid);
                param.Add("source", entry.source);
                param.Add("saveRevision", entry.saveRevision);
                param.Add("occurredUtc", entry.occurredUtc);
                param.Add("appVersion", entry.appVersion);
                param.Add("payloadJson", JsonUtility.ToJson(entry));
                var completion = new TaskCompletionSource<BackendReturnObject>();
                Backend.GameLog.InsertLogV2(LogType, param, result => completion.TrySetResult(result));
                if (await Task.WhenAny(completion.Task, Task.Delay(30000)) != completion.Task)
                    throw new TimeoutException("아이템 로그 전송 시간 초과");
                var response = await completion.Task;
                if (!response.IsSuccess()) throw new InvalidOperationException("아이템 로그 전송 실패: " + response.GetStatusCode());
                if (Backend.UserInDate != owner || BackendGameData.userData != data) return;
                data.progress.itemAuditPending.RemoveAll(x => x.eventId == entry.eventId);
            }
        }
        catch (Exception exception)
        {
            // 로그 장애가 이미 확정된 아이템 지급을 롤백시키면 안 됩니다.
            Debug.LogWarning(exception.Message + " · 로그는 세이브에 보관되며 다음 저장/로그인 때 재전송합니다.");
        }
        finally { sending = false; }
    }
}
