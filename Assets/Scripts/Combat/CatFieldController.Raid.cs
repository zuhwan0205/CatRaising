using System;
using System.Collections.Generic;
using UnityEngine;

public partial class CatFieldController
{
    public event Action<bool> RaidFinished;
    public float RaidSeconds { get; private set; }
    public FieldEnemy RaidBoss { get; private set; }
    public bool RaidRunning { get; private set; }
    private GameObject raidRoot;
    private readonly List<FieldCombatController> raidFighters = new List<FieldCombatController>();
    private readonly List<GameObject> hiddenFieldObjects = new List<GameObject>();
    private Transform previousCameraTarget;

    public void BeginRaid(int index, IList<OwnedCharacter> team, BackendUserData data, CatGameCatalog catalog)
    {
        // 기존 편성 검증을 사용하되 전투 계산은 필드 컨트롤러가 담당합니다.
        _ = new RaidBattle(index, data.progress.accountLevel, team, data, catalog);
        BeginEncounter(index, team, data, catalog, new double[] { 350, 1000, 2500 }[index], 1, true);
    }

    public void BeginDungeon(int stage, IList<OwnedCharacter> team, BackendUserData data, CatGameCatalog catalog)
    {
        if (stage < 1 || stage > 15 || stage > data.progress.dungeonClearedStage + 1 || team == null || team.Count != 2)
            throw new ArgumentException("던전 입장 조건을 확인해주세요.");
        var ids = new HashSet<string>();
        foreach (var cat in team)
            if (cat == null || !data.characters.Contains(cat) || !ids.Add(cat.characterId) || catalog.FindCharacter(cat.characterId) == null)
                throw new ArgumentException("서로 다른 보유 고양이 2마리를 선택해주세요.");
        BeginEncounter(0, team, data, catalog, 1, 1, false, stage);
    }

    private int encounterWaves;
    private int encounterKind;
    private double encounterHealth;

    private void BeginEncounter(int index, IList<OwnedCharacter> team, BackendUserData data, CatGameCatalog catalog, double health, int waves, bool largeBoss, int dungeonStage = 0)
    {
        EndRaid();
        encounterWaves = waves;
        encounterKind = index;
        encounterHealth = health;
        foreach (Transform child in field.transform)
        {
            if (child.gameObject.activeSelf && (child.GetComponent<FieldEnemy>() != null || child.GetComponent<CatMovementController>() != null))
            {
                hiddenFieldObjects.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }
        raidRoot = new GameObject("Raid Field Actors");
        raidRoot.transform.SetParent(field.transform, false);
        var bossObject = new GameObject("Raid Boss");
        bossObject.transform.SetParent(raidRoot.transform, false);
        bossObject.transform.position = FindRaidPosition(new Vector3(0, 0, 3));
        var billboard = Billboard("Boss Artwork", bossObject.transform);
        billboard.localScale = Vector3.one * (largeBoss ? 2 : 1.2f);
        var art = Paint("Boss", billboard, Vector2.zero, Vector2.one, Color.white, 0);
        var bar = BillboardHealth(billboard);
        RaidBoss = bossObject.AddComponent<FieldEnemy>();
        RaidBoss.Initialize(art, bar, 350);
        RaidBoss.ConfigureRaidBoss(index, health);
        if (dungeonStage == 0) RaidBoss.SetRaidArtwork(index);
        var targets = new[] { RaidBoss };
        var targetTransforms = new[] { bossObject.transform };
        if (dungeonStage > 0)
        {
            SetupDungeonWaves(dungeonStage);
            targets = dungeonEnemies.ToArray();
            targetTransforms = dungeonEnemies.ConvertAll(enemy => enemy.transform).ToArray();
        }
        foreach (var owned in team)
        {
            var actorData = new BackendUserData
            {
                characters = data.characters,
                relics = data.relics,
                loadout = new PlayerLoadout { characterId = owned.characterId, relicIds = data.loadout.relicIds }
            };
            var cat = new GameObject("Raid Cat " + owned.characterId);
            cat.transform.SetParent(raidRoot.transform, false);
            cat.transform.position = FindRaidPosition(new Vector3((raidFighters.Count - (team.Count - 1) * .5f) * 2, 0, -4));
            var catArt = Billboard("Cat Artwork", cat.transform);
            catArt.localScale = Vector3.one * 1.2f;
            var fallback = new GameObject("Fallback").transform;
            fallback.SetParent(catArt, false);
            Paint("Body", fallback, Vector2.zero, Vector2.one, Color.yellow, 0);
            var visual = catArt.gameObject.AddComponent<CatCharacterVisual>();
            visual.Build(cat.transform, fieldCamera, actorData, square, fallback);
            var hp = BillboardHealth(catArt);
            var fighter = cat.AddComponent<FieldCombatController>();
            fighter.RaidMode = true;
            fighter.Navigation = Combat.Navigation;
            fighter.Initialize(cat.transform, targets, actorData, catalog);
            var mover = cat.AddComponent<CatMovementController>();
            mover.Navigation = Combat.Navigation;
            mover.Targets = targetTransforms;
            mover.Combat = fighter;
            mover.ManualInputEnabled = false;
            fighter.HealthChanged += (current, maximum) =>
            {
                HealthBarTween.Set(hp, (float)(current / maximum));
                if (current <= 0) cat.SetActive(false);
            };
            fighter.AttackStarted += visual.Attack;
            var effects = cat.AddComponent<FieldCombatEffects>();
            effects.Initialize(fieldCamera, square, visual.ImpactRoot);
            fighter.AttackPerformed += effects.Attack;
            fighter.PlayerHit += effects.Hurt;
            var numbers = cat.AddComponent<FieldDamageNumbers>();
            numbers.Initialize(fieldCamera, fighter);
            raidFighters.Add(fighter);
        }
        var follow = fieldCamera.GetComponent<CatCameraFollow>();
        previousCameraTarget = follow.Target;
        follow.Target = bossObject.transform;
        if (dungeonStage > 0)
        {
            var focus = new GameObject("Dungeon Camera Focus").transform;
            focus.SetParent(raidRoot.transform, false);
            follow.Target = focus;
        }
        follow.Snap();
        RaidSeconds = 60;
        RaidRunning = true;
        SetVisible(true);
    }

    private Transform BillboardHealth(Transform parent)
    {
        Paint("HP Background", parent, new Vector2(0, 1.4f), new Vector2(1, .08f), Color.black, 4);
        var bar = new GameObject("HP Fill Origin").transform;
        bar.SetParent(parent, false);
        bar.localPosition = new Vector3(-.5f, 1.4f, 0);
        Paint("HP", bar, new Vector2(.5f, 0), new Vector2(1, .08f), Color.green, 5);
        return bar;
    }

    private Vector3 FindRaidPosition(Vector3 desired)
    {
        if (Combat.Navigation == null || Combat.Navigation.IsFree(desired)) return desired;
        for (int radius = 1; radius < 18; radius++)
            for (int step = 0; step < 16; step++)
            {
                float angle = step * Mathf.PI / 8;
                var point = desired + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                if (Mathf.Abs(point.x) < 17 && Mathf.Abs(point.z) < 17 && Combat.Navigation.IsFree(point)) return point;
            }
        return Vector3.zero;
    }

    private void LateUpdate()
    {
        if (!RaidRunning || paused || !focused) return;
        if (activeDungeonStage > 0) { TickDungeonWaves(); return; }
        RaidSeconds = Mathf.Max(0, RaidSeconds - Time.deltaTime);
        RaidBoss.StepVisual(Time.deltaTime);
        var living = raidFighters.FindAll(x => x != null && x.gameObject.activeSelf);
        if (!RaidBoss.Alive && encounterWaves > 1 && RaidSeconds > 0 && living.Count > 0)
        {
            encounterWaves--;
            RaidBoss.ConfigureRaidBoss(encounterKind, encounterHealth);
            RaidBoss.Respawn(FindRaidPosition(new Vector3(0, 0, 3)));
        }
        if (!RaidBoss.Alive || RaidSeconds <= 0 || living.Count == 0)
        {
            bool won = !RaidBoss.Alive && encounterWaves == 1 && RaidSeconds > 0;
            RaidRunning = false;
            foreach (var fighter in raidFighters)
            {
                fighter.Blocked = true;
                fighter.GetComponent<CatMovementController>().enabled = false;
            }
            RaidFinished?.Invoke(won);
            return;
        }
        var nearest = living[0].transform;
        foreach (var fighter in living)
            if ((fighter.transform.position - RaidBoss.transform.position).sqrMagnitude < (nearest.position - RaidBoss.transform.position).sqrMagnitude)
                nearest = fighter.transform;
        Vector3 start = RaidBoss.transform.position;
        Vector3 goal = Combat.Navigation == null ? nearest.position : Combat.Navigation.Waypoint(start, nearest.position);
        Vector3 offset = goal - start;
        if ((nearest.position - start).sqrMagnitude > .8f)
        {
            Vector3 step = offset.normalized * Mathf.Min(offset.magnitude, RaidBoss.MoveSpeed * RaidBoss.MovementMultiplier * Time.deltaTime);
            RaidBoss.transform.position = Combat.Navigation == null ? start + step : Combat.Navigation.Move(start, step);
        }
    }

    public void EndRaid()
    {
        activeDungeonStage = 0;
        dungeonEnemies.Clear();
        RaidRunning = false;
        if (raidRoot != null) { raidRoot.SetActive(false); Destroy(raidRoot); }
        raidRoot = null;
        RaidBoss = null;
        raidFighters.Clear();
        foreach (var obj in hiddenFieldObjects) if (obj != null) obj.SetActive(true);
        hiddenFieldObjects.Clear();
        if (previousCameraTarget != null)
        {
            var follow = fieldCamera.GetComponent<CatCameraFollow>();
            follow.Target = previousCameraTarget;
            follow.Snap();
            previousCameraTarget = null;
        }
    }
}
