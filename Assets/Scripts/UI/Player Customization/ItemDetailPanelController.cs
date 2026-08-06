using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls the pop-up animation of the DescriptionPanel.
/// 
/// Assign in Inspector:
///   descriptionPanel   -> your "DescriptionPanel" GameObject
///   itemNameText       -> ItemName TMP text
///   itemDescText       -> ItemDescription TMP text
///   equipButton        -> Equip/UnequipButton
///   equipButtonLabel   -> the TMP text inside the equip button
///   outfitManager      -> the OutfitManager on the character
/// </summary>
public class ItemDetailPanelController : MonoBehaviour
{
    [Header("Panel to Animate")]
    public RectTransform descriptionPanel;

    [Header("Animation Settings")]
    [Tooltip("Seconds for the pop-up animation")]
    public float animDuration = 0.2f;

    [Header("Description Panel Content")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public TextMeshProUGUI statusText;
    public Color equippedColor = Color.green;
    public Color notEquippedColor = Color.red;
    public Image itemIconImage;
    public Button equipButton;
    public TextMeshProUGUI equipButtonLabel;

    [Header("Character")]
    public OutfitManager outfitManager;

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

        // Wire up equip button
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipClicked);
    }

    /// <summary>
    /// Call this when a real clothing item toggle is turned ON.
    /// </summary>
    public void ShowItem(OutfitItem item)
    {
        currentItem = item;

        // Fill text fields
        if (itemNameText != null)
            itemNameText.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;

        if (itemDescText != null)
            itemDescText.text = item.itemDescription;

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

        // Update equip button label based on whether this item is already equipped
        RefreshEquipLabel();

        // Pop open
        if (!isPanelOpen)
            PopOpen();
    }

    /// <summary>
    /// Call this when the "None" toggle is selected, or when nothing is selected.
    /// </summary>
    public void HidePanel()
    {
        currentItem = null;
        if (isPanelOpen)
            PopClose();
    }

    // ------------------------------------------------
    // Equip / Unequip
    // ------------------------------------------------

    private void OnEquipClicked()
    {
        if (currentItem == null || outfitManager == null) return;

        bool isEquipped = IsCurrentItemEquipped();

        if (isEquipped)
        {
            outfitManager.Unequip(currentItem.slot);
        }
        else
        {
            outfitManager.Equip(currentItem);
        }

        RefreshEquipLabel();
    }

    private void RefreshEquipLabel()
    {
        bool isEquipped = IsCurrentItemEquipped();

        if (equipButtonLabel != null) 
            equipButtonLabel.text = isEquipped ? "Unequip" : "Equip";

        if (statusText != null)
        {
            statusText.text = isEquipped ? "Equipped" : "Not Equipped";
            statusText.color = isEquipped ? equippedColor : notEquippedColor;
        }
    }

    private bool IsCurrentItemEquipped()
    {
        if (currentItem == null || outfitManager == null) return false;

        // Check by comparing equipped names
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
            
            if (!gameObject.activeInHierarchy)
            {
                descriptionPanel.localScale = Vector3.zero;
                descriptionPanel.gameObject.SetActive(false);
                return;
            }

            descriptionPanel.gameObject.SetActive(true);
            animCoroutine = StartCoroutine(ScalePanel(descriptionPanel.localScale, Vector3.zero, hideOnComplete: true));
        }
    }

    private IEnumerator ScalePanel(Vector3 fromScale, Vector3 toScale, bool hideOnComplete = false)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            
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
