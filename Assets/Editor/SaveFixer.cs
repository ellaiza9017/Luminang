using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SaveFixer : EditorWindow
{
    [MenuItem("Luminang/Save Fixer & Cache Tools")]
    public static void ShowWindow()
    {
        GetWindow<SaveFixer>("Save Fixer");
    }

    private readonly string[] _cebObjectives = new[]
    {
        "ceb_01", "ceb_02", "ceb_03", "ceb_04", "ceb_05",
        "ceb_06", "ceb_07", "ceb_08", "ceb_09", "ceb_10",
        "ceb_11", "ceb_12", "ceb_13", "ceb_14", "ceb_15"
    };

    private int _syncUpToIndex = 8; // default: sync up to ceb_09

    private void OnGUI()
    {
        GUILayout.Label("Save Fixer & Cache Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Must be in Play Mode and logged in to use the database tools.", MessageType.Info);

        GUILayout.Space(8);

        // ── Section 1: Force Sync Objectives ────────────────────────────────
        GUILayout.Label("Force Sync Cebuano Objectives", EditorStyles.boldLabel);
        GUILayout.Label("Mark all objectives up to (and including) the selected one as complete in Supabase.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(4);
        _syncUpToIndex = EditorGUILayout.Popup("Sync up to:", _syncUpToIndex, _cebObjectives);

        if (GUILayout.Button($"Force Save ceb_01 → {_cebObjectives[_syncUpToIndex]}"))
        {
            if (!Application.isPlaying) { Debug.LogError("[SaveFixer] Must be in Play Mode!"); return; }
            if (UserProfileManager.Instance == null) { Debug.LogError("[SaveFixer] UserProfileManager not found!"); return; }
            _ = ForceSyncObjectivesAsync(_syncUpToIndex);
        }

        GUILayout.Space(12);

        // ── Section 2: Clear Local Cache ─────────────────────────────────────
        GUILayout.Label("Clear Local Cache (PlayerPrefs)", EditorStyles.boldLabel);
        GUILayout.Label("Wipes local objective backup and current objective. Does NOT touch Supabase.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(4);

        if (GUILayout.Button("Clear Local Objective Cache"))
        {
            PlayerPrefs.DeleteKey("LocalCompletedCebuano");
            PlayerPrefs.DeleteKey("LocalCompletedIlokano");
            PlayerPrefs.DeleteKey("CurrentObjective");
            PlayerPrefs.Save();
            Debug.Log("<color=yellow>[SaveFixer] Local objective cache cleared.</color>");
        }

        if (GUILayout.Button("Clear ALL PlayerPrefs (Full Reset)"))
        {
            if (EditorUtility.DisplayDialog("Clear All PlayerPrefs?",
                "This wipes all local game data including login state, coins, and settings. Supabase is untouched.",
                "Clear", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("<color=red>[SaveFixer] All PlayerPrefs cleared.</color>");
            }
        }
    }

    private async Task ForceSyncObjectivesAsync(int upToIndex)
    {
        var ids = new List<string>();
        for (int i = 0; i <= upToIndex; i++)
            ids.Add(_cebObjectives[i]);

        Debug.Log($"[SaveFixer] Force-saving {ids.Count} objectives to Supabase...");
        await UserProfileManager.Instance.BulkMarkObjectivesCompleted(ids, "Cebuano");
        Debug.Log("<color=green>[SaveFixer] Done! Syncing objective HUD...</color>");

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.SyncObjectiveWithDatabase();
    }
}
