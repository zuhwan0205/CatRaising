using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FieldBillboardChecks
{
    [MenuItem("CatRaising/Checks/Field Billboard")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before running this check.");
        var root = new GameObject("Billboard checks");
        try
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform);
            var feet = new GameObject("Feet").transform;
            feet.SetParent(root.transform);
            var art = new GameObject("Art").AddComponent<FieldBillboard>();
            art.transform.SetParent(feet);
            art.ViewCamera = camera; art.Feet = feet;
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(FieldBillboard).GetMethod("Awake", flags).Invoke(art, null);
            var update = (Action)Delegate.CreateDelegate(typeof(Action), art,
                typeof(FieldBillboard).GetMethod("LateUpdate", flags));
            var reset = typeof(FieldBillboard).GetMethod("ResetCache", BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < 100; i++)
            {
                camera.transform.SetPositionAndRotation(new Vector3(i * .3f, 8, -12), Quaternion.Euler(30 + i * .2f, i, 0));
                feet.position = new Vector3(i * .1f, 0, -i * .2f);
                art.CenterHeight = .4f + i * .001f;
                reset.Invoke(null, null); // Simulate the next frame for this synchronous edit-mode check.
                update(); update();
                var expected = feet.position + camera.transform.up * art.CenterHeight;
                int order = 10000 - Mathf.RoundToInt(Vector3.Dot(feet.position - camera.transform.position, camera.transform.forward) * 100);
                if ((art.transform.position - expected).sqrMagnitude > .000001f ||
                    Quaternion.Angle(art.transform.rotation, camera.transform.rotation) > .01f ||
                    art.GetComponent<SortingGroup>().sortingOrder != order)
                    throw new Exception("Billboard output differs from original formula.");
                // External transform/sort changes must be repaired even with unchanged inputs.
                art.transform.position += Vector3.right;
                art.GetComponent<SortingGroup>().sortingOrder = -123;
                update();
                if ((art.transform.position - expected).sqrMagnitude > .000001f || art.GetComponent<SortingGroup>().sortingOrder != order)
                    throw new Exception("Billboard reuse correction failed.");
            }
            Debug.Log("FIELD_BILLBOARD_CHECKS_PASS: 100 camera/feet/height poses, repeated updates and external changes.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
