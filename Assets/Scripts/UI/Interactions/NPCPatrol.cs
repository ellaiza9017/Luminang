using System.Collections;
using UnityEngine;

[System.Serializable]
public struct PatrolWaypoint
{
    [Tooltip("The Transform to walk towards.")]
    public Transform point;
    
    [Tooltip("How long to wait at this waypoint.")]
    public float waitTime;
    
    [Tooltip("The animation state to play while waiting here (e.g. 'Breathing_Idle', 'Waving'). Leave empty to use the default.")]
    public string idleStateName;
}

/// <summary>
/// Moves an NPC along a list of waypoints.
/// Can pause and play specific animations at each waypoint.
/// Hook up PausePatrol() to InteractableNPC's OnInteract, and ResumePatrol() to OnDialogueEnd.
/// </summary>
public class NPCPatrol : MonoBehaviour
{
    [Header("Waypoints")]
    public PatrolWaypoint[] waypoints;

    [Header("Movement")]
    [Tooltip("Walking/Running speed in units per second.")]
    public float speed = 2f;

    [Tooltip("How fast the NPC rotates to face the next waypoint.")]
    public float rotateSpeed = 5f;

    [Tooltip("How close the NPC must get to a waypoint before stopping.")]
    public float waypointReachDistance = 0.5f;

    [Header("Animation States")]
    [Tooltip("Name of the Move/Run state in the Animator Controller.")]
    public string moveStateName = "Run";

    [Tooltip("Default Idle state if a waypoint doesn't specify one.")]
    public string defaultIdleStateName = "Breathing_Idle";

    private int _currentWaypointIndex = 0;
    private bool _isIdling = false;
    private bool _isPausedForInteraction = false;
    private Animator _animator;
    private float _startY;

    void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[NPCPatrol] No waypoints assigned on {gameObject.name}!");
            enabled = false;
            return;
        }

        _animator = GetComponentInChildren<Animator>();

        // Snap to the first waypoint's Y level to prevent floating/sinking
        Vector3 startPos = transform.position;
        _startY = waypoints[0].point.position.y;
        startPos.y = _startY;
        transform.position = startPos;

        SetMoving();
    }

    void Update()
    {
        // Don't patrol if talking to the player or waiting at a waypoint
        if (_isPausedForInteraction || _isIdling || waypoints.Length == 0) return;

        Transform target = waypoints[_currentWaypointIndex].point;

        // Ignore Y difference for rotation/distance checks
        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        // Check if we reached the waypoint
        if (directionToTarget.magnitude <= waypointReachDistance)
        {
            StartCoroutine(IdleAtWaypoint());
            return;
        }

        // Smooth rotation
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        // Move forward but flatten the Y axis to strictly prevent flying!
        Vector3 moveDir = transform.forward;
        moveDir.y = 0f;
        moveDir.Normalize();
        
        transform.position += moveDir * speed * Time.deltaTime;

        // Force snap to original Y to prevent any drift whatsoever
        Vector3 currentPos = transform.position;
        currentPos.y = _startY;
        transform.position = currentPos;
    }

    private IEnumerator IdleAtWaypoint()
    {
        _isIdling = true;
        
        string animToPlay = waypoints[_currentWaypointIndex].idleStateName;
        if (string.IsNullOrEmpty(animToPlay)) animToPlay = defaultIdleStateName;

        if (_animator != null) _animator.Play(animToPlay);

        float waitTime = waypoints[_currentWaypointIndex].waitTime;
        yield return new WaitForSeconds(waitTime);

        // Move to the next waypoint
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
        SetMoving();
        _isIdling = false;
    }

    private void SetMoving()
    {
        if (_animator != null && !_isPausedForInteraction)
        {
            _animator.Play(moveStateName);
        }
    }

    /// <summary>
    /// Call this from InteractableNPC's OnInteract UnityEvent
    /// </summary>
    public void PausePatrol()
    {
        _isPausedForInteraction = true;
        StopAllCoroutines();
        _isIdling = false; // Reset idle state so we can resume properly later
        
        // Let InteractableNPC.SmoothLookAtPlayer() handle the rotation and idle animation during conversation
        if (_animator != null) _animator.Play(defaultIdleStateName);
    }

    /// <summary>
    /// Call this from InteractableNPC's OnDialogueEnd UnityEvent
    /// </summary>
    public void ResumePatrol()
    {
        _isPausedForInteraction = false;
        SetMoving();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i].point == null) continue;
            
            Transform current = waypoints[i].point;
            Transform next = waypoints[(i + 1) % waypoints.Length].point;
            
            if (next != null)
            {
                Gizmos.DrawLine(current.position, next.position);
                Gizmos.DrawSphere(current.position, 0.3f);
            }
        }
    }
#endif
}
