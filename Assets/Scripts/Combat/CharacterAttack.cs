using System.Collections.Generic;
using UnityEngine;

// 공격 대상 선택과 전투의 피해/보상 처리를 분리합니다.
public interface ICharacterAttack
{
    void SelectTargets(Vector3 origin, FieldEnemy closest, FieldEnemy[] enemies, float range, List<FieldEnemy> results);
}

public static class CharacterAttackFactory
{
    public static ICharacterAttack Create(string id)
    {
        switch (id)
        {
            case "claw_melee": return new ClawAttack();
            case "spin_melee": return new SpinAttack();
            default: return null;
        }
    }
}

public sealed class ClawAttack : ICharacterAttack
{
    public void SelectTargets(Vector3 origin, FieldEnemy closest, FieldEnemy[] enemies, float range, List<FieldEnemy> results)
    {
        results.Clear();
        if (closest != null && closest.Alive) results.Add(closest);
    }
}

public sealed class SpinAttack : ICharacterAttack
{
    public void SelectTargets(Vector3 origin, FieldEnemy closest, FieldEnemy[] enemies, float range, List<FieldEnemy> results)
    {
        results.Clear();
        foreach (var enemy in enemies)
            if (enemy != null && enemy.Alive && (enemy.transform.position-origin).sqrMagnitude <= range*range)
                results.Add(enemy);
    }
}
