using UnityEngine;
using UnityEditor;

public class DialogueAssigner : EditorWindow
{
    [MenuItem("Tools/Assign Calle Crisologo Dialogues to Scene")]
    public static void AssignDialogues()
    {
        var interactables = GameObject.FindObjectsByType<InteractableNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int updated = 0;
        foreach (var interactable in interactables)
        {
            string safeName = interactable.gameObject.name.Replace("_Rigged", "");
            
            // Handle naming mismatches
            if (safeName == "vendorKyros") safeName = "VendorKyros";
            if (safeName == "vendorIrah") safeName = "VendorIrah";
            if (safeName == "vendorJom") safeName = "VendorJom";
            
            string path = $"Assets/Dialogues/CalleCrisologo/{safeName}_Start.asset";
            DialogueNode startNode = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
            
            if (startNode != null)
            {
                // Assign to default dialogue
                interactable.defaultDialogue = startNode;
                
                // Removed the code that clears questDialogues to prevent wiping out manual work
                
                EditorUtility.SetDirty(interactable);
                updated++;
                Debug.Log($"[DialogueAssigner] Assigned {safeName}_Start.asset to {interactable.gameObject.name}");
            }
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Finished assigning dialogues. Updated {updated} NPCs.");
    }
}
