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
            { "blackTrouser", ("Black Trousers", "A clean and versatile pair for any occasion.", 100) },
            { "cargoShorts", ("Lakbay Shorts", "Packed with pockets for every little adventure.", 120) },
            { "denimJeans", ("Maong Pants", "A timeless favorite made for everyday wear.", 150) },
            { "fantasyPants", ("Mandirigma Greaves", "Rugged legwear fit for a brave adventurer.", 600) },
            { "pinkSkirt", ("Rosas Skirt", "A sweet and playful skirt that brightens any day.", 300) },

            // Shirts/Dresses
            { "AdvShirt", ("Lakbay Vest", "Made for little explorers who are always ready for the next adventure.", 600) },
            { "baro'tSaya", ("Baro't Saya", "A timeless Filipina outfit that celebrates grace and tradition.", 1000) },
            { "barong", ("Barong Tagalog", "A proudly Filipino classic, perfect for special occasions.", 1000) },
            { "blackShirt", ("Black Tee", "Simple, comfy, and goes with just about anything.", 100) },
            { "bunnyShirt", ("Kuneho Tee", "A soft little shirt with an extra dose of cuteness.", 300) },
            { "guayaberaShirt", ("Guayabera", "A breezy embroidered shirt with timeless island charm.", 350) },
            { "purpleCatShirt", ("Muning Tee", "For cat lovers who carry a little mischief wherever they go.", 300) },
            { "purpleDress", ("Maharlika Dress", "A regal outfit inspired by stories of old Filipino kingdoms.", 800) },
            { "redFantasyDress", ("Diwata Dress", "A magical dress that feels straight out of a Filipino folktale.", 1000) },
            { "SayaDress", ("Filipiniana", "An elegant Filipiniana outfit that celebrates Filipino heritage with pride.", 1000) },
            { "tuckedShirt", ("Polo Blanco", "A neat and polished look that's always in style.", 150) },
            { "whiteShirt", ("White Tee", "The everyday favorite that never goes out of fashion.", 100) },

            // Shoes
            { "beigeHighHeel", ("Mutya Heels", "A graceful pair that adds a touch of elegance to any outfit.", 400) },
            { "blackShoe", ("Black Loafers", "A dependable pair that's ready for school, work, or adventure.", 150) },
            { "blackSneakers", ("Black Sneakers", "Made for busy days and endless exploring.", 120) },
            { "brownBoots", ("Brown Boots", "Sturdy boots built for every trail and every journey.", 600) },
            { "brownShoe", ("Brown Oxfords", "A classic pair with a warm, timeless look.", 300) },
            { "flatSandalsBrown", ("Bakya Sandals", "Light, comfy sandals perfect for sunny days.", 100) },
            { "lacedBrownBoots", ("Lakbay Boots", "Lace up and head out for your next adventure.", 800) },

            // Hairs
            { "blackHairShort", ("Black Hair Short", "A neat bob that never goes out of style.", 100) },
            { "blondHair", ("Golden Layers", "Soft layered hair with a bright, cheerful look.", 250) },
            { "longBlackHair", ("Long Black Hair", "Long, sleek hair that's simple and elegant.", 150) },
            { "pinkHair", ("Pink Twin Braids", "A playful braided style full of personality.", 600) },
            { "purpBlackHairShort", ("Side Fringe", "A short cut with a stylish side-swept fringe.", 300) },
            { "shortBlackHair", ("Textured Crop", "A clean, textured haircut for an everyday look.", 100) },
            { "shortBrownHair", ("Tousled Cut", "A slightly messy style with effortless charm.", 200) },
            { "shortMochaHair", ("Soft Layers", "Light layers that create a gentle, relaxed look.", 250) },
            { "spikyBlonde", ("Spiky Cut", "A bold hairstyle with plenty of attitude.", 600) },

            // Accessories
            { "strawHat", ("Salakot", "A traditional Filipino hat that keeps you cool under the sun.", 350) }
        };

        int count = 0;
        // Find all OutfitItems in the scene (even if they are hidden/disabled)
        OutfitItem[] allItems = Object.FindObjectsByType<OutfitItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (var item in allItems)
        {
            // If the GameObject's name matches one in our dictionary, update it
            if (data.TryGetValue(item.gameObject.name, out var info))
            {
                item.itemName = info.name;
                item.itemDescription = info.desc;
                item.price = info.price; // Apply the new tiered price
                EditorUtility.SetDirty(item);
                count++;
            }
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Successfully updated {count} OutfitItems! Please save the scene (Ctrl+S).");
        }
        else
        {
            Debug.LogWarning("No matching OutfitItems found in the scene. Make sure you are in the CreateCharacterScene!");
        }
    }
}
#endif
