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
            case "trio_melee": return new TrioAttack();
            default: return null;
        }
    }
}

public sealed class TrioAttack : ICharacterAttack
{
    private readonly SpinAttack candidates = new SpinAttack();
    public void SelectTargets(Vector3 origin, FieldEnemy closest, FieldEnemy[] enemies, float range, List<FieldEnemy> results)
    {
        candidates.SelectTargets(origin, closest, enemies, range, results);
        results.Sort((a,b) => a==b?0:a==closest?-1:b==closest?1:
            (a.transform.position-origin).sqrMagnitude.CompareTo((b.transform.position-origin).sqrMagnitude));
    }

    // 장애물에 가려진 적을 제거한 후 가까운 세 마리만 남깁니다.
    public void LimitTargets(List<FieldEnemy> results)
    {
        for(int i=results.Count-1;i>=0;i--)
            if(results.IndexOf(results[i])!=i)results.RemoveAt(i);
        if(results.Count>3)results.RemoveRange(3,results.Count-3);
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
