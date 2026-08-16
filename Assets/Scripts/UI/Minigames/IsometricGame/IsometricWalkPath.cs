using System.Collections.Generic;
using UnityEngine;

namespace Luminang.UI.Minigames.IsometricGame
{
    /// <summary>
    /// Visual Walk Path component for the Isometric/Directions minigame.
    /// Draws colored gizmo lines and node markers in the Scene View so you can map out
    /// the EXACT path line characters will walk on screen.
    /// </summary>
    [ExecuteInEditMode]
    [SelectionBase]
    public class IsometricWalkPath : MonoBehaviour
    {
        [Header("Path Points")]
        [Tooltip("List of corner/waypoint points along this path. If left empty, child Transforms will be used automatically in order.")]
        public List<RectTransform> waypoints = new List<RectTransform>();

        [Header("Visual Gizmos Settings")]
        public Color pathColor = new Color(0.2f, 0.9f, 1f, 1f); // Bright cyan
        public Color startNodeColor = new Color(0.2f, 1f, 0.4f, 1f); // Green for Start
        public Color turnNodeColor = new Color(1f, 0.9f, 0.2f, 1f);  // Yellow for Turns
        public Color endNodeColor = new Color(1f, 0.3f, 0.3f, 1f);   // Red for End
        public float nodeRadius = 20f;
        public bool showDirectionArrows = true;

        /// <summary>
        /// Retrieves the list of RectTransforms that make up this path.
        /// If the waypoints list is empty, automatically collects all child RectTransforms.
        /// </summary>
        public List<RectTransform> GetWaypoints()
        {
            var validList = new List<RectTransform>();

            if (waypoints != null && waypoints.Count > 0)
            {
                foreach (var wp in waypoints)
                {
                    if (wp != null) validList.Add(wp);
                }
            }
            else
            {
                // Auto-collect all direct children in order
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.TryGetComponent<RectTransform>(out var rt))
                    {
                        validList.Add(rt);
                    }
                }
            }

            return validList;
        }

        public bool HasPoints()
        {
            return GetWaypoints().Count >= 2;
        }

        /// <summary>
        /// Returns the EXACT sequence of world positions defined by this path.
        /// Characters will walk strictly along these points with no extra phantom offsets.
        /// </summary>
        public List<Vector3> GetPathWorldPositions()
        {
            var result = new List<Vector3>();
            var nodes = GetWaypoints();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                {
                    result.Add(nodes[i].position);
                }
            }
            return result;
        }

        private void OnDrawGizmos()
        {
            DrawPathGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawPathGizmos(true);
        }

        private void DrawPathGizmos(bool isSelected)
        {
            var nodes = GetWaypoints();
            if (nodes == null || nodes.Count == 0) return;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null) continue;

                Vector3 currentPos = nodes[i].position;

                // Color code: First = Green, Last = Red, Middle = Yellow
                if (i == 0)
                    Gizmos.color = startNodeColor;
                else if (i == nodes.Count - 1)
                    Gizmos.color = endNodeColor;
                else
                    Gizmos.color = turnNodeColor;

                // Draw node sphere
                Gizmos.DrawSphere(currentPos, nodeRadius * (isSelected ? 1.2f : 1f));

                // Draw connecting line to next node
                if (i < nodes.Count - 1 && nodes[i + 1] != null)
                {
                    Vector3 nextPos = nodes[i + 1].position;
                    Gizmos.color = isSelected ? Color.white : pathColor;
                    Gizmos.DrawLine(currentPos, nextPos);

                    if (showDirectionArrows)
                    {
                        Vector3 dir = (nextPos - currentPos).normalized;
                        Vector3 mid = (currentPos + nextPos) * 0.5f;
                        Vector3 right = Vector3.Cross(dir, Vector3.forward).normalized;
                        float arrowSize = nodeRadius * 0.9f;

                        Gizmos.DrawLine(mid, mid - dir * arrowSize + right * (arrowSize * 0.5f));
                        Gizmos.DrawLine(mid, mid - dir * arrowSize - right * (arrowSize * 0.5f));
                    }
                }
            }
        }
    }
}
