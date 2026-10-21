using System;
using UnityEngine;

// 고정 슬롯을 공유하되 실제 오브젝트는 필요할 때 조금씩 준비합니다.
public sealed class FieldEnemyPool : MonoBehaviour
{
    public FieldEnemy[] Enemies { get; private set; }
    public Transform[] Targets { get; private set; }
    private Func<int, FieldEnemy> factory;
    private int count;

    public void Initialize(Func<int, FieldEnemy> create, int capacity)
    {
        factory = create;
        Enemies = new FieldEnemy[capacity];
        Targets = new Transform[capacity];
    }

    public void Grow(int desired, int budget = 2)
    {
        while (count < Mathf.Min(desired, Enemies.Length) && budget-- > 0)
        {
            var enemy = factory(count);
            if (count >= FieldCombatController.InitialMonsterCount)
            {
                enemy.ConfigureMonster(KindForSlot(count));
            }
            enemy.gameObject.SetActive(false);
            Enemies[count] = enemy;
            Targets[count++] = enemy.transform;
        }
    }

    public void Release(FieldEnemy enemy)
    {
        enemy.gameObject.SetActive(false);
    }

    public static int KindForSlot(int index)
    {
        if (index < FieldCombatController.InitialMonsterCount) return 0;
        int step = (index - FieldCombatController.InitialMonsterCount) / 10 + 1;
        return step <= 6 ? Mathf.Min(3, step / 2) : step % 4;
    }
}
