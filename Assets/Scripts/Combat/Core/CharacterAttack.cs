using System.Collections.Generic;
using UnityEngine;

public interface ICharacterHitReceiver
{
    void ApplyIceSlow();
    void ApplyArmorBreak();
}

// 공격 대상 선택과 전투의 피해/보상 처리를 분리합니다.
public interface ICharacterAttack
{
    void SelectTargets(Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results);
}

public static class CharacterAttackFactory
{
    private static readonly Dictionary<string, ICharacterAttack> strategies = new Dictionary<string, ICharacterAttack>
    {
        { "claw_melee", new ClawAttack() }, { "spin_melee", new SpinAttack() }, { "trio_melee", new TrioAttack() },
        { "forward_area", new ForwardAreaAttack() }, { "target_area", new TargetAreaAttack() }, { "breath_area", new BreathAreaAttack() }
    };
    public static ICharacterAttack Create(string id) => id != null && strategies.TryGetValue(id, out var strategy) ? strategy : null;

}

// 메인/레이드(XZ)와 던전(타일 XY)이 공유하는 공격 범위와 고유 효과 수치입니다.
public static class CharacterCombatRules
{
    public const float SplashRadius = 1.5f;
    public const float AuraRadius = 3f;
    public const double AuraMultiplier = 1.2;
    public const float SlowDuration = 2f, SlowMultiplier = .6f;
    public const float ArmorBreakDuration = 3f, ArmorMultiplier = .7f;
    public static void ApplyHit(CharacterHitEffect effect, ICharacterHitReceiver target)
    {
        if (effect == CharacterHitEffect.Slow) target.ApplyIceSlow();
        else if (effect == CharacterHitEffect.ArmorBreak) target.ApplyArmorBreak();
    }
    public static double DamageAfterDefense(double damage, double defense, bool armorBroken)
        => System.Math.Max(.1, damage - defense * (armorBroken ? ArmorMultiplier : 1f));
    public static Vector2 Flat(Vector3 point) => new Vector2(point.x, point.z);
    public static bool Hits(string attackId, Vector2 origin, Vector2 target, Vector2 enemy, float range)
    {
        return AttackAreaStrategies.Find(attackId)?.Contains(origin, target, enemy, range) ?? false;
    }
    public static void Select(string attackId, Vector3 origin, FieldEnemy closest, IReadOnlyList<FieldEnemy> enemies, float range, List<FieldEnemy> results)
    {
        results.Clear();
        if (closest == null || !closest.Alive) return;
        var area = AttackAreaStrategies.Find(attackId);
        if (area == null) return;
        foreach (var enemy in enemies)
            if (enemy != null && enemy.Alive && area.Contains(Flat(origin), Flat(closest.transform.position), Flat(enemy.transform.position), range))
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
