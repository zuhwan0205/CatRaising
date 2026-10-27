using UnityEngine;

// 적별 중첩을 공유하여 파티원이 여러 명이어도 독/화상 각각 5중첩을 넘지 않습니다.
public sealed class FieldSkillStatus : MonoBehaviour
{
    private sealed class Status
    {
        public FieldCombatController source;
        public OwnedCharacter character;
        public int stacks;
        public float remaining, tick;
        public void Clear() { stacks = 0; remaining = tick = 0; source = null; }
    }
    private readonly Status poison = new Status(), burn = new Status();
    private FieldEnemy enemy;
    private CombatHealth life;
    public void Apply(FieldCombatController source, FieldEnemy target, int poisonRank, int burnRank)
    {
        if (life != target.Health) { poison.Clear(); burn.Clear(); }
        enemy = target;
        life = target.Health;
        Add(poison, source, poisonRank);
        Add(burn, source, burnRank);
    }
    private void Add(Status status, FieldCombatController source, int rank)
    {
        if (rank == 0) return;
        if (status.stacks == 0) status.tick = 1;
        status.stacks = Mathf.Min(5, Mathf.Max(status.stacks, Mathf.Min(rank, status.stacks + 1)));
        status.source = source;
        status.character = source.SkillCharacter;
        status.remaining = 5;
    }
    private void Update()
    {
        if (Time.timeScale == 0) return;
        if (enemy == null || !enemy.Alive || enemy.Health != life) { poison.Clear(); burn.Clear(); return; }
        Tick(poison);
        if (enemy.Alive) Tick(burn);
    }
    private void Tick(Status status)
    {
        if (status.stacks == 0) return;
        var source = status.source;
        if (source == null || !source.isActiveAndEnabled || source.PlayerHealth == null || source.PlayerHealth.Dead ||
            source.SkillCharacter != status.character)
        { status.Clear(); return; }
        if (source.Blocked) return;
        float delta = Mathf.Min(Time.deltaTime, .1f);
        status.remaining -= delta;
        status.tick -= delta;
        if (status.tick <= .0001f)
        {
            status.tick += 1;
            source.DealSkillDamage(enemy, enemy.Health.Maximum * .005 * status.stacks * enemy.StatusDamageMultiplier, true);
        }
        if (status.remaining <= 0) status.Clear();
    }
    private void OnDisable() { poison.Clear(); burn.Clear(); }
}
