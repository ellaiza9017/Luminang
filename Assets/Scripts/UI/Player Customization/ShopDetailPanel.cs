using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Drives the DescriptionPanel in the ShopScene.
/// Handles both owned items (Equip/Unequip) and unowned items (Buy Item).
/// </summary>
public class ShopDetailPanel : MonoBehaviour
{
    [Header("Panel to Animate")]
    public RectTransform descriptionPanel;
    public float animDuration = 0.2f;

    [Header("Content")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public TextMeshProUGUI statusText;
    public Image itemIconImage;

    [Header("Action Button")]
    public Button actionButton;
    public TextMeshProUGUI actionButtonLabel;

    [Header("Colors")]
    public Color equippedColor  = Color.green;
    public Color notEquippedColor = Color.white;

    // ---- State ----
    private OutfitItem currentItem;
    private bool currentItemIsOwned;
    private ShopManager shopManager;
    private bool isPanelOpen = false;
    private Coroutine animCoroutine;

    void Awake()
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.gameObject.SetActive(false);
            descriptionPanel.localScale = Vector3.zero;
        }
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    /// <summary>
    /// Call this when a gallery card toggle fires.
    /// isOwned = true  ? show Equip/Unequip
    /// isOwned = false ? show "Buy Item"
    /// </summary>
    public void ShowItem(OutfitItem item, bool isOwned, ShopManager manager)
    {
        currentItem = item;
        currentItemIsOwned = isOwned;
        shopManager = manager;

        if (itemNameText != null)
            itemNameText.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;

        if (itemDescText != null)
            itemDescText.text = item.itemDescription;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = item.icon;
            itemIconImage.color = item.icon != null ? Color.white : new Color(0, 0, 0, 0);
        }

        RefreshButtonState();
        if (!isPanelOpen) PopOpen();
    }

    public void HidePanel()
    {
        currentItem = null;
        if (isPanelOpen) PopClose();
    }

    // -------------------------------------------------------

    private void OnActionButtonClicked()
    {
        if (currentItem == null || shopManager == null) return;

        if (!currentItemIsOwned)
        {
            // Not owned ? trigger purchase flow
            shopManager.TryPurchase(currentItem);
            return;
        }

        // Owned ? toggle equip/unequip
        bool isEquipped = IsCurrentItemEquipped();
        if (isEquipped)
            shopManager.characterManager.Unequip(currentItem.slot);
        else
            shopManager.characterManager.Equip(currentItem);

        shopManager.RefreshAllFrames();
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (actionButtonLabel == null) return;

        if (!currentItemIsOwned)
        {
            actionButtonLabel.text = "Buy Item";
            if (statusText != null) { statusText.text = $"{currentItem.price} coins"; statusText.color = new Color(1f, 0.85f, 0f); }
            return;
        }

        bool isEquipped = IsCurrentItemEquipped();
        actionButtonLabel.text = isEquipped ? "Unequip" : "Equip";

        if (statusText != null)
        {
            statusText.text = isEquipped ? "Equipped" : "Not Equipped";
            statusText.color = isEquipped ? equippedColor : notEquippedColor;
        }
    }

    private bool IsCurrentItemEquipped()
    {
        if (currentItem == null || shopManager?.characterManager == null) return false;
        var equipped = shopManager.characterManager.GetEquippedNames();
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

    // -------------------------------------------------------
    // Animation
    // -------------------------------------------------------

    private void PopOpen()
    {
        isPanelOpen = true;
        if (descriptionPanel == null) return;
        descriptionPanel.gameObject.SetActive(true);
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        if (!gameObject.activeInHierarchy) { descriptionPanel.localScale = Vector3.one; return; }
        animCoroutine = StartCoroutine(ScalePanel(Vector3.zero, Vector3.one));
    }

    private void PopClose()
    {
        isPanelOpen = false;
        if (descriptionPanel == null) return;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        if (!gameObject.activeInHierarchy) { descriptionPanel.localScale = Vector3.zero; descriptionPanel.gameObject.SetActive(false); return; }
        descriptionPanel.gameObject.SetActive(true);
        animCoroutine = StartCoroutine(ScalePanel(descriptionPanel.localScale, Vector3.zero, hideOnComplete: true));
    }

    private IEnumerator ScalePanel(Vector3 from, Vector3 to, bool hideOnComplete = false)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animDuration;
            float easeT = 1f - (1f - t) * (1f - t);
            descriptionPanel.localScale = Vector3.Lerp(from, to, easeT);
            yield return null;
        }
        descriptionPanel.localScale = to;
        if (hideOnComplete) descriptionPanel.gameObject.SetActive(false);
    }
}
