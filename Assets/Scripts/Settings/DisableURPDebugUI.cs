using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// This script automatically runs when the game starts and permanently disables the URP Display Stats/Debug UI
/// that accidentally pops up when 3 fingers touch the screen on mobile devices.
/// You do not need to attach this script to any GameObject; it runs automatically.
/// </summary>
public static class DisableURPDebugUI
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void DisableDebugUI()
    {
        if (DebugManager.instance != null)
        {
            DebugManager.instance.enableRuntimeUI = false;
            Debug.Log("[DisableURPDebugUI] URP Runtime Debug UI has been successfully disabled.");
        }
    }
}
