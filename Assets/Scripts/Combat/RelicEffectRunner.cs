using System;
using System.Collections.Generic;
using UnityEngine;

// 쿨다운은 전투 시간으로만 진행됩니다. 추가 피해는 트리거를 재발행하지 않습니다.
public sealed class RelicEffectRunner
{
    private readonly BackendUserData data;
    private readonly CatGameCatalog catalog;
    private readonly Dictionary<string, double> readyAt = new Dictionary<string, double>();
    private readonly HashSet<string> processed = new HashSet<string>();
    private double elapsed;

    public RelicEffectRunner(BackendUserData data, CatGameCatalog catalog)
    { this.data = data; this.catalog = catalog; }

    public void Tick(float delta) { elapsed += Math.Max(0, delta); }

    public void Trigger(EffectTrigger trigger, Action<double> heal, Action<double> bonusDamage,
        Action<RelicDefinition, double> onTriggered = null)
    {
        processed.Clear();
        foreach (string id in data.loadout.relicIds)
        {
            if (string.IsNullOrEmpty(id) || !processed.Add(id)) continue;
            var owned = data.relics.Find(x => x != null && x.relicId == id);
            var definition = catalog.FindRelic(id);
            if (owned == null || definition == null || definition.trigger != trigger) continue;
            if (readyAt.TryGetValue(id, out double ready) && elapsed < ready) continue;
            double value = definition.effectValue + (double)definition.effectPerLevel * Math.Max(0, owned.level-1);
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) continue;
            Action<double> effect = definition.effectId == "heal" ? heal :
                definition.effectId == "bonus_damage" ? bonusDamage : null;
            float chance = Mathf.Clamp01(definition.chance);
            if (effect == null || float.IsNaN(chance) || chance <= 0 || (chance < 1 && UnityEngine.Random.value >= chance)) continue;
            float cooldown = definition.cooldownSeconds;
            if (float.IsNaN(cooldown) || float.IsInfinity(cooldown)) continue;
            readyAt[id] = elapsed + Math.Max(0, cooldown);
            effect(value);
            onTriggered?.Invoke(definition, value);
        }
    }
}
