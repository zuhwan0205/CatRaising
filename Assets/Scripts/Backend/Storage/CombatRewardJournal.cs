using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

// 서버 스냅샷에 포함되지 않은 전투 보상만 보관합니다. 계정/DB별 파일을 사용합니다.
public sealed class CombatRewardJournal
{
    public sealed class Rewards
    {
        public long gold;
        public Dictionary<string, long> experience = new Dictionary<string, long>();
    }
    public sealed class Attempt
    {
        public string receipt;
        public Rewards included;
    }
    public sealed class Record
    {
        public int version = 1;
        public string account;
        public string baseReceipt;
        public Rewards pending = new Rewards();
        public List<Attempt> attempts = new List<Attempt>();
    }
    private readonly string path;
    private readonly Record record;
    private bool dirty;
    public bool HasPending => record.pending.gold > 0 || record.pending.experience.Count > 0;
    public bool HasAttempts => record.attempts.Count > 0;
    public string BaseReceipt => record.baseReceipt ?? "";

    public CombatRewardJournal(string directory, string account, string serverReceipt)
    {
        Directory.CreateDirectory(directory);
        using (var hash = SHA256.Create())
            path = Path.Combine(directory, BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(account))).Replace("-", "") + ".json");
        record = Read(path, account) ?? Read(path + ".bak", account);
        if (record == null)
        {
            // 손상된 파일을 덮어쓰지 않고 진단용으로 보존합니다.
            if (File.Exists(path)) File.Copy(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
            record = new Record { account = account, baseReceipt = serverReceipt ?? "" };
        }
    }

    private static Record Read(string file, string account)
    {
        if (!File.Exists(file)) return null;
        try
        {
            var value = JsonConvert.DeserializeObject<Record>(File.ReadAllText(file));
            if (value == null || value.version != 1 || value.account != account || !Valid(value.pending) || value.attempts == null) return null;
            foreach (var attempt in value.attempts)
                if (attempt == null || string.IsNullOrEmpty(attempt.receipt) || !Valid(attempt.included)) return null;
            return value;
        }
        catch (JsonException) { return null; }
    }
    private static bool Valid(Rewards rewards)
    {
        if (rewards == null || rewards.gold < 0 || rewards.experience == null) return false;
        foreach (var entry in rewards.experience)
            if (string.IsNullOrEmpty(entry.Key) || entry.Value <= 0) return false;
        return true;
    }
    public void AddGold(long amount)
    {
        record.pending.gold = checked(record.pending.gold + amount);
        dirty = true;
    }
    public void AddExperience(string cat, long amount)
    {
        record.pending.experience.TryGetValue(cat, out long previous);
        record.pending.experience[cat] = checked(previous + amount);
        dirty = true;
    }
    public void Checkpoint()
    {
        if (!dirty) return;
        string temporary = path + ".tmp";
        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(record));
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
        else File.Move(temporary, path);
        dirty = false;
    }
    public string BeginUpload()
    {
        var receipt = Guid.NewGuid().ToString("N");
        record.attempts.Add(new Attempt
        {
            receipt = receipt,
            included = new Rewards
            {
                gold = record.pending.gold,
                experience = new Dictionary<string, long>(record.pending.experience)
            }
        });
        dirty = true;
        Checkpoint();
        return receipt;
    }

    // 저장 응답 유실 시 서버에 기록된 표식으로 포함된 보상만 제거합니다.
    public bool Reconcile(string serverReceipt)
    {
        serverReceipt = serverReceipt ?? "";
        if (serverReceipt == BaseReceipt) return true;
        var completed = record.attempts.Find(x => x.receipt == serverReceipt);
        if (completed == null && !HasPending && !HasAttempts)
        {
            record.baseReceipt = serverReceipt;
            dirty = true;
            Checkpoint();
            return true;
        }
        if (completed == null) return false;
        record.pending.gold = Math.Max(0, record.pending.gold - completed.included.gold);
        foreach (var entry in completed.included.experience)
        {
            if (!record.pending.experience.TryGetValue(entry.Key, out long value)) continue;
            if (value <= entry.Value) record.pending.experience.Remove(entry.Key);
            else record.pending.experience[entry.Key] = value - entry.Value;
        }
        record.attempts.Clear();
        record.baseReceipt = serverReceipt;
        dirty = true;
        Checkpoint();
        return true;
    }

    public void Restore(BackendUserData serverData)
    {
        if (serverData.wallet.gold > long.MaxValue - record.pending.gold)
            throw new InvalidOperationException("미저장 골드를 복구하면 보유 한도를 초과합니다. 로컬 기록은 보존됩니다.");
        foreach (var entry in record.pending.experience)
            if (!serverData.characters.Exists(x => x != null && x.characterId == entry.Key))
                throw new InvalidOperationException("미저장 경험치의 고양이가 서버에 없습니다. 로컬 기록은 보존됩니다.");
        serverData.wallet.gold += record.pending.gold;
        foreach (var entry in record.pending.experience)
            CatProgression.AddExperience(serverData.characters.Find(x => x != null && x.characterId == entry.Key), entry.Value);
    }
}
