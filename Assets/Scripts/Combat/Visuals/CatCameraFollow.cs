using UnityEngine;

[DefaultExecutionOrder(100)]
public class CatCameraFollow : MonoBehaviour
{
    public Transform Target;
    [Range(20, 80)] public float Tilt = 50;
    public float Distance = 16;

    private Transform[] groupTargets;
    private Camera view;
    private float originalSize;

    public void FollowGroup(Transform[] targets)
    {
        ClearGroup();
        view = GetComponent<Camera>();
        originalSize = view.orthographicSize;
        groupTargets = targets;
    }

    public void ClearGroup()
    {
        if (groupTargets != null && view != null) view.orthographicSize = originalSize;
        groupTargets = null;
    }

    private bool FrameGroup()
    {
        if (groupTargets == null || view == null) return false;
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var member in groupTargets)
            if (member != null && member.gameObject.activeInHierarchy) { center += member.position; count++; }
        if (count == 0) return false;
        center /= count;
        transform.rotation = Quaternion.Euler(Tilt, 0, 0);
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        foreach (var member in groupTargets)
        {
            if (member == null || !member.gameObject.activeInHierarchy) continue;
            Vector3 offset = member.position - center;
            float x = Vector3.Dot(offset, transform.right);
            float y = Vector3.Dot(offset, transform.up);
            // 커진 캐릭터와 머리 위 HP 바까지 화면 안에 포함합니다.
            minX = Mathf.Min(minX, x - 1.8f); maxX = Mathf.Max(maxX, x + 1.8f);
            minY = Mathf.Min(minY, y - .5f); maxY = Mathf.Max(maxY, y + 3.5f);
        }
        center += transform.right * ((minX + maxX) * .5f) + transform.up * ((minY + maxY) * .5f);
        float size = Mathf.Max(originalSize, (maxY - minY) / 1.5f,
            (maxX - minX) / (1.5f * Mathf.Max(.1f, view.aspect)));
        // 멀어질 때는 즉시 포함하고, 가까워질 때는 천천히 확대합니다.
        view.orthographicSize = size >= view.orthographicSize ? size :
            Mathf.Lerp(view.orthographicSize, size, 1 - Mathf.Exp(-4 * Time.deltaTime));
        transform.position = center - transform.forward * Distance;
        return true;
    }

    public void Snap()
    {
        if (FrameGroup()) return;
        if (Target == null)
            return;
        transform.rotation = Quaternion.Euler(Tilt, 0, 0);
        transform.position = Target.position - transform.forward * Distance;
    }
    private void LateUpdate()
    {
        Snap();
    }
}
