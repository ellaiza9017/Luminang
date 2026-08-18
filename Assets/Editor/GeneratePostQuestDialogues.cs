using UnityEngine;
using UnityEditor;
using System.IO;

public class GeneratePostQuestDialogues
{
    [MenuItem("Tools/Generate Post Quest Dialogues")]
    public static void GeneratePostQuests()
    {
        string targetFolder = "Assets/Dialogues/PostQuestDialogues";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Dialogues"))
        {
            AssetDatabase.CreateFolder("Assets", "Dialogues");
        }
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            AssetDatabase.CreateFolder("Assets/Dialogues", "PostQuestDialogues");
        }

        int generatedCount = 0;
        int assignedCount = 0;

        // Find all InteractableNPCs in the active scene
        InteractableNPC[] allNPCs = Object.FindObjectsByType<InteractableNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var npc in allNPCs)
        {
            // Only care about NPCs that actually have quests
            if (npc.questDialogues == null || npc.questDialogues.Count == 0) continue;

            // Get a clean name
            string cleanName = npc.gameObject.name.Replace("NPC", "").Replace("_", " ").Replace("Rigged", "").Replace("Casual", "").Replace("vendor", "").Replace("barista", "").Trim();
            if (cleanName.Length > 0)
                cleanName = char.ToUpper(cleanName[0]) + cleanName.Substring(1);
            if (cleanName.ToLower() == "mar-taho") cleanName = "Mar";

            string assetPath = $"{targetFolder}/PostQuest_{cleanName}.asset";

            DialogueNode existingNode = AssetDatabase.LoadAssetAtPath<DialogueNode>(assetPath);
            
            if (existingNode == null)
            {
                // Create new asset
                existingNode = ScriptableObject.CreateInstance<DialogueNode>();
                existingNode.speakerName = cleanName;
                existingNode.dialogueText = "Thank you for your help earlier!";
                
                AssetDatabase.CreateAsset(existingNode, assetPath);
                generatedCount++;
            }

            // Assign it if they don't have one assigned
            if (npc.defaultDialogue == null)
            {
                Undo.RecordObject(npc, "Assign Post-Quest Dialogue");
                npc.defaultDialogue = existingNode;
                PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
                assignedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Post-Quest Generation", 
            $"Done!\n\nGenerated {generatedCount} new DialogueNode files in {targetFolder}.\n\nAssigned to {assignedCount} NPCs.", "OK");
    }

    [MenuItem("Tools/Generate Specific NPC Post Quests (From List)")]
    public static void GenerateSpecificPostQuests()
    {
        string targetFolder = "Assets/Dialogues/PostQuestDialogues";
        
        if (!AssetDatabase.IsValidFolder("Assets/Dialogues")) AssetDatabase.CreateFolder("Assets", "Dialogues");
        if (!AssetDatabase.IsValidFolder(targetFolder)) AssetDatabase.CreateFolder("Assets/Dialogues", "PostQuestDialogues");

        var customDialogues = new System.Collections.Generic.Dictionary<string, string>()
        {
            {"Jen", "Hey! Good luck out there!"},
            {"Jerem", "Thanks again! Let me know if you need any more tips!"},
            {"Dave", "See ya around! Stay safe!"},
            {"Lorraine", "Thanks for the help! I really appreciate it."},
            {"Lina", "You've been so kind, have a wonderful day!"},
            {"Klara", "Catch you later! Keep up the good work."},
            {"Rayo", "Thanks! I'm back to business now."},
            {"AlingRiza", "Salamat, anak! You've been a big help to this old woman."},
            {"Rodrick", "Thanks, buddy. Let's hang out again soon."},
            {"Tomas", "Appreciate it! Have a good one!"},
            {"Lito", "Thanks! Back to work for me now."},
            {"Ronnie", "Alright, I'm all set! See ya!"},
            {"Sally", "Thanks bestie! Let's chat again soon!"},
            {"Wayne", "Thanks man. I owe you one."},
            {"LolaBebang", "Oh, thank you, apo! May God bless you!"},
            {"Neneng", "Thanks! You're a lifesaver."},
            {"Kyros", "Thanks! Come back anytime if you want to buy something!"},
            {"Mishang", "Thanks! Let me know if you want to hear more stories."},
            {"Irah", "Salamat! Check out my shop again next time!"},
            {"Jom", "Thanks for the help! Good deals waiting for you here!"},
            {"Aliyah", "Thanks! Drop by if you need another coffee!"},
            {"Ellai", "Thanks! Keep me in mind when you're looking for souvenirs!"},
            {"Mar", "Taho! Thanks for earlier! Let me know if you want more taho!"},
            {"MangLance", "Salamat! It's always nice to see helpful young folks around."}
        };

        int generatedCount = 0;
        int updatedCount = 0;

        foreach (var kvp in customDialogues)
        {
            string npcName = kvp.Key;
            string dialogueText = kvp.Value;
            string assetPath = $"{targetFolder}/PostQuest_{npcName}.asset";
            
            DialogueNode existingNode = AssetDatabase.LoadAssetAtPath<DialogueNode>(assetPath);
            
            if (existingNode == null)
            {
                existingNode = ScriptableObject.CreateInstance<DialogueNode>();
                existingNode.speakerName = npcName;
                existingNode.dialogueText = dialogueText;
                
                AssetDatabase.CreateAsset(existingNode, assetPath);
                generatedCount++;
            }
            else
            {
                // If it already exists and still has the old generic dialogue, update it!
                if (existingNode.dialogueText == "Thank you for your help earlier!")
                {
                    existingNode.dialogueText = dialogueText;
                    EditorUtility.SetDirty(existingNode);
                    updatedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Specific Post-Quest Generation", 
            $"Done!\n\nGenerated {generatedCount} new DialogueNode files.\nUpdated {updatedCount} existing files with new custom dialogue.", "OK");
    }
}
