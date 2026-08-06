using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Dialogue Node", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("NPC Settings")]
    [Tooltip("The name of the NPC speaking (optional).")]
    public string speakerName;

    [Tooltip("The portrait of the NPC to show during this dialogue. Leave empty for no portrait.")]
    public Sprite speakerPortrait;
    
    [TextArea(3, 5)]
    [Tooltip("What the NPC says in the dialogue box.")]
    public string dialogueText;

    [TextArea(3, 5)]
    [Tooltip("The translation of the dialogue text (optional).")]
    public string translatedText;

    [Tooltip("Trigger name to send to the NPC's Animator (e.g., 'DoPointing'). Leave empty for no animation.")]
    public string animationTrigger;

    [Tooltip("Event fired instantly when this node starts (e.g., Camera Zoom).")]
    public string triggerEventName;
    [Tooltip("Event fired only after this node is completed (e.g., Start Lesson).")]
    public string endEventName;


    [Header("Player Options")]
    [Tooltip("The choices the player has. If this list is empty, the conversation ends.")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Tooltip("If true, the first two choice buttons will be labelled 'Wen' (Yes) and 'Saan' (No) in Ilocano.")]
    public bool isYesNoChoice;

    [Header("Ambient / World-Building")]
    [Tooltip("If true, this node is ambient world-building dialogue only. It will never advance story objectives or trigger quests.")]
    public bool ambientOnly;
}

[System.Serializable]
public class DialogueChoice
{
    [Tooltip("What the player's button will say (e.g., 'Yes', 'No', 'Tell me more').")]
    public string choiceText;

    [Tooltip("The next Dialogue Node to load if the player clicks this option. If left empty, clicking this ends the conversation.")]
    public DialogueNode nextNode;

    [Tooltip("Mark this true if this is a WRONG answer. The NPC's OnWrongAnswer event will fire before advancing to the next node.")]
    public bool isWrong;

    [Tooltip("Event fired ONLY when this specific choice is selected (e.g., 'StartMinigame').")]
    public string choiceEvent;

    [Tooltip("If this choice triggers STT (e.g., choiceEvent = 'StartSTT'), this is the exact phrase STT must recognize to advance to nextNode.")]
    public string expectedSTTWord;
}
