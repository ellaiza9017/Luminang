using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class FixPreQuestAssets
{
    [MenuItem("Tools/Generate Pre-Quest Dialogues")]
    public static void GenerateDialogues()
    {
        string folderPath = "Assets/Resources/PreQuestDialogues";
        
        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/PreQuestDialogues"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "PreQuestDialogues");
        }

        Dictionary<string, string> dialogues = new Dictionary<string, string>()
        {
            { "Jen", "Hi! hmm.. I think you still have something to do right? go do it first! haha" },
            { "Jerem", "I need your help but... I think you need to finish what you're doing right now." },
            { "Dave", "Oh Hello! Aren't you supposed to be somewhere else?" },
            { "Lorraine", "You're here early! we'll talk later, okay?" },
            { "Lina", "Hey there! Finish your current task first, then come see me!" },
            { "Klara", "Oh! I'd love to chat, but it looks like you're busy with something else right now." },
            { "Rayo", "Yo! We can talk later, make sure you finish your errands first!" },
            { "AlingRiza", "Hello dear! Don't let me distract you from your goals." },
            { "Rodrick", "Hey! Seems like you're on a mission. We'll catch up later!" },
            { "Tomas", "Greetings! Better get back to what you were doing, we can talk another time." },
            { "Lito", "Oh hey! I shouldn't keep you, looks like you've got things to do!" },
            { "Ronnie", "Hi! Let's talk later when you're less busy, alright?" },
            { "Sally", "Hiii! You look like you're in a hurry. Go finish your task first!" },
            { "Wayne", "Sup! Get your stuff done first, then we'll hang out." },
            { "LolaBebang", "Oh, hello apo! You seem busy. Go on ahead, we can chat later." },
            { "Neneng", "Hey! I think you're needed somewhere else right now!" },
            { "Kyros", "Hello! Come back when you've finished your current objective." },
            { "Mishang", "Hey there! Don't let me stop you, go finish your errands!" },
            { "Irah", "Oh, hi! I think you still have a task to complete. See you later!" },
            { "Jom", "Hey! Aren't you supposed to be doing something right now?" },
            { "Aliyah", "Hi! I'm a bit busy at the moment, and it looks like you are too!" },
            { "Ellai", "Hello! Let's talk once you're done with your current task." },
            { "Mar", "Taho! Oh, you look busy. Come back when you're free!" },
            { "MangLance", "Hey! Focus on your task for now, we'll talk later." }
        };

        foreach (var kvp in dialogues)
        {
            string name = kvp.Key;
            string text = kvp.Value;

            string assetPath = $"{folderPath}/PreQuest_{name}.asset";
            
            // Delete existing bad yaml files first
            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            // Create new proper DialogueNode
            DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
            node.speakerName = name;
            node.dialogueText = text;
            node.choices = new List<DialogueChoice>();

            AssetDatabase.CreateAsset(node, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Successfully recreated all Pre-Quest DialogueNodes using Unity's serializer!");
    }
}
