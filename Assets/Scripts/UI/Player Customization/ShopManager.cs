using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the full Shop Scene economy.
/// Attach this to your ShopScene root manager object.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Character")]
    public OutfitManager characterManager;

    [Header("Prefab Settings")]
    public GameObject itemFramePrefab;

    [Header("Categories")]
    public List<CustomizationManager.CategoryFolder> categories = new List<CustomizationManager.CategoryFolder>();

    [Header("Default Sprites")]
    public Sprite noneIcon;
    public Sprite noneBackground;

    [Header("Background Sprites")]
    public Sprite activeBackground;
    public Sprite inactiveBackground;

    [Header("Shop Detail Panel")]
    public ShopDetailPanel detailPanel;

    [Header("Coin Display")]
    [Tooltip("The 'Coins' TextMeshProUGUI inside your CoinGroup")]
    public TextMeshProUGUI coinsText;

    [Header("Save / Back Flow")]
    public Button saveChangesButton;
    public Button backButton;
    public PortraitBooth portraitBooth;
    public GenericModal modal;

    [Header("Frame Label Colors")]
    public Color ownedColor = Color.white;
    public Color equippedColor = Color.green;
    public Color priceColor = new Color(1f, 0.85f, 0f);

    [HideInInspector] public List<string> ownedItems = new List<string>();
    private EquippedOutfitData originalOutfit;
    private bool isInitializingUI = false;

    async void Start()
    {
        // Disable all EventSystems and AudioListeners that belong to OTHER scenes (background scenes).
        // This prevents clicks inside the Shop from accidentally hitting UI in the background scene.
        foreach (var es in FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (es.gameObject.scene != gameObject.scene) es.enabled = false;
        }
        foreach (var al in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (al.gameObject.scene != gameObject.scene) al.enabled = false;
        }

        if (modal == null) modal = GenericModal.Instance;
        if (modal == null) modal = FindFirstObjectByType<GenericModal>(FindObjectsInactive.Include);

        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile == null)
            await UserProfileManager.Instance.FetchProfile();

        if (UserProfileManager.Instance != null && characterManager != null)
        {
            var equippedData = UserProfileManager.Instance.GetEquippedOutfitData();
            if (equippedData != null)
            {
                characterManager.LoadOutfit(equippedData);
                originalOutfit = equippedData;
            }
        }

        if (saveChangesButton != null) saveChangesButton.onClick.AddListener(OnSaveChangesClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

        await InitializeGallery();
        RefreshCoinDisplay();
    }

    public async System.Threading.Tasks.Task InitializeGallery()
    {
        if (characterManager == null || itemFramePrefab == null) return;
        ownedItems = await FetchOwnedInventory();
        foreach (var category in categories) GenerateCategory(category);
    }

    private async System.Threading.Tasks.Task<List<string>> FetchOwnedInventory()
    {
        var owned = new List<string>();
        try
        {
            var user = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (user == null) return owned;
            var response = await SupabaseManager.Instance.client.From<InventoryModel>().Where(x => x.UserId == user.Id).Get();
            if (response?.Models != null)
                foreach (var item in response.Models) owned.Add(item.ItemName);
        }
        catch (System.Exception ex) { Debug.LogError("[ShopManager] Inventory fetch error: " + ex.Message); }
        return owned;
    }

    private void GenerateCategory(CustomizationManager.CategoryFolder category)
    {
        if (category.contentParent == null) return;
        isInitializingUI = true;
        foreach (Transform child in category.contentParent) Destroy(child.gameObject);

        ToggleGroup group = category.contentParent.GetComponent<ToggleGroup>();
        Toggle noneToggle = CreateNoneCard(category, group);

        OutfitItem[] allItems = characterManager.GetComponentsInChildren<OutfitItem>(true);
        var slotItems = allItems.Where(i => i.slot == category.slot).ToList();
        bool IsOwned(OutfitItem i) => i.price <= 0 || ownedItems.Contains(i.name);
        var sorted = slotItems.Where(IsOwned).OrderBy(i => i.itemName)
                     .Concat(slotItems.Where(i => !IsOwned(i)).OrderBy(i => i.price)).ToList();

        EquippedOutfitData current = characterManager.GetEquippedNames();
        string equippedName = GetEquippedNameForSlot(current, category.slot);
        Toggle selectedToggle = null;

        foreach (var item in sorted)
        {
            Toggle t = CreateItemCard(category, item, group);
            if (item.name == equippedName) selectedToggle = t;
        }
        if (selectedToggle == null) selectedToggle = noneToggle;
        selectedToggle.isOn = true;
        isInitializingUI = false;
    }

    private Toggle CreateNoneCard(CustomizationManager.CategoryFolder category, ToggleGroup group)
    {
        GameObject frameObj = Instantiate(itemFramePrefab, category.contentParent);
        frameObj.name = $"None_{category.slot}";
        SetCardIcon(frameObj, noneIcon);
        HideChild(frameObj.transform, "Ownership");
        Toggle toggle = SetupToggle(frameObj, group);
        toggle.onValueChanged.AddListener((isOn) =>
        {
            UpdateCardBackground(frameObj, isOn);
            if (isOn)
            {
                if (!isInitializingUI) { characterManager.Unequip(category.slot); RefreshAllFrames(); }
                if (detailPanel != null) detailPanel.HidePanel();
            }
        });
        return toggle;
    }

    private Toggle CreateItemCard(CustomizationManager.CategoryFolder category, OutfitItem item, ToggleGroup group)
    {
        bool isOwned = item.price <= 0 || ownedItems.Contains(item.name);
        GameObject frameObj = Instantiate(itemFramePrefab, category.contentParent);
        frameObj.name = item.name;
        SetCardIcon(frameObj, item.icon);
        ShopFrameUI frameUI = frameObj.AddComponent<ShopFrameUI>();
        frameUI.Init(item, this);
        Toggle toggle = SetupToggle(frameObj, group);
        toggle.onValueChanged.AddListener((isOn) =>
        {
            UpdateCardBackground(frameObj, isOn);
            if (isOn && detailPanel != null) detailPanel.ShowItem(item, isOwned, this);
        });
        frameUI.RefreshVisuals();
        return toggle;
    }

    // ---- Purchase ----

    public void TryPurchase(OutfitItem item)
    {
        if (modal == null || item == null) return;
        var profile = UserProfileManager.Instance?.CurrentProfile;
        if (profile == null) return;

        if (profile.Coins < item.price)
        {
            modal.ShowAlert($"You do not have enough coins to buy <b>{item.itemName}</b>. Play more!");
            return;
        }
        modal.ShowConfirm(
            $"This item costs <b>{item.price}</b> coins. Are you sure you want to buy this?",
            "Yes", () => _ = OnConfirmPurchase(item), "No");
    }

    private async System.Threading.Tasks.Task OnConfirmPurchase(OutfitItem item)
    {
        if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Show();
        try
        {
            var user = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (user == null) throw new System.Exception("Not logged in!");

            await SupabaseManager.Instance.client.From<InventoryModel>().Insert(new InventoryModel
            { UserId = user.Id, ItemName = item.name, Slot = item.slot.ToString() });

            var profile = UserProfileManager.Instance.CurrentProfile;
            profile.Coins -= item.price;
            await UserProfileManager.Instance.UpdateProfile(profile);
            ownedItems.Add(item.name);
            RefreshCoinDisplay();
            if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Hide();

            modal.ShowAlert($"You successfully purchased <b>{item.itemName}</b>! Enjoy!", "Okay", () =>
            {
                var cat = categories.FirstOrDefault(c => c.slot == item.slot);
                if (cat != null) GenerateCategory(cat);
                if (detailPanel != null) detailPanel.ShowItem(item, true, this);
            });
        }
        catch (System.Exception ex)
        {
            if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Hide();
            modal.ShowAlert("Something went wrong: " + ex.Message);
            Debug.LogError("[ShopManager] Purchase error: " + ex.Message);
        }
    }

    // ---- Save ----

    private void OnSaveChangesClicked()
    {
        if (characterManager == null) return;
        if (modal == null) modal = GenericModal.Instance;
        EquippedOutfitData current = characterManager.GetEquippedNames();
        if (originalOutfit != null && current.IsSameAs(originalOutfit))
        { modal.ShowAlert("You didn't change anything."); return; }
        modal.ShowConfirm("Are you sure you want to save these changes?", "Yes", () => _ = OnConfirmSave(current), "No");
    }

    private async System.Threading.Tasks.Task OnConfirmSave(EquippedOutfitData newOutfit)
    {
        if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Show();
        try
        {
            var user = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (user == null) throw new System.Exception("Not logged in!");
            if (portraitBooth != null && AvatarManager.Instance != null)
            {
                portraitBooth.SetupPortrait(newOutfit);
                if (portraitBooth.portraitTexture != null)
                    await AvatarManager.Instance.CaptureAndUpload(user.Id, portraitBooth.portraitTexture);
            }
            var profile = UserProfileManager.Instance.CurrentProfile;
            profile.EquippedOutfit = newOutfit;
            await UserProfileManager.Instance.UpdateProfile(profile);
            originalOutfit = newOutfit;
            if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Hide();
            modal.ShowAlert("Successfully saved the changes!");
        }
        catch (System.Exception ex)
        {
            if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Hide();
            modal.ShowAlert("Something went wrong while saving: " + ex.Message);
            Debug.LogError("[ShopManager] Save error: " + ex.Message);
        }
    }

    // ---- Back ----

    private void OnBackClicked()
    {
        if (modal == null) modal = GenericModal.Instance;
        EquippedOutfitData current = characterManager.GetEquippedNames();
        if (originalOutfit != null && !current.IsSameAs(originalOutfit))
        {
            modal.ShowConfirm("You have some unsaved changes. Are you sure you want to exit the shop?",
                "Yes", GoBack, "No");
        }
        else { GoBack(); }
    }

    private void GoBack()
    {
        // Delay re-enabling EventSystems by 0.25 seconds to prevent the mouse click from passing through
        GameObject enabler = new GameObject("EventSystemReenabler");
        DontDestroyOnLoad(enabler);
        var mono = enabler.AddComponent<ShopExitHelper>();
        mono.StartCoroutine(mono.ReenableNextFrame(gameObject.scene));

        // Notify the player's OutfitManager in the main scene to refresh the player's outfit
        var allOutfitManagers = FindObjectsByType<OutfitManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var outfitManager in allOutfitManagers)
        {
            if (outfitManager.gameObject.scene != gameObject.scene)
            {
                outfitManager.LoadOutfit(UserProfileManager.Instance.GetEquippedOutfitData());
            }
        }
        
        // Notify the Player Info Panel to update the portrait image
        var infoPanels = FindObjectsByType<PlayerInfoPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var panel in infoPanels)
        {
            panel.UpdatePanelData();
        }

        if (UnityEngine.SceneManagement.SceneManager.sceneCount > 1)
        {
            // It was loaded additively over another scene, so we just unload it!
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        else
        {
            // Fallback if they ran the scene directly
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex - 1);
        }
    }

    // ---- Helpers ----

    public void RefreshCoinDisplay()
    {
        if (coinsText == null) return;
        int coins = UserProfileManager.Instance?.CurrentProfile?.Coins ?? 0;
        coinsText.text = coins.ToString("N0");
    }

    public void RefreshAllFrames()
    {
        foreach (var frame in GetComponentsInChildren<ShopFrameUI>(true)) frame.RefreshVisuals();
    }

    private Toggle SetupToggle(GameObject frameObj, ToggleGroup group)
    {
        Toggle rootToggle = frameObj.GetComponent<Toggle>();
        foreach (var t in frameObj.GetComponentsInChildren<Toggle>(true))
            if (t != rootToggle) Destroy(t);
        rootToggle.group = group;
        rootToggle.isOn = false;
        return rootToggle;
    }

    private void UpdateCardBackground(GameObject frameObj, bool isOn)
    {
        Transform bgT = frameObj.transform.Find("Background");
        Image bgImg = bgT != null ? bgT.GetComponent<Image>() : frameObj.GetComponent<Image>();
        if (bgImg != null) bgImg.sprite = isOn ? activeBackground : inactiveBackground;
    }

    private void SetCardIcon(GameObject frameObj, Sprite icon)
    {
        Transform iconT = FindChildRecursive(frameObj.transform, "ItemIcon");
        if (iconT == null) iconT = FindChildRecursive(frameObj.transform, "AssetIcon");
        if (iconT != null)
        {
            Image img = iconT.GetComponent<Image>();
            if (img != null) { img.sprite = icon; img.color = icon != null ? Color.white : new Color(0,0,0,0); }
        }
    }

    private void HideChild(Transform parent, string name)
    {
        Transform child = FindChildRecursive(parent, name);
        if (child != null) child.gameObject.SetActive(false);
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

    private string GetEquippedNameForSlot(EquippedOutfitData data, OutfitItem.Slot slot) => slot switch
    {
        OutfitItem.Slot.Hair        => data.hair,
        OutfitItem.Slot.Top         => data.top,
        OutfitItem.Slot.Bottom      => data.bottom,
        OutfitItem.Slot.Shoes       => data.shoes,
        OutfitItem.Slot.Accessories => data.accessories,
        _                           => ""
    };
}

// ============================================================
// Per-card badge helper � OWNED / Equipped / price
// ============================================================
// Per-card badge helper (Moved to ShopFrameUI.cs)
// ============================================================
public class ShopExitHelper : MonoBehaviour
{
    public System.Collections.IEnumerator ReenableNextFrame(UnityEngine.SceneManagement.Scene unloadedScene)
    {
        // First: immediately block the PlayerInfoPanel from accepting clicks for 3 seconds.
        // This is the safest layer of protection — even if EventSystem re-enables fast,
        // the panel itself will refuse to navigate for 3 full seconds.
        foreach (var panel in FindObjectsByType<PlayerInfoPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (panel.gameObject.scene != unloadedScene)
                panel.TemporarilyDisableInteraction(3f);
        }

        // Then wait 0.25 seconds before re-enabling the background EventSystems
        yield return new WaitForSeconds(0.25f);
        
        foreach (var es in FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (es.gameObject.scene != unloadedScene) es.enabled = true;
        }
        foreach (var al in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (al.gameObject.scene != unloadedScene) al.enabled = true;
        }

        // FIX: Re-enable Canvases and Lights that were hidden by HideCurrentSceneImmediate()
        // Without this, Magellan's UI and lighting never come back after returning from Shop.
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.gameObject.scene != unloadedScene) c.enabled = true;
        }
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.gameObject.scene != unloadedScene) l.enabled = true;
        }

        // Restore the BGM for whichever scene is now active (OnSceneLoaded doesn't fire on unload)
        if (BGMManager.Instance != null)
            BGMManager.Instance.RefreshBGMForActiveScene();
        
        Destroy(gameObject);
    }
}


