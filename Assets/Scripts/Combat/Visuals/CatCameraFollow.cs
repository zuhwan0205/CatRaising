using UnityEngine;

[DefaultExecutionOrder(100)]
public class CatCameraFollow : MonoBehaviour
{
    public Transform Target;
    [Range(20, 80)] public float Tilt = 50;
    public float Distance = 16;
    private float shakeRemaining, nextShake;
    public void CriticalShake()
    {
        if (Time.time < nextShake) return;
        nextShake = Time.time + .25f;
        shakeRemaining = .12f;
    }

    public void Snap()
    {
        if (Target == null)
            return;
        transform.rotation = Quaternion.Euler(Tilt, 0, 0);
        transform.position = Target.position - transform.forward * Distance;
    }
    private void LateUpdate()
    {
        Snap();
        if (Target != null && shakeRemaining > 0)
        {
            shakeRemaining = Mathf.Max(0, shakeRemaining - Time.deltaTime);
            float t = shakeRemaining / .12f;
            transform.position += (transform.right * Mathf.Sin(t * 24) + transform.up * Mathf.Sin(t * 35) * .5f) * (.045f * t);
        }
    }
    private void OnDisable() { shakeRemaining = 0; nextShake = 0; Snap(); }
}
