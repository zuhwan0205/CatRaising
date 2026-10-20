using System;
using System.Collections.Generic;
using UnityEngine;

// XZ 거리 기반 추적과 자동 공격. 이동 모드와 관계없이 공격을 처리합니다.
public partial class FieldCombatController : MonoBehaviour
{
    public event Action<long> GoldEarned;
    public event Action<Vector3, long> EnemyGoldDropped;
    public event Action<Vector3, int> ExperienceDropped;
    public event Action<string> MessageChanged;
    public event Action<string> RelicMessageChanged;
    public event Action<double, double> HealthChanged;
    public event Action PlayerHit;
    public event Action<Vector3, Vector3> AttackPerformed;
    public event Action<Vector3, double> DamageDealt;
    public event Action<Vector3, Vector3> AttackStarted;
    public CombatHealth PlayerHealth
    {
        get; private set;
    }
    public float ContactDistance = 1.05f;
    public float ContactInterval = .75f;
    public double ContactDamage = 5;
    public float PlayerRespawnDelay = 3;
    private float contactCooldown;
    private float deathRemaining;
    private double defense;
    public bool Blocked;
    public bool RaidMode;
    public FieldEnemyPool Pool { get; set; }
    public FieldNavigation Navigation
    {
        get; set;
    }
    public FieldEnemy LockedTarget
    {
        get; private set;
    }
    private Transform player;
    private FieldEnemy[] enemies;
    private BackendUserData data;
    private CatGameCatalog catalog;
    private float cooldown;
    private string selectedId;
    private int selectedLevel, selectedStars, selectedSkills;
    private double attack, speed;
    private bool valid;
    private ICharacterAttack attackBehavior;
    private RelicEffectRunner relicEffects;
    private readonly List<FieldEnemy> attackTargets = new List<FieldEnemy>();
    private int killsThisFrame;
    public float Range = 1.7f;
    public float EnemySpeed = 1.1f;
    public float RespawnDelay = 2;
    public float SurvivalSeconds
    {
        get; private set;
    }
    // 종류는 1분마다, 개체 수는 그 절반인 30초마다 증가합니다.
    private const float MonsterKindIntervalSeconds = 60f;
    public const int InitialMonsterCount = 20;
    public const int MaximumMonsterCount = 200;
    public float SpawnDistance => Mathf.Min(25f, 7f + SurvivalSeconds / 60f * 2f);
    public int UnlockedMonsterKinds => Math.Min(4, 1 + (int)(SurvivalSeconds / MonsterKindIntervalSeconds));
    public int TargetMonsterCount => Math.Min(MaximumMonsterCount, InitialMonsterCount + 10 * (int)(SurvivalSeconds / (MonsterKindIntervalSeconds / 2f)));

    public void Initialize(Transform target, FieldEnemy[] targets, BackendUserData playerData, CatGameCatalog definitions)
    {
        player = target;
        enemies = targets;
        data = playerData;
        catalog = definitions;
        relicEffects = new RelicEffectRunner(data, catalog);
    }

    public void ReceiveRaidSkill(double damage)
    {
        if (!RaidMode || Blocked || !gameObject.activeInHierarchy) return;
        ResolveStats();
        if (PlayerHealth.Dead) return;
        bool died = PlayerHealth.Damage(Math.Max(1, damage - defense));
        TrySkillPotion();
        if (!died) TriggerRelics(EffectTrigger.OnDamaged, null);
        PlayerHit?.Invoke();
        HealthChanged?.Invoke(PlayerHealth.Current, PlayerHealth.Maximum);
        if (died) LockedTarget = null;
    }

    private void Update()
    {
        if (Blocked || player == null)
            return;
        // 재등장 처리 전에 기존 대상의 생존 여부를 확인합니다.
        if (LockedTarget != null && !LockedTarget.Alive)
            LockedTarget = null;
        float delta = Mathf.Min(Time.deltaTime, .1f);
        ResolveStats();
        TickCombatSkills(delta);
        relicEffects.Tick(delta);
        killsThisFrame = 0;
        if (PlayerHealth.Dead)
        {
            if (RaidMode) return;
            LockedTarget = null;
            deathRemaining -= delta;
            if (deathRemaining <= 0)
            {
                player.position = Vector3.zero;
                for (int i = 0; i < enemies.Length; i++)
                {
                    var enemy = enemies[i];
                    if (enemy == null)
                        continue;
                    if (i < InitialMonsterCount)
                    {
                        enemy.ConfigureMonster(0);
                        enemy.Respawn(SpawnPosition());
                    }
                    else
                        enemy.gameObject.SetActive(false);
                }
                PlayerHealth.Reset();
                contactCooldown = 1;
                cooldown = 1;
                HealthChanged?.Invoke(PlayerHealth.Current, PlayerHealth.Maximum);
                MessageChanged?.Invoke("HP 회복 · 전투를 다시 시작합니다.");
            }
            return;
        }
        int previousCount = TargetMonsterCount;
        if (!RaidMode) SurvivalSeconds += delta;
        if (!RaidMode && Pool != null) Pool.Grow(TargetMonsterCount);
        if (TargetMonsterCount > previousCount)
            for (int i = previousCount; i < Math.Min(enemies.Length, TargetMonsterCount); i++)
            {
                var enemy = enemies[i];
                if (enemy == null)
                    continue;
                int step = (i - InitialMonsterCount) / 10 + 1;
                // 새 종류 해금 시 해당 종류를 먼저 추가하고 이후에는 네 종류를 순환합니다.
                int kind = step <= 6 ? Math.Min(3, step / 2) : step % 4;
                enemy.ConfigureMonster(kind);
                enemy.Respawn(SpawnPosition());
            }
        contactCooldown = Mathf.Max(0, contactCooldown - delta);
        cooldown = Mathf.Max(0, cooldown - delta);
        bool touching = false;
        double touchingDamage = 0;
        FieldEnemy contactAttacker = null;
        FieldEnemy closest = null;
        float nearest = Range * Range;
        for (int enemyIndex = 0; enemyIndex < (RaidMode ? enemies.Length : Math.Min(enemies.Length, TargetMonsterCount)); enemyIndex++)
        {
            var enemy = enemies[enemyIndex];
            if (enemy == null || (!RaidMode && enemy.Kind >= UnlockedMonsterKinds))
                continue;
            if (!enemy.Alive)
            {
                if (RaidMode) continue;
                enemy.RespawnRemaining -= delta;
                if (enemy.RespawnRemaining <= 0)
                    enemy.Respawn(SpawnPosition());
                continue;
            }
            Vector3 toward = player.position - enemy.transform.position;
            toward.y = 0;
            float distance = toward.magnitude;
            if (!RaidMode && distance > .9f)
            {
                Vector3 start = enemy.transform.position;
                Vector3 goal = enemy.NextWaypoint(Navigation, player.position);
                Vector3 direction = goal - start;
                direction.y = 0;
                Vector3 step = direction.normalized * Mathf.Min(enemy.MoveSpeed * enemy.MovementMultiplier * delta, Mathf.Min(direction.magnitude, distance - .9f));
                enemy.transform.position = Navigation == null ? start + step : Navigation.Move(start, step);
            }
            if (!RaidMode) enemy.StepVisual(delta);
            float sqr = (enemy.transform.position - player.position).sqrMagnitude;
            bool clear = sqr <= Mathf.Max(ContactDistance * ContactDistance, nearest) && (Navigation == null || Navigation.Clear(player.position, enemy.transform.position));
            if (clear && sqr <= ContactDistance * ContactDistance)
            {
                touching = true;
                if (contactAttacker == null || enemy.ContactDamage > touchingDamage) contactAttacker = enemy;
                touchingDamage = Math.Max(touchingDamage, enemy.ContactDamage);
            }
            if (clear && sqr <= nearest)
            {
                nearest = sqr;
                closest = enemy;
            }
        }
        if (touching && contactCooldown <= 0)
        {
            if (RaidMode && contactAttacker != null && contactAttacker.HasRaidAnimation)
            {
                var source = contactAttacker;
                double damage = touchingDamage;
                if (source.ScheduleRaidAttack(player.position, () =>
                {
                    if (source == null || !source.Alive || player == null || Blocked || !gameObject.activeInHierarchy) return;
                    if ((source.transform.position - player.position).sqrMagnitude > ContactDistance * ContactDistance) return;
                    if (Navigation != null && !Navigation.Clear(player.position, source.transform.position)) return;
                    ReceiveRaidSkill(damage);
                })) contactCooldown = Mathf.Max(ContactInterval, RaidBossVisual.AttackDuration);
            }
            else
            {
            contactCooldown = Mathf.Max(.1f, ContactInterval);
            if (contactAttacker != null) contactAttacker.ShowAttack(player.position);
            bool died = PlayerHealth.Damage(Math.Max(1, touchingDamage - defense));
            TrySkillPotion();
            if (!died)
                TriggerRelics(EffectTrigger.OnDamaged, null);
            PlayerHit?.Invoke();
            HealthChanged?.Invoke(PlayerHealth.Current, PlayerHealth.Maximum);
            if (died)
            {
                if (RaidMode)
                {
                    LockedTarget = null;
                    return;
                }
                SurvivalSeconds = 0;
                foreach (var enemy in enemies)
                    if (enemy != null)
                        enemy.gameObject.SetActive(false);
                LockedTarget = null;
                deathRemaining = Mathf.Max(.1f, PlayerRespawnDelay);
                MessageChanged?.Invoke($"쓰러졌습니다. {deathRemaining:0.#}초 후 회복합니다.");
                return;
            }
        }
        }
        // 매 프레임 사거리 안에서 공격 가능한 가장 가까운 적을 선택합니다.
        // 쿨다운 중에도 범위를 벗어나면 즉시 이전 타겟을 해제합니다.
        LockedTarget = closest;
        UpdateArrowFocus(closest, false);
        if (!valid || cooldown > 0 || closest == null)
            return;
        UpdateArrowFocus(closest, true);
        cooldown = (float)(1 / (speed * (1 + focusStacks * .10)));
        // 여러 적을 맞히는 공격도 몸의 방향/모션은 한 번만 시작합니다.
        AttackStarted?.Invoke(player.position, closest.transform.position);
        // OnAttack은 공격 1회당, OnHit은 기본 공격 명중 대상마다 실행합니다.
        double attackBonus = 0;
        TriggerRelics(EffectTrigger.OnAttack, value => attackBonus += value);
        if (UsesSkillArrows)
        {
            LaunchSkillArrows(closest, attack + attackBonus);
            return;
        }
        attackBehavior.SelectTargets(player.position, closest, enemies, Range, attackTargets);
        ExpandMaceTargets(closest);
        if (Navigation != null)
            attackTargets.RemoveAll(target => !Navigation.Clear(player.position, target.transform.position));
        if (attackBehavior is TrioAttack trio)
            trio.LimitTargets(attackTargets);
        double minDamage = double.PositiveInfinity, maxDamage = 0, maxBonus = 0;
        foreach (var target in attackTargets)
        {
            if (!target.Alive) continue;
            AttackPerformed?.Invoke(player.position, target.transform.position);
            double hitBonus = 0;
            TriggerRelics(EffectTrigger.OnHit, value => hitBonus += value);
            double bonus = attackBonus + hitBonus;
            double damage = attack + bonus;
            maxBonus = Math.Max(maxBonus, bonus);
            DealSkillDamage(target, damage, false, true);
            minDamage = Math.Min(minDamage, target.LastHitDamage);
            maxDamage = Math.Max(maxDamage, target.LastHitDamage);
        }
        if (attackTargets.Count > 0)
        {
            string damageText = minDamage == maxDamage ? $"{maxDamage:0.#}" : $"{minDamage:0.#}~{maxDamage:0.#}";
            string bonusText = maxBonus > 0 ? $" (유물 +{maxBonus:0.#})" : "";
            string result = killsThisFrame > 0 ? $" · {killsThisFrame}마리 처치" : $" · {attackTargets.Count}마리 명중";
            if (attackTargets.Count == 1)
                result += $"\n적 HP {closest.Health.Current:0.#}/{closest.Health.Maximum:0.#}";
            MessageChanged?.Invoke($"공격 피해 {damageText}{bonusText}{result}");
        }
        // 범위 공격은 한 번에 합산 저장하여 저장 중 중복 보상 누락을 막습니다.
    }

    private void TriggerRelics(EffectTrigger trigger, Action<double> bonusDamage)
    {
        double restored = 0;
        relicEffects.Trigger(trigger, value =>
        {
            double before = PlayerHealth.Current;
            PlayerHealth.Heal(value);
            restored = PlayerHealth.Current - before;
            HealthChanged?.Invoke(PlayerHealth.Current, PlayerHealth.Maximum);
        }, bonusDamage, (definition, value) =>
        {
            string effect = definition.effectId == "heal" ? $"HP +{restored:0.#}" : $"추가 피해 +{value:0.#}";
            RelicMessageChanged?.Invoke($"{definition.displayName} 발동 · {effect}");
        });
    }

    private void ResolveStats()
    {
        var owned = data.characters.Find(x => x != null && x.characterId == data.loadout.characterId);
        int level = owned == null ? 0 : owned.level;
        if (PlayerHealth != null && selectedId == data.loadout.characterId && selectedLevel == level && selectedStars == (owned == null ? 0 : owned.ascension) && selectedSkills == CatSkillTree.Signature(owned))
            return;
        selectedId = data.loadout.characterId;
        skillOwner = owned;
        ResetSkillAttacks();
        selectedLevel = level;
        selectedSkills = CatSkillTree.Signature(owned);
        selectedStars = owned == null ? 0 : owned.ascension;
        cooldown = 0;
        valid = false;
        var definition = catalog.FindCharacter(selectedId);
        attackBehavior = CharacterAttackFactory.Create(definition == null ? null : definition.attackId);
        Range = definition != null && definition.attackRange > 0 && !float.IsInfinity(definition.attackRange) ? definition.attackRange : 1.7f;
        Range *= CatSkillTree.Range(owned);
        double maxHp = 100;
        defense = 0;
        if (owned != null && definition != null && definition.baseStats != null && definition.statsPerLevel != null)
        {
            int growth = Math.Max(0, level - 1);
            maxHp = definition.baseStats.maxHealth + (double)definition.statsPerLevel.maxHealth * growth;
            defense = definition.baseStats.defense + (double)definition.statsPerLevel.defense * growth;
        }
        maxHp *= CatProgression.StarMultiplier(selectedStars) * CatSkillTree.Health(owned);
        defense = defense * CatProgression.StarMultiplier(selectedStars) + CatSkillTree.Defense(owned);
        if (double.IsNaN(maxHp) || double.IsInfinity(maxHp) || maxHp <= 0)
            maxHp = 100;
        if (double.IsNaN(defense) || double.IsInfinity(defense))
            defense = 0;
        double ratio = PlayerHealth == null ? 1 : PlayerHealth.Current / PlayerHealth.Maximum;
        PlayerHealth = new CombatHealth(maxHp);
        PlayerHealth.Damage(maxHp * (1 - ratio));
        HealthChanged?.Invoke(PlayerHealth.Current, PlayerHealth.Maximum);
        if (owned != null && definition != null && attackBehavior != null && definition.baseStats != null && definition.statsPerLevel != null)
        {
            int growth = Math.Max(0, level - 1);
            attack = definition.baseStats.attack + (double)definition.statsPerLevel.attack * growth;
            attack *= CatProgression.StarMultiplier(selectedStars) * CatSkillTree.Attack(owned);
            speed = definition.baseStats.attackSpeed + (double)definition.statsPerLevel.attackSpeed * growth;
            speed *= CatSkillTree.Speed(owned);
            valid = attack > 0 && speed > 0 && !double.IsNaN(attack) && !double.IsInfinity(attack) && !double.IsNaN(speed) && !double.IsInfinity(speed);
        }
        MessageChanged?.Invoke(valid ? "자동 공격 중" : "공격 설정이 없습니다. 캐릭터의 Attack Id를 확인해주세요.");
    }

    private Vector3 SpawnPosition()
    {
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < .1f)
                direction = Vector2.right;
            Vector3 position = player.position + new Vector3(direction.x, 0, direction.y) * UnityEngine.Random.Range(SpawnDistance, SpawnDistance + 3f);
            if (Mathf.Abs(position.x) > 53 || Mathf.Abs(position.z) > 53) continue;
            if (Navigation == null || Navigation.IsFree(position))
                return position;
        }
        // 무작위 위치가 막혔으면 먼 거리부터 안쪽으로 빈 지점을 찾습니다.
        for (float radius = SpawnDistance; radius >= 3; radius -= 2)
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                var position = player.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                if (Mathf.Abs(position.x) > 53 || Mathf.Abs(position.z) > 53) continue;
                if (Navigation == null || Navigation.IsFree(position)) return position;
            }
        return player.position;
    }
}
