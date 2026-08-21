using UnityEngine;
using UnityEngine.Events;

public class IrahCoolerLogic : MonoBehaviour
{
    public Transform cooler;
    public Transform dropPosition;
    public InteractableNPC interactableNPC;
    public DialogueNode phase2Dialogue;
    public Animator irahAnimator;

    [Header("Cooler Tweaks")]
    public Vector3 coolerLocalPos = new Vector3(-0.1f, 0.2f, 0f);
    public Vector3 coolerLocalRot = new Vector3(0, -90, 90);
    
    [Header("Animation Fixes")]
    public float carryingYOffset = 0.5f;

    private bool hasDropped = false;
    private bool isCarrying = false;

    void Start()
    {
        if (interactableNPC == null) interactableNPC = GetComponent<InteractableNPC>();
        if (irahAnimator == null) irahAnimator = GetComponent<Animator>();

        // Disable root motion to prevent her from sinking into the floor!
        if (irahAnimator != null)
        {
            irahAnimator.applyRootMotion = false;
        }

        if (ObjectiveManager.Instance != null)
        {
            CheckObjective(ObjectiveManager.Instance.CurrentObjective);
            ObjectiveManager.OnObjectiveChanged += CheckObjective;
        }
    }

    private float idleHipHeight = -1f; // -1 = not yet captured

    void LateUpdate()
    {
        if (irahAnimator != null)
        {
            Transform hips = irahAnimator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                if (!isCarrying)
                {
                    // Record her perfect grounded hip height while she's in Idle
                    idleHipHeight = hips.localPosition.y;
                }
                else if (idleHipHeight >= 0f)
                {
                    // Only clamp once we have a valid captured height.
                    // Force her hips to stay at the exact same height during Carrying
                    Vector3 pos = hips.localPosition;
                    pos.y = idleHipHeight;
                    hips.localPosition = pos;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.OnObjectiveChanged -= CheckObjective;
        }
    }

    private void CheckObjective(string obj)
    {
        if (hasDropped) return;

        if (!string.IsNullOrEmpty(obj) && obj.Contains("Find Irah to learn Requests"))
        {
            if (!isCarrying)
            {
                StartCoroutine(StartCarryingAfterIdleCaptured());
            }
        }
    }

    private System.Collections.IEnumerator StartCarryingAfterIdleCaptured()
    {
        // Wait until we have captured a valid idle hip height from at least one Idle LateUpdate frame.
        // idleHipHeight starts at -1 (sentinel). Once LateUpdate runs with isCarrying=false it becomes >= 0.
        yield return null; // wait one frame so animator runs LateUpdate in Idle state
        yield return new WaitForEndOfFrame();

        // Capture now if we haven't yet (e.g. if animator just started)
        if (idleHipHeight < 0f && irahAnimator != null)
        {
            Transform hips = irahAnimator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null) idleHipHeight = hips.localPosition.y;
        }

        StartCarrying();
    }

    private void StartCarrying()
    {
        isCarrying = true;

        Transform hand = FindDeepChild(transform, "mixamorig:LeftHand");
        if (hand == null) hand = FindDeepChild(transform, "mixamorig:RightHand");

        if (hand != null && cooler != null)
        {
            cooler.SetParent(hand);
            cooler.localPosition = coolerLocalPos;
            cooler.localEulerAngles = coolerLocalRot;
        }

        if (irahAnimator != null)
        {
            irahAnimator.SetTrigger("StartCarrying");
        }
    }

    public void OnDialogueEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (eventName == "IrahPhase1Complete")
        {
            if (interactableNPC != null)
            {
                interactableNPC.promptText = "Help Irah";
                
                // Clear quest dialogues so it doesn't forcefully override the phase 2 dialogue
                if (interactableNPC.questDialogues != null)
                {
                    interactableNPC.questDialogues.Clear();
                }
                
                interactableNPC.defaultDialogue = phase2Dialogue;
            }
        }
        else if (eventName == "DropCoolerEvent")
        {
            DropCooler();
        }
        else if (eventName == "SaluteEvent")
        {
            if (irahAnimator != null) irahAnimator.SetTrigger("Salute");
        }
    }

    private void DropCooler()
    {
        if (hasDropped) return;
        hasDropped = true;
        isCarrying = false; // Turn off the LateUpdate offset!

        if (cooler != null && dropPosition != null)
        {
            cooler.SetParent(dropPosition.parent);
            cooler.position = dropPosition.position;
            cooler.rotation = dropPosition.rotation;
        }

        if (irahAnimator != null)
        {
            irahAnimator.SetTrigger("DropCooler");
        }

        if (interactableNPC != null)
        {
            interactableNPC.promptText = "Talk";
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
