using UnityEngine;
using UnityEditor;
using System.IO;

public class LinaDialogueGenerator
{
    [MenuItem("Tools/Generate Lina Dialogues")]
    public static void GenerateDialogues()
    {
        string path = "Assets/Dialogues/Magellan/Lina";
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogError("Folder not found: " + path);
            return;
        }

        // INTRO
        DialogueNode intro = ScriptableObject.CreateInstance<DialogueNode>();
        intro.speakerName = "Lina";
        intro.dialogueText = "Hello there! I'm Lina.\n\nI spend my days helping visitors explore the city, and I've noticed something interesting...\n\nEvery conversation is full of people!\n\nI talk about myself, talk to visitors, introduce friends, and point out groups all day long.\n\nLuckily, Cebuano has special words that make all of that easy.\n\nWant to learn them with me?";
        AssetDatabase.CreateAsset(intro, $"{path}/Lina_Intro.asset");

        DialogueNode introNo = ScriptableObject.CreateInstance<DialogueNode>();
        introNo.speakerName = "Lina";
        introNo.dialogueText = "No worries! These words aren't going anywhere.\n\nCome find me when you're ready to meet the whole cast of the conversation!";
        AssetDatabase.CreateAsset(introNo, $"{path}/Lina_Intro_No.asset");

        // I -> AKO
        var iWord = CreateWordNodes(path, "i", "ako_i",
            "Let's start with the most important person: you!\n\nWhen talking about yourself, Cebuano uses ako.\n\nFor example: 'Ako si Lina.'\n\nThat means: 'I am Lina.'\n\nHere, ako means 'I.'",
            "Listen carefully: ako.\n\nPoint both thumbs at yourself and say ako!",
            "Ako! That's you talking about yourself!",
            "ako"
        );

        // YOU -> IKAW
        var youWord = CreateWordNodes(path, "you", "ikaw",
            "Now let's talk about the person you're speaking to.\n\nIn Cebuano, that's ikaw.\n\nFor example: 'Ikaw ang akong higala.'\n\nThat means: 'You are my friend.'\n\nHere, ikaw means 'you.'",
            "Listen carefully: ikaw.\n\nPretend you're introducing your best friend and say ikaw!",
            "Ikaw! That's the person right in front of you!",
            "ikaw"
        );

        // HE -> SIYA
        var heWord = CreateWordNodes(path, "he", "siya_he",
            "What if we're talking about someone else?\n\nIn Cebuano, we can use siya.\n\nFor example: 'Siya ang magdudula.'\n\nThat means: 'He is the player.'",
            "Listen carefully: siya.\n\nPoint to an imaginary hero and say siya!",
            "Siya! Now you're talking about someone else!",
            "siya"
        );

        // SHE -> SIYA
        var sheWord = CreateWordNodes(path, "she", "siya_she",
            "Here's something neat!\n\nThe same word, siya, can also mean 'she.'\n\nFor example: 'Siya ang magtutudlo.'\n\nThat means: 'She is the teacher.'\n\nThe meaning depends on who you're talking about.",
            "Listen carefully: siya.\n\nImagine a brave adventurer and say siya!",
            "Siya! One word can do two jobs!",
            "siya"
        );

        // WE -> KAMI
        var weWord = CreateWordNodes(path, "we", "kami_we",
            "Now let's talk about teams!\n\nCebuano has two common words for 'we.'\n\nKami means 'we,' but the person you're talking to is not included.\n\nKita means 'we,' including the person you're talking to.\n\nFor this lesson, let's practice kami.\n\nFor example: 'Kami ang grupo.'\n\nThat means: 'We are the group.'",
            "Listen carefully: kami.\n\nGather your imaginary team and say kami!",
            "Kami! Teamwork makes every adventure better!",
            "kami"
        );

        // THEY -> SILA
        var theyWord = CreateWordNodes(path, "they", "sila_they",
            "Sometimes we talk about a group we're not part of.\n\nIn Cebuano, that's sila.\n\nFor example: 'Sila ang mga bisita.'\n\nThat means: 'They are the visitors.'",
            "Listen carefully: sila.\n\nWave to an imaginary crowd and say sila!",
            "Sila! That's a whole group of people!",
            "sila"
        );

        // ME -> AKO
        var meWord = CreateWordNodes(path, "me", "ako_me",
            "You've already met this word!\n\nAko can also mean 'me,' depending on the sentence.\n\nFor example: 'Tabangi ako.'\n\nThat means: 'Help me.'",
            "Listen carefully: ako.\n\nTap your chest and say ako!",
            "Ako! You're becoming a pronoun pro!",
            "ako"
        );

        // US -> KAMI
        var usWord = CreateWordNodes(path, "us", "kami_us",
            "When talking about yourself together with others, we can use kami for 'us.'\n\nFor example: 'Tabangi kami.'\n\nThat means: 'Help us.'",
            "Listen carefully: kami.\n\nCall your whole team together and say kami!",
            "Kami! Adventures are always better with friends!",
            "kami"
        );

        // THEM -> SILA
        var themWord = CreateWordNodes(path, "them", "sila_them",
            "And if we're talking about another group, we can use sila again.\n\nFor example: 'Tabangi sila.'\n\nThat means: 'Help them.'",
            "Listen carefully: sila.\n\nPoint to the other team and say sila!",
            "Sila! You've learned how to talk about all kinds of people!",
            "sila"
        );

        // COMPLETION
        DialogueNode completion = ScriptableObject.CreateInstance<DialogueNode>();
        completion.speakerName = "Lina";
        completion.dialogueText = "Amazing!\n\nToday you learned the words that help us talk about ourselves, our friends, and everyone around us.\n\nFrom ako to sila, you've met the whole cast of the conversation!\n\nIf you ever need help remembering who's who, come chat with me again!";
        DialogueChoice compChoice = new DialogueChoice { nextNode = null, choiceEvent = "ShowPopup:Pronouns" };
        completion.choices.Add(compChoice);
        AssetDatabase.CreateAsset(completion, $"{path}/Lina_Completion.asset");

        // LINKING
        DialogueChoice yesChoice = new DialogueChoice { choiceText = "Yes", nextNode = iWord.teach1 };
        DialogueChoice noChoice = new DialogueChoice { choiceText = "No", nextNode = introNo };
        intro.choices.Add(yesChoice);
        intro.choices.Add(noChoice);

        iWord.success.choices[0].nextNode = youWord.teach1;
        youWord.success.choices[0].nextNode = heWord.teach1;
        heWord.success.choices[0].nextNode = sheWord.teach1;
        sheWord.success.choices[0].nextNode = weWord.teach1;
        weWord.success.choices[0].nextNode = theyWord.teach1;
        theyWord.success.choices[0].nextNode = meWord.teach1;
        meWord.success.choices[0].nextNode = usWord.teach1;
        usWord.success.choices[0].nextNode = themWord.teach1;
        themWord.success.choices[0].nextNode = completion;

        // SAVE ALL
        EditorUtility.SetDirty(intro);
        EditorUtility.SetDirty(iWord.success);
        EditorUtility.SetDirty(youWord.success);
        EditorUtility.SetDirty(heWord.success);
        EditorUtility.SetDirty(sheWord.success);
        EditorUtility.SetDirty(weWord.success);
        EditorUtility.SetDirty(theyWord.success);
        EditorUtility.SetDirty(meWord.success);
        EditorUtility.SetDirty(usWord.success);
        EditorUtility.SetDirty(themWord.success);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Successfully generated all Lina dialogue assets!");
    }

    private static (DialogueNode teach1, DialogueNode teach2, DialogueNode success) CreateWordNodes(string folder, string english, string fileSuffix, string t1, string t2, string successText, string expectedSTT)
    {
        DialogueNode n1 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n2 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n3 = ScriptableObject.CreateInstance<DialogueNode>();

        n1.speakerName = "Lina";
        n1.dialogueText = t1;
        n1.triggerEventName = "ShowTeachingPanel:" + english;
        
        n2.speakerName = "Lina";
        n2.dialogueText = t2;

        n3.speakerName = "Lina";
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
        AssetDatabase.CreateAsset(n1, $"{folder}/Lina_{fileSuffix}_Teach1.asset");
        AssetDatabase.CreateAsset(n2, $"{folder}/Lina_{fileSuffix}_Teach2.asset");
        AssetDatabase.CreateAsset(n3, $"{folder}/Lina_{fileSuffix}_Success.asset");

        return (n1, n2, n3);
    }
}
