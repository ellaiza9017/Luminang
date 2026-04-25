using UnityEngine;

public class OutfitTester : MonoBehaviour
{
    public OutfitManager manager;
    public OutfitItem testItem;

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            manager.Equip(testItem);
        }
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.yKey.wasPressedThisFrame)
        {
            manager.Unequip(testItem.slot);
        }
    }
}