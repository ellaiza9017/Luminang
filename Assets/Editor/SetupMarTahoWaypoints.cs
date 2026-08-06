using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool to set up Mar-Taho's patrol waypoints and morning-only schedule.
/// Menu: Tools/Luminang/Setup Mar-Taho Waypoints
/// </summary>
public class SetupMarTahoWaypoints : EditorWindow
{
    [MenuItem("Tools/Luminang/Setup Mar-Taho Waypoints")]
    public static void Run()
    {
        // ── Find Mar-Taho ─────────────────────────────────────────────────────
        GameObject marTaho = FindMarTaho();
        if (marTaho == null)
        {
            EditorUtility.DisplayDialog("Mar-Taho Not Found",
                "Could not find a GameObject containing 'Mar' or 'Taho' in its name.\n\n" +
                "Please open the scene containing Mar-Taho and try again, or manually:\n" +
                "1. Add NPCPatrol to Mar-Taho\n" +
                "2. Add NPCSchedule (startHour=6, endHour=12)\n" +
                "3. Create Waypoint GameObjects and assign them.",
                "OK");
            return;
        }

        Debug.Log($"<color=cyan>[MarTaho Setup] Found: {marTaho.name}</color>");

        // ── Create Waypoints container ────────────────────────────────────────
        string waypointContainerName = "MarTaho_Waypoints";
        GameObject container = GameObject.Find(waypointContainerName);
        if (container == null)
        {
            container = new GameObject(waypointContainerName);
            Undo.RegisterCreatedObjectUndo(container, "Create MarTaho Waypoints");
        }

        // Create 4 waypoints around Mar-Taho's current position
        Vector3 basePos = marTaho.transform.position;
        float spread = 8f;
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(0, 0, spread),
            new Vector3(spread, 0, 0),
            new Vector3(0, 0, -spread),
            new Vector3(-spread, 0, 0),
        };

        Transform[] wpTransforms = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            string wpName = $"MarTaho_WP_{i + 1}";
            GameObject existing = GameObject.Find(wpName);
            if (existing != null)
            {
                wpTransforms[i] = existing.transform;
                Debug.Log($"[MarTaho Setup] Waypoint {wpName} already exists, reusing.");
            }
            else
            {
                GameObject wp = new GameObject(wpName);
                wp.transform.parent = container.transform;
                wp.transform.position = basePos + offsets[i];
                Undo.RegisterCreatedObjectUndo(wp, $"Create {wpName}");
                wpTransforms[i] = wp.transform;
                Debug.Log($"<color=cyan>[MarTaho Setup] Created {wpName} at {wp.transform.position}</color>");
            }
        }

        // ── Add or configure NPCPatrol ─────────────────────────────────────────
        NPCPatrol patrol = marTaho.GetComponent<NPCPatrol>();
        if (patrol == null)
        {
            patrol = marTaho.AddComponent<NPCPatrol>();
            Undo.RecordObject(marTaho, "Add NPCPatrol to Mar-Taho");
        }

        // Build waypoint array
        PatrolWaypoint[] waypoints = new PatrolWaypoint[4];
        for (int i = 0; i < 4; i++)
        {
            waypoints[i] = new PatrolWaypoint
            {
                point = wpTransforms[i],
                waitTime = 2f,
                idleStateName = ""
            };
        }
        patrol.waypoints = waypoints;
        patrol.speed = 1.5f;
        EditorUtility.SetDirty(patrol);

        // ── Add or configure NPCSchedule (Morning Only: 6 AM – 12 PM) ─────────
        NPCSchedule schedule = marTaho.GetComponent<NPCSchedule>();
        if (schedule == null)
        {
            schedule = marTaho.AddComponent<NPCSchedule>();
            Undo.RecordObject(marTaho, "Add NPCSchedule to Mar-Taho");
        }

        schedule.slots = new System.Collections.Generic.List<NPCSchedule.TimeSlot>
        {
            new NPCSchedule.TimeSlot { startHour = 6f, endHour = 12f, enablePatrol = true }
        };
        EditorUtility.SetDirty(schedule);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log($"<color=green>[MarTaho Setup] Done! NPCPatrol + NPCSchedule (morning 6–12) configured on {marTaho.name}.\n" +
                  $"Waypoints placed around {basePos}. You can reposition them freely in the Scene view.</color>");

        EditorUtility.DisplayDialog("Mar-Taho Setup Complete",
            $"Mar-Taho ({marTaho.name}) has been configured:\n\n" +
            "• NPCPatrol: 4 waypoints placed around current position\n" +
            "• NPCSchedule: Active only 6:00 AM – 12:00 PM\n\n" +
            "You can move the waypoint GameObjects in the Scene view to adjust the patrol route.\n\n" +
            "Remember to Save the scene!",
            "OK");
    }

    private static GameObject FindMarTaho()
    {
        // Try common naming patterns
        string[] patterns = { "Mar-Taho", "MarTaho", "Mar_Taho", "Mar Taho", "martaho" };
        foreach (var p in patterns)
        {
            GameObject go = GameObject.Find(p);
            if (go != null) return go;
        }

        // Fallback: search all objects for name containing "mar" and "taho"
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            string lower = go.name.ToLower();
            if (lower.Contains("mar") && lower.Contains("taho"))
                return go;
            // Also match just "martaho" as a single token
            if (lower.Contains("martaho"))
                return go;
        }
        return null;
    }
}
