using UnityEditor;

public static class RunWiringTool
{
    [InitializeOnLoadMethod]
    static void Run()
    {
        if (!SessionState.GetBool("WiringToolRun_V4", false))
        {
            SessionState.SetBool("WiringToolRun_V4", true);
            WireFishingMinigamesToStory.InjectFishingQuests();
            InjectKalawFruitChoice.Inject();
            PatchExistingIntros.Patch();
        }
    }
}
