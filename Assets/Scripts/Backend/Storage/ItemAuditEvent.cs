using System;
using System.Collections.Generic;

[Serializable]
public class ItemAuditEvent
{
    public string eventId;
    public string source;
    public string occurredUtc;
    public string appVersion;
    public long saveRevision;
    public long goldBefore, goldAfter, gemsBefore, gemsAfter;
    public List<ItemAuditChange> changes = new List<ItemAuditChange>();
}

[Serializable]
public class ItemAuditChange
{
    public string kind, itemId;
    public long before, after;
}

// 저장 시점에만 비교합니다. 전투 프레임이나 몬스터 처치마다 실행하지 않습니다.
public sealed class ItemAuditSnapshot
{
    private readonly Dictionary<string, long> values = new Dictionary<string, long>();
    private readonly long gold, gems;

    public ItemAuditSnapshot(BackendUserData data)
    {
        gold = data.wallet.gold; gems = data.wallet.gems;
        foreach (var item in data.characters)
        {
            Add("character", item.characterId, 1);
            Add("character_fragment", item.characterId, item.fragments);
            Add("character_star", item.characterId, item.ascension);
        }
        foreach (var item in data.relics)
        {
            Add("relic", item.relicId, 1);
            Add("relic_fragment", item.relicId, item.fragments);
            Add("relic_star", item.relicId, item.ascension);
            Add("relic_level", item.relicId, item.level);
        }
    }

    private void Add(string kind, string id, long value) => values.Add(kind + ":" + id, value);

    public ItemAuditEvent Compare(ItemAuditSnapshot next, string source, long revision, string version, bool baseline = false)
    {
        var result = new ItemAuditEvent
        {
            eventId = Guid.NewGuid().ToString("N"), source = baseline ? "inventory_baseline" : source,
            occurredUtc = DateTime.UtcNow.ToString("O"), appVersion = version, saveRevision = revision,
            goldBefore = gold, goldAfter = next.gold, gemsBefore = gems, gemsAfter = next.gems
        };
        var keys = new SortedSet<string>(values.Keys, StringComparer.Ordinal);
        keys.UnionWith(next.values.Keys);
        foreach (string key in keys)
        {
            values.TryGetValue(key, out long before);
            next.values.TryGetValue(key, out long after);
            if (!baseline && before == after) continue;
            int separator = key.IndexOf(':');
            result.changes.Add(new ItemAuditChange
            {
                kind = key.Substring(0, separator), itemId = key.Substring(separator + 1), before = before, after = after
            });
        }
        return result.changes.Count == 0 ? null : result;
    }
}
