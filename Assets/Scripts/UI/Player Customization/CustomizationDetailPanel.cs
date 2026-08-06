using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the pop-up animation of the DescriptionPanel in the Shop/Customization scene.
/// </summary>
public class CustomizationDetailPanel : MonoBehaviour
{
    [Header("Panel to Animate")]
    public RectTransform descriptionPanel;

    [Header("Animation Settings")]
    [Tooltip("Seconds for the pop-up animation")]
    public float animDuration = 0.2f;

    [Header("Description Panel Content")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public TextMeshProUGUI statusText; // Replaced priceText with statusText
    public Image itemIconImage;
    public Button actionButton; // Equip or Unequip
    public TextMeshProUGUI actionButtonLabel;

    [Header("Character")]
    public OutfitManager outfitManager;

    [Header("Colors")]
    public Color equippedColor = Color.green;
    public Color notEquippedColor = Color.white;

    // ---- private state ----
    private OutfitItem currentItem;
    private bool isPanelOpen = false;
    private Coroutine animCoroutine;

    void Awake()
    {
        if (descriptionPanel != null)
        {
            // Start hidden
            descriptionPanel.gameObject.SetActive(false);
            descriptionPanel.localScale = Vector3.zero;
        }

        // Wire up action button
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    public void ShowItem(OutfitItem item)
    {
        currentItem = item;

        // Fill text fields
        if (itemNameText != null)
            itemNameText.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;

        if (itemDescText != null)
            itemDescText.text = item.itemDescription;

        if (statusText != null)
        {
            bool isEquipped = IsCurrentItemEquipped();
            statusText.text = isEquipped ? "Equipped" : "Not Equipped";
            statusText.color = isEquipped ? equippedColor : notEquippedColor;
        }

        // Set Icon
        if (itemIconImage != null)
        {
            if (item.icon != null)
            {
                itemIconImage.sprite = item.icon;
                itemIconImage.color = Color.white;
            }
            else
            {
                itemIconImage.color = new Color(0, 0, 0, 0); // Hide if no icon
            }
        }

        RefreshButtonState();

        // Pop open
        if (!isPanelOpen)
            PopOpen();
    }

    public void HidePanel()
    {
        currentItem = null;
        if (isPanelOpen)
            PopClose();
    }

    // ------------------------------------------------
    // Logic
    // ------------------------------------------------

    private void OnActionButtonClicked()
    {
        if (currentItem == null || outfitManager == null) return;

        bool isEquipped = IsCurrentItemEquipped();

        if (isEquipped)
        {
            // UNEQUIP Logic
            outfitManager.Unequip(currentItem.slot);
        }
        else
        {
            // EQUIP Logic
            outfitManager.Equip(currentItem);
        }

        RefreshButtonState();
        
        // Update status text on click
        if (statusText != null)
        {
            bool nowEquipped = !isEquipped;
            statusText.text = nowEquipped ? "Equipped" : "Not Equipped";
            statusText.color = nowEquipped ? equippedColor : notEquippedColor;
        }
    }

    private void RefreshButtonState()
    {
        if (currentItem == null || actionButtonLabel == null) return;

        bool isEquipped = IsCurrentItemEquipped();

        if (actionButton != null)
            actionButton.interactable = true;

        if (isEquipped)
        {
            actionButtonLabel.text = "Unequip";
        }
        else
        {
            actionButtonLabel.text = "Equip";
        }
    }

    // --- Placeholders for Supabase / Inventory System ---
    
    public static bool IsItemOwned(OutfitItem item)
    {
        if (item.price <= 0) return true;

        CustomizationManager manager = Object.FindFirstObjectByType<CustomizationManager>();
        if (manager != null)
        {
            return manager.ownedItems.Contains(item.name);
        }
        return false;
    }

    private void SetItemOwned(OutfitItem item, bool owned)
    {
        CustomizationManager manager = Object.FindFirstObjectByType<CustomizationManager>();
        if (manager != null)
        {
            if (owned && !manager.ownedItems.Contains(item.name))
            {
                manager.ownedItems.Add(item.name);
                // TODO: Replace with actual Supabase INSERT into user_inventory
            }
            else if (!owned)
            {
                manager.ownedItems.Remove(item.name);
            }
        }
    }

    private bool IsCurrentItemEquipped()
    {
        if (currentItem == null || outfitManager == null) return false;

        var equipped = outfitManager.GetEquippedNames();
        string equippedName = currentItem.slot switch
        {
            OutfitItem.Slot.Hair        => equipped.hair,
            OutfitItem.Slot.Top         => equipped.top,
            OutfitItem.Slot.Bottom      => equipped.bottom,
            OutfitItem.Slot.Shoes       => equipped.shoes,
            OutfitItem.Slot.Accessories => equipped.accessories,
            _                           => null
        };

        return equippedName == currentItem.gameObject.name;
    }

    // ------------------------------------------------
    // Pop Animation
    // ------------------------------------------------

    private void PopOpen()
    {
        isPanelOpen = true;
        if (descriptionPanel != null)
        {
            descriptionPanel.gameObject.SetActive(true);
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            
            // Check if the script's game object is fully active in hierarchy before starting coroutines
            if (!gameObject.activeInHierarchy)
            {
                descriptionPanel.localScale = Vector3.one;
                return;
            }

            animCoroutine = StartCoroutine(ScalePanel(Vector3.zero, Vector3.one));
        }
    }

    private void PopClose()
    {
        isPanelOpen = false;
        if (descriptionPanel != null)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            
            // If the script's gameobject is inactive, we can't run a coroutine on it. Just snap it closed.
            if (!gameObject.activeInHierarchy)
            {
                descriptionPanel.localScale = Vector3.zero;
                descriptionPanel.gameObject.SetActive(false);
                return;
            }

            // MOBILE FIX: Ensure the panel is active so the coroutine can run.
            // The coroutine itself will SetActive(false) when the animation finishes.
            descriptionPanel.gameObject.SetActive(true);
            animCoroutine = StartCoroutine(ScalePanel(descriptionPanel.localScale, Vector3.zero, hideOnComplete: true));
        }
    }

    private IEnumerator ScalePanel(Vector3 fromScale, Vector3 toScale, bool hideOnComplete = false)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // Simple ease-out curve
            float t = elapsed / animDuration;
            float easeT = 1f - (1f - t) * (1f - t); 
            
            descriptionPanel.localScale = Vector3.Lerp(fromScale, toScale, easeT);
            yield return null;
        }

        descriptionPanel.localScale = toScale;

        if (hideOnComplete)
        {
            descriptionPanel.gameObject.SetActive(false);
        }
    }
}
