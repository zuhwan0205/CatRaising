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
    private readonly Sprite[] frames = new Sprite[8];
    private float animationTime;
    private Vector3 previousPosition;

    public void ConfigureMonster(int kind)
    {
        if (Kind == kind && frames[0] != null)
            return;
        foreach (var frame in frames)
            if (frame != null)
                Destroy(frame);
        System.Array.Clear(frames, 0, frames.Length);
        Kind = kind;
        double[] health = { 4, 6, 24, 14 };
        float[] speed = { .7f, 1.8f, .8f, .6f };
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
        float w = sheet.width / 4f, h = sheet.height / 2f;
        for (int i = 0; i < 8; i++)
            frames[i] = Sprite.Create(sheet, new Rect(i % 4 * w, (1 - i / 4) * h, w, h), new Vector2(.5f, .07f), h / 1.15f, 0, SpriteMeshType.FullRect);
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
                artwork.flipX = horizontal < 0;
        }
        if (frames[0] != null)
            artwork.sprite = frames[flashRemaining > 0 ? 4 + Mathf.Min(3, (int)((.15f - flashRemaining) / .15f * 4)) : (int)(animationTime * 6) % 4];
        artwork.color = flashRemaining > 0 ? Color.white : slowRemaining > 0 ? Color.Lerp(color, new Color(.35f, .8f, 1), .65f) : color;
    }
    public bool Hit(double damage)
    {
        if (!Alive)
            return false;
        LastHitDamage = System.Math.Max(1, damage - Defense);
        bool killed = Health.Damage(LastHitDamage);
        healthBar.localScale = new Vector3((float)(Health.Current / Health.Maximum), 1, 1);
        flashRemaining = .15f;
        artwork.color = Color.white;
        if (killed)
            gameObject.SetActive(false);
        return killed;
    }
    public void Respawn(Vector3 position)
    {
        transform.position = position;
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
        foreach (var frame in frames)
            if (frame != null)
                Destroy(frame);
    }
}
