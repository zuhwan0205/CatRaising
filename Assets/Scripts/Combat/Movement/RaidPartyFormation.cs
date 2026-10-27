using System.Collections.Generic;
using UnityEngine;

// 네 고양이가 하나의 이동 중심을 공유해 장애물에서 대형이 벌어지지 않게 합니다.
[DefaultExecutionOrder(100)]
public sealed class RaidPartyFormation : MonoBehaviour
{
    public CatFieldController Field;
    public CatMovementController Movement;
    [System.NonSerialized] public FieldNavigation Navigation;
    public readonly List<Transform> Members = new List<Transform>();
    private static readonly Vector3[] Offsets =
    {
        new Vector3(-.65f, 0, .80f), new Vector3(.65f, 0, .80f),
        new Vector3(-.65f, 0, -.80f), new Vector3(.65f, 0, -.80f)
    };
    private Vector3 forward = Vector3.forward;

    private void LateUpdate()
    {
        if (Time.timeScale == 0) return;
        bool running = Field != null && Field.RaidRunning;
        Movement.enabled = running;
        if (!running) { Movement.StickInput = Vector2.zero; return; }
        // 대형 전체는 살아있는 파티원 중 가장 느린 이동 보너스에 맞춥니다.
        float multiplier = float.MaxValue;
        foreach (var member in Members)
        {
            if (member == null || !member.gameObject.activeSelf) continue;
            var combat = member.GetComponent<FieldCombatController>();
            if (combat != null) multiplier = Mathf.Min(multiplier, combat.SkillMovementMultiplier);
        }
        Movement.Speed = 3 * (multiplier == float.MaxValue ? 1 : multiplier);
        if (Field.RaidBoss != null)
        {
            Vector3 towardBoss = Field.RaidBoss.transform.position - transform.position;
            towardBoss.y = 0;
            if (towardBoss.sqrMagnitude > .01f) forward = towardBoss.normalized;
        }
        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        for (int i = 0; i < Members.Count; i++)
        {
            var member = Members[i];
            if (member == null || !member.gameObject.activeSelf) continue;
            Vector3 slot = Offsets[i % Offsets.Length];
            Vector3 offset = right * slot.x + forward * slot.z;
            Vector3 current = member.position;
            Vector3 destination = transform.position + offset;
            destination.x = Mathf.Clamp(destination.x, -Movement.FieldHalfSize, Movement.FieldHalfSize);
            destination.z = Mathf.Clamp(destination.z, -Movement.FieldHalfSize, Movement.FieldHalfSize);
            if (Navigation != null && !Navigation.Clear(current, destination))
                destination = Navigation.Waypoint(current, destination);
            // 중심 위치로 재배치하지 않고 현재 위치부터 이동합니다.
            // 장애물 통과 후에는 중심보다 조금 빠르게 움직여 대형에 복귀합니다.
            Vector3 delta = Vector3.ClampMagnitude(destination - current,
                Movement.Speed * 1.5f * Mathf.Min(Time.deltaTime, .1f));
            member.position = Navigation == null ? current + delta : Navigation.Move(current, delta);
        }
    }
}
