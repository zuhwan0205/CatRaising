using System.Collections.Generic;
using UnityEngine;

// 공격 대상 선택과 전투의 피해/보상 처리를 분리합니다.
public interface ICharacterAttack
{
    void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results);
}

public static class CharacterAttackFactory
{
    public static ICharacterAttack Create(string id)
    {
        switch (id)
        {
            case "claw_melee":
                return new ClawAttack();
            case "spin_melee":
                return new SpinAttack();
            case "trio_melee":
                return new TrioAttack();
            case "forward_area":
                return new ForwardAreaAttack();
            case "target_area":
                return new TargetAreaAttack();
            case "breath_area":
                return new BreathAreaAttack();
            default:
                return null;
        }
    }
}

// 메인/레이드(XZ)와 던전(타일 XY)이 공유하는 공격 범위와 고유 효과 수치입니다.
public static class CharacterCombatRules
{
    public const float SplashRadius = 1.5f;
    public const float AuraRadius = 3f;
    public const double AuraMultiplier = 1.2;
    public const float SlowDuration = 2f, SlowMultiplier = .6f;
    public const float ArmorBreakDuration = 3f, ArmorMultiplier = .7f;
    public static double DamageAfterDefense(double damage, double defense, bool armorBroken)
        => System.Math.Max(.1, damage - defense * (armorBroken ? ArmorMultiplier : 1f));
    public static Vector2 Flat(Vector3 point) => new Vector2(point.x, point.z);
    public static bool Hits(string attackId, Vector2 origin, Vector2 target, Vector2 enemy, float range)
    {
        Vector2 offset = enemy - origin;
        if (attackId == "target_area") return (enemy - target).sqrMagnitude <= SplashRadius * SplashRadius;
        if (attackId == "spin_melee" || attackId == "trio_melee") return offset.sqrMagnitude <= range * range;
        Vector2 direction = (target - origin).normalized;
        if (direction.sqrMagnitude < .000001f) direction = Vector2.up;
        float along = Vector2.Dot(offset, direction);
        if (attackId == "breath_area")
            return offset.sqrMagnitude <= range * range && along >= 0 &&
                (offset.sqrMagnitude < .000001f || along >= offset.magnitude * .8660254f); // 전방 60도
        if (attackId == "forward_area")
            return along >= 0 && along <= range && Mathf.Abs(direction.x * offset.y - direction.y * offset.x) <= 1f;
        return false;
    }
    public static void Select(string attackId, Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
    {
        results.Clear();
        if (closest == null || !closest.Alive) return;
        foreach (var enemy in enemies)
            if (enemy != null && enemy.Alive && Hits(attackId, Flat(origin), Flat(closest.transform.position), Flat(enemy.transform.position), range))
                results.Add(enemy);
    }
}

public sealed class ForwardAreaAttack : ICharacterAttack
{
    public void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
        => CharacterCombatRules.Select("forward_area", origin, closest, enemies, range, results);
}
public sealed class TargetAreaAttack : ICharacterAttack
{
    public void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
        => CharacterCombatRules.Select("target_area", origin, closest, enemies, range, results);
}
public sealed class BreathAreaAttack : ICharacterAttack
{
    public void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
        => CharacterCombatRules.Select("breath_area", origin, closest, enemies, range, results);
}

public sealed class TrioAttack : ICharacterAttack
{
    private readonly SpinAttack candidates = new SpinAttack();
    public void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
    {
        candidates.SelectTargets(origin, closest, enemies, range, results);
        results.Sort((a, b) => a == b ? 0 : a == closest ? -1 : b == closest ? 1 :
            (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));
    }

    // 장애물에 가려진 적을 제거한 후 가까운 세 마리만 남깁니다.
    public void LimitTargets(List<FieldEnemy> results)
    {
        for (int i = results.Count - 1; i >= 0; i--)
            if (results.IndexOf(results[i]) != i)
                results.RemoveAt(i);
        if (results.Count > 3)
            results.RemoveRange(3, results.Count - 3);
    }
}

public sealed class ClawAttack : ICharacterAttack
{
    public void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
    {
        results.Clear();
        if (closest != null && closest.Alive)
            results.Add(closest);
    }
}

public sealed class SpinAttack : ICharacterAttack
{
    public void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
        => CharacterCombatRules.Select("spin_melee", origin, closest, enemies, range, results);
}
