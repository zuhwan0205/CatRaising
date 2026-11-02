using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// XZ 평면의 발 위치는 유지하고 2D 그림만 카메라를 향하게 합니다.
[RequireComponent(typeof(SortingGroup))]
[DefaultExecutionOrder(200)]
public class FieldBillboard : MonoBehaviour
{
    public Camera ViewCamera;
    public Transform Feet;
    public float CenterHeight = .4f;
    private SortingGroup group;
    private Transform visual;
    private static int cameraFrame = -1;
    private static readonly Dictionary<Camera, CameraPose> cameras = new Dictionary<Camera, CameraPose>(4);
    private struct CameraPose
    {
        public Vector3 position, up, forward;
        public Quaternion rotation;
    }
    private bool hasDepth;
    private Vector3 lastFeet, lastCameraPosition, lastForward;
    private int desiredOrder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { cameras.Clear(); cameraFrame = -1; }

    private static CameraPose ReadCamera(Camera camera)
    {
        // Camera follow runs at order 100; billboards share its final pose at order 200.
        // Clear keys each frame so cameras from unloaded scenes are not retained.
        if (cameraFrame != Time.frameCount) { cameras.Clear(); cameraFrame = Time.frameCount; }
        if (!cameras.TryGetValue(camera, out var pose))
        {
            var cameraTransform = camera.transform;
            pose = new CameraPose { position = cameraTransform.position, rotation = cameraTransform.rotation,
                up = cameraTransform.up, forward = cameraTransform.forward };
            cameras.Add(camera, pose);
        }
        return pose;
    }
    private void Awake()
    {
        group = GetComponent<SortingGroup>();
        visual = transform;
    }
    private void LateUpdate()
    {
        if (ViewCamera == null || Feet == null)
            return;
        var camera = ReadCamera(ViewCamera);
        var feet = Feet.position;
        var position = feet + camera.up * CenterHeight;
        // Compare world values: moving parents and pooled objects must still be corrected.
        if (!visual.rotation.Equals(camera.rotation)) visual.rotation = camera.rotation;
        if (!visual.position.Equals(position)) visual.position = position;
        if (!hasDepth || !feet.Equals(lastFeet) || !camera.position.Equals(lastCameraPosition) ||
            !camera.forward.Equals(lastForward))
        {
            desiredOrder = 10000 - Mathf.RoundToInt(Vector3.Dot(feet - camera.position, camera.forward) * 100);
            lastFeet = feet; lastCameraPosition = camera.position; lastForward = camera.forward;
            hasDepth = true;
        }
        if (group.sortingOrder != desiredOrder) group.sortingOrder = desiredOrder;
    }
}
