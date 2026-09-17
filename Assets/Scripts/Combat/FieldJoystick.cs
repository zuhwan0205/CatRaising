using UnityEngine;
using UnityEngine.EventSystems;

public class FieldJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public System.Action<Vector2> InputChanged;
    public RectTransform Handle;
    private int? pointer;
    private RectTransform Area => (RectTransform)transform;
    public void OnPointerDown(PointerEventData e)
    {
        if (pointer.HasValue) return;
        pointer=e.pointerId; OnDrag(e);
    }
    public void OnDrag(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Area,e.position,e.pressEventCamera,out Vector2 point)) return;
        float radius=Mathf.Max(1,Mathf.Min(Area.rect.width,Area.rect.height)*.35f);
        Vector2 input=Vector2.ClampMagnitude(point/radius,1);
        Handle.anchoredPosition=input*radius;
        InputChanged?.Invoke(input);
    }
    public void OnPointerUp(PointerEventData e) { if(pointer==e.pointerId) ResetInput(); }
    public void ResetInput() { pointer=null; if(Handle!=null) Handle.anchoredPosition=Vector2.zero; InputChanged?.Invoke(Vector2.zero); }
    private void OnDisable() { ResetInput(); }
}
