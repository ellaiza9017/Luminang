using UnityEngine;
using UnityEditor;
using System.IO;

public class RayoDialogueGenerator
{
    [MenuItem("Tools/Generate Rayo Dialogues")]
    public static void GenerateDialogues()
    {
        string path = "Assets/Dialogues/Magellan/Rayo";
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogError("Folder not found: " + path);
            return;
        }

        // AM -> AKO SI
        var am = CreateWordNodes(path, "am", "ako_si",
            "Awesome!\n\nLet's start with something I say whenever I meet someone new.\n\nIf someone asks who I am, I can say:\n\n'Ako si Rayo.'\n\nIt means 'I am Rayo.'",
            "Listen carefully: ako si.\n\nNow, introduce yourself like a brave adventurer!",
            "Ako si! Nice!\n\nNow everyone knows who's talking!",
            "ako si"
        );

        // IS -> MAO
        var isWord = CreateWordNodes(path, "is", "mao",
            "Let's identify something.\n\nIf someone points at my kite and asks,\n\n'Which one is yours?'\n\nI can say:\n\n'Mao kana!'",
            "Listen carefully: mao.\n\nPoint to the answer with your voice!",
            "Mao! Exactly!\n\nYou found the right one!",
            "mao"
        );

        // ARE -> MAOY
        var are = CreateWordNodes(path, "are", "maoy",
            "What if we're talking about more than one person?\n\nMaybe someone asks,\n\n'Who are the explorers?'\n\nWe can use maoy.",
            "Listen carefully: maoy.\n\nCall the whole team together!",
            "Maoy! Great job!\n\nSounds like the whole crew is ready!",
            "maoy"
        );

        // WAS -> NAHIMO
        var was = CreateWordNodes(path, "was", "nahimo",
            "Things can change over time.\n\nA tiny seed can grow into a huge tree.\n\nWhen something becomes something else, we can use nahimo.",
            "Listen carefully: nahimo.\n\nImagine a little seed growing taller and taller!",
            "Nahimo!\n\nEvery big tree starts small!",
            "nahimo"
        );

        // WERE -> MAO
        var were = CreateWordNodes(path, "were", "mao_were",
            "Stories can tell us what things were like before.\n\nMaybe this busy street was once quiet and empty.\n\nFor this lesson, we'll practice mao again.",
            "Listen carefully: mao.\n\nLet's borrow a voice from the past!",
            "Mao!\n\nYou're becoming quite the storyteller!",
            "mao"
        );

        // BECOME -> MAHIMONG
        var become = CreateWordNodes(path, "become", "mahimong",
            "This one's exciting!\n\nA learner can become a teacher.\n\nA beginner can become an expert.\n\nIn Cebuano, that's mahimong.",
            "Listen carefully: mahimong.\n\nWho knows what you'll become someday?",
            "Mahimong!\n\nYou're already becoming a language explorer!",
            "mahimong"
        );

        // SEEM -> MURAG
        var seem = CreateWordNodes(path, "seem", "murag",
            "Have you ever looked at a cloud and thought,\n\n'That looks like a dragon!'\n\nThat's when something seems like something else.\n\nIn Cebuano, that's murag.",
            "Listen carefully: murag.\n\nLet your imagination run wild!",
            "Murag!\n\nThat dragon cloud looks ready to fly!",
            "murag"
        );

        // REMAIN -> MAGPABILIN
        var remain = CreateWordNodes(path, "remain", "magpabilin_remain",
            "Some things change.\n\nBut some things remain.\n\nGood memories can stay with us for a long time.\n\nIn Cebuano, that's magpabilin.",
            "Listen carefully: magpabilin.\n\nThink of a memory you'd like to keep forever!",
            "Magpabilin!\n\nThe best memories never fade!",
            "magpabilin"
        );

        // STAY -> MAGPABILIN
        var stay = CreateWordNodes(path, "stay", "magpabilin_stay",
            "This word can also mean 'stay.'\n\nLike when a friend says,\n\n'Stay a little longer!'",
            "Listen carefully: magpabilin.\n\nLet's see if this word can stay in your memory too!",
            "Magpabilin!\n\nLooks like it's here to stay!",
            "magpabilin"
        );

        // FEEL -> BATI
        var feel = CreateWordNodes(path, "feel", "bati",
            "Last one!\n\nHow do you feel today?\n\nHappy?\nExcited?\nProud?\n\nIn Cebuano, bati can describe how someone feels.",
            "Listen carefully: bati.\n\nTell that feeling to the world!",
            "Bati!\n\nI feel like you're doing an awesome job!",
            "bati"
        );

        // Completion
        DialogueNode completion = ScriptableObject.CreateInstance<DialogueNode>();
        completion.speakerName = "Rayo";
        completion.dialogueText = "Well done!\n\nFrom what things are to what they can do—\nYou've mastered some linking verbs in Cebuano too!\n\nIf these clever words get mixed up for you,\nCome find me again, and we'll sort them through!";
        
        DialogueChoice compChoice = new DialogueChoice { nextNode = null, choiceEvent = "ShowPopup:Linking Verbs" };
        completion.choices.Add(compChoice);
        AssetDatabase.CreateAsset(completion, $"{path}/Rayo_Completion.asset");

        // Link them in sequence
        am.success.choices[0].nextNode = isWord.teach1;
        isWord.success.choices[0].nextNode = are.teach1;
        are.success.choices[0].nextNode = was.teach1;
        was.success.choices[0].nextNode = were.teach1;
        were.success.choices[0].nextNode = become.teach1;
        become.success.choices[0].nextNode = seem.teach1;
        seem.success.choices[0].nextNode = remain.teach1;
        remain.success.choices[0].nextNode = stay.teach1;
        stay.success.choices[0].nextNode = feel.teach1;
        feel.success.choices[0].nextNode = completion;

        // Save linked nodes
        EditorUtility.SetDirty(am.success);
        EditorUtility.SetDirty(isWord.success);
        EditorUtility.SetDirty(are.success);
        EditorUtility.SetDirty(was.success);
        EditorUtility.SetDirty(were.success);
        EditorUtility.SetDirty(become.success);
        EditorUtility.SetDirty(seem.success);
        EditorUtility.SetDirty(remain.success);
        EditorUtility.SetDirty(stay.success);
        EditorUtility.SetDirty(feel.success);

        // Link from Start 1.asset
        DialogueNode start1 = AssetDatabase.LoadAssetAtPath<DialogueNode>($"{path}/Start 1.asset");
        if (start1 != null && start1.choices.Count > 0)
        {
            start1.choices[0].nextNode = am.teach1;
            EditorUtility.SetDirty(start1);
        }
        else
        {
            Debug.LogWarning("Could not find Start 1.asset to link to the beginning.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Successfully generated all Rayo dialogue assets!");
    }

    private static (DialogueNode teach1, DialogueNode teach2, DialogueNode success) CreateWordNodes(string folder, string english, string fileSuffix, string t1, string t2, string successText, string expectedSTT)
    {
        DialogueNode n1 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n2 = ScriptableObject.CreateInstance<DialogueNode>();
        DialogueNode n3 = ScriptableObject.CreateInstance<DialogueNode>();

        n1.speakerName = "Rayo";
        n1.dialogueText = t1;
        n1.triggerEventName = "ShowTeachingPanel:" + english;
        
        n2.speakerName = "Rayo";
        n2.dialogueText = t2;

        n3.speakerName = "Rayo";
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
        AssetDatabase.CreateAsset(n1, $"{folder}/Rayo_{fileSuffix}_Teach1.asset");
        AssetDatabase.CreateAsset(n2, $"{folder}/Rayo_{fileSuffix}_Teach2.asset");
        AssetDatabase.CreateAsset(n3, $"{folder}/Rayo_{fileSuffix}_Success.asset");

        return (n1, n2, n3);
    }
}
