using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class CustomizationManager : MonoBehaviour
{
    [Header("Manager References")]
    public OutfitManager characterManager;
    public TMPro.TMP_InputField usernameField;
    public TMPro.TextMeshProUGUI reminderText;
    public Button changeButton;
    public Image usernameIcon;

    [Header("Prefab Settings")]
    public GameObject itemFramePrefab;
    
    [Header("Categories")]
    public List<CategoryFolder> categories = new List<CategoryFolder>();

    [Header("Default Sprites")]
    public Sprite noneIcon;
    public Sprite noneBackground;

    [Header("Background Sprites")]
    [Tooltip("Background sprite when item is selected")]
    public Sprite activeBackground;
    [Tooltip("Background sprite when item is not selected")]
    public Sprite inactiveBackground;
    
    [Header("Save Flow")]
    public Button saveChangesButton;
    public PortraitBooth portraitBooth;
    public GameObject loadingOverlay;
    public GenericModal modal;

    [Header("Detail Panel (Shop)")]
    public CustomizationDetailPanel detailPanel;
    public Color ownedColor = Color.white;
    public Color equippedColor = Color.green;

    [HideInInspector]
    public List<string> ownedItems = new List<string>();

    private EquippedOutfitData originalOutfit;

    [System.Serializable]
    public class CategoryFolder
    {
        public string categoryName;
        public OutfitItem.Slot slot;
        public Transform contentParent;
    }

    async void Start()
    {
        // Auto-fetch the singleton modal if the inspector slot lost its reference
        if (modal == null) 
        {
            modal = GenericModal.Instance;
            if (modal == null)
            {
                modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);
            }
        }

        // 1. Ensure the profile is loaded
        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile == null)
        {
            Debug.Log("[Customization] Profile null, fetching now...");
            await UserProfileManager.Instance.FetchProfile();
        }

        // 2. Set the username and handle the 30-day cooldown
        if (usernameField != null && UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
        {
            var profile = UserProfileManager.Instance.CurrentProfile;
            usernameField.text = profile.Username;
            
            // Check for 30-day cooldown
            if (profile.UsernameFinalizedAt.HasValue)
            {
                System.TimeSpan timeSinceFinalized = System.DateTime.UtcNow - profile.UsernameFinalizedAt.Value;
                int daysRemaining = 30 - timeSinceFinalized.Days;

                if (daysRemaining > 0)
                {
                    usernameField.interactable = false;
                    Debug.Log($"[Customization] Username cooldown active. {daysRemaining} days remaining.");
                    
                    if (reminderText != null)
                    {
                        reminderText.text = $"You can change your username again in <color=red>{daysRemaining} days</color>.";
                    }

                    // LOCK BUTTON AND ICON
                    if (changeButton != null)
                    {
                        changeButton.interactable = false;
                        Animator anim = changeButton.GetComponent<Animator>();
                        if (anim != null) anim.enabled = false;

                        // Gray out the button image itself
                        Image btnImg = changeButton.GetComponent<Image>();
                        if (btnImg != null) btnImg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }

                    if (usernameIcon != null)
                    {
                        usernameIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray and semi-transparent
                    }
                }
                else
                {
                    if (reminderText != null)
                    {
                        reminderText.text = "One change allowed every 30 days. Choose wisely!";
                    }

                    if (changeButton != null)
                    {
                        changeButton.interactable = true;
                        Animator anim = changeButton.GetComponent<Animator>();
                        if (anim != null) anim.enabled = true;

                        Image btnImg = changeButton.GetComponent<Image>();
                        if (btnImg != null) btnImg.color = Color.white;
                    }

                    if (usernameIcon != null)
                    {
                        usernameIcon.color = Color.white;
                    }
                }
            }
            else
            {
                // Never finalized? Show the policy reminder
                if (reminderText != null)
                {
                    reminderText.text = "One change allowed every 30 days. Choose wisely!";
                }

                if (changeButton != null)
                {
                    changeButton.interactable = true;
                    Animator anim = changeButton.GetComponent<Animator>();
                    if (anim != null) anim.enabled = true;

                    Image btnImg = changeButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;
                }

                if (usernameIcon != null)
                {
                    usernameIcon.color = Color.white;
                }
            }
            
            Debug.Log("[Customization] Set username to: " + usernameField.text);
        }

        // 2. Load what the character is ALREADY wearing from the database
        if (UserProfileManager.Instance != null && characterManager != null)
        {
            var equippedData = UserProfileManager.Instance.GetEquippedOutfitData();
            if (equippedData != null)
            {
                characterManager.LoadOutfit(equippedData);
                originalOutfit = equippedData;
            }
        }

        if (saveChangesButton != null)
        {
            saveChangesButton.onClick.AddListener(OnSaveChangesClicked);
            Debug.Log("[Customization] Successfully bound saveChangesButton onClick listener!");
        }
        else
        {
            Debug.LogError("[Customization] ERROR: saveChangesButton is NOT ASSIGNED in the Inspector! The button will do nothing.");
        }

        // 3. Fetch inventory and build the UI
        await InitializeGallery();
    }

    public async System.Threading.Tasks.Task InitializeGallery()
    {
        if (characterManager == null || itemFramePrefab == null) return;

        // Fetch owned items from database
        ownedItems = await FetchOwnedInventory();

        foreach (var category in categories)
        {
            GenerateCategory(category, ownedItems);
        }
    }

    private async System.Threading.Tasks.Task<List<string>> FetchOwnedInventory()
    {
        List<string> owned = new List<string>();
        try
        {
            var user = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (user == null) return owned;

            var response = await SupabaseManager.Instance.client
                .From<InventoryModel>()
                .Where(x => x.UserId == user.Id)
                .Get();

            if (response != null && response.Models != null)
            {
                foreach (var item in response.Models)
                {
                    owned.Add(item.ItemName);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Customization] Error fetching inventory: " + ex.Message);
        }
        return owned;
    }

    private bool isInitializingUI = false;

    private void GenerateCategory(CategoryFolder category, List<string> ownedItems)
    {
        if (category.contentParent == null) return;

        isInitializingUI = true;

        // Clear existing
        foreach (Transform child in category.contentParent)
            Destroy(child.gameObject);

        ToggleGroup group = category.contentParent.GetComponent<ToggleGroup>();

        // Add "None" button first
        Toggle noneToggle = CreateItem(category, "None", noneIcon, noneBackground, null, group);

        // Add outfit items
        OutfitItem[] allItems = characterManager.GetComponentsInChildren<OutfitItem>(true);
        Toggle selectedToggle = null;

        // Check what the character is currently wearing
        EquippedOutfitData current = characterManager.GetEquippedNames();
        string equippedName = GetEquippedNameForSlot(current, category.slot);

        foreach (var item in allItems)
        {
            // Only show owned items in Character Customization
            bool isOwned = ownedItems.Contains(item.name) || item.price <= 0;
            if (item.slot == category.slot && isOwned)
            {
                Toggle t = CreateItem(category, item.name, item.icon, null, item, group);
                if (item.name == equippedName)
                    selectedToggle = t;
            }
        }

        // Select the right toggle
        if (selectedToggle == null)
            selectedToggle = noneToggle;

        selectedToggle.isOn = true;
        isInitializingUI = false;
    }

    private Toggle CreateItem(CategoryFolder category, string itemName, Sprite icon, Sprite bg, OutfitItem item, ToggleGroup group)
    {
        GameObject frameObj = Instantiate(itemFramePrefab, category.contentParent);
        frameObj.name = itemName;

        // Add UI Helper
        CustomizationFrameUI frameUI = frameObj.AddComponent<CustomizationFrameUI>();
        frameUI.Init(item, this);

        // Clean up rogue toggles
        Toggle rootToggle = frameObj.GetComponent<Toggle>();
        Toggle[] allToggles = frameObj.GetComponentsInChildren<Toggle>(true);
        
        Sprite activeBg = activeBackground;
        Sprite inactiveBg = inactiveBackground;
        
        foreach (var t in allToggles)
        {
            if (t != rootToggle)
            {
                if (activeBg == null && t.spriteState.selectedSprite != null)
                    activeBg = t.spriteState.selectedSprite;
                Destroy(t);
            }
        }

        rootToggle.group = group;
        rootToggle.isOn = false;

        // Background
        Transform bgTransform = frameObj.transform.Find("Background");
        Image bgImage = bgTransform != null ? bgTransform.GetComponent<Image>() : null;
        if (inactiveBg == null && bgImage != null) inactiveBg = bgImage.sprite;
        if (bg != null && bgImage != null) bgImage.sprite = bg;

        // Icon
        Transform iconTransform = FindChildRecursive(frameObj.transform, "ItemIcon");
        if (iconTransform == null) iconTransform = FindChildRecursive(frameObj.transform, "AssetIcon");
        if (iconTransform != null)
        {
            Image iconImg = iconTransform.GetComponent<Image>();
            if (iconImg != null && icon != null)
            {
                iconImg.sprite = icon;
                iconImg.color = Color.white;
            }
        }

        Transform checkTransform = FindChildRecursive(frameObj.transform, "Checkmark");
        GameObject checkObj = checkTransform != null ? checkTransform.gameObject : null;
        if (checkObj != null) checkObj.SetActive(false);

        // Set up "None" button specifics
        if (item == null)
        {
            HideChild(frameObj.transform, "Ownership");
        }

        // Toggle Listener
        rootToggle.onValueChanged.AddListener((isOn) => {
            if (checkObj != null) checkObj.SetActive(isOn);

            if (bgImage != null)
            {
                if (isOn && activeBg != null) bgImage.sprite = activeBg;
                else if (!isOn && inactiveBg != null) bgImage.sprite = inactiveBg;
            }

            if (isOn)
            {
                if (item == null) 
                {
                    if (!isInitializingUI)
                    {
                        characterManager.Unequip(category.slot);
                        RefreshAllFrames();
                    }
                    if (detailPanel != null) detailPanel.HidePanel();
                }
                else 
                {
                    if (detailPanel != null) detailPanel.ShowItem(item);
                }
            }
        });

        return rootToggle;
    }

    public void RefreshAllFrames()
    {
        CustomizationFrameUI[] allFrames = GetComponentsInChildren<CustomizationFrameUI>(true);
        foreach (var frame in allFrames)
        {
            frame.RefreshVisuals();
        }
    }

    private string GetEquippedNameForSlot(EquippedOutfitData data, OutfitItem.Slot slot)
    {
        switch (slot)
        {
            case OutfitItem.Slot.Hair: return data.hair;
            case OutfitItem.Slot.Top: return data.top;
            case OutfitItem.Slot.Bottom: return data.bottom;
            case OutfitItem.Slot.Shoes: return data.shoes;
            case OutfitItem.Slot.Accessories: return data.accessories;
            default: return "";
        }
    }

    public Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void HideChild(Transform parent, string name)
    {
        Transform child = FindChildRecursive(parent, name);
        if (child != null) child.gameObject.SetActive(false);
    }

    #region Save Logic

    private void OnSaveChangesClicked()
    {
        Debug.Log("[Customization] Save button clicked!");

        // Dynamically find managers if they are missing or destroyed due to scene transitions
        if (characterManager == null) characterManager = FindFirstObjectByType<OutfitManager>(FindObjectsInactive.Include);
        
        if (modal == null || modal.gameObject == null || !modal.gameObject.scene.IsValid()) 
        {
            modal = GenericModal.Instance;
            if (modal == null || modal.gameObject == null)
            {
                modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);
            }
        }

        if (characterManager == null) 
        {
            Debug.LogError("[Customization] Save aborted: characterManager is completely missing from the scene!");
            return;
        }
        if (modal == null) 
        {
            Debug.LogError("[Customization] Save aborted: modal is completely missing from the scene!");
            return;
        }

        EquippedOutfitData currentOutfit = characterManager.GetEquippedNames();

        // Check if there are any changes
        if (originalOutfit != null && currentOutfit.IsSameAs(originalOutfit))
        {
            Debug.Log("[Customization] No changes detected, showing alert.");
            modal.ShowAlert("You didn't change anything in your outfit.");
            return;
        }

        Debug.Log("[Customization] Showing confirmation modal.");
        // Ask for confirmation
        modal.ShowConfirm(
            "Are you sure you want to save these changes?",
            "Yes",
            () => _ = OnConfirmSave(currentOutfit),
            "No",
            null
        );
    }

    private async System.Threading.Tasks.Task OnConfirmSave(EquippedOutfitData newOutfit)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(true);

        try
        {
            var user = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (user == null) throw new System.Exception("User not logged in!");

            // 1. Take snapshot and upload to Supabase Storage
            if (portraitBooth != null && AvatarManager.Instance != null)
            {
                Debug.Log("[Customization] STEP 1: Triggering PortraitBooth setup...");
                portraitBooth.SetupPortrait(newOutfit);
                
                if (portraitBooth.portraitTexture != null)
                {
                    Debug.Log($"[Customization] STEP 2: Uploading snapshot to Supabase (Texture: {portraitBooth.portraitTexture.name})...");
                    string resultUrl = await AvatarManager.Instance.CaptureAndUpload(user.Id, portraitBooth.portraitTexture);
                    
                    if (string.IsNullOrEmpty(resultUrl))
                    {
                        Debug.LogError("[Customization] ERROR: CaptureAndUpload returned null or empty URL!");
                    }
                    else
                    {
                        Debug.Log($"[Customization] SUCCESS: Snapshot uploaded to {resultUrl}");
                    }
                }
                else
                {
                    Debug.LogError("[Customization] ERROR: portraitBooth.portraitTexture is NULL! Cannot take snapshot.");
                }
            }
            else
            {
                Debug.LogWarning($"[Customization] SKIPPING Snapshot: portraitBooth={portraitBooth != null}, AvatarManager={AvatarManager.Instance != null}");
            }

            // 2. Update Equipped Outfit in Database
            if (UserProfileManager.Instance != null)
            {
                Debug.Log("[Customization] STEP 3: Updating equipped_outfit in database...");
                var profile = UserProfileManager.Instance.CurrentProfile;
                profile.EquippedOutfit = newOutfit;
                await UserProfileManager.Instance.UpdateProfile(profile);
                Debug.Log("[Customization] SUCCESS: Database profile updated.");
            }

            // 3. Update local state
            originalOutfit = newOutfit;

            modal.ShowAlert("Changes saved successfully!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Customization] Save error: " + ex.Message);
            modal.ShowAlert("Something went wrong while saving: " + ex.Message);
        }
        finally
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
        }
    }

    #endregion
}

/// <summary>
/// Small helper attached dynamically to frames to handle turning their internal UI on/off
/// </summary>
public class CustomizationFrameUI : MonoBehaviour
{
    private OutfitItem myItem;
    private CustomizationManager myManager;
    
    private TMPro.TextMeshProUGUI nameLabel;
    private TMPro.TextMeshProUGUI ownershipLabel;

    public void Init(OutfitItem item, CustomizationManager manager)
    {
        myItem = item;
        myManager = manager;

        if (myItem == null) return;

        // Cache references
        Transform nameTrans = myManager.FindChildRecursive(transform, "ItemName");
        if (nameTrans != null) nameLabel = nameTrans.GetComponent<TMPro.TextMeshProUGUI>();

        Transform ownerTrans = myManager.FindChildRecursive(transform, "Ownership");
        if (ownerTrans != null) ownershipLabel = ownerTrans.GetComponent<TMPro.TextMeshProUGUI>();

        // Set static data
        if (nameLabel != null) nameLabel.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
    }

    public void RefreshVisuals()
    {
        if (myItem == null) return;

        bool isEquipped = IsItemEquipped();

        if (ownershipLabel != null)
        {
            ownershipLabel.gameObject.SetActive(true);
            
            if (isEquipped)
            {
                ownershipLabel.text = "Equipped";
                ownershipLabel.color = myManager.equippedColor;
            }
            else
            {
                // We're only showing owned items anyway, so we just show "Owned" or "Not Equipped"
                ownershipLabel.text = "Owned";
                ownershipLabel.color = myManager.ownedColor;
            }
        }
    }

    private bool IsItemEquipped()
    {
        if (myManager == null || myManager.characterManager == null) return false;
        
        var equipped = myManager.characterManager.GetEquippedNames();
        string equippedName = myItem.slot switch
        {
            OutfitItem.Slot.Hair        => equipped.hair,
            OutfitItem.Slot.Top         => equipped.top,
            OutfitItem.Slot.Bottom      => equipped.bottom,
            OutfitItem.Slot.Shoes       => equipped.shoes,
            OutfitItem.Slot.Accessories => equipped.accessories,
            _                           => null
        };
        return equippedName == myItem.gameObject.name;
    }
}
