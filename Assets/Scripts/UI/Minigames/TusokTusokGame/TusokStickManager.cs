using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TusokStickManager : MonoBehaviour
{
    public static TusokStickManager Instance { get; private set; }

    [Header("Settings")]
    public int maxItems = 10;
    
    [Header("References")]
    [Tooltip("Drag the 10 invisible slots here, starting from the base of the stick to the tip (or vice versa, depending on your visual preference)")]
    public TusokStickItem[] stickSlots; 
    
    private int currentItemCount = 0;

    private void Awake()
    {
        Instance = this;

        // Hide all slots initially
        foreach (var slot in stickSlots)
        {
            if (slot != null) slot.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetFoodCount(TusokWokItem.FoodType foodType)
    {
        int count = 0;
        for (int i = 0; i < stickSlots.Length; i++)
        {
            if (stickSlots[i] != null && stickSlots[i].gameObject.activeSelf && stickSlots[i].originalWokItem != null && stickSlots[i].originalWokItem.foodType == foodType)
            {
                count++;
            }
        }
        return count;
    }

    public bool CanAddItem()
    {
        return currentItemCount < maxItems && currentItemCount < stickSlots.Length;
    }

    public bool AddFoodToStick(TusokWokItem wokItem)
    {
        if (!CanAddItem()) return false;

        int tipIndex = stickSlots.Length - 1;
        int lowestFilledIndex = stickSlots.Length - currentItemCount;

        // Shift existing items down towards index 0 (the hand)
        for (int i = lowestFilledIndex - 1; i < tipIndex; i++)
        {
            if (i >= 0 && stickSlots[i + 1].gameObject.activeSelf)
            {
                stickSlots[i].gameObject.SetActive(true);
                stickSlots[i].Initialize(stickSlots[i + 1].originalWokItem);
            }
        }

        // Place the newest item at the tip (Element 9)
        stickSlots[tipIndex].gameObject.SetActive(true);
        stickSlots[tipIndex].Initialize(wokItem);
        
        currentItemCount++;
        
        if (TusokTusokGameManager.Instance != null)
        {
            TusokTusokGameManager.Instance.UpdateInventoryUI();
            TusokTusokGameManager.Instance.PlayTusokSFX();
        }
        return true;
    }

    public void RemoveFoodFromStick(TusokStickItem itemToRemove)
    {
        int tipIndex = stickSlots.Length - 1;

        // FILO Logic: Only allow removing if it's the tip (Element 9)
        if (currentItemCount > 0 && itemToRemove == stickSlots[tipIndex])
        {
            // Remove the tip
            itemToRemove.originalWokItem.ResetToTray();
            
            int lowestFilledIndex = stickSlots.Length - currentItemCount;

            // Shift everything back up towards the tip
            for (int i = tipIndex; i > lowestFilledIndex; i--)
            {
                stickSlots[i].Initialize(stickSlots[i - 1].originalWokItem);
                stickSlots[i].gameObject.SetActive(true);
            }
            
            // Turn off the lowest index that just got shifted up
            stickSlots[lowestFilledIndex].gameObject.SetActive(false);
            
            currentItemCount--;
            
            if (TusokTusokGameManager.Instance != null)
            {
                TusokTusokGameManager.Instance.UpdateInventoryUI();
                TusokTusokGameManager.Instance.PlayReturnSFX();
            }
        }
        else
        {
            // It's NOT the tip. Shake the specific item and vibrate!
            StartCoroutine(ShakeItemRoutine(itemToRemove));
        }
    }

    private System.Collections.IEnumerator ShakeItemRoutine(TusokStickItem item)
    {
        // Vibrate phone
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
        
        RectTransform rt = item.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 originalPos = rt.anchoredPosition3D;
        float elapsed = 0f;
        float duration = 0.3f;
        float magnitude = 15f; // pixels to shake

        while (elapsed < duration)
        {
            float xOffset = Random.Range(-1f, 1f) * magnitude;
            float yOffset = Random.Range(-1f, 1f) * magnitude;
            
            rt.anchoredPosition3D = originalPos + new Vector3(xOffset, yOffset, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition3D = originalPos;
    }

    public void ClearStick()
    {
        for (int i = 0; i < stickSlots.Length; i++)
        {
            if (stickSlots[i] != null && stickSlots[i].gameObject.activeSelf)
            {
                if (stickSlots[i].originalWokItem != null) stickSlots[i].originalWokItem.ResetToTray();
                stickSlots[i].gameObject.SetActive(false);
            }
        }
        currentItemCount = 0;
        if (TusokTusokGameManager.Instance != null) TusokTusokGameManager.Instance.UpdateInventoryUI();
    }
}
