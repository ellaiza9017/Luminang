using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class URPDayNightCycle : MonoBehaviour
{
    [Header("Lighting Presets")]
    public DayNightLightingPreset sunrisePreset;
    public DayNightLightingPreset sunnyPreset;
    public DayNightLightingPreset sunsetPreset;
    public DayNightLightingPreset nightPreset;

    [Header("System References")]
    public Light directionalLight;
    public Volume globalVolume;
    
    private ColorAdjustments _colorAdjustments;
    private WhiteBalance _whiteBalance;
    private Vignette _vignette;

    void Start()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _colorAdjustments);
            globalVolume.profile.TryGet(out _whiteBalance);
            globalVolume.profile.TryGet(out _vignette);
        }
    }

    void Update()
    {
        TimeManager tm = TimeManager.Instance;
        if (tm == null)
        {
            // Find TimeManager in scene at runtime/edit time if Instance is not set
            tm = FindFirstObjectByType<TimeManager>();
            if (tm == null)
            {
                return; // Suppress warning to avoid console spam in Edit mode
            }
        }
        if (sunrisePreset == null || sunnyPreset == null || sunsetPreset == null || nightPreset == null) return;

        // Try to get volume components if they were missing (e.g., added at runtime)
        if (_colorAdjustments == null || _vignette == null || _whiteBalance == null)
        {
            if (globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out _colorAdjustments);
                globalVolume.profile.TryGet(out _whiteBalance);
                globalVolume.profile.TryGet(out _vignette);
            }
        }

        float time = tm.CurrentTimeOfDay; // 0 to 24

        // Transition Logic
        DayNightLightingPreset fromPreset = nightPreset;
        DayNightLightingPreset toPreset = nightPreset;
        float percent = 0f;

        if (time >= 5f && time < 8f) // SUNRISE
        {
            fromPreset = nightPreset;
            toPreset = sunrisePreset;
            percent = Mathf.InverseLerp(5f, 8f, time);
        }
        else if (time >= 8f && time < 17f) // SUNNY (8 AM to 5 PM)
        {
            fromPreset = sunrisePreset;
            toPreset = sunnyPreset;
            percent = Mathf.InverseLerp(8f, 17f, time);
        }
        else if (time >= 17f && time < 19f) // SUNSET (5 PM to 7 PM)
        {
            fromPreset = sunnyPreset;
            toPreset = sunsetPreset;
            percent = Mathf.InverseLerp(17f, 19f, time);
        }
        else // NIGHT
        {
            fromPreset = sunsetPreset;
            toPreset = nightPreset;
            
            if (time >= 19f) // 7 PM to Midnight
            {
                percent = Mathf.InverseLerp(19f, 24f, time);
            }
            else // Midnight to 5 AM
            {
                percent = Mathf.InverseLerp(0f, 5f, time);
            }
            
            // To ensure night blends smoothly into night, which doesn't make sense unless it's fading from Sunset to Night
            if (time >= 19f)
            {
                fromPreset = sunsetPreset;
                toPreset = nightPreset;
                percent = Mathf.InverseLerp(19f, 21f, time); // Transition fully to night over 2 hours
                if (time >= 21f) percent = 1f; 
            }
            else
            {
                fromPreset = nightPreset;
                toPreset = nightPreset;
                percent = 1f;
            }
        }

        ApplyInterpolation(fromPreset, toPreset, percent, time);
    }

    private void ApplyInterpolation(DayNightLightingPreset from, DayNightLightingPreset to, float t, float timeOfDay)
    {
        // 1. Directional Light
        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, t);
            directionalLight.color = Color.Lerp(from.sunColor, to.sunColor, t);
            directionalLight.shadowStrength = Mathf.Lerp(from.shadowStrength, to.shadowStrength, t);

            // Sun/Moon Rotation
            // Instead of interpolating elevation directly, we rotate continuously based on the time.
            // 6 AM = 0 degrees (sunrise), 12 PM = 90 degrees (noon), 6 PM = 180 degrees (sunset)
            float sunRotationAngle = (timeOfDay - 6f) / 24f * 360f; 
            directionalLight.transform.localRotation = Quaternion.Euler(sunRotationAngle, -30f, 0f);
        }

        // 2. Ambient Lighting
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(from.skyColor, to.skyColor, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(from.equatorColor, to.equatorColor, t);
        RenderSettings.ambientGroundColor = Color.Lerp(from.groundColor, to.groundColor, t);
        RenderSettings.reflectionIntensity = Mathf.Lerp(from.reflectionIntensity, to.reflectionIntensity, t);

        Material targetSkybox = t > 0.5f ? to.skyboxMaterial : from.skyboxMaterial;
        if (targetSkybox != null && RenderSettings.skybox != targetSkybox)
        {
            RenderSettings.skybox = targetSkybox;
        }

        // 3. URP Volume
        if (_colorAdjustments != null)
        {
            _colorAdjustments.colorFilter.value = Color.Lerp(from.colorFilter, to.colorFilter, t);
            _colorAdjustments.postExposure.value = Mathf.Lerp(from.postExposure, to.postExposure, t);
            _colorAdjustments.contrast.value = Mathf.Lerp(from.contrast, to.contrast, t);
            _colorAdjustments.saturation.value = Mathf.Lerp(from.saturation, to.saturation, t);
        }
        
        if (_whiteBalance != null)
        {
            _whiteBalance.temperature.value = Mathf.Lerp(from.temperature, to.temperature, t);
        }

        if (_vignette != null)
        {
            _vignette.intensity.value = Mathf.Lerp(from.vignetteIntensity, to.vignetteIntensity, t);
            _vignette.smoothness.value = Mathf.Lerp(from.vignetteSmoothness, to.vignetteSmoothness, t);
        }
    }
}
