using System;

// 피해량/사망 판정은 Unity 오브젝트 없이 검증할 수 있습니다.
public sealed class CombatHealth
{
    public double Maximum { get; }
    public double Current { get; private set; }
    public bool Dead => Current <= 0;
    public CombatHealth(double maximum)
    {
        if(double.IsNaN(maximum)||double.IsInfinity(maximum)||maximum<=0) throw new ArgumentOutOfRangeException(nameof(maximum));
        Maximum=Current=maximum;
    }
    // 살아있던 대상이 이번 공격으로 죽었을 때만 true.
    public bool Damage(double damage)
    {
        if(Dead || double.IsNaN(damage)||double.IsInfinity(damage)||damage<=0)return false;
        Current=Math.Max(0,Current-damage);
        return Dead;
    }
    public void Heal(double amount)
    {
        if (Dead || double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0) return;
        Current = Math.Min(Maximum, Current + amount);
    }
    public void Reset() { Current=Maximum; }
}
