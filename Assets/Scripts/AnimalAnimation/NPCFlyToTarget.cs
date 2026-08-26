using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Smoothly moves a flying NPC (like Flowerpecker / Tiptip) from its initial starting position (Pos A)
/// to a target position (Pos B) when triggered, with a gentle arc and rotation.
/// Compatible with HoverAnimation - pauses bobbing during flight.
/// </summary>
public class NPCFlyToTarget : MonoBehaviour
{
    [Header("Flight Target Settings")]
    [Tooltip("The destination Transform where the NPC should fly down to.")]
    public Transform targetPoint;

    [Tooltip("Duration of the flight in seconds.")]
    public float flyDuration = 2.0f;

    [Tooltip("How high the arc peaks above the straight-line path (0 = no arc).")]
    public float arcHeight = 1.5f;

    [Tooltip("Smooth easing curve for the flight path. Default: ease in-out.")]
    public AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("If true, smoothly rotates the NPC toward the target while flying.")]
    public bool rotateTowardFlightDirection = true;

    [Tooltip("If true, rotates the NPC to face the player after landing.")]
    public bool facePlayerOnArrival = true;

    [Header("Arrival Events")]
    [Tooltip("Fires automatically when the NPC finishes flying to the target point.")]
    public UnityEvent OnArrival;

    [Header("State Persistence")]
    [Tooltip("If this objective is already completed when the scene loads, the NPC will instantly snap to the target point instead of waiting to fly.")]
    public string snapToTargetIfObjectiveCompleted;

    private bool _isFlying = false;

    public bool IsFlying => _isFlying;

    private void Start()
    {
        // Automatically snap to ground if the player already passed the trigger objective in a previous session
        if (!string.IsNullOrEmpty(snapToTargetIfObjectiveCompleted) && ObjectiveManager.Instance != null)
        {
            if (ObjectiveManager.Instance.IsObjectiveCompleted(snapToTargetIfObjectiveCompleted))
            {
                SnapToTarget();
            }
        }
    }

    public void SnapToTarget()
    {
        if (targetPoint == null) return;
        
        transform.position = targetPoint.position;
        
        HoverAnimation hover = GetComponent<HoverAnimation>();
        if (hover != null)
        {
            hover.SetBasePosition(targetPoint.position);
            hover.enableHover = true;
        }

        if (facePlayerOnArrival)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Vector3 lookAtPos = player.transform.position;
                lookAtPos.y = transform.position.y;
                transform.rotation = Quaternion.LookRotation((lookAtPos - transform.position).normalized, Vector3.up);
            }
            else transform.rotation = targetPoint.rotation;
        }
        else transform.rotation = targetPoint.rotation;
    }

    /// <summary>
    /// Call this to start the smooth fly sequence.
    /// Can be hooked up directly in a ProximityTrigger's OnTriggered event.
    /// </summary>
    public void FlyToTarget()
    {
        if (_isFlying) return;
        if (targetPoint == null)
        {
            Debug.LogWarning($"[NPCFlyToTarget] No Target Point assigned on {gameObject.name}!");
            OnArrival?.Invoke();
            return;
        }

        StartCoroutine(FlyRoutine());
    }

    private IEnumerator FlyRoutine()
    {
        _isFlying = true;

        // Pause HoverAnimation so it doesn't fight the fly path
        HoverAnimation hover = GetComponent<HoverAnimation>();
        if (hover != null) hover.enableHover = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 endPos = targetPoint.position;

        float elapsed = 0f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);
            float curveT = flightCurve.Evaluate(t);

            // Quadratic arc: straight-line lerp + sine-based upward arc peak
            Vector3 straightPos = Vector3.Lerp(startPos, endPos, curveT);
            float arcOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            Vector3 arcedPos = new Vector3(straightPos.x, straightPos.y + arcOffset, straightPos.z);

            transform.position = arcedPos;

            // Smooth rotation toward direction of flight
            if (rotateTowardFlightDirection && (endPos - startPos).sqrMagnitude > 0.01f)
            {
                // Direction of the arced path tangent (next frame estimate)
                float tNext = Mathf.Clamp01((elapsed + 0.05f) / flyDuration);
                float curveNext = flightCurve.Evaluate(tNext);
                Vector3 nextStraight = Vector3.Lerp(startPos, endPos, curveNext);
                float nextArc = Mathf.Sin(tNext * Mathf.PI) * arcHeight;
                Vector3 nextPos = new Vector3(nextStraight.x, nextStraight.y + nextArc, nextStraight.z);

                Vector3 flyDir = (nextPos - arcedPos);
                if (flyDir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(flyDir.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                }
            }

            yield return null;
        }

        // Snap to exact end position
        transform.position = endPos;

        // Resume hover bobbing at the new landing position
        if (hover != null)
        {
            hover.SetBasePosition(endPos);
            hover.enableHover = true;
        }

        // Face player if requested
        if (facePlayerOnArrival)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Vector3 lookAtPos = player.transform.position;
                lookAtPos.y = transform.position.y;
                transform.rotation = Quaternion.LookRotation((lookAtPos - transform.position).normalized, Vector3.up);
            }
            else
            {
                transform.rotation = targetPoint.rotation;
            }
        }
        else
        {
            transform.rotation = targetPoint.rotation;
        }

        _isFlying = false;
        Debug.Log($"[NPCFlyToTarget] {gameObject.name} arrived at target point.");
        OnArrival?.Invoke();
    }
}
