public static class TumbangPresoGameConfig
{
    // Default to ilokano
    public static string TargetLanguage = "ilokano"; 
    
    // Default to Responses
    public static string CategoryFilter = "Responses"; 

    public static RegionMode GetRegionMode()
    {
        switch (TargetLanguage.ToLower())
        {
            case "ilokano": return RegionMode.Ilokano;
            case "cebuano": return RegionMode.Cebuano;
            default:        return RegionMode.Ilokano;
        }
    }
}
