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

        // Read the sensitivity from PlayerPrefs. Slider range in Inspector should be 1.0 to 10.0.
        float currentSensitivity = PlayerPrefs.GetFloat("LookSensitivity", 5.0f);

        if (_activePointers.Count == 1)
        {
            // Multiply raw pixel delta by sensitivity * 0.03f.
            // Slider range 1-10: at 1 = very slow (0.03x), at 5 = comfortable (0.15x), at 10 = fast (0.3x).
            _lookDelta = new Vector2(eventData.delta.x, eventData.delta.y) * (currentSensitivity * 0.03f);
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
        // Output Look Delta EVERY frame. 
        // If we only output when > 0, the camera keeps spinning when the finger stops moving!
        if (_activePointers.Count < 2)
        {
            touchZoneOutputEvent.Invoke(_lookDelta);
        }
        
        // Always reset lookDelta in Update to prevent build-up or leakage into next frame
        _lookDelta = Vector2.zero;
    }
}
