using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "LightingPreset", menuName = "Luminang/Lighting Preset")]
public class DayNightLightingPreset : ScriptableObject
{
    [Header("Directional Light")]
    public float sunElevation = 45f;
    public float sunIntensity = 1f;
    public Color sunColor = Color.white;
    public float shadowStrength = 1f;
    public Color shadowColor = Color.black; // Not explicitly exposed in standard URP DirLight, but good to have

    [Header("Ambient Lighting (Gradient)")]
    public Color skyColor = Color.blue;
    public Color equatorColor = Color.cyan;
    public Color groundColor = Color.gray;

    [Header("Environment")]
    public float reflectionIntensity = 1f;
    public Material skyboxMaterial;

    [Header("URP Global Volume")]
    public Color colorFilter = Color.white;
    public float temperature = 0f;
    public float postExposure = 0f;
    public float contrast = 0f;
    public float saturation = 0f;
    public float vignetteIntensity = 0f;
    public float vignetteSmoothness = 0.2f;
}
