using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the in-game clock on the HUD and optionally a time-of-day label.
/// Attach this to a Canvas UI element and assign the text references.
/// </summary>
public class TimeWeatherUI : MonoBehaviour
{
    [Header("Clock")]
    [Tooltip("TMP text that shows the formatted in-game time (e.g. 7:30 AM).")]
    public TextMeshProUGUI timeText;

    [Header("Time-of-Day Label (Optional)")]
    [Tooltip("TMP text that shows the Ilocano period label: Bigat / Malem / Rabii.")]
    public TextMeshProUGUI periodText;

    [Header("Time-of-Day Icon (Optional)")]
    [Tooltip("An Image that can swap a sprite to represent day/sunset/night.")]
    public Image periodIcon;
    public Sprite iconMorning;
    public Sprite iconAfternoon;
    public Sprite iconNight;

    // Cached to avoid string allocations every frame
    private string _lastTime = "";
    private string _lastPeriod = "";

    void Update()
    {
        if (TimeManager.Instance == null) return;

        // Clock text
        if (timeText != null)
        {
            string t = TimeManager.Instance.GetTimeString();
            if (t != _lastTime)
            {
                _lastTime = t;
                timeText.text = t;
            }
        }

        // Period label + icon
        string period = GetIlocanoPeriod();
        if (period != _lastPeriod)
        {
            _lastPeriod = period;
            if (periodText != null) periodText.text = period;
            UpdateIcon();
        }
    }

    private string GetIlocanoPeriod()
    {
        if (TimeManager.Instance == null) return "";
        if (TimeManager.Instance.IsMorning)   return "Bigat";    // Morning
        if (TimeManager.Instance.IsAfternoon) return "Malem";    // Afternoon
        return "Rabii";                                            // Evening/Night
    }

    private void UpdateIcon()
    {
        if (periodIcon == null) return;
        if (TimeManager.Instance == null) return;

        if (TimeManager.Instance.IsMorning && iconMorning != null)
            periodIcon.sprite = iconMorning;
        else if (TimeManager.Instance.IsAfternoon && iconAfternoon != null)
            periodIcon.sprite = iconAfternoon;
        else if (iconNight != null)
            periodIcon.sprite = iconNight;
    }
}
