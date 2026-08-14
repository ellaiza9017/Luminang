using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CustomizationBackButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnBackClicked);
    }

    private void OnBackClicked()
    {
        // Find the CustomizationManager
        CustomizationManager manager = FindFirstObjectByType<CustomizationManager>(FindObjectsInactive.Include);
        
        if (manager != null && manager.HasUnsavedChanges())
        {
            // Find GenericModal safely
            GenericModal modal = GenericModal.Instance;
            if (modal == null) modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);

            if (modal != null)
            {
                modal.ShowConfirm(
                    "You have unsaved changes. Are you sure you want to exit Character Customization?",
                    "Yes",
                    () => SceneNavigationManager.ReturnToPreviousScene(),
                    "No"
                );
            }
            else
            {
                // Fallback if modal is missing for some reason
                SceneNavigationManager.ReturnToPreviousScene();
            }
        }
        else
        {
            // No changes, or manager not found (e.g. in a different scene)
            SceneNavigationManager.ReturnToPreviousScene();
        }
    }
}
