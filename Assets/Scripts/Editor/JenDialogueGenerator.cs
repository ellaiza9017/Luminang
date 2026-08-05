using UnityEngine;
using UnityEditor;

public class JenDialogueGenerator
{
    [MenuItem("Tools/Generate Jen Dialogues")]
    public static void GenerateDialogues()
    {
        string path = "Assets/Dialogues/Magellan/Jen";
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogError("Folder not found: " + path);
            return;
        }

        // INTRO
        DialogueNode intro = ScriptableObject.CreateInstance<DialogueNode>();
        intro.speakerName = "Jen";
        intro.dialogueText = "Oh, hi! Perfect timing.\n\nI like learning new things about people and places, but I always seem to have another question waiting in my head.\n\nSometimes I wonder what something is. Sometimes I wonder who someone is. And sometimes I end up asking a whole lot more!\n\nWant to learn some of my favorite question words in Cebuano?";
        AssetDatabase.CreateAsset(intro, $"{path}/Jen_Intro.asset");

        DialogueNode introNo = ScriptableObject.CreateInstance<DialogueNode>();
        introNo.speakerName = "Jen";
        introNo.dialogueText = "That's okay.\n\nQuestions aren't going anywhere. Come find me when you're feeling curious!";
        AssetDatabase.CreateAsset(introNo, $"{path}/Jen_Intro_No.asset");

        // WHAT -> UNSA
        var whatWord = CreateWordNodes(path, "what", "what",
            "Let's start with one of the most useful questions.\n\nImagine you spot something strange on the ground and want to know what it is.\n\nIn Cebuano, the word for 'what' is unsa.\n\nYou can use it whenever you're asking about a thing, object, or piece of information.",
            "Listen carefully: unsa.\n\nCurious what it means? Say unsa with me!",
            "Unsa! Great job!\n\nThat's the first step to discovering something new.",
            "unsa"
        );

        // WHO -> KINSA
        var whoWord = CreateWordNodes(path, "who", "who",
            "Now imagine you see someone you've never met before.\n\nYou might ask, 'Who is that?'\n\nIn Cebuano, the word for 'who' is kinsa.",
            "Listen carefully: kinsa.\n\nLet's see if you can ask about a person too—say kinsa!",
            "Kinsa! Nicely done!\n\nNow you're ready to ask about people you meet.",
            "kinsa"
        );

        // WHERE -> ASA
        var whereWord = CreateWordNodes(path, "where", "where",
            "Have you ever misplaced something important?\n\nMaybe a bag, a snack, or your favorite souvenir?\n\nTo ask where something is, Cebuano uses asa.",
            "Listen carefully: asa.\n\nPretend you're searching for treasure and say asa!",
            "Asa! Perfect!\n\nNo treasure hunt is complete without that question.",
            "asa"
        );

        // WHEN -> KANUS-A
        var whenWord = CreateWordNodes(path, "when", "when",
            "Some questions aren't about people or places.\n\nSometimes we want to know about time.\n\nWhen asking 'when,' Cebuano uses kanus-a.",
            "Listen carefully: kanus-a.\n\nGive it a try and ask about time—say kanus-a!",
            "Kanus-a! Excellent!\n\nNow you can ask when something happens.",
            "kanus-a"
        );

        // WHY -> NGANO
        var whyWord = CreateWordNodes(path, "why", "why",
            "This might be my favorite question word.\n\nWhen something surprises you, you probably want to know why.\n\nIn Cebuano, 'why' is ngano.",
            "Listen carefully: ngano.\n\nLet's hear your curious side—say ngano!",
            "Ngano! Great!\n\nQuestions like that help us understand the world better.",
            "ngano"
        );

        // HOW -> GIUNSA
        var howWord = CreateWordNodes(path, "how", "how",
            "Have you ever watched someone do something amazing and wondered how they did it?\n\nIn Cebuano, the word for 'how' is giunsa.",
            "Listen carefully: giunsa.\n\nTime to solve the mystery—say giunsa!",
            "Giunsa! Nice work!\n\nYou're asking some really good questions now.",
            "giunsa"
        );

        // HOW MANY -> PILA
        var howManyWord = CreateWordNodes(path, "how many", "how_many",
            "Here's another useful one.\n\nMaybe you're counting snacks, flowers, or even new friends.\n\nTo ask 'how many' or 'how much,' Cebuano uses pila.",
            "Listen carefully: pila.\n\nCount along and say pila!",
            "Pila! Wonderful!\n\nYou can now ask about numbers, amounts, and even prices.",
            "pila"
        );

        // COMPLETION
        DialogueNode completion = ScriptableObject.CreateInstance<DialogueNode>();
        completion.speakerName = "Jen";
        completion.dialogueText = "Look at that—we started with questions, and now you've learned answers to so many of them.\n\nFrom unsa to pila, you've learned the Cebuano words that help curious minds explore the world.\n\nKeep asking questions, keep discovering new things, and you'll always find something interesting waiting around the corner.\n\nKalaw is waiting for you back at the plaza.\n\nIt sounds like your biggest challenge yet is about to begin.";
        DialogueChoice compChoice = new DialogueChoice { nextNode = null, choiceEvent = "ShowPopup:Interrogatives" };
        completion.choices.Add(compChoice);
        AssetDatabase.CreateAsset(completion, $"{path}/Jen_Completion.asset");

        // LINKING
        DialogueChoice yesChoice = new DialogueChoice { choiceText = "Yes", nextNode = whatWord.teach1 };
        DialogueChoice noChoice = new DialogueChoice { choiceText = "No", nextNode = introNo };
        intro.choices.Add(yesChoice);
        intro.choices.Add(noChoice);

        whatWord.success.choices[0].nextNode = whoWord.teach1;
        whoWord.success.choices[0].nextNode = whereWord.teach1;
        whereWord.success.choices[0].nextNode = whenWord.teach1;
        whenWord.success.choices[0].nextNode = whyWord.teach1;
        whyWord.success.choices[0].nextNode = howWord.teach1;
        howWord.success.choices[0].nextNode = howManyWord.teach1;
        howManyWord.success.choices[0].nextNode = completion;

        // SAVE ALL
        EditorUtility.SetDirty(intro);
        EditorUtility.SetDirty(whatWord.success);
        EditorUtility.SetDirty(whoWord.success);
        EditorUtility.SetDirty(whereWord.success);
        EditorUtility.SetDirty(whenWord.success);
        EditorUtility.SetDirty(whyWord.success);
        EditorUtility.SetDirty(howWord.success);
        EditorUtility.SetDirty(howManyWord.success);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Successfully generated all Jen dialogue assets!");
    }

    private static (DialogueNode teach1, DialogueNode teach2, DialogueNode success) CreateWordNodes(string folder, string english, string fileSuffix, string t1, string t2, string successText, string expectedSTT)
    {
        DialogueNode n1 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n2 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n3 = ScriptableObject.CreateInstance<DialogueNode>();

        n1.speakerName = "Jen";
        n1.dialogueText = t1;
        n1.triggerEventName = "ShowTeachingPanel:" + english;
        
        n2.speakerName = "Jen";
        n2.dialogueText = t2;

        n3.speakerName = "Jen";
        n3.dialogueText = successText;
        n3.endEventName = "HideTeachingPanel";

        // Node 1 -> Node 2
        DialogueChoice c1 = new DialogueChoice { nextNode = n2 };
        n1.choices.Add(c1);

        // Node 2 STT
        DialogueChoice c2 = new DialogueChoice { nextNode = null, expectedSTTWord = expectedSTT };
        n2.choices.Add(c2);

        // Node 3 (Success) -> placeholder, will be linked later to the next word
        DialogueChoice c3 = new DialogueChoice { nextNode = null };
        n3.choices.Add(c3);

        // Save assets
        AssetDatabase.CreateAsset(n1, $"{folder}/Jen_{fileSuffix}_Teach1.asset");
        AssetDatabase.CreateAsset(n2, $"{folder}/Jen_{fileSuffix}_Teach2.asset");
        AssetDatabase.CreateAsset(n3, $"{folder}/Jen_{fileSuffix}_Success.asset");

        return (n1, n2, n3);
    }
}
