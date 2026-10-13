using System.Collections.Generic;
using UnityEngine;

public partial class CatFieldController
{
    private readonly List<FieldEnemy> raidAdds = new List<FieldEnemy>();
    private Transform skillMarker;
    private readonly List<SpriteRenderer> skillPixels = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> skillFill = new List<SpriteRenderer>();
    private float skillCooldown, summonCooldown, skillWindup, skillFlash;
    private Vector3 skillCenter;
    private float skillRadius;
    private int skillKind;
    public string RaidSkillMessage { get; private set; }

    private void SetupRaidSkills(int kind)
    {
        skillKind = kind;
        skillCooldown = 4;
        summonCooldown = 7;
        skillWindup = skillFlash = 0;
        RaidSkillMessage = "보스 처치 시 클리어";
        skillPixels.Clear();
        skillFill.Clear();
        skillMarker = new GameObject("Boss Area Warning").transform;
        skillMarker.SetParent(raidRoot.transform, false);
        // 가로 띠를 채워 추가 텍스처 없이 원형 판정 범위를 표시합니다.
        for (int i = 0; i < 40; i++)
        {
            float z = -1 + (i + .5f) * .05f;
            float width = 2 * Mathf.Sqrt(1 - z * z);
            var fill = Paint("Danger Area Fill", skillMarker, Vector2.zero, new Vector2(width, .05f), new Color(1, .05f, .03f, .35f), 0);
            fill.transform.localPosition = new Vector3(0, .08f, z);
            fill.transform.localRotation = Quaternion.Euler(90, 0, 0);
            skillFill.Add(fill);
        }
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2 / 64;
            var pixel = Paint("Warning Pixel", skillMarker, Vector2.zero, new Vector2(.12f, .12f), Color.yellow, 1);
            pixel.transform.localPosition = new Vector3(Mathf.Cos(angle) * .94f, .09f, Mathf.Sin(angle) * .94f);
            pixel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            skillPixels.Add(pixel);
        }
        skillMarker.gameObject.SetActive(false);
        for (int i = 0; i < 6; i++)
        {
            var obj = new GameObject("Raid Summon " + i);
            obj.transform.SetParent(raidRoot.transform, false);
            var billboard = Billboard("Summon Artwork", obj.transform);
            billboard.localScale = Vector3.one * 1.2f;
            var art = Paint("Monster", billboard, Vector2.zero, Vector2.one, Color.white, 0);
            var enemy = obj.AddComponent<FieldEnemy>();
            enemy.Initialize(art, BillboardHealth(billboard), 10);
            enemy.ConfigureRaidBoss(kind, 20 + kind * 10);
            obj.SetActive(false);
            raidAdds.Add(enemy);
        }
    }

    private void TickRaidSkills(List<FieldCombatController> living)
    {
        float delta = Time.deltaTime;
        summonCooldown -= delta;
        if (summonCooldown <= 0)
        {
            summonCooldown = 9;
            int spawned = 0;
            foreach (var enemy in raidAdds)
            {
                if (enemy.Alive) continue;
                float angle = spawned * Mathf.PI * 2 / 3 + RaidSeconds;
                var desired = RaidBoss.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 3;
                desired.x = Mathf.Clamp(desired.x, -16, 16);
                desired.z = Mathf.Clamp(desired.z, -16, 16);
                enemy.Respawn(FindRaidPosition(desired));
                if (++spawned == 3) break;
            }
            if (spawned > 0) RaidSkillMessage = $"몬스터 {spawned}마리 소환!";
        }
        foreach (var enemy in raidAdds)
        {
            if (!enemy.Alive) continue;
            enemy.StepVisual(delta);
            Transform nearest = null;
            float distance = float.MaxValue;
            foreach (var fighter in living)
            {
                if (!fighter.gameObject.activeSelf) continue;
                float sqr = (fighter.transform.position - enemy.transform.position).sqrMagnitude;
                if (sqr < distance) { distance = sqr; nearest = fighter.transform; }
            }
            if (nearest == null || distance <= .8f) continue;
            Vector3 start = enemy.transform.position;
            Vector3 goal = EncounterNavigation == null ? nearest.position : EncounterNavigation.Waypoint(start, nearest.position);
            Vector3 offset = goal - start;
            Vector3 step = offset.normalized * Mathf.Min(offset.magnitude, enemy.MoveSpeed * enemy.MovementMultiplier * delta);
            enemy.transform.position = EncounterNavigation == null ? start + step : EncounterNavigation.Move(start, step);
        }
        if (skillWindup > 0)
        {
            skillWindup = Mathf.Max(0, skillWindup - delta);
            foreach (var pixel in skillPixels) pixel.color = Color.Lerp(new Color(1, .2f, .1f, .5f), Color.yellow, Mathf.PingPong(skillWindup * 4, 1));
            foreach (var fill in skillFill) fill.color = new Color(1, .05f, .03f, .32f + Mathf.PingPong(skillWindup * .6f, .20f));
            RaidSkillMessage = $"⚠ 광역 공격 {skillWindup:0.0}초 · 붉은 영역 위험!";
            if (skillWindup > 0) return;
            bool scheduled = RaidBoss.ScheduleRaidAttack(skillCenter, () =>
            {
                if (!RaidRunning || RaidBoss == null || !RaidBoss.Alive) return;
                foreach (var fighter in raidFighters)
                {
                    if (fighter == null || !fighter.gameObject.activeSelf) continue;
                    Vector3 offset = fighter.transform.position - skillCenter;
                    offset.y = 0;
                    if (offset.sqrMagnitude <= skillRadius * skillRadius) fighter.ReceiveRaidSkill(24 + skillKind * 12);
                }
                foreach (var fill in skillFill) fill.color = new Color(1, .5f, .1f, .55f);
            });
            if (!scheduled) { skillWindup = .05f; return; }
            skillFlash = RaidBossVisual.AttackDuration;
            RaidSkillMessage = new[] { "먼지 충격파!", "생쥐 대장 회전 타격!", "털실 폭발!" }[skillKind];
            foreach (var pixel in skillPixels) pixel.color = new[] { new Color(.75f, .6f, 1), new Color(1, .75f, .2f), new Color(.3f, 1, .8f) }[skillKind];
        }
        if (skillFlash > 0)
        {
            skillFlash = Mathf.Max(0, skillFlash - delta);
            // 경고와 타격 영역을 같은 크기로 유지합니다.
            skillMarker.localScale = Vector3.one * skillRadius;
            if (skillFlash <= 0) skillMarker.gameObject.SetActive(false);
            return;
        }
        skillCooldown -= delta;
        if (skillCooldown > 0) return;
        skillCooldown = 6;
        skillWindup = 1.5f;
        skillRadius = skillKind == 0 ? 3.5f : skillKind == 1 ? 2.8f : 3;
        skillCenter = skillKind == 2 ? living[0].transform.position : RaidBoss.transform.position;
        skillMarker.position = skillCenter;
        skillMarker.localScale = Vector3.one * skillRadius;
        skillMarker.gameObject.SetActive(true);
        foreach (var fill in skillFill) fill.color = new Color(1, .05f, .03f, .4f);
        foreach (var pixel in skillPixels) pixel.color = Color.yellow;
        RaidSkillMessage = "광역 공격 준비! 표시된 범위에 피해를 줍니다.";
    }
}
