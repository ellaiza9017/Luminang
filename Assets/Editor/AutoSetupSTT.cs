using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoSetupSTT
{
    static AutoSetupSTT()
    {
        // DISABLED: This was automatically wiping manual Inspector changes every time Play was pressed!
        // EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Calle_Crisologo")
            {
                Debug.Log("[AutoSetupSTT] Automatically running Setup STT Flow before playing...");
                SetupSTTFlow.SetupFlow();
                AddMicToDialogue.InjectMicButton();
            }
        }
    }
}
