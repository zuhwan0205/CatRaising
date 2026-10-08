using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ButtonTweenEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Button button;
    private Vector3 initialScale;
    private Tween animation;
    private void Awake()
    {
        button = GetComponent<Button>();
        initialScale = transform.localScale;
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left && button.IsInteractable())
            Animate(.95f, .08f);
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            Animate(1, .16f);
    }
    public void OnPointerExit(PointerEventData e)
    {
        Animate(1, .12f);
    }
    private void Animate(float scale, float duration)
    {
        animation?.Kill();
        animation = DOTween.To(() => transform.localScale, v => transform.localScale = v, initialScale * scale, duration)
            .SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
    private void OnDisable()
    {
        animation?.Kill();
        transform.localScale = initialScale;
    }
    private void OnDestroy()
    {
        animation?.Kill();
    }
}
