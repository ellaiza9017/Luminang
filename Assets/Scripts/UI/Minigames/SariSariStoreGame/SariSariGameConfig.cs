using UnityEngine;

public static class SariSariGameConfig
{
    /// <summary>The language to display and evaluate. e.g. "cebuano" or "ilokano"</summary>
    public static string TargetLanguage = "ilokano"; // Fallback language
    
    /// <summary>The category/JSON name to load. e.g. "IdentityExpressions"</summary>
    public static string TargetCategory = "IdentityExpressions"; // Fallback category

    public static RegionMode GetRegionMode()
    {
        return TargetLanguage.ToLower() == "cebuano" ? RegionMode.Cebuano : RegionMode.Ilokano;
    }
}
