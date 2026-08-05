using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace StarterAssets
{
    public class UIVirtualTouchpad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [System.Serializable]
        public class Event : UnityEvent<Vector2> { }

        [Header("Settings")]
        [Tooltip("Base sensitivity for swiping to look around.")]
        public float lookSensitivity = 1f;
        [Tooltip("If true, moving finger UP will look DOWN.")]
        public bool invertY = false;
        [Tooltip("If true, moving finger RIGHT will look LEFT.")]
        public bool invertX = false;

        [Header("Output")]
        public Event touchZoneOutputEvent;

        private float _screenFactor = 1f;
        private Vector2 _currentDelta;
        private bool _isDragging;

        void Start()
        {
            // Calculate a factor to make sensitivity feel the same across all screen sizes and DPIs.
            if (Screen.dpi > 0)
            {
                _screenFactor = 100f / Screen.dpi;
            }
            else
            {
                _screenFactor = 1000f / Screen.height;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _currentDelta = Vector2.zero;
            OutputPointerEventValue(Vector2.zero);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Capture the delta for this frame
            _currentDelta = eventData.delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _currentDelta = Vector2.zero;
            OutputPointerEventValue(Vector2.zero);
        }

        void LateUpdate()
        {
            if (_isDragging)
            {
                // StarterAssets treats mobile input as a Gamepad and multiplies it by Time.deltaTime.
                // However, on a Desktop PC (in the Editor), it treats it as a high-precision mouse 
                // and DOES NOT multiply it by Time.deltaTime. 
                // We dynamically check if we are on a PC or Phone to fix the 60x speed difference!
                bool isDesktop = SystemInfo.deviceType == DeviceType.Desktop;
                float deltaTime = isDesktop ? 1f : (Time.deltaTime > 0f ? Time.deltaTime : 1f);

                Vector2 scaledDelta = (_currentDelta * _screenFactor * lookSensitivity) / deltaTime;

                if (invertX) scaledDelta.x = -scaledDelta.x;
                if (invertY) scaledDelta.y = -scaledDelta.y;

                OutputPointerEventValue(scaledDelta);

                // Reset delta so if the user stops moving their finger but keeps it held down, 
                // it outputs (0,0) next frame instead of infinitely spinning.
                _currentDelta = Vector2.zero;
            }
        }

        void OutputPointerEventValue(Vector2 pointerPosition)
        {
            touchZoneOutputEvent.Invoke(pointerPosition);
        }
    }
}
