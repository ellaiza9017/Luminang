using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this script to any NPC, quest trigger, or pickup item in the scene.
/// When the ObjectiveManager's active objective matches 'requiredObjective',
/// the QuestPathTracker will draw a Genshin-style sparkle path towards this target.
/// </summary>
public class QuestTargetMarker : MonoBehaviour
{
    [Header("Objective Sync")]
    [Tooltip("The objective string that activates tracking to this target (e.g. 'Talk to Kalaw').")]
    public string requiredObjective = "";

    [Header("Optional Settings")]
    [Tooltip("Custom point where the line should end. If unassigned, uses this object's Transform position.")]
    public Transform customTargetPoint;

    [Tooltip("Offset applied to the ground target position (e.g. Y + 0.1f to keep above ground).")]
    public Vector3 positionOffset = Vector3.zero;

    private static readonly List<QuestTargetMarker> _allMarkers = new List<QuestTargetMarker>();
    public static IReadOnlyList<QuestTargetMarker> AllMarkers => _allMarkers;

    private void OnEnable()
    {
        if (!_allMarkers.Contains(this))
        {
            _allMarkers.Add(this);
        }
        QuestPathTracker.NotifyMarkersChanged();
    }

    private void OnDisable()
    {
        _allMarkers.Remove(this);
        QuestPathTracker.NotifyMarkersChanged();
    }

    /// <summary>
    /// Checks if this marker matches the currently active objective.
    /// </summary>
    public bool MatchesObjective(string activeObjective)
    {
        if (string.IsNullOrEmpty(requiredObjective) || string.IsNullOrEmpty(activeObjective))
            return false;

        string activeTrim = activeObjective.Trim();
        string reqTrim = requiredObjective.Trim();

        // Exact match is always true
        if (activeTrim.Equals(reqTrim, System.StringComparison.OrdinalIgnoreCase))
            return true;

        // Hide tracker for Counter Objectives (e.g., "Objective (0/4)") so the player explores on their own
        if (activeTrim.Contains("(") && activeTrim.Contains("/"))
            return false;

        return activeTrim.StartsWith(reqTrim, System.StringComparison.OrdinalIgnoreCase) ||
               reqTrim.StartsWith(activeTrim, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the exact world position for the tracker target.
    /// </summary>
    public Vector3 TargetPosition
    {
        get
        {
            Vector3 pos = customTargetPoint != null ? customTargetPoint.position : transform.position;
            return pos + positionOffset;
        }
    }
}
