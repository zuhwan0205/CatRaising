using UnityEngine;

[CreateAssetMenu(menuName = "Cat Raising/Relic", fileName = "Relic")]
public sealed class CatRelicAsset : ScriptableObject
{
    public RelicDefinition definition = new RelicDefinition();

    private void OnValidate()
    {
        if (definition == null) return;
        if (string.IsNullOrWhiteSpace(definition.id)) Debug.LogWarning("유물 ID를 입력해주세요.", this);
        if (definition.effectId != "heal" && definition.effectId != "bonus_damage")
            Debug.LogWarning("지원하는 Effect Id: heal, bonus_damage", this);
        if (definition.effectId == "bonus_damage" && definition.trigger != EffectTrigger.OnAttack && definition.trigger != EffectTrigger.OnHit)
            Debug.LogWarning("추가 피해는 OnAttack 또는 OnHit 트리거를 사용해주세요.", this);
    }
}
