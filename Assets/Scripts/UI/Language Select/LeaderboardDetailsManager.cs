using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the details side panel (RightGroup) of the Leaderboard UI.
/// </summary>
public class LeaderboardDetailsManager : MonoBehaviour
{
    [Header("UI References")]
    public Image avatarImage;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI overallProgressText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI ilokanoProgressText;
    public TextMeshProUGUI ilokanoLessonsText;  // e.g. "4 / 12 Lessons"
    public TextMeshProUGUI cebuanoProgressText;
    public TextMeshProUGUI cebuanoLessonsText;  // e.g. "3 / 12 Lessons"
    public TextMeshProUGUI lastActiveText;

    [Header("Progress Sliders")]
    [Tooltip("Slider for overall progress (0 to 1 range expected).")]
    public Slider overallProgressSlider;
    [Tooltip("Slider for Ilokano progress.")]
    public Slider ilokanoProgressSlider;
    [Tooltip("Slider for Cebuano progress.")]
    public Slider cebuanoProgressSlider;
    
    [Header("Journal Preview")]
    public LeaderboardJournalPreview journalPreview;

    public void DisplayPlayerDetails(LeaderboardEntry entry)
    {
        if (entry == null) return;

        if (usernameText != null) usernameText.text = entry.Username;
        if (rankText != null) rankText.text = $"Rank #{entry.Rank}";
        if (overallProgressText != null) overallProgressText.text = $"{entry.OverallProgress:F1}%";
        if (coinsText != null) coinsText.text = $"{entry.OverallCoins.ToString("N0")} Coins";
        if (ilokanoProgressText != null) ilokanoProgressText.text = $"{entry.IlokanoProgress:F1}%";
        if (ilokanoLessonsText != null) ilokanoLessonsText.text = $"{entry.IlokanoLessonsCompleted} / 12";
        if (cebuanoProgressText != null) cebuanoProgressText.text = $"{entry.CebuanoProgress:F1}%";
        if (cebuanoLessonsText != null) cebuanoLessonsText.text = $"{entry.CebuanoLessonsCompleted} / 12";
        
        // Compute relative active time
        if (lastActiveText != null)
        {
            lastActiveText.text = GetRelativeTimeString(entry.LastActive);
        }

        // Set Slider values
        SetSliderValue(overallProgressSlider, entry.OverallProgress);
        SetSliderValue(ilokanoProgressSlider, entry.IlokanoProgress);
        SetSliderValue(cebuanoProgressSlider, entry.CebuanoProgress);

        if (journalPreview != null)
        {
            journalPreview.SetPlayer(entry);
        }

        if (avatarImage != null)
        {
            avatarImage.color = Color.white; // Default fallback color
            if (!string.IsNullOrEmpty(entry.AvatarUrl) && AvatarManager.Instance != null)
            {
                LoadAvatarAsync(entry.AvatarUrl);
            }
        }
    }

    private async void LoadAvatarAsync(string url)
    {
        var texture = await AvatarManager.Instance.GetAvatarTexture(url);
        if (texture != null && avatarImage != null)
        {
            avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            avatarImage.color = Color.white;
        }
    }

    private void SetSliderValue(Slider slider, float percentageValue)
    {
        if (slider == null) return;

        // If the slider is configured for 0-1, convert the percentage (e.g. 85.5) to 0.0-1.0
        if (slider.maxValue <= 1.1f)
        {
            slider.value = percentageValue / 100f;
        }
        else
        {
            slider.value = percentageValue;
        }
    }

    public void ClearDetails()
    {
        if (usernameText != null) usernameText.text = "-";
        if (rankText != null) rankText.text = "-";
        if (overallProgressText != null) overallProgressText.text = "-";
        if (coinsText != null) coinsText.text = "-";
        if (ilokanoProgressText != null) ilokanoProgressText.text = "-";
        if (ilokanoLessonsText != null) ilokanoLessonsText.text = "-";
        if (cebuanoProgressText != null) cebuanoProgressText.text = "-";
        if (cebuanoLessonsText != null) cebuanoLessonsText.text = "-";
        if (lastActiveText != null) lastActiveText.text = "-";

        if (overallProgressSlider != null) overallProgressSlider.value = 0f;
        if (ilokanoProgressSlider != null) ilokanoProgressSlider.value = 0f;
        if (cebuanoProgressSlider != null) cebuanoProgressSlider.value = 0f;
    }

    private string GetRelativeTimeString(System.DateTime? lastActiveTimeNullable)
    {
        if (!lastActiveTimeNullable.HasValue) return "Played long ago";

        System.DateTime lastActiveTime = lastActiveTimeNullable.Value;
        
        System.TimeSpan difference = System.DateTime.Now - lastActiveTime;

        if (difference.TotalDays < 0)
        {
            return "Active now";
        }
        if (difference.TotalSeconds < 60)
        {
            return "Active now";
        }
        if (difference.TotalMinutes < 60)
        {
            int mins = Mathf.Max(1, (int)difference.TotalMinutes);
            return $"Played {mins}m ago";
        }
        if (difference.TotalHours < 24)
        {
            int hours = Mathf.Max(1, (int)difference.TotalHours);
            return $"Played {hours}h ago";
        }
        if (difference.TotalDays < 2)
        {
            return "Played yesterday";
        }
        if (difference.TotalDays < 30)
        {
            int days = Mathf.Max(1, (int)difference.TotalDays);
            return $"Played {days}d ago";
        }
        
        return $"Played on {lastActiveTime:yyyy-MM-dd}";
    }
}
