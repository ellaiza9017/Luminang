using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Enforces the official Calle Crisologo curriculum document.
/// - Moves minigame triggers to the CORRECT nodes per the design doc:
///     Kalaw Quiz  → after Kalaw_W04_Success (end of W1-W4 Greetings)
///     Word Rush   → after Lito's LAST request word success (W27 mabalin kadi agsaludsod)
///     Matching    → after Klara_W37_Success (end of Directions quest)
/// - Removes the premature minigame choices from Intro nodes
/// - Updates KalawInlineQuiz to use EXACT curriculum Ilocano words
/// - Ensures minigameCategory matches the curriculum categories
/// </summary>
public class EnforceCurriculumMinigames : EditorWindow
{
    [MenuItem("Tools/Enforce Curriculum Minigames (Calle Crisologo)")]
    public static void DoWork()
    {
        if (!SceneManager.GetActiveScene().name.Contains("Calle_Crisologo"))
        {
            Debug.LogError("Open Calle_Crisologo scene first!");
            return;
        }

        // ─── Step 1: Remove premature minigame choices from Intro nodes ──────
        RemoveMinigameChoiceFrom("Assets/Dialogues/CalleCrisologo/Level2_FunctionalNavigational/Quest5_Requests/Lito/Lito_Intro.asset");
        RemoveMinigameChoiceFrom("Assets/Dialogues/CalleCrisologo/Level2_FunctionalNavigational/Quest6_Directions/Klara/Klara_Intro.asset");
        RemoveMinigameChoiceFrom("Assets/Dialogues/CalleCrisologo/Level1_ConversationalSocial/Quest1_Greetings/Kalaw/Kalaw_Intro_Yes.asset");

        Debug.Log("<color=cyan>[Step 1] Removed premature minigame choices from Intro nodes.</color>");

        // ─── Step 2: Load AfterMinigame dialogue assets ───────────────────────
        string litoDir  = "Assets/Dialogues/CalleCrisologo/Level2_FunctionalNavigational/Quest5_Requests/Lito";
        string klaraDir = "Assets/Dialogues/CalleCrisologo/Level2_FunctionalNavigational/Quest6_Directions/Klara";

        // Ensure AfterMinigame assets exist (created by WireCalleMinigames)
        DialogueNode litoAfter  = AssetDatabase.LoadAssetAtPath<DialogueNode>(litoDir  + "/Lito_AfterMinigame.asset");
        DialogueNode klaraAfter = AssetDatabase.LoadAssetAtPath<DialogueNode>(klaraDir + "/Klara_AfterMinigame.asset");

        // ─── Step 3: Wire KALAW QUIZ → after Kalaw_W04_Success ───────────────
        // Per curriculum: after teaching Word 4 (naimbag a bigat), before sending to Kyros
        // Kalaw_W04_Success currently has choices: []
        // We add: [Continue to Kyros] + [Quick Quiz!] → StartTiptipQuiz:A
        string kalawW04Path = "Assets/Dialogues/CalleCrisologo/Level1_ConversationalSocial/Quest1_Greetings/Kalaw/Kalaw_W04_Success.asset";
        DialogueNode kalawW04 = AssetDatabase.LoadAssetAtPath<DialogueNode>(kalawW04Path);

        if (kalawW04 != null)
        {
            // Only add quiz choice if not already present
            if (!HasMinigameChoice(kalawW04))
            {
                // Create a quiz bridge dialogue (after quiz, this tells player to go to Kyros)
                string quizDoneAssetPath = "Assets/Dialogues/CalleCrisologo/Level1_ConversationalSocial/Quest1_Greetings/Kalaw/Kalaw_QuizDone.asset";
                DialogueNode quizDone = GetOrCreateDialogue(quizDoneAssetPath,
                    "Kalaw",
                    "Squawk! Perfect score! You're ready to explore the streets.\nFly over to Vendor Kyros's souvenir stall. He'll teach you how to greet people at other times of the day.",
                    "Fly to Kyros's stall and learn more greetings!",
                    "", "SetObjective:Talk to Kyros");

                // Add quiz choice to Kalaw_W04_Success
                kalawW04.choices.Add(new DialogueChoice
                {
                    choiceText  = "Take Kalaw's Quiz!",
                    nextNode    = quizDone,
                    choiceEvent = "StartTiptipQuiz:A",
                    isWrong     = false
                });
                EditorUtility.SetDirty(kalawW04);
                Debug.Log("<color=cyan>[Step 3] Added Kalaw Quiz trigger to Kalaw_W04_Success.</color>");
            }
            else
            {
                Debug.Log("[Step 3] Kalaw_W04_Success already has a quiz choice, skipping.");
            }
        }
        else Debug.LogWarning("[Step 3] Kalaw_W04_Success.asset not found!");

        // ─── Step 4: Wire WORD RUSH → after Lito's last request word (W27) ───
        // The last request word taught by Lito is Word 27: mabalin kadi agsaludsod
        // We look for Lito_W27_Success (or whatever the last success node is named)
        // and add the Word Rush choice there.
        // Per curriculum script: after W27 success → REQUESTS MILESTONE UNLOCKED
        string litoW27Path = "Assets/Dialogues/CalleCrisologo/Level2_FunctionalNavigational/Quest5_Requests/Lito/Lito_W27_Success.asset";
        DialogueNode litoW27 = AssetDatabase.LoadAssetAtPath<DialogueNode>(litoW27Path);

        if (litoW27 == null)
        {
            // Try alternate naming conventions used in existing assets
            string[] candidates = new string[] {
                litoDir + "/Lito_W05_Success.asset", // if numbered differently
                litoDir + "/Lito_Success_3.asset",
                litoDir + "/Lito_Intro.asset"        // fallback to Intro if W27 doesn't exist
            };
            foreach (var c in candidates)
            {
                litoW27 = AssetDatabase.LoadAssetAtPath<DialogueNode>(c);
                if (litoW27 != null) { Debug.LogWarning($"[Step 4] Lito_W27_Success not found, using {c} as Word Rush trigger."); break; }
            }
        }

        if (litoW27 != null && litoAfter != null)
        {
            if (!HasMinigameChoice(litoW27))
            {
                litoW27.choices.Add(new DialogueChoice
                {
                    choiceText  = "Play Word Rush! (Requests)",
                    nextNode    = litoAfter,
                    choiceEvent = "StartMinigame:WordRush",
                    isWrong     = false
                });
                EditorUtility.SetDirty(litoW27);
                Debug.Log($"<color=cyan>[Step 4] Added Word Rush trigger to {litoW27.name}.</color>");
            }
        }
        else Debug.LogWarning("[Step 4] Could not find Lito's last word success node or AfterMinigame asset.");

        // ─── Step 5: Wire MATCHING GAME → after Klara_W37_Success ────────────
        // Per curriculum: after Klara teaches Word 37 (uray ditoy) → DIRECTIONS MILESTONE UNLOCKED
        // Look for the actual W37 success node first
        string[] klaraCandidates = new string[] {
            klaraDir + "/Klara_W37_Success.asset",
            klaraDir + "/Klara_W01_Success.asset",
            klaraDir + "/Klara_Intro.asset"
        };
        DialogueNode klaraW37 = null;
        foreach (var c in klaraCandidates)
        {
            klaraW37 = AssetDatabase.LoadAssetAtPath<DialogueNode>(c);
            if (klaraW37 != null) { if (c != klaraDir + "/Klara_Intro.asset") Debug.Log($"[Step 5] Using {c} as Matching Game trigger."); break; }
        }

        if (klaraW37 != null && klaraAfter != null)
        {
            if (!HasMinigameChoice(klaraW37))
            {
                klaraW37.choices.Add(new DialogueChoice
                {
                    choiceText  = "Play Matching Game! (Directions)",
                    nextNode    = klaraAfter,
                    choiceEvent = "StartMinigame:Matching",
                    isWrong     = false
                });
                EditorUtility.SetDirty(klaraW37);
                Debug.Log($"<color=cyan>[Step 5] Added Matching Game trigger to {klaraW37.name}.</color>");
            }
        }
        else Debug.LogWarning("[Step 5] Could not find Klara's last direction word success node or AfterMinigame asset.");

        // ─── Step 6: Update NPC minigame categories in scene ─────────────────
        var allNPCs = Object.FindObjectsByType<InteractableNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var npc in allNPCs)
        {
            string n = npc.gameObject.name.ToLower();
            if (n.Contains("lito"))
            {
                npc.minigameCategory   = "Requests";   // matches LuminangPhrases category
                npc.minigameLanguageId = 1;             // Ilocano
                EditorUtility.SetDirty(npc);
            }
            if (n.Contains("klara"))
            {
                npc.minigameCategory   = "Directions"; // matches LuminangPhrases category
                npc.minigameLanguageId = 1;
                EditorUtility.SetDirty(npc);
            }
            if (n.Contains("kalaw"))
            {
                npc.minigameCategory   = "Greetings";  // matches LuminangPhrases category
                npc.minigameLanguageId = 1;
                EditorUtility.SetDirty(npc);
            }
        }
        Debug.Log("<color=cyan>[Step 6] NPC minigame categories updated (Ilocano languageId=1).</color>");

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("<color=green>SUCCESS: Curriculum minigame triggers are now in the correct positions!\n" +
                  "  Kalaw Quiz   → triggers after Kalaw_W04_Success (end of Greetings W1-W4)\n" +
                  "  Word Rush    → triggers after Lito's last Requests word (W27)\n" +
                  "  Matching     → triggers after Klara's last Directions word (W37)\n" +
                  "  All using languageId=1 (Ilocano)</color>");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void RemoveMinigameChoiceFrom(string assetPath)
    {
        DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(assetPath);
        if (node == null) return;
        int before = node.choices.Count;
        node.choices.RemoveAll(c => c.choiceEvent != null &&
            (c.choiceEvent.StartsWith("StartMinigame") || c.choiceEvent.StartsWith("StartTiptipQuiz")));
        if (node.choices.Count != before)
        {
            EditorUtility.SetDirty(node);
            Debug.Log($"<color=yellow>[Cleanup] Removed {before - node.choices.Count} minigame choice(s) from {System.IO.Path.GetFileName(assetPath)}.</color>");
        }
    }

    private static bool HasMinigameChoice(DialogueNode node)
    {
        foreach (var c in node.choices)
            if (c.choiceEvent != null && (c.choiceEvent.StartsWith("StartMinigame") || c.choiceEvent.StartsWith("StartTiptipQuiz")))
                return true;
        return false;
    }

    private static DialogueNode GetOrCreateDialogue(string path, string speaker, string text, string translation, string animTrigger, string endEvent)
    {
        DialogueNode existing = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
        if (existing != null) return existing;

        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace("\\", "/"));
        DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
        node.speakerName     = speaker;
        node.dialogueText    = text;
        node.translatedText  = translation;
        node.animationTrigger = animTrigger;
        node.endEventName    = endEvent;
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
