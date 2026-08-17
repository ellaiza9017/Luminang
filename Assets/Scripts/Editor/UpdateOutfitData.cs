#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class UpdateOutfitData : MonoBehaviour
{
    [MenuItem("Tools/Update Outfit Data")]
    public static void UpdateData()
    {
        // Data from your image, now with tiered pricing
        var data = new Dictionary<string, (string name, string desc, int price)>
        {
            // Pants/Skirts
            { "beigeTrouser", ("Lino Trousers", "Lightweight trousers that keep you comfy wherever you go.", 250) },
            { "blackTrouser", ("Black Trousers", "A clean and versatile pair for any occasion.", 0) },
            { "cargoShorts", ("Lakbay Shorts", "Packed with pockets for every little adventure.", 100) },
            { "denimJeans", ("Maong Pants", "A timeless favorite made for everyday wear.", 300) },
            { "fantasyPants", ("Mandirigma Greaves", "Rugged legwear fit for a brave adventurer.", 400) },
            { "pinkSkirt", ("Rosas Skirt", "A sweet and playful skirt that brightens any day.", 500) },

            // Shirts/Dresses
            { "AdvShirt", ("Lakbay Vest", "Made for little explorers who are always ready for the next adventure.", 375) },
            { "baro'tSaya1", ("Baro't Saya", "A timeless Filipina outfit that celebrates grace and tradition.", 650) },
            { "barong", ("Barong Tagalog", "A proudly Filipino classic, perfect for special occasions.", 400) },
            { "blackShirt", ("Black Tee", "Simple, comfy, and goes with just about anything.", 100) },
            { "bunnyShirt", ("Kuneho Tee", "A soft little shirt with an extra dose of cuteness.", 250) },
            { "guayaberaShirt", ("Guayabera", "A breezy embroidered shirt with timeless island charm.", 200) },
            { "purpleCatShirt", ("Muning Tee", "For cat lovers who carry a little mischief wherever they go.", 300) },
            { "purpleDress", ("Maharlika Dress", "A regal outfit inspired by stories of old Filipino kingdoms.", 450) },
            { "redFantasyDress", ("Diwata Dress", "A magical dress that feels straight out of a Filipino folktale.", 500) },
            { "SayaDress", ("Filipiniana", "An elegant Filipiniana outfit that celebrates Filipino heritage with pride.", 650) },
            { "tuckedShirt", ("Polo Blanco", "A neat and polished look that's always in style.", 150) },
            { "whiteShirt", ("White Tee", "The everyday favorite that never goes out of fashion.", 0) },

            // Shoes
            { "beigeHighHeel", ("Mutya Heels", "A graceful pair that adds a touch of elegance to any outfit.", 250) },
            { "blackShoe", ("Black Loafers", "A dependable pair that's ready for school, work, or adventure.", 0) },
            { "blackSneakers", ("Black Sneakers", "Made for busy days and endless exploring.", 250) },
            { "brownBoots", ("Brown Boots", "Sturdy boots built for every trail and every journey.", 300) },
            { "brownShoe", ("Brown Oxfords", "A classic pair with a warm, timeless look.", 150) },
            { "flatSandalsBrown", ("Bakya Sandals", "Light, comfy sandals perfect for sunny days.", 350) },
            { "lacedBrownBoots", ("Lakbay Boots", "Lace up and head out for your next adventure.", 350) },

            // Hairs
            { "blackHairShort", ("Black Hair Short", "A neat bob that never goes out of style.", 0) },
            { "blondHair", ("Golden Layers", "Soft layered hair with a bright, cheerful look.", 225) },
            { "longBlackHair", ("Long Black Hair", "Long, sleek hair that's simple and elegant.", 0) },
            { "pinkHair", ("Pink Twin Braids", "A playful braided style full of personality.", 300) },
            { "purpBlackHairShort", ("Side Fringe", "A short cut with a stylish side-swept fringe.", 250) },
            { "shortBlackHair", ("Textured Crop", "A clean, textured haircut for an everyday look.", 100) },
            { "shortBrownHair", ("Tousled Cut", "A slightly messy style with effortless charm.", 150) },
            { "shortMochaHair", ("Soft Layers", "Light layers that create a gentle, relaxed look.", 200) },
            { "spikyBlonde", ("Spiky Cut", "A bold hairstyle with plenty of attitude.", 250) },

            // Accessories
            { "strawHat", ("Salakot", "A traditional Filipino hat that keeps you cool under the sun.", 0) }
        };

        int count = 0;
        
        // Find ALL prefabs in the project
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Search the prefab and all its children for OutfitItems!
            OutfitItem[] itemsInPrefab = prefab.GetComponentsInChildren<OutfitItem>(true);
            bool prefabModified = false;

            foreach (var item in itemsInPrefab)
            {
                if (data.TryGetValue(item.gameObject.name, out var info))
                {
                    item.itemName = info.name;
                    item.itemDescription = info.desc;
                    item.price = info.price; // Apply the new tiered price
                    
                    EditorUtility.SetDirty(item);
                    prefabModified = true;
                    count++;
                }
            }

            if (prefabModified)
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        // Also update any loose ones in the active scene just in case
        OutfitItem[] sceneItems = Object.FindObjectsByType<OutfitItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool sceneModified = false;
        foreach (var item in sceneItems)
        {
            if (data.TryGetValue(item.gameObject.name, out var info))
            {
                item.itemName = info.name;
                item.itemDescription = info.desc;
                item.price = info.price;
                EditorUtility.SetDirty(item);
                sceneModified = true;
                count++;
            }
        }

        if (sceneModified)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        if (count > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully updated {count} OutfitItems across prefabs and the active scene!");
        }
        else
        {
            Debug.LogWarning("No matching OutfitItems found anywhere!");
        }
    }
}
#endif
