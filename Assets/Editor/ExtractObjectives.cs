using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ExtractObjectives : EditorWindow
{
    [MenuItem("Tools/Extract Objectives")]
    public static void DoWork()
    {
        string path = @"C:\Users\Asus\.gemini\antigravity-ide\brain\8f601297-6156-4d8e-841a-f026039c661c\playable_objectives.md";
        
        // Ensure the directory exists
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string[] guids = AssetDatabase.FindAssets("t:DialogueNode", new string[] { "Assets/Dialogues" });
        
        var objectives = new List<string>();
        objectives.Add("# Game Objectives List");
        objectives.Add("Below is the comprehensive list of all objectives triggered across all dialogue nodes, categorized by their location in the project structure.\n");
        
        var categorized = new Dictionary<string, HashSet<string>>();

        foreach(string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string folderPath = Path.GetDirectoryName(assetPath).Replace("Assets\\Dialogues\\", "").Replace("\\", "/");
            
            string[] lines = File.ReadAllLines(assetPath);
            foreach(string line in lines)
            {
                if (line.Contains("SetObjective") || line.Contains("Setobjective"))
                {
                    string obj = line.Substring(line.IndexOf("Set"));
                    obj = obj.Replace("SetObjective:", "").Replace("SetObjective_", "").Replace("Setobjective:", "").Trim();
                    
                    if (!categorized.ContainsKey(folderPath)) categorized[folderPath] = new HashSet<string>();
                    categorized[folderPath].Add(obj);
                }
            }
        }

        foreach(var kvp in categorized)
        {
            objectives.Add($"## {kvp.Key}");
            int i = 1;
            foreach(var obj in kvp.Value)
            {
                objectives.Add($"{i++}. {obj}");
            }
            objectives.Add("");
        }

        File.WriteAllLines(path, objectives);
        Debug.Log("Objectives extracted to " + path);
    }
}
