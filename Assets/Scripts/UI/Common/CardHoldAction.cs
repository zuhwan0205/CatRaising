using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Keep drag handling on the parent ScrollRect so holding never steals scrolling.
public sealed class CardHoldAction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Action Held;
    public RectTransform Progress;
    public bool SuppressClick { get; private set; }
    private PointerEventData pointer;
    private Vector2 start;
    private float elapsed;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        SuppressClick = false;
        pointer = eventData;
        start = eventData.position;
        elapsed = 0;
        SetProgress(0);
    }
    public void OnPointerUp(PointerEventData eventData) => Cancel();
    public void OnPointerExit(PointerEventData eventData) { SuppressClick = true; Cancel(); }
    private void Update()
    {
        if (pointer == null) return;
        float threshold = EventSystem.current == null ? 10 : Mathf.Max(10, EventSystem.current.pixelDragThreshold);
        if (pointer.dragging || (pointer.position - start).sqrMagnitude > threshold * threshold)
        { SuppressClick = true; Cancel(); return; }
        elapsed += Time.unscaledDeltaTime;
        SetProgress(Mathf.Clamp01(elapsed / .6f));
        if (elapsed < .6f) return;
        SuppressClick = true;
        Cancel();
        Held?.Invoke();
    }
    private void SetProgress(float value)
    {
        if (Progress != null) Progress.anchorMax = new Vector2(value, 1);
    }
    private void Cancel() { pointer = null; SetProgress(0); }
    private void OnDisable() => Cancel();
}
