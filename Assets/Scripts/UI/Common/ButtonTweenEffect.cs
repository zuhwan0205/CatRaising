using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[RequireComponent(typeof(Button))]
public sealed class ButtonTweenEffect : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Button button;
    private Vector3 initialScale;
    private Tween animation, glowAnimation;
    private Image glow;
    private bool hovered, pressed;
    public void SetDesignScale(Vector3 scale)
    {
        animation?.Kill();
        initialScale = scale;
        transform.localScale = scale;
    }
    private void Awake()
    {
        button = GetComponent<Button>();
        CozyButtonStyle.RoundCorners(button);
        initialScale = transform.localScale;
    }
    public void OnPointerEnter(PointerEventData e)
    {
        if (!button.IsInteractable() || !IsHoverPointer(e)) return;
        hovered = true;
        RefreshFeedback();
    }
    private static bool IsHoverPointer(PointerEventData e)
    {
#if ENABLE_INPUT_SYSTEM
        // Input System uses device IDs: a mouse ID can also be positive.
        if (e is ExtendedPointerEventData extended)
            return extended.pointerType == UIPointerType.MouseOrPen;
#endif
        return e.pointerId < 0;
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;
        pressed = true;
        RefreshFeedback();
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        pressed = false;
        RefreshFeedback();
    }
    public void OnPointerExit(PointerEventData e)
    {
        hovered = pressed = false;
        RefreshFeedback();
    }
    private void Update()
    {
        if ((hovered || pressed) && !button.IsInteractable())
        {
            hovered = pressed = false;
            RefreshFeedback();
        }
    }
    private void RefreshFeedback()
    {
        bool active = button.IsInteractable();
        Animate(active && pressed ? .95f : active && hovered ? 1.045f : 1, .12f);
        if (hovered && glow == null)
        {
            var rect = new GameObject("Hover Highlight", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            glow = rect.gameObject.AddComponent<Image>();
            glow.sprite = RoundedUIShape.GetSprite();
            glow.type = Image.Type.Sliced;
            glow.pixelsPerUnitMultiplier = .65f;
            glow.raycastTarget = false;
            glow.color = new Color(1, .95f, .75f, 0);
        }
        if (glow == null) return;
        glowAnimation?.Kill();
        glowAnimation = DOTween.To(() => glow.color.a,
            value => glow.color = new Color(1, .95f, .75f, value), active && hovered ? .16f : 0, .12f)
            .SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
    private void Animate(float scale, float duration)
    {
        animation?.Kill();
        animation = DOTween.To(() => transform.localScale, v => transform.localScale = v, initialScale * scale, duration)
            .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
    private void OnDisable()
    {
        hovered = pressed = false;
        animation?.Kill(); glowAnimation?.Kill();
        transform.localScale = initialScale;
        if (glow != null) glow.color = new Color(1, .95f, .75f, 0);
    }
    private void OnDestroy()
    {
        animation?.Kill(); glowAnimation?.Kill();
    }
}
