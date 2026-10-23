using System.Collections.Generic;
using UnityEngine;

public partial class CatFieldController
{
    private int activeDungeonStage;
    private readonly List<FieldEnemy> dungeonEnemies = new List<FieldEnemy>();
    public int DungeonWave { get; private set; }
    public int DungeonEnemiesRemaining
    {
        get
        {
            int count = 0;
            foreach (var enemy in dungeonEnemies) if (enemy != null && enemy.Alive) count++;
            return count;
        }
    }
    public float DungeonWaveCountdown { get; private set; }

    private void SetupDungeonWaves(int stage)
    {
        activeDungeonStage = stage;
        DungeonWave = 1;
        DungeonWaveCountdown = 0;
        dungeonEnemies.Add(RaidBoss);
        int count = DungeonStageRules.EnemyCount(stage, DungeonStageRules.WaveCount);
        for (int i = 1; i < count; i++)
        {
            var obj = new GameObject("Dungeon Monster " + i);
            obj.transform.SetParent(raidRoot.transform, false);
            var billboard = Billboard("Monster Artwork", obj.transform);
            var art = Paint("Monster", billboard, Vector2.zero, Vector2.one, Color.white, 0);
            var enemy = obj.AddComponent<FieldEnemy>();
            enemy.Initialize(art, BillboardHealth(billboard), 10);
            dungeonEnemies.Add(enemy);
        }
        SpawnDungeonWave();
    }

    private void SpawnDungeonWave()
    {
        int count = DungeonStageRules.EnemyCount(activeDungeonStage, DungeonWave);
        for (int i = 0; i < dungeonEnemies.Count; i++)
        {
            var enemy = dungeonEnemies[i];
            if (i >= count) { enemy.gameObject.SetActive(false); continue; }
            int kind = DungeonStageRules.Kind(activeDungeonStage, DungeonWave, i);
            enemy.ConfigureRaidBoss(kind, DungeonStageRules.Health(activeDungeonStage, DungeonWave, i));
            var billboard = enemy.GetComponentInChildren<FieldBillboard>(true);
            billboard.transform.localScale = Vector3.one * (DungeonStageRules.IsBoss(activeDungeonStage, DungeonWave, i) ? 2 : 1.2f);
            float angle = Mathf.PI * 2 * i / count;
            enemy.Respawn(FindRaidPosition(new Vector3(Mathf.Cos(angle) * 9, 0, Mathf.Sin(angle) * 9)));
        }
    }

    private void FinishDungeon(bool won)
    {
        RaidRunning = false;
        foreach (var fighter in raidFighters)
        {
            fighter.Blocked = true;
            fighter.GetComponent<CatMovementController>().enabled = false;
        }
        RaidFinished?.Invoke(won);
    }

    private void TickDungeonWaves()
    {
        RaidSeconds = Mathf.Max(0, RaidSeconds - Time.deltaTime);
        bool anyLiving = raidFighters.Exists(fighter => fighter != null && fighter.gameObject.activeSelf);
        if (!anyLiving || RaidSeconds <= 0) { FinishDungeon(false); return; }
        if (DungeonEnemiesRemaining == 0)
        {
            if (DungeonWave == DungeonStageRules.WaveCount) { FinishDungeon(true); return; }
            if (DungeonWaveCountdown <= 0) DungeonWaveCountdown = 3;
            DungeonWaveCountdown -= Time.deltaTime;
            if (DungeonWaveCountdown <= 0)
            {
                DungeonWave++;
                SpawnDungeonWave();
            }
            return;
        }
        foreach (var enemy in dungeonEnemies)
        {
            if (!enemy.Alive) continue;
            enemy.StepVisual(Time.deltaTime);
            Transform nearest = null;
            Vector3 start = enemy.transform.position;
            foreach (var fighter in raidFighters)
                if (fighter != null && fighter.gameObject.activeSelf &&
                    (nearest == null || (fighter.transform.position - start).sqrMagnitude < (nearest.position - start).sqrMagnitude))
                    nearest = fighter.transform;
            if (nearest == null || (nearest.position - start).sqrMagnitude <= .8f) continue;
            Vector3 goal = enemy.NextWaypoint(Combat.Navigation, nearest.position, staggered: true);
            Vector3 offset = goal - start;
            Vector3 step = offset.normalized * Mathf.Min(offset.magnitude, enemy.MoveSpeed * enemy.MovementMultiplier * Time.deltaTime);
            enemy.transform.position = Combat.Navigation == null ? start + step : Combat.Navigation.Move(start, step);
        }
    }
}
