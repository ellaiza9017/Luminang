using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Global manager that controls branching conversations.
/// Takes over when InteractionManager triggers a dialogue start.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    /// <summary>Fired when a dialogue begins with a specific NPC. NPCPatrol subscribes to auto-pause.</summary>
    public static System.Action<InteractableNPC> OnNPCDialogueStarted;
    /// <summary>Fired when a dialogue ends with a specific NPC. NPCPatrol subscribes to auto-resume.</summary>
    public static System.Action<InteractableNPC> OnNPCDialogueEnded;

    /// <summary>
    /// True for the entire duration of a conversation.
    /// InteractionManager and QuestIndicator use this to hide themselves.
    /// </summary>
    public bool IsInDialogue { get; private set; } = false;

    [Header("References")]
    [Tooltip("The script that handles the visual display of the dialogue box.")]
    public DialogueUIController uiController;

    private Animator _currentNPCAnimator;
    private InteractableNPC _currentNPC;

    // ── History for Prev button ───────────────────────────────────
    private readonly Stack<DialogueNode> _nodeHistory = new Stack<DialogueNode>();
    private DialogueNode _activeNode;
    private bool _navigatingBack = false;
    private bool _keepOverlayForOneNode = false;

    public bool CanGoBack => _nodeHistory.Count > 0;
    private string _pendingEventName;

    /// <summary>Returns the currently active dialogue node.</summary>
    public DialogueNode GetActiveNode() => _activeNode;

    // The next node to advance to after a minigame completes (stored separately so it survives clone destruction)
    private DialogueNode _pendingMinigameNextNode;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Called by UnityEvents or cutscenes with just a DialogueNode.
    /// </summary>
    public void StartDialogue(DialogueNode startNode)
    {
        StartDialogue(startNode, null, null);
    }

    /// <summary>
    /// Called by InteractableNPC when the player clicks the Talk button.
    /// </summary>
    public void StartDialogue(DialogueNode startNode, Animator npcAnimator, InteractableNPC npc)
    {
        Debug.Log($"[DialogueManager] StartDialogue called with node: {(startNode == null ? "NULL" : startNode.name)}");
        _currentNPCAnimator = npcAnimator;
        _currentNPC = npc;
        IsInDialogue = true;
        ToggleCursor(false);

        // Notify NPCPatrol and other listeners
        OnNPCDialogueStarted?.Invoke(npc);

        // Hide the proximity Talk button
        if (InteractionManager.Instance != null && InteractionManager.Instance.talkButton != null)
        {
            InteractionManager.Instance.talkButton.gameObject.SetActive(false);
        }

        // Process the first node
        ProcessNode(startNode);
    }

    private void ProcessNode(DialogueNode node, bool skipAnimation = false)
    {
        if (node == null)
        {
            Debug.Log("<color=red>[DialogueManager] ProcessNode received NULL node! Ending dialogue.</color>");
            EndDialogue();
            return;
        }

        Debug.Log($"<color=yellow>[DialogueManager] ProcessNode -> Loading Node: '{node.name}', Dialogue Text: '{node.dialogueText}'</color>");

        // Track history (skip when navigating back to avoid double-pushing)
        if (!_navigatingBack && _activeNode != null)
            _nodeHistory.Push(_activeNode);
        _navigatingBack = false;
        _activeNode = node;

        // 1. Play Animation (or reset to Idle if none specified)
        if (_currentNPCAnimator != null)
        {
            if (!string.IsNullOrEmpty(node.animationTrigger))
            {
                SafeSetTrigger(_currentNPCAnimator, node.animationTrigger);
            }
            else
            {
                SafeSetTrigger(_currentNPCAnimator, "Idle"); // Force back to idle if bubble is empty
            }
        }

        // 1.5 Fire Start Event (Immediate)
        if (!string.IsNullOrEmpty(node.triggerEventName))
        {
            HandleGlobalDialogueEvent(node.triggerEventName);

            // If the trigger event synchronously loaded a different node (e.g., a routing script jumped to a new branch),
            // abort processing this current node so we don't display it.
            if (_activeNode != node)
            {
                Debug.Log($"<color=cyan>[DialogueManager] Trigger event redirected dialogue to '{_activeNode.name}'. Skipping display of '{node.name}'.</color>");
                return;
            }
        }

        // 1.6 Store End Event to fire when this node is COMPLETED
        _pendingEventName = node.endEventName;

        // Check if the node has an STT choice and assign PendingSTTChoice immediately so the UI controller knows.
        PendingSTTChoice = null;
        if (node.choices != null)
        {
            foreach (var choice in node.choices)
            {
                bool isSTTChoice = !string.IsNullOrEmpty(choice.expectedSTTWord) ||
                                  (choice.choiceEvent != null && choice.choiceEvent.Trim().Equals("StartSTT", System.StringComparison.OrdinalIgnoreCase));

                if (isSTTChoice)
                {
                    PendingSTTChoice = choice;
                    break;
                }
            }
        }

        // Auto-inject STT choice if text contains "try saying" or "Try saying" but is missing STT configuration
        if (PendingSTTChoice == null && !string.IsNullOrEmpty(node.dialogueText) && 
            (node.dialogueText.Contains("try saying") || node.dialogueText.Contains("Try saying")))
        {
            string expectedWord = "";
            int firstQuote = node.dialogueText.IndexOf('\'');
            if (firstQuote >= 0)
            {
                int secondQuote = node.dialogueText.IndexOf('\'', firstQuote + 1);
                if (secondQuote > firstQuote)
                {
                    expectedWord = node.dialogueText.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                }
            }
            if (string.IsNullOrEmpty(expectedWord))
            {
                firstQuote = node.dialogueText.IndexOf('\"');
                if (firstQuote >= 0)
                {
                    int secondQuote = node.dialogueText.IndexOf('\"', firstQuote + 1);
                    if (secondQuote > firstQuote)
                    {
                        expectedWord = node.dialogueText.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                    }
                }
            }

            if (!string.IsNullOrEmpty(expectedWord))
            {
                expectedWord = expectedWord.Trim().Trim('?', '.', '!', ',', ';', ':', '`', '\'', '\"');
            }

            if (!string.IsNullOrEmpty(expectedWord))
            {
                Debug.Log($"[DialogueManager] Auto-injecting STT choice for word: '{expectedWord}'");
                if (node.choices == null) node.choices = new List<DialogueChoice>();
                
                // If there is an existing empty or continue choice, update it to be the STT choice
                if (node.choices.Count > 0)
                {
                    node.choices[0].expectedSTTWord = expectedWord;
                    node.choices[0].choiceEvent = "StartSTT";
                    if (string.IsNullOrEmpty(node.choices[0].choiceText))
                    {
                        node.choices[0].choiceText = $"Say: \"{expectedWord}\"";
                    }
                    PendingSTTChoice = node.choices[0];
                }
                else
                {
                    DialogueChoice newChoice = new DialogueChoice
                    {
                        choiceText = $"Say: \"{expectedWord}\"",
                        nextNode = null,
                        isWrong = false,
                        choiceEvent = "StartSTT",
                        expectedSTTWord = expectedWord
                    };
                    node.choices.Add(newChoice);
                    PendingSTTChoice = newChoice;
                }
            }
        }

        // Automatically hide InSceneLessonController or TeachingOverlayPanel if we've moved to a non-STT node.
        // We DO NOT automatically show them here anymore. They will be shown at the end of the text when OnChoiceSelected(StartSTT) is triggered!
        if (PendingSTTChoice == null)
        {
            if (_keepOverlayForOneNode)
            {
                // Let the overlay stay visible for this success node!
                _keepOverlayForOneNode = false; 
            }
            else
            {
                // Clear prompt text, "Great job!", and "Tap to stop" when moving to a non-STT dialogue node
                if (InSceneLessonController.Instance != null && InSceneLessonController.Instance.IsLessonActive)
                {
                    InSceneLessonController.Instance.ClearPromptAndFeedbackUI();
                }
                if (TeachingOverlayPanel.Instance != null)
                {
                    TeachingOverlayPanel.Instance.Hide();
                }
            }
        }

        // 2. Display UI and update nav buttons
        if (uiController != null)
        {
            if (!string.IsNullOrEmpty(_injectedPrefix))
            {
                uiController.injectedPrefixText = _injectedPrefix;
                _injectedPrefix = "";
            }
            uiController.DisplayNode(node, OnChoiceSelected, skipAnimation);
            uiController.SetNavigation(canGoBack: _nodeHistory.Count > 0);
        }
        else
        {
            Debug.LogError("[DialogueManager] uiController is NULL! The dialogue panel cannot be shown!");
        }
    }

    public DialogueChoice PendingSTTChoice { get; set; }
    public DialogueChoice PendingMinigameChoice { get; set; }

    /// <summary>
    /// Called by the Prev button in DialogueUIController.
    /// </summary>
    public void GoToPreviousNode()
    {
        if (_nodeHistory.Count == 0) return;
        _navigatingBack = true;
        _activeNode = null;
        ProcessNode(_nodeHistory.Pop());
    }

    /// <summary>
    /// Triggered by the DialogueUIController when the player clicks a choice button.
    /// </summary>
    private void OnChoiceSelected(DialogueChoice choice)
    {
        DialogueNode nodeBeforeEvent = _activeNode;

        if (choice == null)
        {
            // Fire the event BEFORE we clean up the NPC reference
            FirePendingEvent(_currentNPC);

            // If FirePendingEvent caused a redirect (e.g., Evaluate → JumpToNode),
            // the dialogue is already on a new node. Don't EndDialogue.
            if (_activeNode != nodeBeforeEvent)
            {
                Debug.Log($"<color=cyan>[DialogueManager] FirePendingEvent redirected dialogue (null choice). Skipping EndDialogue.</color>");
                return;
            }

            EndDialogue();
            return;
        }

        FirePendingEvent(_currentNPC); 

        // If FirePendingEvent caused a redirect (e.g., Evaluate → JumpToNode),
        // the dialogue is already on a new node. Don't continue processing.
        if (_activeNode != nodeBeforeEvent)
        {
            Debug.Log($"<color=cyan>[DialogueManager] FirePendingEvent redirected dialogue. Aborting choice processing.</color>");
            return;
        }

        // ── Handle Choice-Specific Events ──
        if (!string.IsNullOrEmpty(choice.choiceEvent))
        {
            string choiceEventTrimmed = choice.choiceEvent.Trim();

            // ── STT: pause dialogue and show the Teaching Panel with mic ──
            if (choiceEventTrimmed.Equals("StartSTT", System.StringComparison.OrdinalIgnoreCase))
            {
                PendingSTTChoice = choice;
                // Show the TeachingOverlayPanel with the mic so the player can speak
                if (InSceneLessonController.Instance != null && InSceneLessonController.Instance.IsLessonActive)
                {
                    InSceneLessonController.Instance.ShowInSceneMic(choice.expectedSTTWord);
                }
                else if (TeachingOverlayPanel.Instance != null)
                {
                    TeachingOverlayPanel.Instance.Show(choice.expectedSTTWord);
                }
                // PAUSE here. TeachingOverlayPanel.HandleSuccess -> CompleteSTT(true) resumes dialogue.
                return;
            }

            // ── Minigame: pause until MinigameManager calls CompleteMinigame ──
            if (choiceEventTrimmed.StartsWith("StartMinigame", System.StringComparison.OrdinalIgnoreCase))
            {
                PendingMinigameChoice = choice;
                _pendingMinigameNextNode = choice.nextNode; // Store nextNode NOW before any clone destroys the choice reference
                if (uiController != null) uiController.HideChoicesOnly();
                // Broadcast to ALL NPCs — the one with this event mapped will respond
                BroadcastDialogueEvent(choiceEventTrimmed);
                Debug.Log($"<color=cyan>[DialogueManager] StartMinigame event fired ('{choiceEventTrimmed}'). Next node: '{(choice.nextNode != null ? choice.nextNode.name : "NULL")}'. Dialogue PAUSED until CompleteMinigame() is called.</color>");
                return; // PAUSE here. MinigameManager.HideMinigame -> CompleteMinigame() resumes dialogue.
            }

            // ── Popup: pause until PopupManager finishes showing queued popups ──
            if (choiceEventTrimmed.StartsWith("ShowPopup:", System.StringComparison.OrdinalIgnoreCase))
            {
                string popupNames = choiceEventTrimmed.Substring("ShowPopup:".Length);
                if (uiController != null) uiController.HideDialogue();
                
                // Hide floating text/overlay if it was active
                if (TeachingOverlayPanel.Instance != null)
                {
                    TeachingOverlayPanel.Instance.Hide();
                }

                if (PopupManager.Instance != null)
                {
                    PopupManager.Instance.ShowPopups(popupNames, () => 
                    {
                        // Resume dialogue after popups are dismissed
                        if (choice.isWrong)
                        {
                            InteractableNPC talkingNPC = GetActiveNPC();
                            DialogueNode returnNode = choice.nextNode != null ? choice.nextNode : _activeNode;
                            StartCoroutine(HandleWrongAnswer(returnNode));
                        }
                        else
                        {
                            ProcessNode(choice.nextNode);
                        }
                    });
                    return; // PAUSE here.
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] ShowPopup event triggered, but PopupManager.Instance is null! Skipping popups.");
                }
            }

            // All other events: broadcast to ALL NPCs in case speaker changed mid-conversation
            BroadcastDialogueEvent(choiceEventTrimmed);
        }

        if (choice.isWrong)
        {
            InteractableNPC talkingNPC = GetActiveNPC();
            DialogueNode returnNode = choice.nextNode != null ? choice.nextNode : _activeNode;
            StartCoroutine(HandleWrongAnswer(returnNode));
            return;
        }

        ProcessNode(choice.nextNode);
    }

    /// <summary>
    /// Call this from MinigameManager.onMinigameComplete (or HideMinigame) to resume the dialogue
    /// after a minigame that was started via a StartMinigame choiceEvent.
    /// </summary>
    public void CompleteMinigame()
    {
        Debug.Log("<color=green>[DialogueManager] CompleteMinigame called – resuming dialogue.</color>");
        DialogueChoice choice = PendingMinigameChoice;
        PendingMinigameChoice = null;
        DialogueNode next = _pendingMinigameNextNode;
        _pendingMinigameNextNode = null;

        if (next != null)
        {
            Debug.Log($"<color=green>[DialogueManager] CompleteMinigame -> ProcessNode({next.name})</color>");
            ProcessNode(next);
        }
        else if (choice != null && choice.nextNode != null)
        {
            Debug.Log($"<color=green>[DialogueManager] CompleteMinigame (choice fallback) -> ProcessNode({choice.nextNode.name})</color>");
            ProcessNode(choice.nextNode);
        }
        else
        {
            Debug.LogWarning("[DialogueManager] CompleteMinigame: No next node to advance to! (PendingMinigameChoice and _pendingMinigameNextNode were both null)");
        }
    }


    private string _injectedPrefix = "";

    public void CompleteSTT(bool success, string prefixText = "")
    {
        Debug.Log($"<color=cyan>[DialogueManager] CompleteSTT called with success={success}. PendingSTTChoice: {(PendingSTTChoice != null ? PendingSTTChoice.expectedSTTWord : "NULL")}</color>");

        DialogueChoice choice = PendingSTTChoice;
        if (choice == null && _activeNode != null && _activeNode.choices != null && _activeNode.choices.Count > 0)
        {
            choice = _activeNode.choices[0];
            Debug.Log($"<color=cyan>[DialogueManager] Used activeNode.choices[0] fallback. Target nextNode: {(choice != null && choice.nextNode != null ? choice.nextNode.name : "NULL")}</color>");
        }

        if (choice != null)
        {
            PendingSTTChoice = null;
            
            if (success)
            {
                if (!string.IsNullOrEmpty(prefixText))
                {
                    _injectedPrefix = prefixText;
                }

                Debug.Log($"<color=green>[DialogueManager] STT SUCCESS! Loading nextNode: {(choice.nextNode != null ? choice.nextNode.name : "NULL (Ends Dialogue)")}</color>");
                _keepOverlayForOneNode = true;
                ProcessNode(choice.nextNode);
            }
            else
            {
                Debug.Log("<color=red>[DialogueManager] STT FAILED!</color>");
                GetActiveNPC();
                StartCoroutine(HandleWrongAnswer(_activeNode));
            }
        }
        else
        {
            Debug.LogWarning("[DialogueManager] CompleteSTT called but no choice or activeNode choice could be found!");
        }
    }

    public InteractableNPC GetActiveNPC()
    {
        if (_currentNPC != null) return _currentNPC;

        if (_activeNode != null && !string.IsNullOrEmpty(_activeNode.speakerName))
        {
            string speaker = _activeNode.speakerName.Trim();
            var allNPCs = FindObjectsByType<InteractableNPC>(FindObjectsSortMode.None);
            foreach (var npc in allNPCs)
            {
                if (npc.gameObject.name.IndexOf(speaker, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _currentNPC = npc;
                    Debug.Log($"<color=cyan>[DialogueManager] Matched speaker '{speaker}' to NPC GameObject '{npc.name}'!</color>");
                    return _currentNPC;
                }
            }

            foreach (var npc in allNPCs)
            {
                if (npc.defaultDialogue == _activeNode || (npc.questDialogues != null && npc.questDialogues.Exists(q => q.dialogueNode == _activeNode)))
                {
                    _currentNPC = npc;
                    Debug.Log($"<color=cyan>[DialogueManager] Matched dialogue node '{_activeNode.name}' to NPC GameObject '{npc.name}'!</color>");
                    return _currentNPC;
                }
            }
        }

        _currentNPC = FindFirstObjectByType<InteractableNPC>();
        return _currentNPC;
    }

    private System.Collections.IEnumerator HandleWrongAnswer(DialogueNode returnToNode)
    {
        Debug.Log($"[DialogueManager] Handling wrong answer. Returning to: {(returnToNode != null ? returnToNode.name : "NULL")}");

        // Only hide the dialogue UI visually — do NOT touch movementUI since we're still in dialogue
        if (uiController != null)
            uiController.HideChoicesOnly();

        // Trigger the NPC wrong-answer animation on the active speaker NPC
        InteractableNPC npcToAnimate = GetActiveNPC();
        if (npcToAnimate != null)
        {
            Debug.Log($"<color=orange>[DialogueManager] Triggering TriggerWrongAnswerAnimation on '{npcToAnimate.name}'...</color>");
            npcToAnimate.TriggerWrongAnswerAnimation();
        }

        yield return null;

        // Wait for wrong-answer animation to finish (max 6 sec safety timeout)
        float elapsed = 0f;
        while (_currentNPC != null && _currentNPC.isWrongAnswerPlaying && elapsed < 6f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= 6f) Debug.LogWarning("[DialogueManager] Wrong answer animation timed out.");

        if (returnToNode != null)
        {
            // Clear history so Prev button doesn't show when we loop back to the start
            _nodeHistory.Clear();
            _activeNode = null;
            ProcessNode(returnToNode, skipAnimation: true); // SKIP ANIMATION HERE
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        Debug.Log("[DialogueManager] Conversation ended.");
        IsInDialogue = false;
        ToggleCursor(true);
        _nodeHistory.Clear();
        _activeNode = null;

        // Notify NPCPatrol and other listeners before clearing _currentNPC
        OnNPCDialogueEnded?.Invoke(_currentNPC);
        
        if (uiController != null)
            uiController.HideDialogue();

        if (_currentNPC != null)
        {
            // Force NPC back to Idle when dialogue ends
            if (_currentNPCAnimator != null)
            {
                SafeSetTrigger(_currentNPCAnimator, "Idle");
            } // Automatically exit close up to restore camera state
            _currentNPC.ExitCloseUp();

            if (_currentNPC.OnDialogueEnd != null)
                _currentNPC.OnDialogueEnd.Invoke();
            
            // Re-disable interaction if it's a one-time thing, or if we are launching a lesson/tutorial flow
            if (_currentNPC.disableAfterInteraction)
            {
                _currentNPC.interactionEnabled = false;
            }
        }

        // Hide TeachingOverlayPanel if active
        if (TeachingOverlayPanel.Instance != null)
        {
            TeachingOverlayPanel.Instance.Hide();
        }

        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.ForceCheckProximity();
        }

        _currentNPCAnimator = null;
        _currentNPC = null;
    }

    private void ToggleCursor(bool isLocked)
    {
        var input = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        if (input != null)
        {
            input.cursorLocked = false;
            input.cursorInputForLook = true; // Let them look around if they drag? Actually, keep true.
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void FirePendingEvent(InteractableNPC npc)
    {
        if (!string.IsNullOrEmpty(_pendingEventName))
        {
            // Support comma-separated events (e.g., "ConversationTest_Correct,ConversationTest_Evaluate")
            string[] events = _pendingEventName.Split(',');
            foreach (string evt in events)
            {
                string trimmed = evt.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                Debug.Log($"[DialogueManager] FirePendingEvent: Sending '{trimmed}' to NPC: {(npc != null ? npc.name : "NULL")}");
                HandleGlobalDialogueEvent(trimmed);
            }
        }
        
        if (npc != null)
        {
            npc.isWrongAnswerPlaying = false;
        }
        
        _pendingEventName = null;
    }

    /// <summary>
    /// Handles dialogue events globally (SetObjective, TeachingOverlayPanel, and custom NPC events).
    /// Works even if _currentNPC is null!
    /// </summary>
    private void HandleGlobalDialogueEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        string[] events = eventName.Split(',');
        foreach (string evt in events)
        {
            string cleanEventName = evt.Trim();
            if (string.IsNullOrEmpty(cleanEventName)) continue;
            
            Debug.Log($"[DialogueManager] Handling Event: '{cleanEventName}'");

        // 1. Handle SetObjective: or SetObjective_
        if (cleanEventName.StartsWith("SetObjective:", System.StringComparison.OrdinalIgnoreCase))
        {
            string newObjText = cleanEventName.Substring("SetObjective:".Length).Trim();
            if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjText))
            {
                ObjectiveManager.Instance.SetObjective(newObjText);
                Debug.Log($"[DialogueManager] Objective set to: '{newObjText}'");
            }
        }
        else if (cleanEventName.StartsWith("SetObjective_", System.StringComparison.OrdinalIgnoreCase))
        {
            string newObjText = cleanEventName.Substring("SetObjective_".Length).Trim();
            if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjText))
            {
                ObjectiveManager.Instance.SetObjective(newObjText);
                Debug.Log($"[DialogueManager] Objective set to: '{newObjText}'");
            }
        }

        // 2. Handle TeachingOverlayPanel events
        if (cleanEventName.StartsWith("ShowTeachingPanel", System.StringComparison.OrdinalIgnoreCase))
        {
            if (TeachingOverlayPanel.Instance != null)
            {
                TeachingOverlayPanel.Instance.ShowFromEvent(cleanEventName);
            }
        }
        else if (cleanEventName.Equals("HideTeachingPanel", System.StringComparison.OrdinalIgnoreCase))
        {
            if (TeachingOverlayPanel.Instance != null)
            {
                TeachingOverlayPanel.Instance.Hide();
            }
        }
        else if (cleanEventName.StartsWith("ShowFloatingText:", System.StringComparison.OrdinalIgnoreCase))
        {
            string customText = cleanEventName.Substring("ShowFloatingText:".Length).Trim();
            if (TeachingOverlayPanel.Instance != null)
            {
                TeachingOverlayPanel.Instance.ShowCustomText(customText);
                _keepOverlayForOneNode = true; // Prevent ProcessNode from immediately hiding it
            }
        }
        else if (cleanEventName.StartsWith("ShowPopup:", System.StringComparison.OrdinalIgnoreCase))
        {
            string popupName = cleanEventName.Substring("ShowPopup:".Length).Trim();
            if (PopupManager.Instance != null && !string.IsNullOrEmpty(popupName))
            {
                PopupManager.Instance.ShowPopups(popupName);
            }
        }
        else if (cleanEventName.StartsWith("StartInSceneLesson", System.StringComparison.OrdinalIgnoreCase))
        {
            string camName = cleanEventName.Contains(":") ? cleanEventName.Split(':')[1].Trim() : "";
            if (InSceneLessonController.Instance != null)
            {
                InSceneLessonController.Instance.StartInSceneLesson(camName);
            }
        }
        else if (cleanEventName.Equals("EndInSceneLesson", System.StringComparison.OrdinalIgnoreCase))
        {
            if (InSceneLessonController.Instance != null)
            {
                InSceneLessonController.Instance.EndInSceneLesson();
            }
        }
        else if (TeachingOverlayPanel.Instance != null && PendingSTTChoice != null)
        {
            // Fallback: if triggerEventName is just a background name (e.g. "maayongBuntag" or "bg_morning"), pass it to TeachingOverlayPanel
            TeachingOverlayPanel.Instance.ShowForPendingSTT(cleanEventName);
        }

        // 3. Handle ConversationTest_ events — route to ConversationTestManager
        if (cleanEventName.StartsWith("ConversationTest_", System.StringComparison.OrdinalIgnoreCase))
        {
            if (ConversationTestManager.Instance != null)
            {
                ConversationTestManager.Instance.HandleEvent(cleanEventName);
                Debug.Log($"[DialogueManager] ConversationTest event forwarded: '{cleanEventName}'");
            }
            else
            {
                Debug.LogWarning($"[DialogueManager] ConversationTest event '{cleanEventName}' fired but ConversationTestManager.Instance is null. Make sure a ConversationTestManager GameObject is in the scene.");
            }
        }

        // 4. Forward to ALL NPCs — the one with the event mapping will handle it
        BroadcastDialogueEvent(cleanEventName);
        } // End foreach event loop
    }

    /// <summary>
    /// Sends a dialogue event to EVERY InteractableNPC in the scene.
    /// Each NPC only reacts if it has a matching entry in its Dialogue Events list.
    /// This solves the problem where _currentNPC changes mid-conversation but the 
    /// event mapping lives on a different NPC (e.g. flowerPecker vs Mishang_Rrrigged).
    /// </summary>
    private void BroadcastDialogueEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;
        var allNPCs = FindObjectsByType<InteractableNPC>(FindObjectsSortMode.None);
        foreach (var npc in allNPCs)
            npc.HandleDialogueEvent(eventName);
    }

    private void SafeSetTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger && param.name == triggerName)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }

    /// <summary>
    /// Allows external scripts (e.g., ConversationTestManager) to redirect the
    /// active dialogue to a specific node. Called during a trigger event, it causes
    /// the _activeNode redirect check to skip displaying the current (empty) node.
    /// </summary>
    public void JumpToNode(DialogueNode node)
    {
        if (node == null) return;
        Debug.Log($"<color=green>[DialogueManager] JumpToNode → '{node.name}'</color>");
        ProcessNode(node);
    }

    /// <summary>
    /// Programmatically advances the dialogue by choosing the first choice
    /// (or ending dialogue if there are no choices). Used by STT adapter.
    /// </summary>
    public void AdvanceDialogue()
    {
        if (_activeNode != null)
        {
            if (_activeNode.choices != null && _activeNode.choices.Count > 0)
            {
                // Select the first non-wrong choice if possible, or just the first choice
                DialogueChoice choice = _activeNode.choices.Find(c => !c.isWrong);
                if (choice == null) choice = _activeNode.choices[0];
                OnChoiceSelected(choice);
            }
            else
            {
                OnChoiceSelected(null);
            }
        }
    }
}

