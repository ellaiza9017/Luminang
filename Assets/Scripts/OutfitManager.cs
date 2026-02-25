using System.Collections.Generic;
using UnityEngine;

public class OutfitManager : MonoBehaviour
{
    [Header("Body parts")]
    public GameObject head;
    public GameObject arms;
    public GameObject torso;
    public GameObject legs;
    public GameObject feet;
    public GameObject hands;

    private readonly Dictionary<OutfitItem.Slot, OutfitItem> equipped = new();

    public void Equip(OutfitItem item)
    {
        if (item == null) return;

        // Dress overrides Top + Bottom
        if (item.slot == OutfitItem.Slot.Dress)
        {
            Unequip(OutfitItem.Slot.Top);
            Unequip(OutfitItem.Slot.Bottom);
        }
        // Equipping top/bottom removes dress
        else if (item.slot == OutfitItem.Slot.Top || item.slot == OutfitItem.Slot.Bottom)
        {
            Unequip(OutfitItem.Slot.Dress);
        }

        Unequip(item.slot);

        item.gameObject.SetActive(true);
        equipped[item.slot] = item;

        RefreshBodyVisibility();
    }

    public void Unequip(OutfitItem.Slot slot)
    {
        if (equipped.TryGetValue(slot, out var existing) && existing != null)
        {
            existing.gameObject.SetActive(false);
        }
        equipped.Remove(slot);
        RefreshBodyVisibility();
    }

    private void RefreshBodyVisibility()
    {
        bool showHead = true, showArms = true, showTorso = true, showLegs = true, showFeet = true, showHands = true;
;

        foreach (var kv in equipped)
        {
            var item = kv.Value;
            if (item == null) continue;

            if (item.hideHead) showHead = false;
            if (item.hideArms) showArms = false;
            if (item.hideTorso) showTorso = false;
            if (item.hideLegs) showLegs = false;
            if (item.hideFeet) showFeet = false;
            if (item.hideHands) showHands = false; 
        }

        if (head) head.SetActive(showHead);
        if (arms) arms.SetActive(showArms);
        if (torso) torso.SetActive(showTorso);
        if (legs) legs.SetActive(showLegs);
        if (feet) feet.SetActive(showFeet);
         if (hands) hands.SetActive(showHands);
    }
}