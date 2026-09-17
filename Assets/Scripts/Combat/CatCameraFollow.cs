using UnityEngine;

[DefaultExecutionOrder(100)]
public class CatCameraFollow : MonoBehaviour
{
    public Transform Target;
    [Range(20,80)] public float Tilt = 50;
    public float Distance = 16;

    public void Snap()
    {
        if (Target == null) return;
        transform.rotation = Quaternion.Euler(Tilt,0,0);
        transform.position = Target.position - transform.forward * Distance;
    }
    private void LateUpdate() { Snap(); }
}
