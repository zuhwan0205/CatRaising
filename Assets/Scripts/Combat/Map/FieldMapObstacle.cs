using UnityEngine;

public sealed class FieldMapObstacle : MonoBehaviour
{
    [Min(0)] public float Radius = .65f;
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)));
    }
}
