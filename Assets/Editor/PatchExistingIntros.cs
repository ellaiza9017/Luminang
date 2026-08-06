using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class PatchExistingIntros
{
    [MenuItem("Tools/Luminang/Patch Existing Fishing Intros")]
    public static void Patch()
    {
        string[] searchFolders = new string[] { "Assets/Dialogues/FishingQuests/InjectedIntros" };
        string[] allGuids = AssetDatabase.FindAssets("*_Intro3 t:DialogueNode", searchFolders);

        int patchedCount = 0;

        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DialogueNode n3 = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
            if (n3 == null) continue;

            string speakerName = n3.speakerName;
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

            n3.dialogueText = uniqueReason;

            // Generate/Find n5
            string n5Path = path.Replace("_Intro3", "_Intro5");
            DialogueNode n5 = AssetDatabase.LoadAssetAtPath<DialogueNode>(n5Path);
            if (n5 == null)
            {
                n5 = ScriptableObject.CreateInstance<DialogueNode>();
                n5.speakerName = speakerName;
                n5.speakerPortrait = n3.speakerPortrait;
                n5.dialogueText = "That's alright. Come back if you change your mind.";
                n5.animationTrigger = "Nod";
                AssetDatabase.CreateAsset(n5, n5Path);
            }
            else
            {
                n5.dialogueText = "That's alright. Come back if you change your mind.";
                EditorUtility.SetDirty(n5);
            }

            // Ensure choices are wired correctly
            if (n3.choices == null || n3.choices.Count < 2)
            {
                n3.choices = new List<DialogueChoice>();
                n3.choices.Add(new DialogueChoice { choiceText = "Wen", isWrong = false });
                n3.choices.Add(new DialogueChoice { choiceText = "Saan", isWrong = false, nextNode = n5 });
            }
            else
            {
                n3.choices[0].choiceText = "Wen";
                n3.choices[0].isWrong = false;
                
                n3.choices[1].choiceText = "Saan";
                n3.choices[1].isWrong = false;
                n3.choices[1].nextNode = n5;
            }

            EditorUtility.SetDirty(n3);

            // Merge n3 into n1 for an immediate prompt (bypassing the "Continue" step)
            string n1Path = path.Replace("_Intro3", "_Intro1");
            DialogueNode n1 = AssetDatabase.LoadAssetAtPath<DialogueNode>(n1Path);
            if (n1 != null)
            {
                n1.dialogueText = uniqueReason;
                n1.choices = new List<DialogueChoice>();
                n1.choices.Add(new DialogueChoice { choiceText = "Wen", isWrong = false, nextNode = n3.choices[0].nextNode });
                n1.choices.Add(new DialogueChoice { choiceText = "Saan", isWrong = false, nextNode = n5 });
                EditorUtility.SetDirty(n1);
            }

            patchedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PatchExistingIntros] Patched {patchedCount} existing fishing intros!");
    }
}
