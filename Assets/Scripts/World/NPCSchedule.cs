using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Time-based schedule for NPCs. Enables or disables NPCPatrol (and optionally
/// other behaviours) based on the current in-game hour from TimeManager.
/// 
/// Usage:
///   1. Add this component alongside NPCPatrol on an NPC.
///   2. Define time slots in the Inspector.
///   3. The component automatically enables/disables NPCPatrol each frame.
/// 
/// Example for a morning-only patrol:
///   Slot: startHour=6, endHour=12, enablePatrol=true
///   All other hours: patrol disabled.
/// </summary>
public class NPCSchedule : MonoBehaviour
{
    [System.Serializable]
    public class TimeSlot
    {
        [Tooltip("Hour of day to START this behaviour (0–23).")]
        [Range(0, 23)] public float startHour = 6f;

        [Tooltip("Hour of day to END this behaviour (0–23).")]
        [Range(0, 23)] public float endHour = 12f;

        [Tooltip("If true, NPCPatrol will be active during this slot. If false, it will be paused.")]
        public bool enablePatrol = true;
    }

    [Header("Schedule Slots")]
    [Tooltip("Define when this NPC should patrol. Outside all slots, patrol is disabled.")]
    public List<TimeSlot> slots = new List<TimeSlot>();

    [Header("Idle Position (Optional)")]
    [Tooltip("If set, the NPC will snap to this Transform when not patrolling.")]
    public Transform idlePosition;

    private NPCPatrol _patrol;
    private bool _patrolWasActive = false;

    void Awake()
    {
        _patrol = GetComponent<NPCPatrol>();
    }

    void Update()
    {
        if (TimeManager.Instance == null) return;
        if (_patrol == null) return;

        float hour = TimeManager.Instance.CurrentTimeOfDay;
        bool shouldPatrol = IsInActiveSlot(hour);

        if (shouldPatrol != _patrolWasActive)
        {
            _patrolWasActive = shouldPatrol;
            _patrol.enabled = shouldPatrol;

            if (!shouldPatrol)
            {
                // Move to idle position if assigned
                if (idlePosition != null)
                {
                    transform.position = idlePosition.position;
                    transform.rotation = idlePosition.rotation;
                }
                // Play default idle animation
                var anim = GetComponentInChildren<Animator>();
                if (anim != null && !string.IsNullOrEmpty(_patrol.defaultIdleStateName))
                {
                    if (anim.HasState(0, Animator.StringToHash(_patrol.defaultIdleStateName)))
                        anim.Play(_patrol.defaultIdleStateName);
                }
                Debug.Log($"[NPCSchedule] {gameObject.name}: Patrol DISABLED (hour={hour:F1}).");
            }
            else
            {
                Debug.Log($"[NPCSchedule] {gameObject.name}: Patrol ENABLED (hour={hour:F1}).");
            }
        }
    }

    private bool IsInActiveSlot(float hour)
    {
        foreach (var slot in slots)
        {
            if (slot.enablePatrol && hour >= slot.startHour && hour < slot.endHour)
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (idlePosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(idlePosition.position, 0.4f);
        }
    }
#endif
}
