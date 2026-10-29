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
    private Transform healthBar;
    private Color color;
    private float flashRemaining;
    private float slowRemaining;
    private float armorBreakRemaining;
    public double EffectiveDefense => Defense * (armorBreakRemaining > 0 ? CharacterCombatRules.ArmorMultiplier : 1f);
    public void ApplyArmorBreak() { if (Alive) armorBreakRemaining = CharacterCombatRules.ArmorBreakDuration; }
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
    }

    public void ApplyIceSlow()
    {
        if (Alive)
            slowRemaining = CharacterCombatRules.SlowDuration;
    }

    public void Initialize(SpriteRenderer art, Transform bar, double hp)
    {
        artwork = art;
        billboard = art.GetComponentInParent<FieldBillboard>();
        color = art.color;
        healthBar = bar;
        Health = new CombatHealth(hp);
    }
    public void StepVisual(float delta)
    {
        flashRemaining = Mathf.Max(0, flashRemaining - delta);
        slowRemaining = Mathf.Max(0, slowRemaining - delta);
        armorBreakRemaining = Mathf.Max(0, armorBreakRemaining - delta);
        animationTime += delta;
        if (raidArtwork == null && !artwork.isVisible)
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
        if (raidArtwork != null)
            artwork.sprite = raidArtwork;
        else if (frames[0] != null)
            artwork.sprite = frames[flashRemaining > 0 ? 4 + Mathf.Min(3, (int)((.15f - flashRemaining) / .15f * 4)) : (int)(animationTime * 6) % 4];
        artwork.color = flashRemaining > 0 ? (raidArtwork != null ? new Color(1, .55f, .55f) : Color.white) : slowRemaining > 0 ? Color.Lerp(color, new Color(.35f, .8f, 1), .65f) : armorBreakRemaining > 0 ? Color.Lerp(color, new Color(.85f, .55f, 1), .45f) : color;
        if (raidVisual != null) raidVisual.Step(delta, moved);
    }
    public bool Hit(double damage, bool ignoreDefense = false)
    {
        if (!Alive)
            return false;
        LastHitDamage = ignoreDefense ? System.Math.Max(0, damage) : CharacterCombatRules.DamageAfterDefense(damage, Defense, armorBreakRemaining > 0);
        bool killed = Health.Damage(LastHitDamage);
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
