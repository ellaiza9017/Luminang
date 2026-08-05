using UnityEngine;
using TMPro;

public class LeaderboardWordRow : MonoBehaviour
{
    public TextMeshProUGUI wordText;

    public void Setup(string phrase)
    {
        if (wordText != null)
        {
            wordText.text = phrase;
        }
    }
}
