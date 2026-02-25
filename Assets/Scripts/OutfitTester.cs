using UnityEngine;

public class OutfitTester : MonoBehaviour
{
    public OutfitManager manager;
    public OutfitItem testItem;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            manager.Equip(testItem);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            manager.Unequip(testItem.slot);
        }
    }
}