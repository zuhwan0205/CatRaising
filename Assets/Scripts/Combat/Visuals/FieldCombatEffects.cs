using UnityEngine;
using UnityEngine.Rendering;

// 재사용하는 할퀴기 3줄과 피격 색상. 피해 판정에는 관여하지 않습니다.
[DefaultExecutionOrder(210)]
public class FieldCombatEffects : MonoBehaviour
{
    private Camera view;
    private Transform slash;
    private SortingGroup group;
    private SpriteRenderer[] lines;
    private Transform catArt;
    private CatCharacterVisual characterVisual;
    private SpriteRenderer[] catSprites;
    private Color[] originalColors;
    private Vector3 target;
    private Vector3 arrowOrigin;
    private bool arrowAttack;
    private float slashTime;
    private float hurtTime;
    private float attackTime;
    private float facing = 1;
    private const float Duration = .25f;

    public void Initialize(Camera camera, Sprite sprite, Transform artwork)
    {
        view = camera;
        catArt = artwork;
        characterVisual = artwork.GetComponentInParent<CatCharacterVisual>();
        catSprites = artwork.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[catSprites.Length];
        for (int i = 0; i < catSprites.Length; i++)
            originalColors[i] = catSprites[i].color;
        slash = new GameObject("Claw Slash Effect").transform;
        slash.SetParent(transform, false);
        group = slash.gameObject.AddComponent<SortingGroup>();
        lines = new SpriteRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            var line = new GameObject("Claw " + i);
            line.layer = 2;
            line.transform.SetParent(slash, false);
            line.transform.localPosition = new Vector3((i - 1) * .23f, 0, 0);
            line.transform.localRotation = Quaternion.Euler(0, 0, -35);
            line.transform.localScale = new Vector3(.07f, .85f, 1);
            lines[i] = line.AddComponent<SpriteRenderer>();
            lines[i].sprite = sprite;
        }
        slash.gameObject.SetActive(false);
    }
    public void Attack(Vector3 source, Vector3 destination)
    {
        target = destination;
        slashTime = Duration;
        attackTime = .15f;
        arrowOrigin = source;
        arrowAttack = characterVisual != null && characterVisual.UsesBow;
        facing = Vector3.Dot(destination - source, view.transform.right) < 0 ? -1 : 1;
        slash.gameObject.SetActive(true);
        RenderSlash();
    }
    public void Hurt()
    {
        hurtTime = .2f;
    }
    private void Update()
    {
        float delta = Mathf.Min(Time.deltaTime, .1f);
        slashTime = Mathf.Max(0, slashTime - delta);
        hurtTime = Mathf.Max(0, hurtTime - delta);
        attackTime = Mathf.Max(0, attackTime - delta);
        if (slashTime <= 0)
            slash.gameObject.SetActive(false);
        catArt.localScale = Vector3.one * (1 + attackTime);
        for (int i = 0; i < catSprites.Length; i++)
            catSprites[i].color = hurtTime > 0 ? new Color(1, .25f, .25f, originalColors[i].a) : originalColors[i];
    }
    private void LateUpdate()
    {
        if (slashTime > 0)
            RenderSlash();
    }
    private void RenderSlash()
    {
        slash.position = target + view.transform.up * .55f;
        slash.rotation = view.transform.rotation;
        float progress = 1 - slashTime / Duration;
        slash.localScale = new Vector3(facing * (.65f + progress * .6f), .65f + progress * .6f, 1);
        group.sortingOrder = 10002 - Mathf.RoundToInt(Vector3.Dot(target - view.transform.position, view.transform.forward) * 100);
        bool ice = characterVisual != null && characterVisual.UsesIce;
        bool magic = characterVisual != null && (characterVisual.UsesMagic || ice);
        bool fire = characterVisual != null && characterVisual.UsesFireBreath;
        bool hammer = characterVisual != null && characterVisual.UsesHammer;
        if (arrowAttack)
        {
            slash.position = Vector3.Lerp(arrowOrigin, target, progress) + view.transform.up * .55f;
            Vector3 direction = target - arrowOrigin;
            float angle = Mathf.Atan2(Vector3.Dot(direction, view.transform.up), Vector3.Dot(direction, view.transform.right)) * Mathf.Rad2Deg;
            slash.rotation = view.transform.rotation * Quaternion.Euler(0, 0, angle);
            slash.localScale = Vector3.one;
        }
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            line.transform.localPosition = magic ? Vector3.zero : new Vector3((i - 1) * .23f, 0, 0);
            line.transform.localRotation = Quaternion.Euler(0, 0, magic ? i * 60 + progress * 90 : -35);
            line.transform.localScale = magic ? new Vector3(.10f, .9f, 1) : new Vector3(.07f, .85f, 1);
            line.color = magic ? new Color(.72f, .4f, 1, slashTime / Duration) : new Color(1, .97f, .65f, slashTime / Duration);
            if (ice)
                line.color = new Color(.45f, .88f, 1, slashTime / Duration);
            if (fire)
            {
                line.transform.localPosition = new Vector3((i - 1) * .18f, progress * .3f, 0);
                line.transform.localRotation = Quaternion.Euler(0, 0, (i - 1) * 20);
                line.transform.localScale = new Vector3(.18f, .35f + (i % 2) * .2f, 1);
                line.color = new Color(1, .3f + i * .2f, .05f, slashTime / Duration);
            }
            if (arrowAttack)
            {
                line.transform.localPosition = i == 0 ? Vector3.zero : new Vector3(.22f, i == 1 ? .05f : -.05f, 0);
                line.transform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 0 : i == 1 ? -35 : 35);
                line.transform.localScale = i == 0 ? new Vector3(.55f, .045f, 1) : new Vector3(.18f, .045f, 1);
                line.color = i == 0 ? new Color(.65f, .42f, .2f) : new Color(.85f, .93f, 1);
            }
            if (hammer)
            {
                line.transform.localPosition = new Vector3((i - 1) * (.2f + progress * .3f), -.2f + progress * .15f, 0);
                line.transform.localRotation = Quaternion.Euler(0, 0, (i - 1) * 45);
                line.transform.localScale = new Vector3(.16f, .55f, 1);
                line.color = new Color(1, .72f, .2f, slashTime / Duration);
            }
        }
    }
    private void OnDisable()
    {
        slashTime = hurtTime = attackTime = 0;
        if (slash != null)
            slash.gameObject.SetActive(false);
        if (catArt != null)
            catArt.localScale = Vector3.one;
        if (catSprites != null)
            for (int i = 0; i < catSprites.Length; i++)
                catSprites[i].color = originalColors[i];
    }
}
