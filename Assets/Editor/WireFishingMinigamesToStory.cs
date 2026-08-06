using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class WireFishingMinigamesToStory
{
    [MenuItem("Tools/Luminang/Wire Fishing Quests To Story")]
    public static void InjectFishingQuests()
    {
        string[] searchFolders = new string[] { "Assets/Dialogues" };
        string[] allGuids = AssetDatabase.FindAssets("t:DialogueNode", searchFolders);

        string outputFolder = "Assets/Dialogues/FishingQuests/InjectedIntros";
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Dialogues/FishingQuests"))
                AssetDatabase.CreateFolder("Assets/Dialogues", "FishingQuests");
            AssetDatabase.CreateFolder("Assets/Dialogues/FishingQuests", "InjectedIntros");
        }

        int patchedCount = 0;

        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Skip already injected intros to avoid infinite loops/double processing
            if (path.Contains("FishingQuests")) continue;
            
            DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
            if (node == null) continue;

            bool modified = false;

            // 1. Check choices
            if (node.choices != null)
            {
                for (int i = 0; i < node.choices.Count; i++)
                {
                    var choice = node.choices[i];
                    if (choice != null && !string.IsNullOrEmpty(choice.choiceEvent) && choice.choiceEvent.Trim().Equals("StartMinigame:FishingGame", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Found a minigame trigger!
                        Debug.Log($"[WireFishingMinigames] Found minigame trigger in choice on node: {node.name}");
                        
                        DialogueNode originalNext = choice.nextNode;
                        choice.choiceEvent = ""; // Remove abrupt trigger
                        
                        // Generate the intro chain
                        DialogueNode intro1 = GenerateIntroChain(node.name + "_Choice" + i, node.speakerName, node.speakerPortrait, originalNext, outputFolder);
                        choice.nextNode = intro1; // Wire choice to intro
                        modified = true;
                    }
                }
            }

            // 2. Check endEventName
            if (!string.IsNullOrEmpty(node.endEventName) && node.endEventName.Trim().Equals("StartMinigame:FishingGame", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[WireFishingMinigames] Found minigame trigger in endEventName on node: {node.name}");
                node.endEventName = ""; // Remove abrupt trigger
                
                // Usually endEventName means no choices, so we must add a "Continue" choice to start the intro
                DialogueNode intro1 = GenerateIntroChain(node.name + "_EndEvent", node.speakerName, node.speakerPortrait, null, outputFolder);
                
                if (node.choices == null) node.choices = new List<DialogueChoice>();
                node.choices.Clear();
                node.choices.Add(new DialogueChoice {
                    choiceText = "Continue",
                    nextNode = intro1,
                    isWrong = false
                });
                
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(node);
                patchedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[WireFishingMinigames] Finished wiring! Patched {patchedCount} story nodes to properly introduce the minigame.");
    }

    private static DialogueNode GenerateIntroChain(string baseName, string speakerName, Sprite speakerPortrait, DialogueNode finalNextNode, string outputFolder)
    {
        // Node 1
        DialogueNode n1 = ScriptableObject.CreateInstance<DialogueNode>();
        n1.speakerName = speakerName;
        n1.speakerPortrait = speakerPortrait;
        n1.dialogueText = "Before we continue, could you help me with a small task?";
        n1.animationTrigger = "Talk";

        string uniqueReason = "I need some fresh fish, but I haven't caught enough yet. Could you help me catch a few?";
        if (speakerName.IndexOf("Kyros", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I am preparing dinner tonight, but I haven't caught enough yet. Could you help me catch a few fish?";
        else if (speakerName.IndexOf("Irah", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I'm hoping to sell fresh fish at the market today, but I haven't caught enough yet. Could you help me?";
        else if (speakerName.IndexOf("Jom", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I'm helping neighbors gather food for a community feast later. Could you catch a few more fish for us?";
        else if (speakerName.IndexOf("Sally", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I am feeding my family this evening and we are short on fish. Would you mind catching a few for me?";
        else if (speakerName.IndexOf("Lito", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I am gathering food for some of the local guides. Can you assist me with catching some fish?";
        else if (speakerName.IndexOf("Klara", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I am preparing a special recipe that calls for fresh fish. Can you help me catch some?";
        else if (speakerName.IndexOf("Lance", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "There's a village celebration coming up and they asked me to bring fish. Can you lend me a hand?";
        else if (speakerName.IndexOf("Rosa", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I'm preparing tomorrow's trip to the neighboring town and I want to bring some fresh fish. Will you catch some?";
        else if (speakerName.IndexOf("Riza", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I am restocking supplies for the restaurant today. Could you catch some fish for our daily special?";
        else if (speakerName.IndexOf("Bebang", System.StringComparison.OrdinalIgnoreCase) >= 0)
            uniqueReason = "I am offering a meal to the local church later today, and some fresh fish would be wonderful. Can you help me?";

        // Node 3 (Explanation)
        DialogueNode n3 = ScriptableObject.CreateInstance<DialogueNode>();
        n3.speakerName = speakerName;
        n3.speakerPortrait = speakerPortrait;
        n3.dialogueText = uniqueReason;
        n3.animationTrigger = "Talk";
        n3.choices = new List<DialogueChoice>();
        
        // Node 5 (Polite Decline - Ends Conversation)
        DialogueNode n5 = ScriptableObject.CreateInstance<DialogueNode>();
        n5.speakerName = speakerName;
        n5.speakerPortrait = speakerPortrait;
        n5.dialogueText = "That's alright. Come back if you change your mind.";
        n5.animationTrigger = "Nod";
        
        // "Wen" choice
        DialogueChoice yesChoice = new DialogueChoice {
            choiceText = "Wen",
            isWrong = false,
        };
        
        // "Saan" choice
        DialogueChoice noChoice = new DialogueChoice {
            choiceText = "Saan",
            isWrong = false,
            nextNode = n5
        };
        n3.choices.Add(yesChoice);
        n3.choices.Add(noChoice);

        // Node 4 (Accept)
        DialogueNode n4 = ScriptableObject.CreateInstance<DialogueNode>();
        n4.speakerName = speakerName;
        n4.speakerPortrait = speakerPortrait;
        n4.dialogueText = "Thank you! Let me know when you're done.";
        n4.animationTrigger = "Happy";
        n4.endEventName = "StartMinigame:FishingGame";

        if (finalNextNode != null)
        {
            n4.endEventName = ""; 
            n4.choices = new List<DialogueChoice>();
            n4.choices.Add(new DialogueChoice {
                choiceText = "Continue",
                choiceEvent = "StartMinigame:FishingGame",
                nextNode = finalNextNode
            });
        }

        // Link them
        yesChoice.nextNode = n4;
        
        n1.choices = new List<DialogueChoice> { new DialogueChoice { choiceText = "Continue", nextNode = n3 } }; // Skip Player Node (n2) to save clicks, it's just a nod

        // Save assets
        AssetDatabase.CreateAsset(n1, $"{outputFolder}/{baseName}_Intro1.asset");
        AssetDatabase.CreateAsset(n3, $"{outputFolder}/{baseName}_Intro3.asset");
        AssetDatabase.CreateAsset(n4, $"{outputFolder}/{baseName}_Intro4.asset");
        AssetDatabase.CreateAsset(n5, $"{outputFolder}/{baseName}_Intro5.asset");

        return n1;
    }
}
