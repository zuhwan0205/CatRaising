using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SettingsGearTween : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    private Transform gear;
    private Tween spin;
    private Quaternion restRotation;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) Play();
    }
    public void OnSubmit(BaseEventData eventData) => Play();

    private void Play()
    {
        if (!GetComponent<Button>().IsInteractable()) return;
        if (gear == null)
        {
            gear = transform.Find("Gear Icon");
            if (gear == null) return;
            restRotation = gear.localRotation;
        }
        ResetSpin();
        float angle = 0;
        spin = DOTween.To(() => angle, value =>
        {
            angle = value;
            gear.localRotation = restRotation * Quaternion.Euler(0, 0, -value);
        }, 180f, .38f).SetEase(Ease.OutCubic).SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() => { if (gear != null) gear.localRotation = restRotation; });
    }
    private void ResetSpin()
    {
        spin?.Kill();
        spin = null;
        if (gear != null) gear.localRotation = restRotation;
    }
    private void OnDisable() => ResetSpin();
    private void OnDestroy() => ResetSpin();
}
