using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CatMovementController : MonoBehaviour
{
    public bool Automatic { get; private set; } = true;
    public Vector2 StickInput { get; set; }
    public Transform[] Targets { get; set; }
    public float Speed = 3;
    public float StopDistance = 1.5f;
    public float FieldHalfSize = 18;

    public void SetAutomatic(bool automatic) { Automatic = automatic; StickInput = Vector2.zero; }

    private void Update()
    {
        Vector2 direction = Automatic ? AutoDirection() : ManualDirection();
        Vector2 next = new Vector2(transform.position.x, transform.position.z) + Vector2.ClampMagnitude(direction, 1) * Speed * Mathf.Min(Time.deltaTime, .1f);
        next.x = Mathf.Clamp(next.x, -FieldHalfSize, FieldHalfSize);
        next.y = Mathf.Clamp(next.y, -FieldHalfSize, FieldHalfSize);
        transform.position = new Vector3(next.x,0,next.y);
    }

    private Vector2 AutoDirection()
    {
        Transform nearest = null;
        float best = float.MaxValue;
        if (Targets != null) foreach (var target in Targets)
        {
            if (target == null || !target.gameObject.activeInHierarchy) continue;
            Vector3 offset = target.position-transform.position;
            float distance = offset.x*offset.x + offset.z*offset.z;
            if (distance < best) { best=distance; nearest=target; }
        }
        if (nearest == null) return Vector2.zero;
        Vector3 displacement = nearest.position-transform.position;
        Vector2 delta = new Vector2(displacement.x,displacement.z);
        // 이동 한 번으로 유지 거리를 지나치지 않습니다.
        float step = Speed * Mathf.Min(Time.deltaTime,.1f);
        return delta.normalized * Mathf.Clamp01((delta.magnitude-StopDistance)/Mathf.Max(step,.0001f));
    }

    private Vector2 ManualDirection()
    {
        Vector2 keys = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            keys.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            keys.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
        }
#else
        keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        return keys.sqrMagnitude > 0 ? keys : StickInput;
    }

    private void OnDisable() { StickInput = Vector2.zero; }
}
