import re

with open(r"C:\Users\Irah\Documents\Unity Projects\Luminang_New\Assets\Scripts\UI\Player Customization\ShopManager.cs", "r", encoding="utf-8") as f:
    content = f.read()

# Replace CreateNoneCard
old_none = r"""    private Toggle CreateNoneCard(CustomizationManager.CategoryFolder category, ToggleGroup group)
    {
        GameObject frameObj = Instantiate(itemFramePrefab, category.contentParent);
        frameObj.name = $"None_{category.slot}";
        SetCardIcon(frameObj, noneIcon);
        HideChild(frameObj.transform, "Ownership");
        Toggle toggle = SetupToggle(frameObj, group);"""

new_none = r"""    private Toggle CreateNoneCard(CustomizationManager.CategoryFolder category, ToggleGroup group)
    {
        GameObject frameObj = Instantiate(itemFramePrefab, category.contentParent);
        frameObj.name = $"None_{category.slot}";
        SetCardIcon(frameObj, noneIcon);
        HideChild(frameObj.transform, "Ownership");
        HideChild(frameObj.transform, "CoinIcon");
        HideChild(frameObj.transform, "Coins");
        HideChild(frameObj.transform, "OwnedEquipped");
        Toggle toggle = SetupToggle(frameObj, group);"""
        
content = content.replace(old_none, new_none)

pattern = r"public class ShopFrameUI : MonoBehaviour\s*\{.*?\s+private bool IsItemEquipped\(\)"

new_class_top = r"""public class ShopFrameUI : MonoBehaviour
{
    private OutfitItem myItem;
    private ShopManager myManager;
    private TMP_Text nameLabel;
    
    private GameObject coinIcon;
    private TMP_Text coinsLabel;
    private TMP_Text ownedEquippedLabel;

    public void Init(OutfitItem item, ShopManager manager)
    {
        myItem = item;
        myManager = manager;
        Transform nameTrans = myManager.FindChildRecursive(transform, "ItemName");
        if (nameTrans != null) nameLabel = nameTrans.GetComponent<TMP_Text>();
        
        Transform coinIconTrans = myManager.FindChildRecursive(transform, "CoinIcon");
        if (coinIconTrans != null) coinIcon = coinIconTrans.gameObject;
        
        Transform coinsTrans = myManager.FindChildRecursive(transform, "Coins");
        if (coinsTrans != null) coinsLabel = coinsTrans.GetComponent<TMP_Text>();
        
        Transform ownedTrans = myManager.FindChildRecursive(transform, "OwnedEquipped");
        if (ownedTrans != null) ownedEquippedLabel = ownedTrans.GetComponent<TMP_Text>();

        if (nameLabel != null) nameLabel.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
    }

    public void RefreshVisuals()
    {
        if (myItem == null || myManager == null) return;
        bool isOwned  = myItem.price <= 0 || myManager.ownedItems.Contains(myItem.name);
        bool isEquipped = IsItemEquipped();
        
        if (isEquipped)
        {
            if (coinIcon != null) coinIcon.SetActive(false);
            if (coinsLabel != null) coinsLabel.gameObject.SetActive(false);
            if (ownedEquippedLabel != null)
            {
                ownedEquippedLabel.gameObject.SetActive(true);
                ownedEquippedLabel.text = "EQUIPPED";
                ownedEquippedLabel.color = myManager.equippedColor;
            }
        }
        else if (isOwned)
        {
            if (coinIcon != null) coinIcon.SetActive(false);
            if (coinsLabel != null) coinsLabel.gameObject.SetActive(false);
            if (ownedEquippedLabel != null)
            {
                ownedEquippedLabel.gameObject.SetActive(true);
                ownedEquippedLabel.text = "OWNED";
                ownedEquippedLabel.color = myManager.ownedColor;
            }
        }
        else
        {
            if (ownedEquippedLabel != null) ownedEquippedLabel.gameObject.SetActive(false);
            if (coinIcon != null) coinIcon.SetActive(true);
            if (coinsLabel != null)
            {
                coinsLabel.gameObject.SetActive(true);
                coinsLabel.text = myItem.price.ToString();
                coinsLabel.color = myManager.priceColor;
            }
        }
    }

    private bool IsItemEquipped()"""

content = re.sub(pattern, new_class_top, content, flags=re.DOTALL)

with open(r"C:\Users\Irah\Documents\Unity Projects\Luminang_New\Assets\Scripts\UI\Player Customization\ShopManager.cs", "w", encoding="utf-8") as f:
    f.write(content)
