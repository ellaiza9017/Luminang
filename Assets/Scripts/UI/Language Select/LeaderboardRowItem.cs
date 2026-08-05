using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls a single row in the Leaderboard list (top 10 or current player rank).
/// </summary>
public class LeaderboardRowItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rankText;
    public Image badgeImage;
    public Image avatarImage;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI progressText;
    public Button rowButton;

    // Badges for Top 3 (can be passed in during setup)
    [Header("Badge Setup")]
    public Sprite goldBadge;
    public Sprite silverBadge;
    public Sprite bronzeBadge;
    [Tooltip("Special badge shown on the current player's own row (the 'Your Rank' circle).")]
    public Sprite currentPlayerBadge;

    [Header("Text Coloring")]
    [Tooltip("Color of the rank text when the player is in the top 3 (usually white or styled by user).")]
    public Color top3TextColor = Color.white;
    [Tooltip("Color of the rank text when the player is not in the top 3.")]
    public Color normalTextColor = Color.black;

    private LeaderboardEntry _entryData;
    private LeaderboardManager _manager;
    private ColorBlock _originalColors;
    private bool _colorsStored = false;

    public LeaderboardEntry EntryData => _entryData;

    public void Setup(LeaderboardEntry entry, LeaderboardManager manager, bool isFooterRow = false)
    {
        _entryData = entry;
        _manager = manager;

        // Set Rank Number & Badge
        if (rankText != null)
        {
            rankText.text = entry.Rank.ToString();
            rankText.gameObject.SetActive(true);

            // Apply coloring based on whether they are in the top 3 or the current player
            if (entry.Rank <= 3 || entry.IsCurrentPlayer)
            {
                rankText.color = top3TextColor;
            }
            else
            {
                rankText.color = normalTextColor;
            }
        }

        if (badgeImage != null)
        {
            if (isFooterRow && currentPlayerBadge != null)
            {
                badgeImage.sprite = currentPlayerBadge;
                badgeImage.gameObject.SetActive(true);
            }
            else if (entry.Rank == 1 && goldBadge != null)
            {
                badgeImage.sprite = goldBadge;
                badgeImage.gameObject.SetActive(true);
            }
            else if (entry.Rank == 2 && silverBadge != null)
            {
                badgeImage.sprite = silverBadge;
                badgeImage.gameObject.SetActive(true);
            }
            else if (entry.Rank == 3 && bronzeBadge != null)
            {
                badgeImage.sprite = bronzeBadge;
                badgeImage.gameObject.SetActive(true);
            }
            else
            {
                badgeImage.gameObject.SetActive(false);
            }
        }

        // Set Username & Progress
        if (usernameText != null)
            usernameText.text = entry.Username;

        if (progressText != null)
            progressText.text = $"{entry.OverallProgress:F1}%";

        // Set Avatar (or default white image if null)
        if (avatarImage != null)
        {
            avatarImage.color = Color.white; // Default fallback color
            if (!string.IsNullOrEmpty(entry.AvatarUrl) && AvatarManager.Instance != null)
            {
                LoadAvatarAsync(entry.AvatarUrl);
            }
        }

        // Click Handler
        if (rowButton != null)
        {
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(OnClick);
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

    public void SetSelected(bool isSelected)
    {
        if (rowButton != null)
        {
            if (!_colorsStored)
            {
                _originalColors = rowButton.colors;
                _colorsStored = true;
            }

            ColorBlock cb = rowButton.colors;
            if (isSelected)
            {
                cb.normalColor = _originalColors.selectedColor;
                cb.highlightedColor = _originalColors.selectedColor;
            }
            else
            {
                cb.normalColor = _originalColors.normalColor;
                cb.highlightedColor = _originalColors.highlightedColor;
            }
            rowButton.colors = cb;
        }
    }

    private void OnClick()
    {
        if (_manager != null && _entryData != null)
        {
            _manager.SelectRow(this, _entryData);
        }
    }
}
