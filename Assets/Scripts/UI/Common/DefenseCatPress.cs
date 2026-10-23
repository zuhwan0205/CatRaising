using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DefenseCatPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action Click, Hold;
    public Func<PointerEventData, bool> BeginDrag;
    public Action<PointerEventData> Drag, EndDrag;
    private bool dragging, dragged;
    private bool pressed, held;
    private float started;
    public void OnPointerDown(PointerEventData e) { pressed=true; held=false; dragged=false; started=Time.unscaledTime; }
    public void OnPointerUp(PointerEventData e) { pressed=false; }
    public void OnPointerExit(PointerEventData e) { pressed=false; }
    public void OnPointerClick(PointerEventData e) { if(!held && !dragged) Click?.Invoke(); }
    public void OnBeginDrag(PointerEventData e)
    {
        pressed=false; dragged=true;
        if(!held) dragging=BeginDrag?.Invoke(e) == true;
    }
    public void OnDrag(PointerEventData e) { if(dragging) Drag?.Invoke(e); }
    public void OnEndDrag(PointerEventData e) { if(dragging) { dragging=false; EndDrag?.Invoke(e); } }
    private void Update()
    {
        if(!pressed || held || Time.unscaledTime-started<.5f) return;
        held=true; pressed=false; Hold?.Invoke();
    }
    private void OnDisable() { pressed=false; if(dragging) { dragging=false; EndDrag?.Invoke(null); } }
}
