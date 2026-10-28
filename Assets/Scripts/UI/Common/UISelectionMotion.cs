using DG.Tweening;
using UnityEngine;

// 버튼 자체의 눌림 효과와 충돌하지 않도록 아이콘 또는 팝업 프레임에 붙입니다.
public sealed class UISelectionMotion : MonoBehaviour
{
    private Vector3 restScale;
    private Tween motion;
    private void Awake() => restScale = transform.localScale;

    public static void Play(Transform target, bool opening)
    {
        if (target == null) return;
        var effect = target.GetComponent<UISelectionMotion>();
        if (effect == null) effect = target.gameObject.AddComponent<UISelectionMotion>();
        effect.Animate(opening);
    }
    private void Animate(bool opening)
    {
        ResetMotion();
        if (opening)
        {
            transform.localScale = restScale * .94f;
            motion = DOTween.To(() => transform.localScale, value => transform.localScale = value,
                restScale, .22f).SetEase(Ease.OutCubic).SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        else
        {
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => transform.localScale, value => transform.localScale = value,
                restScale * 1.12f, .10f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(() => transform.localScale, value => transform.localScale = value,
                restScale, .20f).SetEase(Ease.OutBack));
            motion = sequence;
        }
    }
    private void ResetMotion()
    {
        motion?.Kill();
        motion = null;
        transform.localScale = restScale;
    }
    private void OnDisable() => ResetMotion();
    private void OnDestroy() => ResetMotion();
}
