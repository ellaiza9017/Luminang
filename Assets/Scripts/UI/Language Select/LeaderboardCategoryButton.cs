using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardCategoryButton : MonoBehaviour
{
    public TextMeshProUGUI categoryText;
    public Button button;

    private string _categoryName;
    private LeaderboardJournalPreview _previewManager;

    public void Setup(string categoryName, LeaderboardJournalPreview previewManager)
    {
        _categoryName = categoryName;
        _previewManager = previewManager;

        if (categoryText != null)
        {
            categoryText.text = categoryName;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_previewManager != null)
        {
            _previewManager.SelectCategory(_categoryName);
        }
    }
}
