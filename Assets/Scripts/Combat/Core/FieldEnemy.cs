using UnityEngine;

public class FieldEnemy : MonoBehaviour, ICharacterHitReceiver
{
    public CombatHealth Health
    {
        get; private set;
    }
    public bool Alive => gameObject.activeSelf && !Health.Dead;
    public float RespawnRemaining;
    private SpriteRenderer artwork;
    private FieldBillboard billboard;
    private MonsterGroundShadow groundShadow;
    private Transform healthBar;
    private Color color;
    private float flashRemaining;
    private float nextImpactTime, impactRemaining;
    private Vector3 impactOffset, artworkRest;
    private float slowRemaining;
    private float armorBreakRemaining;
    public double EffectiveDefense => Defense * (armorBreakRemaining > 0 ? CharacterCombatRules.ArmorMultiplier : 1f);
    public void ApplyArmorBreak() => ApplyArmorBreak(CharacterCombatRules.ArmorBreakDuration);
    public void ApplyArmorBreak(float duration) { if (Alive) armorBreakRemaining = Mathf.Max(armorBreakRemaining, duration); }
    private double baseMonsterHealth;
    private double baseMonsterDamage;
    public double StatusDamageMultiplier => raidArtwork != null ? .4 : 1;
    private float survivalGrowth = 1f;
    public int Kind
    {
        get; private set;
    }
    public float MoveSpeed { get; private set; } = 1.1f;
    public double ContactDamage { get; private set; } = 5;
    public double Defense
    {
        get; private set;
    }
    public long GoldReward { get; private set; } = 10;
    public double LastHitDamage
    {
        get; private set;
    }
    private Sprite[] frames = new Sprite[8];
    private static readonly System.Collections.Generic.Dictionary<int, Sprite[]> sharedFrames = new System.Collections.Generic.Dictionary<int, Sprite[]>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSharedFrames()
    {
        foreach (var set in sharedFrames.Values)
            foreach (var sprite in set) if (sprite != null) Destroy(sprite);
        sharedFrames.Clear();
    }
    private float simulationElapsed, nextSimulationTime;
    public float SimulationDelta(float delta, bool distant)
    {
        simulationElapsed += delta;
        if (distant && Time.time < nextSimulationTime) return 0;
        float elapsed = simulationElapsed;
        simulationElapsed = 0;
        nextSimulationTime = Time.time + .10f + UnityEngine.Random.Range(0f, .035f);
        return elapsed;
    }
    private float nextPathTime;
    private Vector3 pathGoal;
    public Vector3 NextWaypoint(FieldNavigation navigation, Vector3 target)
    {
        if (navigation == null) return target;
        if (Time.time >= nextPathTime && navigation.TryEnemyWaypoint(transform.position, target, out var next))
        {
            pathGoal = next;
            nextPathTime = Time.time + .25f + UnityEngine.Random.Range(0f, .1f);
        }
        return pathGoal;
    }
    private Sprite raidArtwork;
    private RaidBossVisual raidVisual;
    public bool HasRaidAnimation => raidVisual != null;
    public bool IsPlayingRaidAttack => raidVisual != null && raidVisual.IsAttacking;
    public bool ScheduleRaidAttack(Vector3 target, System.Action onImpact)
    {
        return raidVisual != null && raidVisual.Attack(target, onImpact);
    }
    public void ShowAttack(Vector3 target)
    {
        if (raidVisual != null) raidVisual.Attack(target);
    }
    private float animationTime;
    private Vector3 previousPosition;

    public void ConfigureMonster(int kind)
    {
        if (raidArtwork != null)
        {
            Destroy(raidArtwork);
            raidArtwork = null;
        }
        if (Kind == kind && frames[0] != null)
            return;

        frames = new Sprite[8];
        Kind = kind;
        double[] health = { 6, 10, 36, 24 };
        float[] speed = { 1.05f, 2.7f, 1.2f, .9f };
        double[] contact = { 3, 5, 7, 9 };
        long[] gold = { 10, 15, 25, 40 };
        GoldReward = gold[kind];
        baseMonsterHealth = health[kind];
        survivalGrowth = 1f;
        if (groundShadow != null) groundShadow.SetSize(1);
        Health = new CombatHealth(baseMonsterHealth);
        MoveSpeed = speed[kind];
        ContactDamage = baseMonsterDamage = contact[kind];
        Defense = kind == 3 ? 2 : 0;
        healthBar.localScale = Vector3.one;
        string[] names = { "DustBunnyMonster", "CheeseMouseMonster", "YarnSlimeMonster", "TinCanCrabMonster" };
        if (!sharedFrames.TryGetValue(kind, out var cachedFrames))
        {
        var sheet = Resources.Load<Texture2D>("MonsterArt/" + names[kind]);
        if (sheet == null) return;
        frames = new Sprite[8];
        float w = sheet.width / 4f, h = sheet.height / 2f;
        for (int i = 0; i < 8; i++)
            frames[i] = Sprite.Create(sheet, new Rect(i % 4 * w, (1 - i / 4) * h, w, h), new Vector2(.5f, .07f), h / 1.15f, 0, SpriteMeshType.FullRect);
        sharedFrames[kind] = frames;
        }
        else frames = cachedFrames;
        artwork.sprite = frames[0];
        if (billboard != null) billboard.VisibilityRenderer = artwork;
        artwork.transform.localScale = Vector3.one;
        color = Color.white;
        artwork.color = color;
        if (billboard != null)
            billboard.CenterHeight = 0;
        previousPosition = transform.position;
    }
    public float MovementMultiplier => slowRemaining > 0 ? CharacterCombatRules.SlowMultiplier : 1f;

    // 메인 필드 전용. 현재 체력 비율을 유지해 성장 시 완전히 회복되지 않게 합니다.
    public void ApplySurvivalGrowth(float multiplier)
    {
        multiplier = Mathf.Clamp(multiplier, 1f, 2f);
        if (baseMonsterHealth <= 0 || survivalGrowth == multiplier) return;
        survivalGrowth = multiplier;
        ContactDamage = baseMonsterDamage * (1 + (multiplier - 1) * .5);
        Health.SetMaximumPreservingRatio(baseMonsterHealth * multiplier);
        artwork.transform.localScale = Vector3.one * multiplier;
        if (groundShadow != null) groundShadow.SetSize(multiplier);
    }
    public void ConfigureRaidBoss(int kind, double health)
    {
        ConfigureMonster(kind);
        Health = new CombatHealth(health);
        GoldReward = 0;
        ContactDamage = 8 + kind * 5;
        MoveSpeed = 1.2f;
    }

    public void StrengthenRaidBoss()
    {
        ContactDamage = 22 + Kind * 14;
        Defense = 2 + Kind;
        MoveSpeed = 1.65f;
    }

    public void SetRaidArtwork(int index)
    {
        if (index < 0 || index >= RaidBattle.ArtworkNames.Length) return;
        var texture = Resources.Load<Texture2D>("RaidArt/" + RaidBattle.ArtworkNames[index]);
        if (texture == null)
        {
            Debug.LogWarning("레이드 보스 이미지가 없습니다: " + RaidBattle.ArtworkNames[index]);
            return;
        }
        if (raidArtwork != null) Destroy(raidArtwork);
        raidArtwork = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .07f), texture.height / 1.4f, 0, SpriteMeshType.FullRect);
        artwork.sprite = raidArtwork;
        artwork.transform.localScale = Vector3.one;
        artwork.color = color = Color.white;
        if (raidVisual == null) raidVisual = gameObject.AddComponent<RaidBossVisual>();
        raidVisual.Build(artwork, index);
        if (billboard != null) billboard.VisibilityRenderer = null;
        if (groundShadow != null) groundShadow.SetSize(2);
    }

    public void ApplyIceSlow()
    {
        if (Alive)
            slowRemaining = CharacterCombatRules.SlowDuration;
    }

    public void Initialize(SpriteRenderer art, Transform bar, double hp)
    {
        artwork = art;
        artworkRest = art.transform.localPosition;
        billboard = art.GetComponentInParent<FieldBillboard>();
        color = art.color;
        healthBar = bar;
        Health = new CombatHealth(hp);
        var shadow = transform.Find("Target Shadow");
        if (shadow == null)
        {
            shadow = new GameObject("Target Shadow").transform;
            shadow.SetParent(transform, false);
        }
        groundShadow = shadow.GetComponent<MonsterGroundShadow>();
        if (groundShadow == null) groundShadow = shadow.gameObject.AddComponent<MonsterGroundShadow>();
    }
    public void StepVisual(float delta)
    {
        flashRemaining = Mathf.Max(0, flashRemaining - delta);
        slowRemaining = Mathf.Max(0, slowRemaining - delta);
        armorBreakRemaining = Mathf.Max(0, armorBreakRemaining - delta);
        animationTime += delta;
        if (impactRemaining > 0)
        {
            impactRemaining = Mathf.Max(0, impactRemaining - delta);
            if (raidVisual == null)
                artwork.transform.localPosition = artworkRest + impactOffset * (impactRemaining / .12f);
        }
        if (raidArtwork == null && !FieldBillboard.IsNearView(billboard != null ? billboard.ViewCamera : null, artwork))
        {
            previousPosition = transform.position;
            return;
        }
        Vector3 moved = transform.position - previousPosition;
        previousPosition = transform.position;
        if (billboard != null && billboard.ViewCamera != null)
        {
            float horizontal = Vector3.Dot(moved, billboard.ViewCamera.transform.right);
            if (Mathf.Abs(horizontal) > .0001f)
                artwork.flipX = raidArtwork != null ? horizontal > 0 : horizontal < 0;
        }
        var frame = raidArtwork != null ? raidArtwork : frames[flashRemaining > 0 ? 4 + Mathf.Min(3, (int)((.15f - flashRemaining) / .15f * 4)) : (int)(animationTime * 6) % 4];
        if (frame != null && artwork.sprite != frame) artwork.sprite = frame;
        var tint = flashRemaining > 0 ? (raidArtwork != null ? new Color(1, .55f, .55f) : Color.white) : slowRemaining > 0 ? Color.Lerp(color, new Color(.35f, .8f, 1), .65f) : armorBreakRemaining > 0 ? Color.Lerp(color, new Color(.85f, .55f, 1), .45f) : color;
        if (raidVisual == null && impactRemaining > .05f) tint = new Color(1, .82f, .55f);
        if (artwork.color != tint) artwork.color = tint;
        if (raidVisual != null) raidVisual.Step(delta, moved);
    }
    public bool Hit(double damage, bool ignoreDefense = false, Vector3? sourcePosition = null)
    {
        if (!Alive)
            return false;
        LastHitDamage = ignoreDefense ? System.Math.Max(0, damage) : CharacterCombatRules.DamageAfterDefense(damage, Defense, armorBreakRemaining > 0);
        bool killed = Health.Damage(LastHitDamage);
        if (LastHitDamage > 0 && Time.time >= nextImpactTime && billboard != null &&
            FieldBillboard.IsNearView(billboard.ViewCamera, artwork))
        {
            nextImpactTime = Time.time + .1f;
            impactRemaining = .12f;
            var camera = billboard.ViewCamera;
            if (camera != null)
            {
                var direction = transform.position - (sourcePosition ?? transform.position - camera.transform.right);
                impactOffset = new Vector3(Vector3.Dot(direction.normalized, camera.transform.right), 0, 0) * .055f;
                HitSparkPool.Emit(camera.transform, artwork.bounds.center, camera.transform.rotation, .32f);
            }
        }
        HealthBarTween.Set(healthBar, (float)(Health.Current / Health.Maximum));
        flashRemaining = .15f;
        artwork.color = Color.white;
        if (raidVisual != null) raidVisual.Hurt();
        if (killed)
            gameObject.SetActive(false);
        return killed;
    }
    public void Respawn(Vector3 position)
    {
        transform.position = position;
        nextPathTime = 0;
        simulationElapsed = 0;
        nextSimulationTime = 0;
        pathGoal = position;
        Health.Reset();
        flashRemaining = 0;
        nextImpactTime = impactRemaining = 0;
        if (raidVisual == null) artwork.transform.localPosition = artworkRest;
        slowRemaining = 0;
        armorBreakRemaining = 0;
        animationTime = 0;
        previousPosition = position;
        artwork.color = color;
        healthBar.localScale = Vector3.one;
        gameObject.SetActive(true);
    }
    private void OnDestroy()
    {
        if (raidArtwork != null) Destroy(raidArtwork);

    }
}
