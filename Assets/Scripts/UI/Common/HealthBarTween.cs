using DG.Tweening;
using UnityEngine;

public sealed class HealthBarTween : MonoBehaviour
{
    private Tween animation;
    private float target = 1;
    public static void Set(Transform bar, float ratio)
    {
        var effect = bar.GetComponent<HealthBarTween>();
        if (effect == null) effect = bar.gameObject.AddComponent<HealthBarTween>();
        effect.target = Mathf.Clamp01(ratio);
        effect.animation?.Kill();
        effect.animation = DOTween.To(() => bar.localScale.x,
            x => bar.localScale = new Vector3(x, 1, 1), effect.target, .22f)
            .SetEase(Ease.OutQuad).SetLink(bar.gameObject, LinkBehaviour.KillOnDisable);
    }
    private void OnDisable()
    {
        animation?.Kill();
        transform.localScale = new Vector3(target, 1, 1);
    }
}
