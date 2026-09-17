using UnityEngine;

[CreateAssetMenu(menuName = "Cat Raising/Character", fileName = "Character")]
public sealed class CatCharacterAsset : ScriptableObject
{
    public CharacterDefinition definition = new CharacterDefinition();

    private void OnValidate()
    {
        if (definition == null) return;
        if (string.IsNullOrWhiteSpace(definition.id)) Debug.LogWarning("캐릭터 ID를 입력해주세요.", this);
        if (CharacterAttackFactory.Create(definition.attackId) == null)
            Debug.LogWarning("지원하는 Attack Id: claw_melee, spin_melee", this);
    }
}
