using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool that auto-generates fishing quest intro dialogues for every NPC
/// that has a StartMinigame:FishingGame dialogue event configured.
///
/// Menu: Tools/Luminang/Create Fishing Quest Dialogues
///
/// For each eligible NPC it creates:
///   1. <NPC>_FishingIntro1   – "Before we continue, could you help me with a small task?"
///   2. <NPC>_FishingQuestion – Unique fish reason + "Could you catch a few fish for me?" (isYesNoChoice=true)
///   3. <NPC>_FishingDecline  – "That's alright. If you change your mind, I'll still be here." (Saan branch)
///   4. <NPC>_FishingAccept   – "Thank you! Catch a few fish, then come back." → fires StartMinigame:FishingGame
///   5. <NPC>_FishingReturn   – Post-minigame thank-you that resumes the NPC's stored next node
/// </summary>
public class CreateFishingQuestDialogues : EditorWindow
{
    // ── Unique fishing motivation table ───────────────────────────────────────
    // Keys are lowercase partial NPC GameObject name matches.
    private static readonly Dictionary<string, string> MotivationTable = new Dictionary<string, string>
    {
        { "irah",     "I'm hoping to sell fresh fish at the market today, but I haven't caught enough yet." },
        { "dave",     "I need to prepare a proper dinner for the family tonight, and fresh fish is the only thing they'll eat." },
        { "rodrick",  "The community gathering is tomorrow and we promised to bring food. Fish would be perfect." },
        { "aliyah",   "We're preparing for a village feast this evening and I still need more fish for the main course." },
        { "mishang",  "I have guests arriving later and my kitchen is nearly empty. Fresh fish would really save me." },
        { "lito",     "The elders haven't had a proper meal yet today. I promised them fresh fish." },
        { "klara",    "I'm working on a special recipe that calls for fresh fish, but I've run out." },
        { "lorraine", "My neighbor is sick and I promised to bring them a meal. Fresh fish would be just right." },
        { "wayne",    "I'm leaving on a journey tomorrow and I need to prepare supplies for the road. Fish keeps well." },
        { "alingriza","The village's food supply has been running low. Any fish you can catch will help everyone." },
        { "riza",     "The village's food supply has been running low. Any fish you can catch will help everyone." },
    };

    private const string BasePath = "Assets/Dialogues/FishingQuests";

    [MenuItem("Tools/Luminang/Create Fishing Quest Dialogues")]
    public static void Run()
    {
        // ── Find all NPCs that trigger StartMinigame:FishingGame ──────────────
        var allNPCs = Object.FindObjectsByType<InteractableNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        foreach (var npc in allNPCs)
        {
            if (!HasFishingGameEvent(npc)) continue;

            Debug.Log($"<color=cyan>[FishingQuest] Processing: {npc.gameObject.name}</color>");
            BuildFishingDialogueChain(npc);
            count++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (count == 0)
        {
            Debug.LogWarning("[FishingQuest] No NPCs found with StartMinigame:FishingGame event. " +
                             "Make sure the NPCs are in the active scene and have the event mapped in their Dialogue Events list.");
            EditorUtility.DisplayDialog("No Eligible NPCs Found",
                "No NPCs with a 'StartMinigame:FishingGame' dialogue event mapping were found in the active scene.\n\n" +
                "Make sure the scene is open and the NPCs have 'StartMinigame:FishingGame' in their Dialogue Events list.",
                "OK");
            return;
        }

        Debug.Log($"<color=green>[FishingQuest] Done! Fishing intro dialogues created for {count} NPC(s). Save the scene to preserve wiring.</color>");
        EditorUtility.DisplayDialog("Fishing Quest Dialogues Created",
            $"Successfully created fishing intro dialogue chains for {count} NPC(s).\n\n" +
            "Assets saved to:\n  Assets/Dialogues/FishingQuests/\n\n" +
            "For each NPC:\n" +
            "• Intro → Unique reason → Wen/Saan choice\n" +
            "• Wen → Thank you → Start Fishing Minigame\n" +
            "• Saan → Polite decline (re-offerable)\n\n" +
            "Save the scene to preserve the dialogue wiring.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static bool HasFishingGameEvent(InteractableNPC npc)
    {
        if (npc.dialogueEvents == null) return false;
        foreach (var mapping in npc.dialogueEvents)
        {
            if (mapping.eventName != null &&
                mapping.eventName.Trim().Equals("StartMinigame:FishingGame", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void BuildFishingDialogueChain(InteractableNPC npc)
    {
        string npcName = npc.gameObject.name
            .Replace(" ", "")
            .Replace("_Rigged", "")
            .Replace("_rigged", "")
            .Replace("_Rrrigged", "")
            .Replace("Vendor", "")
            .Replace("barista", "")
            .Trim();

        string folder = $"{BasePath}/{npcName}";
        EnsureFolder(folder);

        string motivation = GetMotivation(npc.gameObject.name);
        string speakerName = npc.gameObject.name.Split('_')[0];

        // ── Node 1: Opening hook ──────────────────────────────────────────────
        DialogueNode intro1 = GetOrCreate(folder, $"{npcName}_FishingIntro1", n =>
        {
            n.speakerName   = speakerName;
            n.dialogueText  = "Before we continue, could you help me with a small task?";
            n.translatedText = "";
            n.choices       = new List<DialogueChoice>(); // No choices → next button → advances to Intro2
        });

        // ── Node 2: The ask (Wen / Saan) ─────────────────────────────────────
        DialogueNode intro2 = GetOrCreate(folder, $"{npcName}_FishingQuestion", n =>
        {
            n.speakerName    = speakerName;
            n.dialogueText   = $"{motivation}\nCould you catch a few fish for me?";
            n.translatedText = "";
            n.isYesNoChoice  = true;
            n.choices        = new List<DialogueChoice>(); // Filled below after creating accept/decline nodes
        });

        // ── Node 3: Decline (Saan) ────────────────────────────────────────────
        DialogueNode decline = GetOrCreate(folder, $"{npcName}_FishingDecline", n =>
        {
            n.speakerName   = speakerName;
            n.dialogueText  = "That's alright. If you change your mind, I'll still be here.";
            n.translatedText = "";
            n.choices       = new List<DialogueChoice>(); // Ends dialogue; player can return
        });

        // ── Node 4: Accept (Wen) → fires fishing minigame ────────────────────
        DialogueNode accept = GetOrCreate(folder, $"{npcName}_FishingAccept", n =>
        {
            n.speakerName   = speakerName;
            n.dialogueText  = "Thank you! Catch a few fish, then come back and let me know when you're done.";
            n.translatedText = "";
            // The StartMinigame:FishingGame event fires via the NPC's existing UnityEvent mapping.
            // We wire a silent choice so dialogue ends → NPC's event handler triggers the game.
            n.endEventName  = "StartMinigame:FishingGame";
            n.choices       = new List<DialogueChoice>();
        });

        // ── Node 5: Post-minigame return ─────────────────────────────────────
        DialogueNode returnNode = GetOrCreate(folder, $"{npcName}_FishingReturn", n =>
        {
            n.speakerName   = speakerName;
            n.dialogueText  = "Wonderful! Thank you so much for your help. Now, let's get back to what we were doing.";
            n.translatedText = "";
            n.choices       = new List<DialogueChoice>();
        });

        // ── Wire intro1 → intro2 ─────────────────────────────────────────────
        if (intro1.choices == null || intro1.choices.Count == 0)
        {
            intro1.choices = new List<DialogueChoice>
            {
                new DialogueChoice { choiceText = "Continue", nextNode = intro2, isWrong = false }
            };
            EditorUtility.SetDirty(intro1);
        }

        // ── Wire intro2: Wen → accept, Saan → decline ────────────────────────
        if (intro2.choices == null || intro2.choices.Count < 2)
        {
            intro2.choices = new List<DialogueChoice>
            {
                new DialogueChoice { choiceText = "Sure, I'd be happy to help.", nextNode = accept, isWrong = false },
                new DialogueChoice { choiceText = "Sorry, maybe later.",         nextNode = decline, isWrong = false }
            };
            EditorUtility.SetDirty(intro2);
        }

        // ── Wire the intro chain into the NPC ────────────────────────────────
        // Replace the quest dialogue that previously directly triggered the minigame.
        // We prepend our intro chain to the NPC's existing default or first quest dialogue.
        // Strategy: Set the NPC's defaultDialogue to intro1 only if it currently points
        // to something that would immediately fire the fishing game, OR if there's no
        // defaultDialogue and the NPC relies solely on quest dialogue matching.
        // This is conservative — we only wire if not already wired.
        bool alreadyWired = (npc.defaultDialogue == intro1) ||
                            (npc.questDialogues != null && npc.questDialogues.Exists(qd => qd.dialogueNode == intro1));

        if (!alreadyWired)
        {
            // Preserve the NPC's current default dialogue as a post-fishing backup:
            // after the fishing game, the return node continues to it.
            if (npc.defaultDialogue != null && npc.defaultDialogue != intro1)
            {
                returnNode.choices = new List<DialogueChoice>
                {
                    new DialogueChoice { choiceText = "Let's continue.", nextNode = npc.defaultDialogue, isWrong = false }
                };
                EditorUtility.SetDirty(returnNode);
            }

            // Set intro1 as the new defaultDialogue entry point
            npc.defaultDialogue = intro1;
            EditorUtility.SetDirty(npc);
            Debug.Log($"<color=cyan>[FishingQuest] Wired {npcName} defaultDialogue → FishingIntro1</color>");
        }
        else
        {
            Debug.Log($"[FishingQuest] {npcName} already has fishing intro wired, skipping.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string GetMotivation(string npcGameObjectName)
    {
        string lower = npcGameObjectName.ToLower();
        foreach (var kv in MotivationTable)
        {
            if (lower.Contains(kv.Key))
                return kv.Value;
        }
        // Generic fallback
        return "I have a small task that could really use your help.";
    }

    private static DialogueNode GetOrCreate(string folder, string assetName, System.Action<DialogueNode> initializer)
    {
        string path = $"{folder}/{assetName}.asset";
        DialogueNode existing = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
        if (existing != null) return existing;

        DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
        initializer(node);
        AssetDatabase.CreateAsset(node, path);
        return node;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
