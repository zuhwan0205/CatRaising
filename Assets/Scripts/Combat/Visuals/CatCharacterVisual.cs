using UnityEngine;

// 몸의 연출만 담당합니다. 이동/피해/HP 바와 별도의 계층에서 움직입니다.
[DefaultExecutionOrder(205)]
public sealed class CatCharacterVisual : MonoBehaviour
{
    public Transform ImpactRoot
    {
        get; private set;
    }
    private Transform feet, animated, shadow;
    private Camera view;
    private BackendUserData data;
    private GameObject fallback;
    private SpriteRenderer catSprite;
    private Sprite generatedSprite, shadowSprite;
    private Vector3 previous;
    private float phase, motion;
    private string selectedId;
    private Sprite[] combatFrames = new Sprite[4];
    public bool UsesHammer => selectedId == "cat_tiger";
    public bool UsesIce => selectedId == "cat_shadow";
    public bool UsesBow => selectedId == "cat_ninja";
    public bool UsesFireBreath => selectedId == "cat_lion";
    public bool UsesMagic => selectedId == "cat_moon";
    private Sprite[] activePixelFrames;
    private bool usePixelCat;
    private FieldBillboard billboard;
    private float originalCenterHeight;
    private float idleTime;
    private float attackRemaining, facingRemaining;
    private bool facingBack;
    private Vector2 attackDirection;
    private int pendingFacing;
    private float pendingFacingTime;
    private float AttackDuration => UsesHammer ? .52f : .32f;

    public void Build(Transform player, Camera camera, BackendUserData playerData, Sprite square, Transform fallbackArt)
    {
        feet = player;
        view = camera;
        data = playerData;
        previous = feet.position;
        billboard = GetComponent<FieldBillboard>();
        if (billboard != null)
            originalCenterHeight = billboard.CenterHeight;
        ImpactRoot = new GameObject("Cat Impact Visual").transform;
        ImpactRoot.SetParent(transform, false);
        fallback = fallbackArt.gameObject;
        fallbackArt.SetParent(ImpactRoot, false);
        animated = new GameObject("Shared Cat Artwork").transform;
        animated.SetParent(ImpactRoot, false);
        animated.localPosition = new Vector3(0, .35f, 0);
        catSprite = animated.gameObject.AddComponent<SpriteRenderer>();
        catSprite.sortingOrder = 1;
        animated.gameObject.layer = 2;
        shadowSprite = Sprite.Create(square.texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        var vertices = new Vector2[24];
        var indices = new ushort[66];
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / vertices.Length;
            vertices[i] = new Vector2(.5f + Mathf.Cos(angle) * .49f, .5f + Mathf.Sin(angle) * .49f);
        }
        for (int i = 0; i < 22; i++)
        {
            indices[i * 3] = 0;
            indices[i * 3 + 1] = (ushort)(i + 1);
            indices[i * 3 + 2] = (ushort)(i + 2);
        }
        shadowSprite.OverrideGeometry(vertices, indices);
        shadow = new GameObject("Soft Foot Shadow").transform;
        shadow.SetParent(feet, false);
        shadow.gameObject.layer = 2;
        shadow.localPosition = new Vector3(0, .025f, 0);
        shadow.localRotation = Quaternion.Euler(90, 0, 0);
        shadow.localScale = new Vector3(1, .65f, 1);
        var renderer = shadow.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = shadowSprite;
        renderer.color = new Color(.12f, .18f, .10f, .3f);
        renderer.sortingOrder = -9000;
        ApplySelection();
    }

    private void ApplySelection()
    {
        selectedId = data.loadout.characterId;
        activePixelFrames = CatAnimationFrames.Get(selectedId);
        usePixelCat = activePixelFrames != null && activePixelFrames[0] != null;
        if (!usePixelCat && combatFrames[0] == null && generatedSprite == null) LoadFallback();
        // 픽셀 시트는 발바닥 pivot이므로 중앙 pivot용 높이 보정을 적용하지 않습니다.
        if (billboard != null)
            billboard.CenterHeight = usePixelCat ? 0 : originalCenterHeight;
        bool detailed = catSprite != null && (usePixelCat || combatFrames[0] != null || generatedSprite != null);
        if (detailed)
            catSprite.sprite = usePixelCat ? activePixelFrames[0] : combatFrames[0] != null ? combatFrames[0] : generatedSprite;
        animated.gameObject.SetActive(detailed);
        fallback.SetActive(!detailed);
        phase = motion = idleTime = 0;
        previous = feet.position;
        attackRemaining = facingRemaining = 0;
        pendingFacing = 0;
        pendingFacingTime = 0;
    }

    public void Attack(Vector3 source, Vector3 target)
    {
        if (selectedId != data.loadout.characterId)
            ApplySelection();
        Face(target - source, false);
        attackRemaining = AttackDuration;
        facingRemaining = Mathf.Max(.45f, AttackDuration + .1f);
    }

    private void Face(Vector3 direction, bool moving)
    {
        Vector2 screen = new Vector2(Vector3.Dot(direction, view.transform.right), Vector3.Dot(direction, view.transform.up));
        if (screen.sqrMagnitude < .00001f)
            return;
        screen = screen.normalized;
        attackDirection = screen;
        // 세로 이동 중 경로 보정으로 생긴 작은 X 값은 바라보는 방향을 바꾸지 않습니다.
        int desired = Mathf.Abs(screen.x) >= .25f ? (screen.x < 0 ? -1 : 1) : 0;
        if (!moving)
        {
            pendingFacing = 0;
            pendingFacingTime = 0;
            if (catSprite != null && desired != 0) catSprite.flipX = desired < 0;
        }
        else if (desired == 0 || catSprite == null || (catSprite.flipX ? -1 : 1) == desired)
        {
            pendingFacing = 0;
            pendingFacingTime = 0;
        }
        else
        {
            if (pendingFacing != desired) { pendingFacing = desired; pendingFacingTime = 0; }
            pendingFacingTime += Mathf.Min(Time.deltaTime, .1f);
            if (pendingFacingTime >= .08f) catSprite.flipX = desired < 0;
        }
        if (Mathf.Abs(screen.y) > .25f)
            facingBack = screen.y > 0;
    }
    private void LateUpdate()
    {
        if (feet == null)
            return;
        if (selectedId != data.loadout.characterId)
            ApplySelection();
        Vector3 delta = feet.position - previous;
        previous = feet.position;
        float distance = delta.magnitude;
        // 재등장 순간의 순간이동을 걷기 애니메이션으로 처리하지 않습니다.
        bool walking = distance > .0001f && distance < .75f;
        float dt = Mathf.Min(Time.deltaTime, .1f);
        idleTime += dt;
        attackRemaining = Mathf.Max(0, attackRemaining - dt);
        facingRemaining = Mathf.Max(0, facingRemaining - dt);
        motion = Mathf.MoveTowards(motion, walking ? 1 : 0, dt * 10);
        if (walking)
        {
            phase += distance * 13;
            if (facingRemaining <= 0)
                Face(delta, true);
        }
        if (!walking || facingRemaining > 0)
        {
            pendingFacing = 0;
            pendingFacingTime = 0;
        }
        float progress = 1 - attackRemaining / AttackDuration;
        bool striking = attackRemaining > 0 && progress >= .15f && progress < .8f;
        float lunge = attackRemaining > 0 && !UsesMagic && !UsesFireBreath && !UsesBow && !UsesIce ? Mathf.Sin(progress * Mathf.PI) * .12f : 0;
        float walk = attackRemaining > 0 ? 0 : motion;
        if (usePixelCat)
        {
            int frame = attackRemaining > 0 ? 8 + Mathf.Min(3, Mathf.FloorToInt(progress * 4)) :
                walking ? 4 + Mathf.FloorToInt(phase / 1.5f) % 4 : Mathf.FloorToInt(idleTime * 4) % 4;
            catSprite.sprite = activePixelFrames[frame];
            animated.localPosition = new Vector3(attackDirection.x * lunge, attackDirection.y * lunge, 0);
            animated.localRotation = Quaternion.identity;
            animated.localScale = Vector3.one;
            shadow.localScale = new Vector3(.75f, .45f, 1);
            return;
        }
        if (catSprite != null && combatFrames[0] != null)
            catSprite.sprite = combatFrames[(facingBack ? 2 : 0) + (striking ? 1 : 0)];
        animated.localPosition = new Vector3(attackDirection.x * lunge, .35f + attackDirection.y * lunge + Mathf.Abs(Mathf.Sin(phase)) * .055f * walk, 0);
        animated.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(phase) * 2.5f * walk);
        animated.localScale = new Vector3(1 + Mathf.Sin(phase * 2) * .018f * walk, 1 - Mathf.Sin(phase * 2) * .018f * walk, 1);
        shadow.localScale = new Vector3(1 - Mathf.Abs(Mathf.Sin(phase)) * .06f * motion, .65f, 1);
    }
    private void LoadFallback()
    {
        combatFrames = CatAnimationFrames.GetFallback();
        generatedSprite = combatFrames[4];
    }

    private void OnDestroy()
    {
        if (shadowSprite != null)
            Destroy(shadowSprite);
    }
}
