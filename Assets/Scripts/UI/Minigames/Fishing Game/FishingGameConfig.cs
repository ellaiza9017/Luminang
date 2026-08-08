/// <summary>
/// A simple static config that acts as a "messenger" between scenes.
/// Set these values in the scene BEFORE loading the FishingGameScene,
/// and every system inside the game will use them automatically.
/// </summary>
public static class FishingGameConfig
{
    /// <summary>The language to display and evaluate. e.g. "cebuano" or "ilokano"</summary>
    public static string TargetLanguage = "ilokano";

    /// <summary>The category of phrases to use. e.g. "Greetings"</summary>
    public static string CategoryFilter = "Greetings";

    /// <summary>Helper to get the matching RegionMode for the STT evaluator.</summary>
    public static RegionMode GetRegionMode()
    {
        switch (TargetLanguage.ToLower())
        {
            case "ilokano": return RegionMode.Ilokano;
            case "cebuano": return RegionMode.Cebuano;
            default:        return RegionMode.Cebuano;
        }
    }
}
