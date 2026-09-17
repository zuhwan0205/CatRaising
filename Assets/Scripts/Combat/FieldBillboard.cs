using UnityEngine;
using UnityEngine.Rendering;

// XZ 평면의 발 위치는 유지하고 2D 그림만 카메라를 향하게 합니다.
[RequireComponent(typeof(SortingGroup))]
[DefaultExecutionOrder(200)]
public class FieldBillboard : MonoBehaviour
{
    public Camera ViewCamera;
    public Transform Feet;
    public float CenterHeight = .4f;
    private SortingGroup group;
    private void Awake() { group=GetComponent<SortingGroup>(); }
    private void LateUpdate()
    {
        if(ViewCamera==null || Feet==null)return;
        transform.rotation=ViewCamera.transform.rotation;
        transform.position=Feet.position+ViewCamera.transform.up*CenterHeight;
        float depth=Vector3.Dot(Feet.position-ViewCamera.transform.position,ViewCamera.transform.forward);
        group.sortingOrder=10000-Mathf.RoundToInt(depth*100);
    }
}
