
using UnityEngine;
using UnityEngine.UI;

// 피해 확정 이벤트만 표시합니다. 범위 공격과 처치 순간도 같은 경로를 사용합니다.
public sealed class FieldDamageNumbers : MonoBehaviour
{
    private sealed class Entry
    {
        public RectTransform Root;
        public CanvasGroup Fade;
        public Text Label;
        public GameObject Coin;
        public Vector3 Start, Travel; public float Age; public bool Gold, Active;
    }
    private readonly Entry[] entries = new Entry[32];
    private Camera view;
    private FieldCombatController combat;
    private int next;

    public void Initialize(Camera camera, FieldCombatController source)
    {
        view = camera;
        combat = source;
        var font = Resources.Load<Font>("Fonts/Mona12TextKR");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 48);
        for (int i = 0; i < entries.Length; i++)
        {
            var go = new GameObject("Damage Number " + i, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            go.layer = 2;
            go.transform.SetParent(transform, false);
            var root = go.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(260, 80);
            root.localScale = Vector3.one * .01f;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = view;
            canvas.sortingOrder = 20000;
            var textObject = new GameObject("Value", typeof(RectTransform), typeof(Text));
            textObject.layer = 2;
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = textObject.GetComponent<Text>();
            label.font = font; label.fontSize = 48;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, .91f, .48f);
            label.raycastTarget = false;
            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(.12f, .08f, .04f, 1);
            outline.effectDistance = new Vector2(2, -2);
            var fade = go.GetComponent<CanvasGroup>();
            fade.blocksRaycasts = false; fade.interactable = false;
            var coin = new GameObject("Gold Coin", typeof(RectTransform), typeof(Image));
            coin.layer = 2;
            var coinRect = coin.GetComponent<RectTransform>();
            coinRect.SetParent(root, false);
            coinRect.sizeDelta = new Vector2(32, 32);
            coinRect.anchoredPosition = new Vector2(-95, 0);
            coin.GetComponent<Image>().color = new Color(1f,.72f,.1f);
            coin.GetComponent<Image>().raycastTarget = false;
            var rim = coin.AddComponent<Outline>();
            rim.effectColor = new Color(.55f,.30f,.02f);rim.effectDistance = new Vector2(3,-3);
            entries[i] = new Entry { Root = root, Fade = fade, Label = label, Coin = coin };
            go.SetActive(false);
        }
        combat.DamageDealt += Show;
        combat.EnemyGoldDropped += ShowGold;
    }

    private void Show(Vector3 position, double damage, bool critical)
    {
        ShowValue(position, damage.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + (critical ? "!" : ""), false, critical);
        if (critical && view != null)
        {
            var point = view.WorldToViewportPoint(position);
            if (point.z > 0 && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1)
                view.GetComponent<CatCameraFollow>()?.CriticalShake();
        }
    }

    private void ShowGold(Vector3 position, long amount)
    {
        ShowValue(position, "+" + amount.ToString() + " G", true);
    }

    private void ShowValue(Vector3 position, string value, bool gold, bool critical = false)
    {
        if (!isActiveAndEnabled || view == null) return;
        var entry = entries[next];
        float side = ((next % 3) - 1) * .16f;
        next = (next + 1) % entries.Length;
        entry.Age = 0; entry.Gold = gold; entry.Active = true;
        entry.Root.gameObject.SetActive(true);
        entry.Root.rotation = view.transform.rotation;
        var start = position + view.transform.up * (gold ? .35f : 1.35f)
            + view.transform.right * (gold ? .6f : side);
        entry.Root.position = start;
        entry.Root.localScale = Vector3.one * (gold ? .009f : .012f);
        entry.Fade.alpha = 1;
        entry.Label.text = value;
        entry.Coin.SetActive(gold);
        entry.Label.fontSize = gold ? 34 : critical ? 62 : 48;
        entry.Label.color = gold ? new Color(1f, .78f, .18f) : critical ? new Color(1, .38f, .22f) : new Color(1f, .97f, .94f);
        entry.Start = start;
        entry.Travel = gold ? view.transform.up * .55f + view.transform.right * .4f : view.transform.up * .8f;
    }

    private void LateUpdate()
    {
        if (view == null) return;
        var rotation = view.transform.rotation;
        foreach (var entry in entries)
        {
            if (entry == null || !entry.Active) continue;
            entry.Age += Time.deltaTime;
            float duration = entry.Gold ? 1.05f : .5f;
            if (entry.Age >= duration)
            {
                entry.Active = false; entry.Root.gameObject.SetActive(false); continue;
            }
            float t = Mathf.Clamp01(entry.Age / duration);
            float ease = entry.Gold ? Mathf.Sin(t * Mathf.PI * .5f) : 1 - Mathf.Pow(1 - t, 3);
            entry.Root.position = entry.Start + entry.Travel * ease;
            entry.Root.rotation = rotation;
            if (!entry.Gold)
            {
                float scaleTime = Mathf.Clamp01(entry.Age / .18f);
                entry.Root.localScale = Vector3.one * Mathf.Lerp(.012f, .01f, 1 - (1 - scaleTime) * (1 - scaleTime));
            }
            float fadeTime = Mathf.Clamp01((entry.Age - (entry.Gold ? .65f : .2f)) / (entry.Gold ? .4f : .3f));
            entry.Fade.alpha = (1 - fadeTime) * (1 - fadeTime);
        }
    }
    private void OnDisable()
    {
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            entry.Active = false;
            if (entry.Root != null) entry.Root.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (combat != null) combat.DamageDealt -= Show;
        if (combat != null) combat.EnemyGoldDropped -= ShowGold;
    }
}
