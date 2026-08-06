using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

public class FixHUDClockUI : EditorWindow
{
    [MenuItem("Tools/Luminang/Fix HUD Clock UI")]
    public static void Run()
    {
        // 1. Find the clock text
        GameObject clockGO = GameObject.Find("HUDClock");
        if (clockGO == null)
        {
            Debug.LogWarning("HUDClock not found in scene!");
            return;
        }

        TextMeshProUGUI clockText = clockGO.GetComponentInChildren<TextMeshProUGUI>();
        if (clockText == null) return;

        // 2. Find DialogueUIController to steal its font
        DialogueUIController dialogueUI = Object.FindFirstObjectByType<DialogueUIController>();
        if (dialogueUI != null && dialogueUI.dialogueText != null)
        {
            clockText.font = dialogueUI.dialogueText.font;
        }

        // 3. Fix opacity and alignment
        clockText.color = new Color(1f, 1f, 1f, 0.7f); // Less opacity
        clockText.fontSize = 28;
        clockText.alignment = TextAlignmentOptions.Right;
        
        // 4. Also fix the Period Text if it exists
        TimeWeatherUI ui = clockGO.GetComponent<TimeWeatherUI>();
        if (ui != null && ui.periodText != null)
        {
            if (dialogueUI != null && dialogueUI.dialogueText != null)
                ui.periodText.font = dialogueUI.dialogueText.font;
            ui.periodText.color = new Color(1f, 1f, 1f, 0.7f);
        }

        EditorUtility.SetDirty(clockText);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        Debug.Log("HUD Clock Font and Opacity fixed!");
        EditorUtility.DisplayDialog("HUD Clock Fixed", "The HUD Clock now uses the game's font and has reduced opacity.", "OK");
    }
}
