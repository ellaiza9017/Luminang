using UnityEngine;
using UnityEditor;
using System.IO;

public class AlingRizaDialogueGenerator
{
    [MenuItem("Tools/Generate Aling Riza Dialogues")]
    public static void GenerateDialogues()
    {
        string path = "Assets/Dialogues/Magellan/AlignRiza";
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogError("Folder not found: " + path);
            return;
        }

        // Inom (Drink)
        CreateWordNodes(path, "inom", "drink",
            "Mmm, fresh bread is delicious, but it can make you super thirsty!",
            "In Cebuano, 'inom' means 'drink.' Whether it's water, juice, or a cold soda, you'd use 'inom'!",
            "Listen carefully: inom. Say it loud and clear for me!",
            "Inom! Spot on! Remember to stay hydrated on your adventures!"
        );

        // Adto (Go)
        CreateWordNodes(path, "adto", "go",
            "Adventurers like you are always on the move, going from place to place!",
            "In Cebuano, 'adto' means 'go.' You use it when you're heading somewhere exciting!",
            "Listen carefully: adto. Ready to say it? Go for it!",
            "Adto! Perfect! Off you go to your next big quest!"
        );

        // Anhi (Come)
        CreateWordNodes(path, "anhi", "come",
            "Whenever someone visits the bakery, I always give them a warm welcome to come inside!",
            "In Cebuano, 'anhi' means 'come' or 'come here.' Use it when you want someone to come over to you!",
            "Listen carefully: anhi. Let's hear you say it!",
            "Anhi! Awesome! You can come by the bakery anytime!"
        );

        // Tulog (Sleep)
        CreateWordNodes(path, "tulog", "sleep",
            "Bakers wake up before the sun rises! Getting a good night's sleep is our secret superpower.",
            "In Cebuano, 'tulog' means 'sleep.' It's the best way to recharge your energy!",
            "Listen carefully: tulog. Don't fall asleep on me yet—say it out loud!",
            "Tulog! Great job! Even heroes need their beauty sleep!"
        );

        // Makita (See)
        CreateWordNodes(path, "makita", "see",
            "Look around at all these colorful treats! We use our eyes to see all the wonderful things in the world.",
            "In Cebuano, 'makita' means 'see' or 'to see something.' Like seeing a giant chocolate cake!",
            "Listen carefully: makita. Give it a try!",
            "Makita! Amazing! I can clearly see you're going to be a Cebuano master!"
        );

        // Madungog (Hear)
        CreateWordNodes(path, "madungog", "hear",
            "Shhh... can you hear the sounds of the bakery? The sizzling ovens and happy customers?",
            "In Cebuano, 'madungog' means 'hear.' You use it when your ears pick up a sound!",
            "Listen carefully: madungog. Let me hear you say it!",
            "Madungog! Loud and clear! You've got great ears!"
        );

        // Sulti (Speak)
        CreateWordNodes(path, "sulti", "speak",
            "You're actually doing this action right now! Whenever we use our voices to share ideas, we speak.",
            "In Cebuano, 'sulti' means 'speak' or 'talk.' It's how we connect with friends!",
            "Listen carefully: sulti. Now speak up and say it!",
            "Sulti! Brilliant! You're already speaking like a true local!"
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Successfully generated all Aling Riza dialogue assets for the remaining 7 words!");
    }

    private static void CreateWordNodes(string folder, string cebuano, string english, string t1, string t2, string t3, string success)
    {
        DialogueNode n1 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n2 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n3 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n4 = ScriptableObject.CreateInstance<DialogueNode>();

        n1.speakerName = "Aling Riza";
        n1.dialogueText = t1;
        n1.triggerEventName = "ShowTeachingPanel:" + english;
        
        n2.speakerName = "Aling Riza";
        n2.dialogueText = t2;

        n3.speakerName = "Aling Riza";
        n3.dialogueText = t3;

        n4.speakerName = "Aling Riza";
        n4.dialogueText = success;

        // Node 1 -> Node 2
        DialogueChoice c1 = new DialogueChoice { nextNode = n2 };
        n1.choices.Add(c1);

        // Node 2 -> Node 3
        DialogueChoice c2 = new DialogueChoice { nextNode = n3 };
        n2.choices.Add(c2);

        // Node 3 STT (success jumps to null, which indicates STT success handles it)
        DialogueChoice c3 = new DialogueChoice { nextNode = null, expectedSTTWord = cebuano };
        n3.choices.Add(c3);

        // Node 4 (Success node) ends conversation
        DialogueChoice c4 = new DialogueChoice { nextNode = null };
        n4.choices.Add(c4);

        // Save assets
        AssetDatabase.CreateAsset(n1, $"{folder}/AlingRiza_{cebuano}.asset");
        AssetDatabase.CreateAsset(n2, $"{folder}/AlingRiza_{cebuano} 1.asset");
        AssetDatabase.CreateAsset(n3, $"{folder}/AlingRiza_{cebuano} 2.asset");
        AssetDatabase.CreateAsset(n4, $"{folder}/AlingRiza_{cebuano}_Success.asset");
    }
}
