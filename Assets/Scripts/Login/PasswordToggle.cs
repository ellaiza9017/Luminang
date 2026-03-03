using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordToggle : MonoBehaviour
{
    public TMP_InputField passwordField;
    public Image eyeIcon;

    public Sprite eyeOpen;
    public Sprite eyeClosed;

    private bool isHidden = true;

    public void TogglePassword()
    {
        if (isHidden)
        {
            passwordField.contentType = TMP_InputField.ContentType.Standard;
            eyeIcon.sprite = eyeOpen;
            isHidden = false;
        }
        else
        {
            passwordField.contentType = TMP_InputField.ContentType.Password;
            eyeIcon.sprite = eyeClosed;
            isHidden = true;
        }

        passwordField.ForceLabelUpdate();
    }
}