using UnityEngine;

// 기존 보스 원화를 보존하는 런타임 모션. 피해 판정은 전투 컨트롤러가 담당합니다.
public sealed class RaidBossVisual : MonoBehaviour
{
    public const float AttackDuration = .64f;
    public const float ImpactTime = .32f;
    public bool IsAttacking => attack > 0;
    private readonly Sprite[] attackFrames = new Sprite[4];
    private Sprite idleSprite;
    private Vector3 attackTarget;
    private bool impactPlayed;
    private System.Action impactCallbacks;
    private SpriteRenderer body;
    private Camera view;
    private int kind;
    private Vector3 origin;
    private float phase, attack, hurt;
    private float facing = -1;
    private Texture2D pixel;
    private Sprite square;
    private readonly SpriteRenderer[] particles = new SpriteRenderer[32];
    private readonly Vector3[] starts = new Vector3[32];
    private readonly Vector3[] velocities = new Vector3[32];
    private readonly float[] life = new float[32];
    private readonly float[] duration = new float[32];
    private readonly Color[] colors = new Color[32];
    private readonly Vector2[] sizes = new Vector2[32];
    private int cursor;

    public void Build(SpriteRenderer renderer, int bossKind)
    {
        body = renderer;
        kind = bossKind;
        origin = body.transform.localPosition;
        view = body.GetComponentInParent<FieldBillboard>().ViewCamera;
        idleSprite = body.sprite;
        var sheet = Resources.Load<Texture2D>("RaidArt/" + RaidBattle.ArtworkNames[kind] + "Attack");
        if (sheet != null && attackFrames[0] == null)
        {
            for (int i = 0; i < 4; i++)
            {
                // 생성 시트의 꼬리와 뻗은 팔을 포함하는 행별 분할입니다.
                float split = kind == 1 ? .55f : kind == 2 && i >= 2 ? .55f : .5f;
                float left = i % 2 == 0 ? 0 : split;
                float width = i % 2 == 0 ? split : 1 - split;
                var rect = new Rect(left * sheet.width, (1 - i / 2) * sheet.height / 2f, width * sheet.width, sheet.height / 2f);
                attackFrames[i] = Sprite.Create(sheet, rect, new Vector2(.5f, i < 2 ? .025f : .14f), sheet.height / 2f / 1.4f, 0, SpriteMeshType.FullRect);
            }
        }
        if (square != null) return;
        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
        square = Sprite.Create(pixel, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        for (int i = 0; i < particles.Length; i++)
        {
            var obj = new GameObject("Boss Effect Pixel " + i);
            obj.layer = gameObject.layer;
            // 레이드 액터의 루트는 기본 레이어이므로 렌더링 레이어를 명시합니다.
            obj.layer = 2;
            obj.transform.SetParent(transform, false);
            particles[i] = obj.AddComponent<SpriteRenderer>();
            particles[i].sprite = square;
            particles[i].enabled = false;
        }
    }

    public bool Attack(Vector3 target, System.Action onImpact = null)
    {
        if (body == null) return false;
        if (attack > 0)
        {
            if (impactPlayed) return false;
            impactCallbacks += onImpact;
            return true;
        }
        attack = AttackDuration;
        impactPlayed = false;
        impactCallbacks = onImpact;
        attackTarget = target;
        facing = Vector3.Dot(target - transform.position, view.transform.right) >= 0 ? 1 : -1;
        body.flipX = facing > 0;
        if (attackFrames[0] != null) body.sprite = attackFrames[0];
        return true;
    }

    private void PlayImpact()
    {
        Vector3 center = attackTarget + view.transform.up * .45f;
        Color tint = kind == 0 ? new Color(.70f, .60f, .83f) : kind == 1 ? new Color(1, .78f, .30f) : new Color(.30f, 1, .83f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2 / 12;
            Vector3 radial = view.transform.right * Mathf.Cos(angle) + view.transform.up * Mathf.Sin(angle);
            if (kind == 0)
                Emit(center + radial * .12f, radial * 2, Vector2.one * .15f, tint, .40f);
            else if (kind == 1)
            {
                float arc = Mathf.Lerp(-1.2f, 1.2f, i / 11f);
                Vector3 point = view.transform.right * (Mathf.Cos(arc) * facing) + view.transform.up * Mathf.Sin(arc);
                Emit(center + point * .6f, point * .4f, new Vector2(.10f, .23f), tint, .25f);
            }
            else
            {
                Vector3 start = transform.position + view.transform.up * .8f;
                Vector3 point = Vector3.Lerp(start, center, i / 11f) + view.transform.up * Mathf.Sin(i * .8f) * .18f;
                Emit(point, radial * .25f, Vector2.one * .12f, tint, .38f);
            }
        }
    }

    public void Hurt()
    {
        if (body == null) return;
        // 연타에도 파티클 수가 증가하지 않도록 고정 풀을 사용합니다.
        if (hurt <= .06f)
        {
            for (int i = 0; i < 5; i++)
            {
                float angle = (i + .5f) * Mathf.PI * 2 / 5;
                Vector3 direction = view.transform.right * Mathf.Cos(angle) + view.transform.up * Mathf.Sin(angle);
                Emit(transform.position + view.transform.up, direction * 1.8f, Vector2.one * .09f, new Color(1, .86f, .65f), .22f);
            }
        }
        hurt = .18f;
    }

    private void Emit(Vector3 position, Vector3 velocity, Vector2 size, Color tint, float seconds)
    {
        int i = cursor++ % particles.Length;
        starts[i] = position;
        velocities[i] = velocity;
        sizes[i] = size;
        colors[i] = tint;
        life[i] = duration[i] = seconds;
        particles[i].enabled = true;
    }

    public void Step(float delta, Vector3 moved)
    {
        if (body == null) return;
        phase += delta;
        attack = Mathf.Max(0, attack - delta);
        hurt = Mathf.Max(0, hurt - delta);
        bool walking = moved.sqrMagnitude > .000001f;
        float pulse = Mathf.Sin(phase * (walking ? 12 : 3));
        float squash = pulse * (kind == 2 ? .055f : .025f);
        float hop = walking ? Mathf.Abs(pulse) * (kind == 2 ? .065f : .025f) : 0;
        float rotation = walking ? pulse * (kind == 1 ? 4 : 2) : 0;
        float lunge = 0;
        if (attack > 0)
        {
            int frame = Mathf.Min(3, (int)((AttackDuration - attack) / (AttackDuration / 4)));
            if (attackFrames[frame] != null) body.sprite = attackFrames[frame];
            squash = hop = rotation = 0;
            body.flipX = facing > 0;
        }
        else body.sprite = idleSprite;
        if (!impactPlayed && attack > 0 && attack <= AttackDuration - ImpactTime)
        {
            impactPlayed = true;
            PlayImpact();
            var callbacks = impactCallbacks;
            impactCallbacks = null;
            callbacks?.Invoke();
        }
        float shake = hurt > 0 ? Mathf.Sin(hurt * 130) * .025f : 0;
        body.transform.localPosition = origin + new Vector3(lunge + shake, hop, 0);
        body.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        body.transform.localScale = new Vector3(1 + squash, 1 - squash, 1);
        if (hurt > 0) body.color = Color.Lerp(new Color(1, .35f, .35f), Color.white, Mathf.PingPong(hurt * 24, 1));
        for (int i = 0; i < particles.Length; i++)
        {
            if (life[i] <= 0) continue;
            life[i] = Mathf.Max(0, life[i] - delta);
            var particle = particles[i];
            particle.enabled = life[i] > 0;
            float remaining = life[i] / duration[i];
            particle.transform.position = starts[i] + velocities[i] * (duration[i] - life[i]);
            particle.transform.rotation = view.transform.rotation;
            float parentScale = Mathf.Max(.001f, transform.lossyScale.x);
            particle.transform.localScale = new Vector3(sizes[i].x, sizes[i].y, 1) * (Mathf.Lerp(.4f, 1, remaining) / parentScale);
            particle.sortingOrder = body.sortingOrder + 10;
            Color tint = colors[i];
            tint.a = remaining;
            particle.color = tint;
        }
    }

    private void OnDisable()
    {
        attack = hurt = 0;
        impactCallbacks = null;
        impactPlayed = true;
        if (body != null)
        {
            body.transform.localPosition = origin;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;
            body.color = Color.white;
        }
        for (int i = 0; i < particles.Length; i++)
        {
            life[i] = 0;
            if (particles[i] != null) particles[i].enabled = false;
        }
    }

    private void OnDestroy()
    {
        foreach (var frame in attackFrames) if (frame != null) Destroy(frame);
        if (square != null) Destroy(square);
        if (pixel != null) Destroy(pixel);
    }
}
