using System.Collections;
using DG.Tweening;
using UnityEngine;

// Position/fade only, so pointer hover and press scales remain independent.
[RequireComponent(typeof(CanvasGroup))]
public sealed class MenuButtonReveal : MonoBehaviour
{
    public int Order;
    private CanvasGroup group;
    private RectTransform rect;
    private Vector2 destination;
    private bool started;
    private Sequence sequence;
    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.interactable = group.blocksRaycasts = false;
    }
    private IEnumerator Start()
    {
        // Let saved designer overrides and layout settle before capturing the endpoint.
        yield return null;
        destination = rect.anchoredPosition;
        started = true;
        rect.anchoredPosition = destination + new Vector2(-28, -8);
        sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        sequence.AppendInterval(Order * .085f);
        sequence.Append(DOTween.To(() => group.alpha, value => group.alpha = value, 1, .18f));
        sequence.Join(DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value,
            destination, .25f).SetEase(Ease.OutCubic));
        sequence.OnComplete(() => group.interactable = group.blocksRaycasts = true);
    }
    private void OnDisable()
    {
        sequence?.Kill();
        if (started) rect.anchoredPosition = destination;
    }
    private void OnDestroy() => sequence?.Kill();
}
