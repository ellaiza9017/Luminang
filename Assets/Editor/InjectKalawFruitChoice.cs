using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class InjectKalawFruitChoice
{
    [MenuItem("Tools/Luminang/Inject Kalaw Fruit Choice")]
    public static void Inject()
    {
        string introPath = "Assets/Dialogues/CalleCrisologo/Level1_ConversationalSocial/Quest1_Greetings/Kalaw/Kalaw_Intro.asset";
        string yesPath = "Assets/Dialogues/CalleCrisologo/Level1_ConversationalSocial/Quest1_Greetings/Kalaw/Kalaw_Intro_Yes.asset";
        string noPath = "Assets/Dialogues/CalleCrisologo/Level1_ConversationalSocial/Quest1_Greetings/Kalaw/Kalaw_Intro_No.asset";

        DialogueNode introNode = AssetDatabase.LoadAssetAtPath<DialogueNode>(introPath);
        DialogueNode yesNode = AssetDatabase.LoadAssetAtPath<DialogueNode>(yesPath);
        DialogueNode noNode = AssetDatabase.LoadAssetAtPath<DialogueNode>(noPath);

        if (introNode != null && yesNode != null && noNode != null)
        {
            // 1. Move the trigger event to the YES node so the quest only progresses if they give the fruit
            if (!string.IsNullOrEmpty(introNode.triggerEventName))
            {
                yesNode.triggerEventName = introNode.triggerEventName;
                introNode.triggerEventName = "";
                EditorUtility.SetDirty(introNode);
                EditorUtility.SetDirty(yesNode);
            }

            // 2. Update the NO node to politely decline and end the conversation
            noNode.dialogueText = "That's alright. Come back if you change your mind.";
            if (noNode.choices != null)
            {
                noNode.choices.Clear(); // Remove choices so the conversation ends
            }
            noNode.triggerEventName = "";
            noNode.endEventName = "";
            EditorUtility.SetDirty(noNode);

            // 3. Ensure Intro has choices Wen and Saan, and isWrong is FALSE for both
            if (introNode.choices != null && introNode.choices.Count >= 2)
            {
                introNode.choices[0].isWrong = false; // Wen
                introNode.choices[1].isWrong = false; // Saan - polite decline should not trigger wrong animation
                EditorUtility.SetDirty(introNode);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[InjectKalawFruitChoice] Successfully updated Kalaw's fruit quest to use the Wen/Saan global choice system.");
        }
        else
        {
            Debug.LogError("[InjectKalawFruitChoice] Could not find one or more Kalaw dialogue assets!");
        }
    }
}
