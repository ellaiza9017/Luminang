using UnityEngine;

/// <summary>
/// Place this script on any interactable object in the scene (like "potatoes", "Yarns").
/// It will automatically hide the object until the specified objective becomes active.
/// 
/// Note: Keep the GameObject ACTIVE in the Unity Editor so this script can run its Start() method.
/// The script will automatically hide the object's components/children when the game starts if needed.
/// </summary>
public class ObjectiveInteractable : MonoBehaviour
{
    [Header("Objective Settings")]
    [Tooltip("The exact objective text (or start of it) that must be active for this object to appear. e.g. 'Find the potatoes'")]
    public string requiredObjectiveText;

    [Tooltip("Should this object hide again after the objective is completed?")]
    public bool hideAfterCompletion = true;

    [Header("Behavior")]
    [Tooltip("If true, it will toggle all child GameObjects on/off. If false, it only toggles Colliders and Renderers on this specific object.")]
    public bool toggleChildren = true;

    private void Start()
    {
        // Subscribe to objective changes so it appears instantly when the objective updates
        ObjectiveManager.OnObjectiveChanged += HandleObjectiveChanged;
        
        // Initial check on load
        if (ObjectiveManager.Instance != null)
        {
            CheckVisibility(ObjectiveManager.Instance.CurrentObjective);
        }
        else
        {
            // If ObjectiveManager hasn't initialized yet, hide by default
            SetVisible(false);
        }
    }

    private void OnDestroy()
    {
        ObjectiveManager.OnObjectiveChanged -= HandleObjectiveChanged;
    }

    private void HandleObjectiveChanged(string newObjective)
    {
        CheckVisibility(newObjective);
    }

    public void CheckVisibility(string currentObjectiveText)
    {
        if (string.IsNullOrEmpty(requiredObjectiveText)) return;

        string cleanCurrent = currentObjectiveText.Trim();
        string cleanRequired = requiredObjectiveText.Trim();

        // Check if the current objective matches our required objective
        bool isActiveObjective = cleanCurrent.Equals(cleanRequired, System.StringComparison.OrdinalIgnoreCase) || 
                                 cleanCurrent.StartsWith(cleanRequired, System.StringComparison.OrdinalIgnoreCase);

        if (isActiveObjective)
        {
            // It's the current objective! Show it.
            SetVisible(true);
        }
        else
        {
            // It's not the current objective. Has it been completed?
            bool isCompleted = ObjectiveManager.Instance != null && ObjectiveManager.Instance.IsObjectiveCompleted(cleanRequired);

            if (isCompleted && !hideAfterCompletion)
            {
                // Completed, but we want it to stay visible in the world
                SetVisible(true);
            }
            else
            {
                // Not active yet (pre-quest), or completed and we want it hidden
                SetVisible(false);
            }
        }
    }

    /// <summary>
    /// Toggles the object's visibility without disabling the GameObject itself.
    /// If we disabled the root GameObject, Unity would stop running Coroutines and might mess with event subscriptions.
    /// </summary>
    private void SetVisible(bool isVisible)
    {
        // 1. Toggle children if requested
        if (toggleChildren)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(isVisible);
            }
        }

        // 2. Toggle any Colliders on this specific object
        var colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = isVisible;
        }

        // 3. Toggle any Renderers on this specific object
        var renderers = GetComponents<Renderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = isVisible;
        }
        
        // 4. Toggle Canvas if it's a UI element
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = isVisible;
        }
    }
}
