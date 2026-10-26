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
    private RaidPartyFormation raidParty;
    private FieldNavigation raidNavigation;
    private readonly List<FieldCombatController> livingRaidFighters = new List<FieldCombatController>(4);
    private FieldNavigation EncounterNavigation => raidNavigation ?? fieldNavigation;

    public void BeginRaid(int index, IList<OwnedCharacter> team, BackendUserData data, CatGameCatalog catalog)
    {
        // 기존 편성 검증을 사용하되 전투 계산은 필드 컨트롤러가 담당합니다.
        RaidBattle.ValidateTeam(index, data.progress.accountLevel, team, data, catalog);
        double health = index == 0 ? 2500 : index == 1 ? 9000 : 24000;
        EndRaid();
        raidNavigation = fieldNavigation?.CreateRaidNavigation();
        raidRoot = new GameObject("Raid Field Actors");
        raidRoot.transform.SetParent(field.transform, false);
        var bossObject = new GameObject("Raid Boss");
        bossObject.transform.SetParent(raidRoot.transform, false);
        bossObject.transform.position = FindRaidPosition(new Vector3(0, 0, 3));
        var billboard = Billboard("Boss Artwork", bossObject.transform);
        billboard.localScale = Vector3.one * 2;
        var art = Paint("Boss", billboard, Vector2.zero, Vector2.one, Color.white, 0);
        var bar = BillboardHealth(billboard);
        RaidBoss = bossObject.AddComponent<FieldEnemy>();
        RaidBoss.Initialize(art, bar, 350);
        RaidBoss.ConfigureRaidBoss(index, health);
        RaidBoss.SetRaidArtwork(index);
        RaidBoss.StrengthenRaidBoss();
        SetupRaidSkills(index);
        var targets = new FieldEnemy[raidAdds.Count + 1];
        var targetTransforms = new Transform[targets.Length];
        targets[0] = RaidBoss;
        raidAdds.CopyTo(targets, 1);
        for (int i = 0; i < targets.Length; i++) targetTransforms[i] = targets[i].transform;
        var center = new GameObject("Raid Party Center");
        center.transform.SetParent(raidRoot.transform, false);
        center.transform.position = FindRaidPosition(new Vector3(0, 0, -4));
        var partyMovement = center.AddComponent<CatMovementController>();
        partyMovement.Navigation = EncounterNavigation;
        partyMovement.SetAutomatic(false);
        raidParty = center.AddComponent<RaidPartyFormation>();
        raidParty.Field = this;
        raidParty.Movement = partyMovement;
        raidParty.Navigation = EncounterNavigation;
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
            cat.transform.position = raidParty.transform.position;
            var catArt = Billboard("Cat Artwork", cat.transform);
            catArt.localScale = Vector3.one * 1.716f;
            var fallback = new GameObject("Fallback").transform;
            fallback.SetParent(catArt, false);
            Paint("Body", fallback, Vector2.zero, Vector2.one, Color.yellow, 0);
            var visual = catArt.gameObject.AddComponent<CatCharacterVisual>();
            visual.Build(cat.transform, fieldCamera, actorData, square, fallback);
            var hp = BillboardHealth(catArt);
            var fighter = cat.AddComponent<FieldCombatController>();
            fighter.RaidMode = true;
            fighter.Navigation = EncounterNavigation;
            fighter.Initialize(cat.transform, targets, actorData, catalog);
            var mover = cat.AddComponent<CatMovementController>();
            mover.Navigation = EncounterNavigation;
            mover.Targets = targetTransforms;
            mover.Combat = fighter;
            mover.SetAutomatic(false);
            mover.ManualInputEnabled = true;
            mover.enabled = false;
            raidParty.Members.Add(cat.transform);
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
        follow.Target = raidFighters[0].transform;
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
        if (EncounterNavigation == null || EncounterNavigation.IsFree(desired)) return desired;
        for (int radius = 1; radius < FieldMapSize.HalfSize; radius++)
            for (int step = 0; step < 16; step++)
            {
                float angle = step * Mathf.PI / 8;
                var point = desired + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                if (Mathf.Abs(point.x) < 53 && Mathf.Abs(point.z) < 53 && EncounterNavigation.IsFree(point)) return point;
            }
        return Vector3.zero;
    }

    private void LateUpdate()
    {
        if (!RaidRunning || paused || !focused) return;
        RaidSeconds = Mathf.Max(0, RaidSeconds - Time.deltaTime);
        RaidBoss.StepVisual(Time.deltaTime);
        var living = livingRaidFighters;
        living.Clear();
        foreach (var fighter in raidFighters)
            if (fighter != null && fighter.gameObject.activeSelf) living.Add(fighter);
        if (!RaidBoss.Alive || RaidSeconds <= 0 || living.Count == 0)
        {
            bool won = !RaidBoss.Alive && RaidSeconds > 0;
            RaidRunning = false;
            if (skillMarker != null) skillMarker.gameObject.SetActive(false);
            foreach (var fighter in raidFighters)
            {
                fighter.Blocked = true;
                fighter.GetComponent<CatMovementController>().enabled = false;
            }
            RaidFinished?.Invoke(won);
            return;
        }
        TickRaidSkills(living);
        fieldCamera.GetComponent<CatCameraFollow>().Target = living[0].transform;
        if (skillWindup > 0 || RaidBoss.IsPlayingRaidAttack) return;
        var nearest = living[0].transform;
        foreach (var fighter in living)
            if ((fighter.transform.position - RaidBoss.transform.position).sqrMagnitude < (nearest.position - RaidBoss.transform.position).sqrMagnitude)
                nearest = fighter.transform;
        Vector3 start = RaidBoss.transform.position;
        Vector3 goal = EncounterNavigation == null ? nearest.position : EncounterNavigation.Waypoint(start, nearest.position);
        Vector3 offset = goal - start;
        if ((nearest.position - start).sqrMagnitude > .8f)
        {
            Vector3 step = offset.normalized * Mathf.Min(offset.magnitude, RaidBoss.MoveSpeed * RaidBoss.MovementMultiplier * Time.deltaTime);
            RaidBoss.transform.position = EncounterNavigation == null ? start + step : EncounterNavigation.Move(start, step);
        }
    }

    public void EndRaid()
    {
        raidNavigation = null;
        raidAdds.Clear();
        skillMarker = null;
        RaidRunning = false;
        raidParty = null;
        if (raidRoot != null) { raidRoot.SetActive(false); Destroy(raidRoot); }
        raidRoot = null;
        RaidBoss = null;
        raidFighters.Clear();
        livingRaidFighters.Clear();
    }
}
