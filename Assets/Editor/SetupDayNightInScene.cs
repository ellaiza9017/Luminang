using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool to validate and set up the Day/Night Cycle and HUD Clock in the active scene.
/// Menu: Tools/Luminang/Setup Day-Night in Active Scene
/// </summary>
public class SetupDayNightInScene : EditorWindow
{
    [MenuItem("Tools/Luminang/Setup Day-Night in Active Scene")]
    public static void Run()
    {
        int fixes = 0;

        // ── 1. Ensure TimeManager exists ──────────────────────────────────────
        TimeManager tm = Object.FindFirstObjectByType<TimeManager>();
        if (tm == null)
        {
            GameObject tmGO = new GameObject("TimeManager");
            tm = tmGO.AddComponent<TimeManager>();
            tm.startingTime = 8f;
            tm.realSecondsPerHour = 50f;
            EditorUtility.SetDirty(tmGO);
            Undo.RegisterCreatedObjectUndo(tmGO, "Create TimeManager");
            Debug.Log("<color=cyan>[SetupDayNight] Created TimeManager GameObject.</color>");
            fixes++;
        }
        else
        {
            Debug.Log("[SetupDayNight] TimeManager already exists.");
        }

        // ── 2. Ensure URPDayNightCycle exists ─────────────────────────────────
        URPDayNightCycle cycle = Object.FindFirstObjectByType<URPDayNightCycle>();
        if (cycle == null)
        {
            GameObject cycleGO = new GameObject("URPDayNightCycle");
            cycle = cycleGO.AddComponent<URPDayNightCycle>();
            EditorUtility.SetDirty(cycleGO);
            Undo.RegisterCreatedObjectUndo(cycleGO, "Create URPDayNightCycle");
            Debug.Log("<color=cyan>[SetupDayNight] Created URPDayNightCycle GameObject.</color>");
            fixes++;
        }
        else
        {
            Debug.Log("[SetupDayNight] URPDayNightCycle already exists.");
        }

        // ── 3. Auto-assign Directional Light ──────────────────────────────────
        if (cycle.directionalLight == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    cycle.directionalLight = l;
                    EditorUtility.SetDirty(cycle);
                    Debug.Log($"<color=cyan>[SetupDayNight] Assigned Directional Light: {l.gameObject.name}</color>");
                    fixes++;
                    break;
                }
            }
            if (cycle.directionalLight == null)
                Debug.LogWarning("[SetupDayNight] No Directional Light found in scene! Please assign one manually to URPDayNightCycle.");
        }
        else
        {
            Debug.Log("[SetupDayNight] Directional Light already assigned.");
        }

        // ── 4. Auto-assign lighting presets if they exist ─────────────────────
        AssignPresetIfMissing(cycle, ref cycle.sunrisePreset, "Sunrise");
        AssignPresetIfMissing(cycle, ref cycle.sunnyPreset,   "Sunny");
        AssignPresetIfMissing(cycle, ref cycle.sunsetPreset,  "Sunset");
        AssignPresetIfMissing(cycle, ref cycle.nightPreset,   "Night");

        // ── 5. Ensure TimeWeatherUI exists in HUD ─────────────────────────────
        TimeWeatherUI hudClock = Object.FindFirstObjectByType<TimeWeatherUI>();
        if (hudClock == null)
        {
            // Try to find a Canvas to parent it to
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject clockGO = new GameObject("HUDClock");
                clockGO.transform.SetParent(canvas.transform, false);

                // RectTransform - top-right corner
                RectTransform rt = clockGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-20f, -20f);
                rt.sizeDelta = new Vector2(200f, 50f);

                // Add TMP text for the time
                GameObject textGO = new GameObject("TimeText");
                textGO.transform.SetParent(clockGO.transform, false);
                RectTransform textRT = textGO.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = Vector2.zero;
                textRT.anchoredPosition = Vector2.zero;

                var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "8:00 AM";
                tmp.fontSize = 24;
                tmp.alignment = TMPro.TextAlignmentOptions.Right;
                tmp.color = Color.white;

                hudClock = clockGO.AddComponent<TimeWeatherUI>();
                hudClock.timeText = tmp;
                EditorUtility.SetDirty(clockGO);
                Undo.RegisterCreatedObjectUndo(clockGO, "Create HUDClock");
                Debug.Log("<color=cyan>[SetupDayNight] Created HUDClock UI element in Canvas (top-right).</color>");
                fixes++;
            }
            else
            {
                Debug.LogWarning("[SetupDayNight] No Canvas found in scene! Please add a Canvas and re-run, or manually create a HUD Clock element.");
            }
        }
        else
        {
            Debug.Log("[SetupDayNight] TimeWeatherUI (HUD Clock) already exists.");
        }

        // ── Done ───────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (fixes > 0)
        {
            Debug.Log($"<color=green>[SetupDayNight] Done! {fixes} item(s) set up. Save the scene to preserve changes.</color>");
            EditorUtility.DisplayDialog("Day/Night Setup Complete",
                $"{fixes} item(s) were set up:\n" +
                "• TimeManager\n" +
                "• URPDayNightCycle\n" +
                "• Directional Light\n" +
                "• HUDClock UI\n\n" +
                "Remember to:\n" +
                "1. Assign lighting presets to URPDayNightCycle if not auto-found.\n" +
                "2. Save the scene.",
                "OK");
        }
        else
        {
            Debug.Log("<color=green>[SetupDayNight] Everything was already set up correctly!</color>");
            EditorUtility.DisplayDialog("Day/Night Check", "All Day/Night systems are already present and configured.", "OK");
        }
    }

    private static void AssignPresetIfMissing(URPDayNightCycle cycle, ref DayNightLightingPreset field, string keyword)
    {
        if (field != null) return;

        string[] guids = AssetDatabase.FindAssets($"t:DayNightLightingPreset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains(keyword.ToLower()))
            {
                field = AssetDatabase.LoadAssetAtPath<DayNightLightingPreset>(path);
                EditorUtility.SetDirty(cycle);
                Debug.Log($"<color=cyan>[SetupDayNight] Assigned {keyword} preset: {path}</color>");
                return;
            }
        }
        Debug.LogWarning($"[SetupDayNight] Could not find a {keyword} DayNightLightingPreset. Please assign manually.");
    }
}
