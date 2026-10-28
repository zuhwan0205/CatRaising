using UnityEngine;
using UnityEngine.Rendering;

// 재사용하는 할퀴기 3줄과 피격 색상. 피해 판정에는 관여하지 않습니다.
[DefaultExecutionOrder(210)]
public class FieldCombatEffects : MonoBehaviour
{
    // 전투 개체의 깊이 정렬(10000 기준)보다 앞, 피해 숫자(20000)보다 뒤에 표시합니다.
    public const int AttackSortingOrder = 18000;
    private Camera view;
    public Camera EffectCamera => view;
    private FieldCombatController combat;
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

    private sealed class SpellVisual
    {
        public SpriteRenderer renderer;
        public Sprite[] frames;
        public Vector3 source, destination;
        public float age, range;
        public bool active, fire;
    }
    private readonly System.Collections.Generic.List<SpellVisual> spellPool = new System.Collections.Generic.List<SpellVisual>();

    private bool PlaySpell(Vector3 source, Vector3 destination, bool impactOnly = false)
    {
        string id = characterVisual != null ? characterVisual.CharacterId : null;
        var frames = CatSpellSprites.Get(id);
        if (frames == null) return false;
        SpellVisual spell = null;
        foreach (var item in spellPool) if (!item.active) { spell = item; break; }
        if (spell == null)
        {
            if (spellPool.Count >= 16) spell = spellPool[0];
            else
            {
                var obj = new GameObject("Pooled Cat Spell Sprite");
                obj.layer = 2;
                obj.transform.SetParent(transform, false);
                spell = new SpellVisual { renderer = obj.AddComponent<SpriteRenderer>() };
                spellPool.Add(spell);
            }
        }
        spell.frames = frames;
        spell.source = source;
        spell.destination = destination;
        spell.age = impactOnly && id != "cat_lion" ? CatSpellSprites.FlightSeconds : 0;
        spell.range = combat != null ? combat.Range : 4;
        spell.fire = id == "cat_lion";
        spell.active = true;
        spell.renderer.gameObject.SetActive(true);
        return true;
    }

    private void RenderSpells()
    {
        foreach (var spell in spellPool)
        {
            if (!spell.active) continue;
            bool impact = !spell.fire && spell.age >= CatSpellSprites.FlightSeconds;
            var sprite = spell.renderer;
            sprite.sprite = CatSpellSprites.Frame(spell.frames, spell.fire, spell.age, impact);
            Vector3 direction = spell.destination - spell.source;
            direction.y = 0;
            if (direction.sqrMagnitude < .001f) direction = Vector3.forward;
            Vector3 ray = direction.normalized * spell.range;
            Vector2 projected = new Vector2(Vector3.Dot(ray, view.transform.right), Vector3.Dot(ray, view.transform.up));
            float angle = Mathf.Atan2(projected.y, projected.x) * Mathf.Rad2Deg;
            Vector3 position = spell.fire ? spell.source : impact ? spell.destination :
                Vector3.Lerp(spell.source, spell.destination, Mathf.Clamp01(spell.age / CatSpellSprites.FlightSeconds));
            sprite.transform.position = spell.fire && characterVisual != null
                ? characterVisual.DragonMouthPosition : position + view.transform.up * .55f;
            sprite.transform.rotation = view.transform.rotation * Quaternion.Euler(0, 0, impact ? 0 : angle);
            float size = spell.fire ? Mathf.Max(.3f, projected.magnitude) : impact ? CharacterCombatRules.SplashRadius * 2 : 1.2f;
            sprite.transform.localScale = Vector3.one * size;
            float fade = spell.fire ? Mathf.Clamp01((CatSpellSprites.BreathSeconds - spell.age) / .10f) :
                impact ? Mathf.Clamp01(1 - (spell.age - CatSpellSprites.FlightSeconds) / CatSpellSprites.ImpactSeconds) : 1;
            sprite.color = new Color(1, 1, 1, fade);
            sprite.sortingOrder = AttackSortingOrder;
        }
    }

    public void Initialize(Camera camera, Sprite sprite, Transform artwork)
    {
        view = camera;
        combat = GetComponent<FieldCombatController>();
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
        attackTime = .15f;
        if (PlaySpell(source, destination))
        {
            slashTime = 0;
            slash.gameObject.SetActive(false);
            return;
        }
        target = destination;
        slashTime = Duration;
        attackTime = .15f;
        arrowOrigin = source;
        arrowAttack = characterVisual != null && characterVisual.UsesBow;
        facing = Vector3.Dot(destination - source, view.transform.right) < 0 ? -1 : 1;
        slash.gameObject.SetActive(true);
        RenderSlash();
    }
    public void SpellImpact(Vector3 source, Vector3 destination) => PlaySpell(source, destination, true);
    public void Hurt()
    {
        hurtTime = .2f;
        characterVisual?.Hurt();
    }
    private void Update()
    {
        float delta = Mathf.Min(Time.deltaTime, .1f);
        foreach (var spell in spellPool)
        {
            if (!spell.active) continue;
            spell.age += delta;
            float duration = spell.fire ? CatSpellSprites.BreathSeconds : CatSpellSprites.FlightSeconds + CatSpellSprites.ImpactSeconds;
            if (spell.age < duration) continue;
            spell.active = false;
            spell.renderer.gameObject.SetActive(false);
        }
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
        RenderSpells();
        if (slashTime > 0)
            RenderSlash();
    }
    private void RenderSlash()
    {
        slash.position = target + view.transform.up * .55f;
        slash.rotation = view.transform.rotation;
        float progress = 1 - slashTime / Duration;
        slash.localScale = new Vector3(facing * (.65f + progress * .6f), .65f + progress * .6f, 1);
        group.sortingOrder = AttackSortingOrder;
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
        if (fire)
        {
            slash.position = arrowOrigin + view.transform.up * .55f;
            slash.rotation = view.transform.rotation;
            slash.localScale = Vector3.one;
            Vector3 aim = target - arrowOrigin;
            aim.y = 0;
            if (aim.sqrMagnitude < .001f) aim = Vector3.forward;
            float range = combat != null ? combat.Range : 4f;
            for (int i = 0; i < lines.Length; i++)
            {
                Vector3 ray = Quaternion.Euler(0, (i - 1) * 22, 0) * aim.normalized * range;
                Vector2 screen = new Vector2(Vector3.Dot(ray, view.transform.right), Vector3.Dot(ray, view.transform.up));
                lines[i].transform.localPosition = screen * .5f;
                lines[i].transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(screen.y, screen.x) * Mathf.Rad2Deg);
                lines[i].transform.localScale = new Vector3(screen.magnitude, .18f + progress * .18f, 1);
                lines[i].color = new Color(1, .35f + i * .18f, .08f, (1 - progress) * .8f);
            }
            return;
        }
        for (int i = 0; i < lines.Length; i++)
        {
            var part = CatAttackVisualStyle.Sample(i, progress, ice, magic, fire, arrowAttack, hammer, characterVisual != null ? characterVisual.CharacterId : null);
            lines[i].transform.localPosition = part.Position;
            lines[i].transform.localRotation = part.Rotation;
            lines[i].transform.localScale = part.Scale;
            lines[i].color = part.Color;
        }
    }
    private void OnDisable()
    {
        slashTime = hurtTime = attackTime = 0;
        foreach (var spell in spellPool)
        {
            spell.active = false;
            if (spell.renderer != null) spell.renderer.gameObject.SetActive(false);
        }
        if (slash != null)
            slash.gameObject.SetActive(false);
        if (catArt != null)
            catArt.localScale = Vector3.one;
        if (catSprites != null)
            for (int i = 0; i < catSprites.Length; i++)
                catSprites[i].color = originalColors[i];
    }
}
