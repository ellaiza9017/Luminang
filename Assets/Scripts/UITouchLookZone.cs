using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections.Generic;

public class UITouchLookZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Look Sensitivity")]
    [Tooltip("Adjust this to make the camera move faster or slower when swiping.")]
    public float sensitivity = 2.2f;

    [Header("Output")]
    public UnityEvent<Vector2> touchZoneOutputEvent;

    private Vector2 _lookDelta;
    
    // Multi-touch tracking
    private Dictionary<int, Vector2> _activePointers = new Dictionary<int, Vector2>();
    private bool _isTracking = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        _isTracking = true;
        
        if (!_activePointers.ContainsKey(eventData.pointerId))
        {
            _activePointers.Add(eventData.pointerId, eventData.position);
        }

        if (_activePointers.Count == 2)
        {
            _lookDelta = Vector2.zero;
        }
        
        // Debug.Log($"[LookZone] Pointer Down: {eventData.pointerId}. Active: {_activePointers.Count}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isTracking) return;

        if (_activePointers.ContainsKey(eventData.pointerId))
        {
            _activePointers[eventData.pointerId] = eventData.position;
        }

        // If we have multiple touches on THIS zone, we should NEVER look
        if (_activePointers.Count >= 2) 
        {
            _lookDelta = Vector2.zero;
            return;
        }

        // Normalize based on physical Screen DPI instead of resolution!
        // This guarantees that a 1-inch physical swipe on the glass will rotate the camera
        // the EXACT same amount on EVERY phone, regardless of how big or small the screen is.
        float dpi = Screen.dpi;
        if (dpi == 0) dpi = 160f; // Fallback if Unity can't detect DPI

        float normalizedX = eventData.delta.x / dpi;
        float normalizedY = eventData.delta.y / dpi;

        if (_activePointers.Count == 1)
        {
            // Since we are now using DPI (inches), we lower the multiplier so it feels natural.
            _lookDelta = new Vector2(normalizedX, normalizedY) * (sensitivity * 1.5f);
        }
    }



    public void OnPointerUp(PointerEventData eventData)
    {
        if (_activePointers.ContainsKey(eventData.pointerId))
        {
            _activePointers.Remove(eventData.pointerId);
        }

        if (_activePointers.Count == 0)
        {
            _isTracking = false;
            _lookDelta = Vector2.zero;
            touchZoneOutputEvent.Invoke(Vector2.zero);
        }
    }

    private void Update()
    {
        // Output Look Delta
        if (_activePointers.Count < 2 && _lookDelta.sqrMagnitude > 0.001f)
        {
            touchZoneOutputEvent.Invoke(_lookDelta);
        }
        
        // Always reset lookDelta in Update to prevent build-up or leakage into next frame
        _lookDelta = Vector2.zero;
    }
}
