using UnityEngine;
using UnityEditor;
using System.IO;

public static class AssignSpeakerPortraits
{
    [MenuItem("Tools/Luminang/Assign Speaker Portraits")]
    public static void AssignPortraits()
    {
        int patchedNodes = 0;
        string[] dialoguesFolders = new string[] { "Assets/Dialogues" };
        string spritesFolder = "Assets/Sprites/NPCs";

        // Find all DialogueNode assets
        string[] nodeGuids = AssetDatabase.FindAssets("t:DialogueNode", dialoguesFolders);
        
        // Find all sprite assets in the NPCs folder
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { spritesFolder });

        foreach (string nodeGuid in nodeGuids)
        {
            string nodePath = AssetDatabase.GUIDToAssetPath(nodeGuid);
            DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(nodePath);
            if (node == null || string.IsNullOrEmpty(node.speakerName)) continue;

            string speaker = node.speakerName.Trim();
            
            // Special cases or simplifications
            if (speaker.Equals("Mang Lance", System.StringComparison.OrdinalIgnoreCase)) speaker = "MangLance";
            if (speaker.Equals("Apo Lakay", System.StringComparison.OrdinalIgnoreCase)) speaker = "ApoLakay";
            if (speaker.Equals("Aling Rosa", System.StringComparison.OrdinalIgnoreCase)) speaker = "AlingRosa";

            Sprite foundSprite = null;

            // Search for a matching sprite
            foreach (string spriteGuid in spriteGuids)
            {
                string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuid);
                string spriteName = Path.GetFileNameWithoutExtension(spritePath);

                // E.g. If speaker is "KALAW" and sprite is "KalawImage"
                if (spriteName.Contains(speaker, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    break;
                }
            }

            // ONLY assign if we actually found a portrait for this NPC
            if (foundSprite != null && node.speakerPortrait != foundSprite)
            {
                node.speakerPortrait = foundSprite;
                EditorUtility.SetDirty(node);
                patchedNodes++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[AssignSpeakerPortraits] Done! Updated {patchedNodes} Dialogue Nodes with speaker portraits.");
        EditorUtility.DisplayDialog("Portraits Assigned", $"Assigned or updated portraits for {patchedNodes} dialogue nodes based on their speaker names!", "OK");
    }
}
