using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AssessmentProgressBar : MonoBehaviour
{
    [Header("Fill Images (Set Image Type to Filled -> Horizontal)")]
    public Image section1Fill; // Green
    public Image section2Fill; // Blue
    public Image section3Fill; // Purple

    [Header("Handle (The Diamond)")]
    public RectTransform handle;

    [Header("Section Texts (Optional)")]
    public TextMeshProUGUI section1Text;
    public TextMeshProUGUI section2Text;
    public TextMeshProUGUI section3Text;
    public TextMeshProUGUI mainProgressText; // "Question X of 50"

    [Header("Settings")]
    public int maxSection1 = 15;
    public int maxSection2 = 15;
    public int maxSection3 = 20;

    public void UpdateProgress(int currentQuestionIndex)
    {
        int qNum = currentQuestionIndex + 1;
        int total = maxSection1 + maxSection2 + maxSection3;
        
        if (mainProgressText != null)
            mainProgressText.text = $"QUESTION {qNum} OF {total}";

        // Calculate progress per section
        int s1Progress = Mathf.Clamp(qNum, 0, maxSection1);
        int s2Progress = Mathf.Clamp(qNum - maxSection1, 0, maxSection2);
        int s3Progress = Mathf.Clamp(qNum - maxSection1 - maxSection2, 0, maxSection3);

        // Update Text
        if (section1Text != null) section1Text.text = $"{s1Progress} / {maxSection1}";
        if (section2Text != null) section2Text.text = $"{s2Progress} / {maxSection2}";
        if (section3Text != null) section3Text.text = $"{s3Progress} / {maxSection3}";

        // Update Fills
        float s1FillAmount = (float)s1Progress / maxSection1;
        float s2FillAmount = (float)s2Progress / maxSection2;
        float s3FillAmount = (float)s3Progress / maxSection3;

        if (section1Fill != null) section1Fill.fillAmount = s1FillAmount;
        if (section2Fill != null) section2Fill.fillAmount = s2FillAmount;
        if (section3Fill != null) section3Fill.fillAmount = s3FillAmount;

        // Position the handle at the edge of the active fill
        UpdateHandlePosition(s1FillAmount, s2FillAmount, s3FillAmount);
    }

    private void UpdateHandlePosition(float f1, float f2, float f3)
    {
        if (handle == null) return;

        RectTransform activeFillRect = null;
        float activeFillAmount = 0f;

        // Determine which section the player is currently in
        if (f1 > 0 && f1 < 1f || (f1 == 1f && f2 == 0)) 
        {
            activeFillRect = section1Fill.rectTransform;
            activeFillAmount = f1;
        }
        else if (f2 > 0 && f2 < 1f || (f2 == 1f && f3 == 0))
        {
            activeFillRect = section2Fill.rectTransform;
            activeFillAmount = f2;
        }
        else if (f3 > 0)
        {
            activeFillRect = section3Fill.rectTransform;
            activeFillAmount = f3;
        }

        if (activeFillRect != null)
        {
            // Calculate the world position of the right edge of the fill
            Vector3[] corners = new Vector3[4];
            activeFillRect.GetWorldCorners(corners);
            
            // corners[0] is bottom-left, corners[3] is bottom-right of the ENTIRE rect
            float totalWidth = Vector3.Distance(corners[0], corners[3]);
            float currentWidth = totalWidth * activeFillAmount;

            // Move handle to that exact X position
            handle.position = new Vector3(corners[0].x + currentWidth, handle.position.y, handle.position.z);
        }
    }
}
