using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class InteractableNPC : InteractableBase
{
    [Header("Dialogue Settings")]
    [Tooltip("The casual dialogue used when the NPC has nothing specific to do with the current quest.")]
    public DialogueNode defaultDialogue;

    [Header("Quest Integration")]
    [Tooltip("Check this if this NPC is one of the targets for a scavenger hunt/greeting quest.")]
    public bool isOrganizer = false;
    private bool _hasBeenGreeted = false;

    [System.Serializable]
    public class QuestDialogue
    {
        [Tooltip("The objective that must be active for this dialogue to trigger.")]
        public string requiredObjective;
        [Tooltip("The dialogue to play during this specific quest stage.")]
        public DialogueNode dialogueNode;
    }

    [Tooltip("List of special dialogues that only trigger during specific quest objectives.")]
    public List<QuestDialogue> questDialogues = new List<QuestDialogue>();

    [Header("Visibility Settings")]
    [Tooltip("If true, the NPC is completely hidden (mesh and colliders disabled) until their quest objective is active.")]
    public bool hideUntilObjective = false;

    public Animator npcAnimator;

    [Header("One-Time Interaction")]
    [Tooltip("If true, the interaction button will NEVER appear again after the first conversation ends.")]
    public bool disableAfterInteraction = false;
    [HideInInspector] public bool isWrongAnswerPlaying = false;
    
    [Header("Minigame Settings")]
    [Tooltip("The category name (e.g. Greetings) to pass to the minigame when StartMinigame is called.")]
    public string minigameCategory;
    [Tooltip("The language ID (1=Ilokano, 2=Cebuano, etc.) to pass to the minigame.")]
    public int minigameLanguageId = 1;

    [Header("Events")]
    public UnityEvent OnDialogueEnd;
    public UnityEvent OnWrongAnswer;

    public override void Interact()
    {
        Debug.Log($"[InteractableNPC] {gameObject.name} Interact() called. interactionEnabled={interactionEnabled}");
        if (!interactionEnabled || npcAnimator == null) 
        {
            if (npcAnimator == null) Debug.LogWarning($"[InteractableNPC] {gameObject.name} blocked from interaction because it has no Animator (T-pose/static constraint).");
            return;
        }

        DialogueNode nodeToPlay = GetCurrentDialogueNode();
        Debug.Log($"[InteractableNPC] nodeToPlay is {(nodeToPlay == null ? "NULL!" : nodeToPlay.name)}");
        ForceStartDialogue(nodeToPlay);

        OnInteract?.Invoke();
    }

    /// <summary>
    /// Manually triggers a specific dialogue node on this NPC.
    /// Great for location triggers or cutscenes!
    /// </summary>
    public void ForceStartDialogue(DialogueNode node)
    {
        if (node != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(node, npcAnimator, this);
        }
    }

    private DialogueNode GetCurrentDialogueNode()
    {
        if (ObjectiveManager.Instance != null && questDialogues != null)
        {
            string currentObj = ObjectiveManager.Instance.CurrentObjective;
            foreach (var qd in questDialogues)
            {
                if (currentObj != null && currentObj.StartsWith(qd.requiredObjective, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[{gameObject.name}] Match found! Using Quest Dialogue: {qd.dialogueNode.name}");
                    return qd.dialogueNode;
                }
            }
        }

        // 2. Fallback to default dialogue
        return defaultDialogue;
    }

    public void EnableInteraction() 
    {
        if (npcAnimator != null) interactionEnabled = true;
    }
    public void DisableInteraction() => interactionEnabled = false;

    protected override void Start()
    {
        base.Start();
        if (npcAnimator == null)
        {
            interactionEnabled = false; // Disable if no animator on start
        }

        if (hideUntilObjective)
        {
            SetVisibility(false);
            interactionEnabled = false;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ObjectiveManager.OnObjectiveChanged += HandleObjective;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ObjectiveManager.OnObjectiveChanged -= HandleObjective;
    }

    private void HandleObjective(string obj)
    {
        if (string.IsNullOrEmpty(obj)) return;
        
        bool isTarget = false;

        // First check if any questDialogue matches this objective directly
        if (questDialogues != null)
        {
            foreach (var qd in questDialogues)
            {
                if (!string.IsNullOrEmpty(qd.requiredObjective) && obj.StartsWith(qd.requiredObjective, System.StringComparison.OrdinalIgnoreCase))
                {
                    isTarget = true;
                    break;
                }
            }
        }

        // Check implicit targeting if not already found in questDialogues
        if (!isTarget && (obj.StartsWith("Talk to ", System.StringComparison.OrdinalIgnoreCase) || 
            obj.StartsWith("Return to ", System.StringComparison.OrdinalIgnoreCase) ||
            obj.StartsWith("Meet ", System.StringComparison.OrdinalIgnoreCase)))
        {
            string targetName = obj;
            if (obj.StartsWith("Talk to ", System.StringComparison.OrdinalIgnoreCase)) targetName = obj.Substring("Talk to ".Length).Trim();
            else if (obj.StartsWith("Return to ", System.StringComparison.OrdinalIgnoreCase)) targetName = obj.Substring("Return to ".Length).Trim();
            else if (obj.StartsWith("Meet ", System.StringComparison.OrdinalIgnoreCase)) targetName = obj.Substring("Meet ".Length).Trim();
            
            string cleanTarget = targetName.Replace(" ", "").Replace("_", "").ToLower();
            string cleanName = gameObject.name.Replace(" ", "").Replace("_", "").ToLower()
                                              .Replace("rigged", "")
                                              .Replace("vendor", "")
                                              .Replace("npc", "");

            bool isMatch = cleanTarget.StartsWith(cleanName) || cleanName.StartsWith(cleanTarget);
            
            // Check Tiptip <-> flowerpecker alias
            if (!isMatch)
            {
                bool isTiptipTarget = cleanTarget.Contains("tiptip") || cleanTarget.Contains("flowerpecker");
                bool isTiptipName = cleanName.Contains("tiptip") || cleanName.Contains("flowerpecker");
                if (isTiptipTarget && isTiptipName) isMatch = true;
            }

            isTarget = isMatch;
        }
        
        if (isTarget)
        {
            if (npcAnimator != null) interactionEnabled = true;
            if (hideUntilObjective) SetVisibility(true);
            if (InteractionManager.Instance != null) InteractionManager.Instance.ForceCheckProximity();
        }
        else
        {
            // Only disable interaction if we don't have defaultDialogue enabled
            if (defaultDialogue == null)
            {
                interactionEnabled = false;
            }
            
            if (hideUntilObjective)
            {
                SetVisibility(false);
                interactionEnabled = false;
            }
        }
    }

    /// <summary>
    /// Helper method to teleport the NPC. Easily callable from UnityEvents.
    /// </summary>
    public void TeleportTo(Transform targetTransform)
    {
        if (targetTransform != null)
        {
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
        }
    }

    /// <summary>
    /// Forces the player's third-person camera to immediately snap and look at this NPC.
    /// Easily callable from UnityEvents.
    /// </summary>
    public void ForcePlayerCameraToLookAtMe()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null)
            {
                tpc.ForceCameraLookAt(transform.position);
            }
        }
    }

    /// <summary>
    /// Safely hides the player's 3D model without breaking their physics or controller.
    /// Useful for dialogue close-ups! Easily callable from UnityEvents.
    /// </summary>
    public void HidePlayer()
    {
        SetPlayerVisibility(false);
    }

    /// <summary>
    /// Shows the player's 3D model again. Easily callable from UnityEvents.
    /// </summary>
    public void ShowPlayer()
    {
        SetPlayerVisibility(true);
    }

    private void SetPlayerVisibility(bool isVisible)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var renderers = player.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = isVisible;
            }
        }
    }

    private void SetVisibility(bool isVisible)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.enabled = isVisible;
        }

        var colliders = GetComponentsInChildren<Collider>();
        foreach(var c in colliders)
        {
            c.enabled = isVisible;
        }
    }

    private Coroutine _cameraCoroutine;
    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;

    /// <summary>
    /// Foolproof method to smoothly transition the main camera to a specific close-up spot.
    /// It automatically disables Cinemachine temporarily so you don't have to mess with priorities!
    /// </summary>
    public void EnterCloseUp(Transform closeUpSpot)
    {
        if (closeUpSpot == null) return;
        
        HidePlayer(); // Automatically hide the player
        
        GameObject mainCam = GameObject.FindWithTag("MainCamera");
        if (mainCam != null)
        {
            // Support both Cinemachine 2 and 3 namespaces
            Behaviour brain = mainCam.GetComponent("CinemachineBrain") as Behaviour;
            if (brain != null) brain.enabled = false;

            if (_cameraCoroutine != null) StopCoroutine(_cameraCoroutine);
            _cameraCoroutine = StartCoroutine(LerpCamera(mainCam.transform, closeUpSpot.position, closeUpSpot.rotation, 1f));
        }
    }

    /// <summary>
    /// Smoothly transitions the camera back to normal gameplay.
    /// </summary>
    public void ExitCloseUp()
    {
        ShowPlayer(); // Bring player back
        
        GameObject mainCam = GameObject.FindWithTag("MainCamera");
        if (mainCam != null)
        {
            Behaviour brain = mainCam.GetComponent("CinemachineBrain") as Behaviour;
            if (brain != null) 
            {
                brain.enabled = true; // Cinemachine will automatically smooth-blend back!
            }
            else if (_originalCamPos != Vector3.zero) 
            {
                 mainCam.transform.position = _originalCamPos;
                 mainCam.transform.rotation = _originalCamRot;
            }
        }
    }

    private System.Collections.IEnumerator LerpCamera(Transform cam, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        _originalCamPos = cam.position;
        _originalCamRot = cam.rotation;

        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Smooth ease in/out
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            cam.position = Vector3.Lerp(startPos, targetPos, t);
            cam.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
        cam.position = targetPos;
        cam.rotation = targetRot;
    }

    /// <summary>
    /// Helper method to update the player's objective. 
    /// Easily callable from UnityEvents (like OnDialogueEnd).
    /// </summary>
    public void SetNewObjective(string objective)
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.SetObjective(objective);
        }
    }

    [Header("Custom Scene Events")]
    [Tooltip("Map event strings from Dialogue Nodes to Unity Events in the scene.")]
    public List<DialogueEventMapping> dialogueEvents = new List<DialogueEventMapping>();

    public void TriggerWrongAnswerAnimation()
    {
        if (npcAnimator == null)
        {
            npcAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }
        
        Debug.Log($"<color=orange>[InteractableNPC] TriggerWrongAnswerAnimation called on '{gameObject.name}' (Animator: {(npcAnimator != null ? npcAnimator.name : "NULL")})</color>");
        StopCoroutine("WrongAnswerRoutine");
        StartCoroutine(WrongAnswerRoutine());
    }

    private IEnumerator WrongAnswerRoutine()
    {
        isWrongAnswerPlaying = true;
        
        // 1. Invoke Inspector UnityEvent (OnWrongAnswer)
        Debug.Log($"[InteractableNPC] Invoking OnWrongAnswer UnityEvent for '{gameObject.name}'...");
        OnWrongAnswer?.Invoke();

        // 2. Direct Fallback: Try playing headShake state/trigger directly on Animator if defined
        if (npcAnimator != null)
        {
            bool played = false;
            foreach (var param in npcAnimator.parameters)
            {
                if (param.name.Equals("headShake", System.StringComparison.OrdinalIgnoreCase) ||
                    param.name.Equals("wrongAnswer", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        npcAnimator.SetTrigger(param.name);
                        played = true;
                    }
                    else if (param.type == AnimatorControllerParameterType.Bool)
                    {
                        npcAnimator.SetBool(param.name, true);
                        played = true;
                    }
                }
            }

            if (!played)
            {
                try { npcAnimator.Play("headShake", 0, 0f); } catch {}
            }
        }

        // Wait a moment for the animator to transition
        yield return new WaitForSeconds(0.3f);

        if (npcAnimator != null)
        {
            float elapsed = 0f;
            while (elapsed < 3f) // Safety timeout
            {
                var state = npcAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("Idle") || state.IsName("apoLakay_Idle") || state.normalizedTime >= 0.95f) 
                    break;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        isWrongAnswerPlaying = false;
        Debug.Log($"[InteractableNPC] WrongAnswerRoutine finished for '{gameObject.name}'.");
    }

    public void HandleDialogueEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;
        
        string cleanEventName = eventName.Trim();
        
        // Forward the event to custom scripts (e.g. IrahCoolerLogic)
        gameObject.SendMessage("OnDialogueEvent", cleanEventName, SendMessageOptions.DontRequireReceiver);

        // NOTE: Logging is intentionally placed inside the match block below to avoid
        // console spam — this method is called on ALL NPCs via the broadcast pattern.

        // Automatic system handler for TeachingOverlayPanel events
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
        else if (cleanEventName.StartsWith("SetObjective:", System.StringComparison.OrdinalIgnoreCase))
        {
            string newObjText = cleanEventName.Substring("SetObjective:".Length).Trim();
            if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(newObjText))
            {
                ObjectiveManager.Instance.SetObjective(newObjText);
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
        else if (cleanEventName.StartsWith("ShowInSceneMic", System.StringComparison.OrdinalIgnoreCase))
        {
            string targetPhrase = cleanEventName.Contains(":") ? cleanEventName.Split(':')[1].Trim() : "";
            if (InSceneLessonController.Instance != null)
            {
                InSceneLessonController.Instance.ShowInSceneMic(targetPhrase);
            }
        }
        else if (cleanEventName.Equals("EndInSceneLesson", System.StringComparison.OrdinalIgnoreCase))
        {
            if (InSceneLessonController.Instance != null)
            {
                InSceneLessonController.Instance.EndInSceneLesson();
            }
        }

        foreach (var mapping in dialogueEvents)
        {
            if (mapping.eventName != null && mapping.eventName.Trim() == cleanEventName)
            {
                Debug.Log($"[InteractableNPC] '{gameObject.name}' matched event '{cleanEventName}' — firing UnityEvent.");
                mapping.onEventTriggered?.Invoke();
            }
        }
    }

    /// <summary>
    /// Smoothly rotates the NPC to face the player.
    /// Can be called from the OnInteract event.
    /// </summary>
    public void SmoothLookAtPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            StartCoroutine(LookAtRoutine(player.transform));
        }
    }

    /// <summary>
    /// Helper to trigger the lesson panel with a specific category.
    /// Redirects to the LessonIntroPanel first if available.
    /// </summary>
    public void StartLessonWithCategory(string category)
    {
        if (LessonIntroPanel.Instance != null)
        {
            LessonIntroPanel.Instance.ShowForCategory(category);
        }
        else if (LessonManager.Instance != null)
        {
            LessonManager.Instance.ShowLessonWithCategory(category);
        }
    }

    /// <summary>
    /// Helper to trigger a minigame. Drag a prefab into the UnityEvent slot!
    /// It will automatically use the 'minigameCategory' field set above.
    /// </summary>
    public void StartMinigame(GameObject minigamePrefab)
    {
        StartMinigameWithCategory(minigamePrefab, minigameCategory, minigameLanguageId);
    }

    /// <summary>
    /// Helper to trigger a minigame with a specific category tag.
    /// Useful for dynamic minigames that load content based on the lesson.
    /// </summary>
    public void StartMinigameWithCategory(GameObject minigamePrefab, string category, int languageId)
    {
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.StartMinigameWithCategory(minigamePrefab, category, languageId);
        }
    }

    /// <summary>
    /// Call this via Dialogue Events to progress the 'Identify Organizers' quest.
    /// Only works if 'isOrganizer' is checked and they haven't been greeted yet.
    /// </summary>
    public void GreetOrganizer()
    {
        Debug.Log($"[{gameObject.name}] GreetOrganizer called! isOrganizer: {isOrganizer}, hasBeenGreeted: {_hasBeenGreeted}");
        if (isOrganizer && !_hasBeenGreeted)
        {
            _hasBeenGreeted = true;
            Debug.Log($"[{gameObject.name}] Success! Adding progress to ObjectiveManager.");
            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.AddProgress();
            }
        }
    }

    private IEnumerator LookAtRoutine(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0; // Keep the NPC upright
        
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            
            float elapsed = 0f;
            float duration = 0.6f; // Time it takes to turn

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            transform.rotation = targetRot;
        }
    }
}

[System.Serializable]
public class DialogueEventMapping
{
    [Tooltip("The string defined in the Dialogue Node's 'Trigger Event Name' field.")]
    public string eventName;
    public UnityEvent onEventTriggered;
}
