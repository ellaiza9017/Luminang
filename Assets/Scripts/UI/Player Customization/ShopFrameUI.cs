using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopFrameUI : MonoBehaviour
{
    private OutfitItem myItem;
    private ShopManager myManager;
    
    [Header("UI References (Optional)")]
    public TMP_Text nameLabel;
    public GameObject coinIcon;
    public TMP_Text coinsLabel;
    public TMP_Text ownedEquippedLabel;

    public void Init(OutfitItem item, ShopManager manager)
    {
        myItem = item;
        myManager = manager;
        
        // Auto-find references if not assigned in the inspector
        if (nameLabel == null)
        {
            Transform nameTrans = myManager.FindChildRecursive(transform, "ItemName");
            if (nameTrans != null) nameLabel = nameTrans.GetComponent<TMP_Text>();
        }
        
        if (coinIcon == null)
        {
            Transform coinIconTrans = myManager.FindChildRecursive(transform, "CoinIcon");
            if (coinIconTrans != null) coinIcon = coinIconTrans.gameObject;
        }
        
        if (coinsLabel == null)
        {
            Transform coinsTrans = myManager.FindChildRecursive(transform, "Coins");
            if (coinsTrans != null) coinsLabel = coinsTrans.GetComponent<TMP_Text>();
        }
        
        if (ownedEquippedLabel == null)
        {
            Transform ownedTrans = myManager.FindChildRecursive(transform, "OwnedEquipped");
            if (ownedTrans != null) ownedEquippedLabel = ownedTrans.GetComponent<TMP_Text>();
        }

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

    private bool IsItemEquipped()
    {
        if (myManager?.characterManager == null) return false;
        var equipped = myManager.characterManager.GetEquippedNames();
        return (myItem.slot switch
        {
            OutfitItem.Slot.Hair        => equipped.hair,
            OutfitItem.Slot.Top         => equipped.top,
            OutfitItem.Slot.Bottom      => equipped.bottom,
            OutfitItem.Slot.Shoes       => equipped.shoes,
            OutfitItem.Slot.Accessories => equipped.accessories,
            _                           => null
        }) == myItem.gameObject.name;
    }
}

