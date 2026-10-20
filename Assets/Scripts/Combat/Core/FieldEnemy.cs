using UnityEngine;

public class FieldEnemy : MonoBehaviour
{
    public CombatHealth Health
    {
        get; private set;
    }
    public bool Alive => gameObject.activeSelf && !Health.Dead;
    public float RespawnRemaining;
    private SpriteRenderer artwork;
    private Transform healthBar;
    private Color color;
    private float flashRemaining;
    private float slowRemaining;
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
    private float nextPathTime;
    private Vector3 pathGoal;
    public Vector3 NextWaypoint(FieldNavigation navigation, Vector3 target)
    {
        if (navigation == null) return target;
        if (Time.time >= nextPathTime || (pathGoal - transform.position).sqrMagnitude < .16f)
        {
            pathGoal = navigation.Waypoint(transform.position, target);
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
        double[] health = { 4, 6, 24, 14 };
        float[] speed = { 1.05f, 2.7f, 1.2f, .9f };
        double[] contact = { 2, 3, 4, 5 };
        long[] gold = { 10, 15, 25, 40 };
        GoldReward = gold[kind];
        Health = new CombatHealth(health[kind]);
        MoveSpeed = speed[kind];
        ContactDamage = contact[kind];
        Defense = kind == 3 ? 2 : 0;
        healthBar.localScale = Vector3.one;
        string[] names = { "DustBunnyMonster", "CheeseMouseMonster", "YarnSlimeMonster", "TinCanCrabMonster" };
        var sheet = Resources.Load<Texture2D>("MonsterArt/" + names[kind]);
        if (sheet == null)
            return;
        if (!sharedFrames.TryGetValue(kind, out frames))
        {
        frames = new Sprite[8];
        float w = sheet.width / 4f, h = sheet.height / 2f;
        for (int i = 0; i < 8; i++)
            frames[i] = Sprite.Create(sheet, new Rect(i % 4 * w, (1 - i / 4) * h, w, h), new Vector2(.5f, .07f), h / 1.15f, 0, SpriteMeshType.FullRect);
        sharedFrames[kind] = frames;
        }
        artwork.sprite = frames[0];
        artwork.transform.localScale = Vector3.one;
        color = Color.white;
        artwork.color = color;
        var billboard = artwork.GetComponentInParent<FieldBillboard>();
        if (billboard != null)
            billboard.CenterHeight = 0;
        previousPosition = transform.position;
    }
    public float MovementMultiplier => slowRemaining > 0 ? .6f : 1f;
    public void ConfigureRaidBoss(int kind, double health)
    {
        ConfigureMonster(kind);
        Health = new CombatHealth(health);
        GoldReward = 0;
        ContactDamage = 5 + kind * 3;
        MoveSpeed = 1.2f;
    }

    public void StrengthenRaidBoss()
    {
        ContactDamage = 15 + Kind * 9;
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
            slowRemaining = 2f;
    }

    public void Initialize(SpriteRenderer art, Transform bar, double hp)
    {
        artwork = art;
        color = art.color;
        healthBar = bar;
        Health = new CombatHealth(hp);
    }
    public void StepVisual(float delta)
    {
        flashRemaining = Mathf.Max(0, flashRemaining - delta);
        slowRemaining = Mathf.Max(0, slowRemaining - delta);
        animationTime += delta;
        Vector3 moved = transform.position - previousPosition;
        previousPosition = transform.position;
        var billboard = artwork.GetComponentInParent<FieldBillboard>();
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
        artwork.color = flashRemaining > 0 ? (raidArtwork != null ? new Color(1, .55f, .55f) : Color.white) : slowRemaining > 0 ? Color.Lerp(color, new Color(.35f, .8f, 1), .65f) : color;
        if (raidVisual != null) raidVisual.Step(delta, moved);
    }
    public bool Hit(double damage, bool ignoreDefense = false)
    {
        if (!Alive)
            return false;
        LastHitDamage = ignoreDefense ? System.Math.Max(0, damage) : System.Math.Max(1, damage - Defense);
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
        Health.Reset();
        flashRemaining = 0;
        slowRemaining = 0;
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
