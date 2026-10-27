using System;
using System.Collections.Generic;
using UnityEngine;

public partial class FieldCombatController
{
    private const float EnemyCellSize = 4f;
    private readonly System.Collections.Generic.Dictionary<Vector2Int, List<int>> enemyCells = new System.Collections.Generic.Dictionary<Vector2Int, List<int>>();
    private readonly List<int> nearbyIndices = new List<int>(200);
    private readonly List<FieldEnemy> basicCandidates = new List<FieldEnemy>(200);
    private readonly List<FieldEnemy> arrowImpactCandidates = new List<FieldEnemy>(200);
    private readonly List<FieldEnemy> arrowCandidates = new List<FieldEnemy>(200);
    private readonly List<FieldEnemy> lightningCandidates = new List<FieldEnemy>(200);
    private readonly List<FieldEnemy> areaCandidates = new List<FieldEnemy>(200);
    private int enemyCellsFrame = -1;

    private void NearbyEnemies(Vector3 center, float radius, List<FieldEnemy> result)
    {
        if (enemyCellsFrame != Time.frameCount)
        {
            foreach (var bucket in enemyCells.Values) bucket.Clear();
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.Alive) continue;
                var position = enemy.transform.position;
                var key = new Vector2Int(Mathf.FloorToInt(position.x / EnemyCellSize), Mathf.FloorToInt(position.z / EnemyCellSize));
                if (!enemyCells.TryGetValue(key, out var bucket)) enemyCells[key] = bucket = new List<int>();
                bucket.Add(i);
            }
            enemyCellsFrame = Time.frameCount;
        }
        nearbyIndices.Clear();
        for (int z = Mathf.FloorToInt((center.z - radius) / EnemyCellSize); z <= Mathf.FloorToInt((center.z + radius) / EnemyCellSize); z++)
        for (int x = Mathf.FloorToInt((center.x - radius) / EnemyCellSize); x <= Mathf.FloorToInt((center.x + radius) / EnemyCellSize); x++)
            if (enemyCells.TryGetValue(new Vector2Int(x, z), out var bucket)) nearbyIndices.AddRange(bucket);
        // 동시 명중 순서를 기존 몬스터 배열과 동일하게 유지합니다.
        nearbyIndices.Sort();
        result.Clear();
        foreach (int index in nearbyIndices) result.Add(enemies[index]);
    }

    private OwnedCharacter skillOwner;
    private FieldEnemy focusEnemy;
    private CombatHealth focusLife;
    private int focusStacks;
    private float potionCooldown;
    private long pendingSkillGold;
    private readonly List<SkillArrow> skillArrows = new List<SkillArrow>();
    private Material skillMaterial;
    public int MagnetRank => CatSkillTree.Rank(skillOwner, CatSkillTree.Magnet);
    public OwnedCharacter SkillCharacter => skillOwner;
    public float SkillMovementMultiplier => CatSkillTree.Movement(skillOwner);
    private int Skill(int node) => CatSkillTree.Rank(skillOwner, node);
    private bool UsesSkillArrows => Skill(CatSkillTree.SteelArrow) > 0 || Skill(CatSkillTree.DoubleArrow) > 0;

    private void LateUpdate()
    {
        // 직접 공격·투사체·지속 피해 보상을 같은 프레임에 모아 한 번만 저장합니다.
        long gold = pendingSkillGold;
        pendingSkillGold = 0;
        if (gold > 0) GoldEarned?.Invoke(gold);
    }

    private void UpdateArrowFocus(FieldEnemy target, bool attacking)
    {
        if (target != focusEnemy || target == null || target.Health != focusLife)
        {
            focusEnemy = target;
            focusLife = target == null ? null : target.Health;
            focusStacks = 0;
        }
        if (attacking) focusStacks = Math.Min(Skill(CatSkillTree.ArrowStacks), focusStacks + 1);
    }

    private void ResetSkillAttacks()
    {
        focusEnemy = null;
        focusLife = null;
        focusStacks = 0;
        foreach (var arrow in skillArrows) arrow.Stop();
    }

    private void TickCombatSkills(float delta)
    {
        potionCooldown = Mathf.Max(0, potionCooldown - delta);
        if (PlayerHealth.Dead) { ResetSkillAttacks(); return; }
        TrySkillPotion();
        foreach (var arrow in skillArrows) if (arrow.active) TickArrow(arrow, delta);
    }

    private void TrySkillPotion()
    {
        int rank = Skill(CatSkillTree.Potion);
        if (rank == 0 || potionCooldown > 0 || PlayerHealth == null || PlayerHealth.Dead ||
            PlayerHealth.Current >= PlayerHealth.Maximum * .5) return;
        potionCooldown = 60;
        HealSkill(PlayerHealth.Maximum * rank * .10);
        RelicMessageChanged?.Invoke("회복 물약 사용 · 재사용 60초");
    }

    private void HealSkill(double amount)
    {
        if (PlayerHealth == null || PlayerHealth.Dead || amount <= 0) return;
        PlayerHealth.Heal(amount);
        HealthChanged?.Invoke(PlayerHealth.Current, PlayerHealth.Maximum);
    }

    private void ExpandMaceTargets(FieldEnemy closest)
    {
        int rank = Skill(CatSkillTree.Mace);
        if (rank == 0) return;
        float radius = rank * .4f;
        NearbyEnemies(closest.transform.position, radius, areaCandidates);
        foreach (var target in areaCandidates)
            if (target != null && target.Alive && !attackTargets.Contains(target) &&
                FlatDistanceSquared(target.transform.position, closest.transform.position) <= radius * radius)
                attackTargets.Add(target);
    }

    // DOT는 방어 무시, 일반 공격만 명중 효과를 발동하여 재귀적인 번개/흡혈을 방지합니다.
    public void DealSkillDamage(FieldEnemy target, double damage, bool trueDamage = false, bool basicHit = false)
    {
        if (target == null || !target.Alive || damage <= 0) return;
        var position = target.transform.position;
        double before = target.Health.Current;
        bool killed = target.Hit(damage, trueDamage);
        double actual = before - target.Health.Current;
        DamageDealt?.Invoke(position, actual);
        if (basicHit)
        {
            HealSkill(actual * Skill(CatSkillTree.Vampire) * .02);
            if (!killed)
            {
                if (selectedId == "cat_shadow") target.ApplyIceSlow();
                if (selectedId == "cat_moon") target.ApplyArmorBreak();
                int poison = Skill(CatSkillTree.Poison), burn = Skill(CatSkillTree.Burn);
                if (poison > 0 || burn > 0)
                {
                    var status = target.GetComponent<FieldSkillStatus>();
                    if (status == null) status = target.gameObject.AddComponent<FieldSkillStatus>();
                    status.Apply(this, target, poison, burn);
                }
            }
        }
        if (killed)
        {
            if (target == focusEnemy) { focusEnemy = null; focusLife = null; focusStacks = 0; }
            if (target == LockedTarget) LockedTarget = null;
            target.RespawnRemaining = RespawnDelay;
            if (!RaidMode && Pool != null) Pool.Release(target);
            killsThisFrame++;
            if (!RaidMode) ExperienceDropped?.Invoke(position, CatProgression.DropExperience(target.Kind));
            pendingSkillGold += target.GoldReward;
            EnemyGoldDropped?.Invoke(position, target.GoldReward);
            TriggerRelics(EffectTrigger.OnKill, null);
        }
        if (basicHit && Skill(CatSkillTree.Lightning) > 0)
        {
            NearbyEnemies(position, 2f, lightningCandidates);
            foreach (var nearby in lightningCandidates)
            {
                if (nearby == null || nearby == target || !nearby.Alive ||
                    FlatDistanceSquared(position, nearby.transform.position) > 4 ||
                    (Navigation != null && !Navigation.Clear(position, nearby.transform.position))) continue;
                SkillFlash(position, nearby.transform.position, new Color(1, .9f, .2f));
                DealSkillDamage(nearby, attack * AllyAttackMultiplier * Skill(CatSkillTree.Lightning) * .2);
            }
        }
    }

    private static float FlatDistanceSquared(Vector3 a, Vector3 b)
    {
        var d = a - b;
        return d.x * d.x + d.z * d.z;
    }

    private sealed class SkillArrow
    {
        public bool active, piercing;
        public Vector3 position, direction;
        public float remaining, scale;
        public double damage;
        public int explosion;
        public LineRenderer visual;
        public readonly HashSet<FieldEnemy> hit = new HashSet<FieldEnemy>();
        public void Stop() { active = false; if (visual != null) visual.gameObject.SetActive(false); }
    }

    private LineRenderer SkillLineRenderer(string name, Color color, float width)
    {
        if (skillMaterial == null) skillMaterial = new Material(Shader.Find("Sprites/Default"));
        var obj = new GameObject(name);
        obj.layer = 2;
        obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = skillMaterial;
        line.useWorldSpace = true;
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.sortingOrder = 11000;
        return line;
    }

    private void LaunchSkillArrows(FieldEnemy target, double damage)
    {
        int count = Skill(CatSkillTree.DoubleArrow) > 0 ? 2 : 1;
        Vector3 direction = target.transform.position - player.position;
        direction.y = 0;
        if (direction.sqrMagnitude < .001f) direction = Vector3.forward;
        direction.Normalize();
        Vector3 side = new Vector3(direction.z, 0, -direction.x);
        for (int i = 0; i < count; i++)
        {
            var arrow = skillArrows.Find(x => !x.active);
            if (arrow == null)
            {
                arrow = new SkillArrow { visual = SkillLineRenderer("Skill Arrow", new Color(.85f, .95f, 1), .07f) };
                skillArrows.Add(arrow);
            }
            arrow.active = true;
            arrow.piercing = Skill(CatSkillTree.SteelArrow) > 0;
            arrow.scale = 1 + Skill(CatSkillTree.LargeArrow) * .1f;
            arrow.remaining = Range;
            arrow.explosion = Skill(CatSkillTree.Explosion);
            arrow.position = player.position + side * (count == 2 ? (i == 0 ? -.16f : .16f) : 0);
            arrow.direction = direction;
            arrow.damage = damage * (arrow.piercing ? .6 + Skill(CatSkillTree.SteelArrow) * .08 : 1);
            if (i == 1) arrow.damage *= .4 + Skill(CatSkillTree.DoubleArrow) * .12;
            arrow.hit.Clear();
            arrow.visual.gameObject.SetActive(true);
            DrawArrow(arrow);
        }
    }

    private void TickArrow(SkillArrow arrow, float delta)
    {
        Vector3 start = arrow.position;
        float travel = Mathf.Min(arrow.remaining, 14 * delta);
        Vector3 end = start + arrow.direction * travel;
        if (Navigation != null && !Navigation.Clear(start, end)) { arrow.Stop(); return; }
        FieldEnemy first = null;
        float firstAlong = float.MaxValue;
        NearbyEnemies((start + end) * .5f, travel * .5f + .40f + .15f * arrow.scale, arrowCandidates);
        foreach (var target in arrowCandidates)
        {
            if (target == null || !target.Alive || arrow.hit.Contains(target)) continue;
            Vector3 offset = target.transform.position - start;
            offset.y = 0;
            float along = Mathf.Clamp(Vector3.Dot(offset, arrow.direction), 0, travel);
            float radius = .40f + .15f * arrow.scale;
            if ((offset - arrow.direction * along).sqrMagnitude > radius * radius) continue;
            if (arrow.piercing) HitArrow(arrow, target);
            else if (along < firstAlong) { first = target; firstAlong = along; }
        }
        if (first != null) { HitArrow(arrow, first); arrow.Stop(); return; }
        arrow.position = end;
        arrow.remaining -= travel;
        DrawArrow(arrow);
        if (arrow.remaining > .001f) return;
        if (arrow.explosion > 0)
        {
            float radius = 1 + arrow.explosion * .2f;
            SkillExplosion(end, radius);
            NearbyEnemies(end, radius, areaCandidates);
            foreach (var target in areaCandidates)
                if (target != null && target.Alive && FlatDistanceSquared(end, target.transform.position) <= radius * radius &&
                    (Navigation == null || Navigation.Clear(end, target.transform.position)))
                    DealSkillDamage(target, arrow.damage * arrow.explosion * .2);
        }
        arrow.Stop();
    }

    private void HitArrow(SkillArrow arrow, FieldEnemy target)
    {
        arrow.hit.Add(target);
        double bonus = 0;
        TriggerRelics(EffectTrigger.OnHit, value => bonus += value);
        // 투사체 스킬을 배운 뒤에도 얼음/마법의 폭발, 용의 불길 특성을 유지합니다.
        if (attackBehavior is TargetAreaAttack || attackBehavior is BreathAreaAttack)
        {
            bool splash = attackBehavior is TargetAreaAttack;
            Vector3 origin = player.position, center = target.transform.position;
            NearbyEnemies(splash ? center : origin, splash ? CharacterCombatRules.SplashRadius : Range, arrowImpactCandidates);
            AttackPerformed?.Invoke(origin, center);
            foreach (var nearby in arrowImpactCandidates)
                if (nearby != null && nearby.Alive && CharacterCombatRules.Hits(splash ? "target_area" : "breath_area",
                    CharacterCombatRules.Flat(origin), CharacterCombatRules.Flat(center), CharacterCombatRules.Flat(nearby.transform.position), Range) &&
                    (Navigation == null || Navigation.Clear(origin, nearby.transform.position)))
                    DealSkillDamage(nearby, arrow.damage + bonus, false, true);
        }
        else DealSkillDamage(target, arrow.damage + bonus, false, true);
        // 기존 무기 비행 이펙트를 중복 생성하지 않고 실제 명중 위치만 강조합니다.
        SkillFlash(target.transform.position, target.transform.position + Vector3.right * .25f, Color.white);
    }

    private void DrawArrow(SkillArrow arrow)
    {
        Vector3 tip = arrow.position + Vector3.up * .45f;
        Vector3 forward = arrow.direction * arrow.scale;
        Vector3 side = new Vector3(forward.z, 0, -forward.x);
        arrow.visual.widthMultiplier = .07f * arrow.scale;
        arrow.visual.positionCount = 5;
        arrow.visual.SetPosition(0, tip - forward * .55f);
        arrow.visual.SetPosition(1, tip);
        arrow.visual.SetPosition(2, tip - forward * .20f + side * .14f);
        arrow.visual.SetPosition(3, tip);
        arrow.visual.SetPosition(4, tip - forward * .20f - side * .14f);
    }

    private sealed class SkillFlashVisual
    {
        public LineRenderer line;
        public float remaining;
    }
    private readonly List<SkillFlashVisual> skillFlashes = new List<SkillFlashVisual>();
    private LineRenderer AcquireSkillFlash(Color color, float width, float duration)
    {
        var effect = skillFlashes.Find(x => x.remaining <= 0);
        if (effect == null)
        {
            if (skillFlashes.Count >= 32) return null;
            effect = new SkillFlashVisual { line = SkillLineRenderer("Skill Impact", color, width) };
            skillFlashes.Add(effect);
        }
        effect.remaining = duration;
        effect.line.gameObject.SetActive(true);
        effect.line.startColor = effect.line.endColor = color;
        effect.line.startWidth = effect.line.endWidth = width;
        return effect.line;
    }
    private void SkillFlash(Vector3 a, Vector3 b, Color color)
    {
        var line = AcquireSkillFlash(color, .09f, .15f);
        if (line == null) return;
        line.positionCount = 3;
        line.SetPosition(0, a + Vector3.up * .5f);
        line.SetPosition(1, (a + b) * .5f + Vector3.up * .65f + Vector3.right * .12f);
        line.SetPosition(2, b + Vector3.up * .5f);
    }
    private void SkillExplosion(Vector3 center, float radius)
    {
        var line = AcquireSkillFlash(new Color(1, .5f, .1f), .12f, .22f);
        if (line == null) return;
        line.positionCount = 25;
        for (int i = 0; i < 25; i++)
        {
            float angle = i * Mathf.PI * 2 / 24;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, .25f, Mathf.Sin(angle) * radius));
        }
    }
    private void FixedUpdate()
    {
        for (int i = skillFlashes.Count - 1; i >= 0; i--)
        {
            var item = skillFlashes[i];
            if (item.remaining <= 0) continue;
            item.remaining -= Time.fixedDeltaTime;
            if (item.remaining <= 0) item.line.gameObject.SetActive(false);
        }
    }
    private void OnDisable()
    {
        ResetSkillAttacks();
        foreach (var item in skillFlashes)
        {
            item.remaining = 0;
            if (item.line != null) item.line.gameObject.SetActive(false);
        }
    }
    private void OnDestroy()
    {
        if (skillMaterial != null) Destroy(skillMaterial);
    }
}
