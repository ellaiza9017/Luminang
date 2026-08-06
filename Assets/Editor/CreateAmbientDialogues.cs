using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Creates an AmbientDialogueLibrary ScriptableObject populated with default 
/// world-building lines, and optionally assigns it to all NPCs in the scene.
/// Menu: Tools/Luminang/Create Ambient Dialogue Library
/// </summary>
public class CreateAmbientDialogues : EditorWindow
{
    private const string LibraryPath = "Assets/Dialogues/Ambient/AmbientDialogueLibrary.asset";
    private const string NodeFolder  = "Assets/Dialogues/Ambient/Lines";

    [MenuItem("Tools/Luminang/Create Ambient Dialogue Library")]
    public static void Run()
    {
        // Ensure folders exist
        EnsureFolder("Assets/Dialogues/Ambient");
        EnsureFolder(NodeFolder);

        // Create or load library
        AmbientDialogueLibrary library = AssetDatabase.LoadAssetAtPath<AmbientDialogueLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<AmbientDialogueLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            Debug.Log("<color=cyan>[AmbientDialogue] Created AmbientDialogueLibrary asset.</color>");
        }

        // Default ambient lines
        string[][] lines = new string[][]
        {
            new[] { "Naimbag a bigat!",        "Good morning! Enjoy your walk today." },
            new[] { "Napintas ti Calle Crisologo!", "Calle Crisologo is beautiful, isn't it?" },
            new[] { "Nag-annad ak kenka.",      "I've been looking forward to seeing you around." },
            new[] { "Naimas ti tinapay ditoy.", "The bakery here has the best bread in town!" },
            new[] { "Napigsa ti angin!",        "The breeze feels wonderful today." },
            new[] { "Nag-impas ti peria.",      "The market has fresh goods today!" },
            new[] { "Mapan ako mangda.",        "I'm heading to get something to eat. This place always makes me hungry!" },
            new[] { "Nagtaray dagiti ubbing.", "The children were running around here earlier. So lively!" },
            new[] { "Maymaysa ti lugar ditoy.", "This is such a peaceful place. I could stay here all day." },
            new[] { "Kastoy ti kinabukasan.", "Every day here feels like a blessing. I hope you're enjoying Vigan!" },
        };

        library.sharedLines = new System.Collections.Generic.List<DialogueNode>();

        for (int i = 0; i < lines.Length; i++)
        {
            string assetName = $"Ambient_Line_{i + 1:D2}";
            string path = $"{NodeFolder}/{assetName}.asset";
            DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
            if (node == null)
            {
                node = ScriptableObject.CreateInstance<DialogueNode>();
                AssetDatabase.CreateAsset(node, path);
            }
            node.speakerName    = ""; // No speaker — generic ambient
            node.dialogueText   = lines[i][0];
            node.translatedText = lines[i][1];
            node.ambientOnly    = true;
            node.choices        = new System.Collections.Generic.List<DialogueChoice>();
            EditorUtility.SetDirty(node);
            library.sharedLines.Add(node);
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        // Optionally assign to all NPCs in the scene that have no custom ambient dialogues
        int assigned = 0;
        var allNPCs = Object.FindObjectsByType<InteractableNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var npc in allNPCs)
        {
            if (npc.ambientLibrary == null)
            {
                npc.ambientLibrary = library;
                EditorUtility.SetDirty(npc);
                assigned++;
            }
        }

        Debug.Log($"<color=green>[AmbientDialogue] Done! Library created with {lines.Length} lines. Assigned to {assigned} NPC(s) in scene.</color>");
        EditorUtility.DisplayDialog("Ambient Dialogue Library Created",
            $"Created {lines.Length} ambient dialogue lines.\n" +
            $"Assigned the library to {assigned} NPC(s) in the scene.\n\n" +
            "You can add custom ambient lines to individual NPCs via their\n" +
            "'Ambient Dialogues' list in the Inspector.\n\n" +
            "Save the scene to preserve changes.",
            "OK");
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
