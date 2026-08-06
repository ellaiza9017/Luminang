using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A shared ScriptableObject pool of ambient world-building dialogue lines.
/// NPCs that don't have custom ambient dialogues can draw from this library
/// so they still feel alive even when they aren't the story objective.
/// 
/// Create via: Tools/Luminang/Create Ambient Dialogue Library
/// </summary>
[CreateAssetMenu(fileName = "AmbientDialogueLibrary", menuName = "Luminang/Ambient Dialogue Library")]
public class AmbientDialogueLibrary : ScriptableObject
{
    [Tooltip("Pool of ambient dialogue nodes all NPCs can use as fallback.")]
    public List<DialogueNode> sharedLines = new List<DialogueNode>();

    /// <summary>
    /// Returns a random ambient node from the shared pool, or null if empty.
    /// </summary>
    public DialogueNode GetRandom()
    {
        if (sharedLines == null || sharedLines.Count == 0) return null;
        return sharedLines[Random.Range(0, sharedLines.Count)];
    }
}
