using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls in-scene NPC close-up lessons.
/// Pans the Main Camera smoothly from the player's viewpoint to the NPC close-up position.
/// Uses the SallyCloseUp (or any named) camera object purely as a transform waypoint.
/// Fully modular — does not break existing dialogue or teaching systems.
/// </summary>
public class InSceneLessonController : MonoBehaviour
{
    public static InSceneLessonController Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────────────────────────

    [Header("UI Controls (In-Scene Floating Mic)")]
    [Tooltip("Canvas or Panel containing the floating in-scene mic button and prompt text.")]
    public GameObject inSceneMicPanel;
    public Button micButton;
    public Image micButtonImage;
    public Sprite micInactiveSprite;
    public Sprite micActiveSprite;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI tapToStopText;

    [Header("Camera & NPC Settings")]
    [Tooltip("Default close-up camera object used as a target waypoint if none is specified by name.")]
    public GameObject defaultCloseUpCamera;
    public float npcTurnSpeed = 5.0f;

    [Header("Pan Transition Settings")]
    [Tooltip("Seconds for the camera to pan from player to NPC close-up position and back.")]
    public float panDuration = 0.8f;

    [Header("STT Language Settings")]
    [Tooltip("The spoken language region for this scene. Ilokano = ilo, Cebuano = ceb. Set in Inspector per scene.")]
    public RegionMode sttRegion = RegionMode.Ilokano;

    // ─────────────────────────────────────────────────────────────────
    // Private State
    // ─────────────────────────────────────────────────────────────────

    private GameObject _currentCloseUpCam;
    private InteractableNPC _currentNPC;
    private Animator _currentAnimator;
    private string _targetPhrase = "";
    private bool _isRecording = false;
    private bool _isLessonActive = false;

    // Suppresses the next ClearPromptAndFeedbackUI call so "Great job!" stays on screen
    private bool _suppressNextClear = false;

    // Saved main camera state so we can pan back when lesson ends
    private Vector3 _savedMainCamPos;
    private Quaternion _savedMainCamRot;
    private Transform _mainCamTransform;
    private Coroutine _panCoroutine;

    // Cinemachine virtual camera on PlayerFollowCamera — disabled during lesson so we can pan freely
    private Behaviour _cinemachineBrain;
    private Behaviour _playerVirtualCam;

    // Player renderers — hidden during NPC close-up so the player doesn't block the view
    private SkinnedMeshRenderer[] _playerRenderers;

    public bool IsLessonActive => _isLessonActive;

    // ─────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        HideMicUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Auto-find TapToStopText if not assigned
        if (tapToStopText == null)
        {
            GameObject tObj = GameObject.Find("TapToStopText");
            if (tObj != null) tapToStopText = tObj.GetComponent<TextMeshProUGUI>();
        }

        if (micButton != null)
        {
            micButton.onClick.RemoveAllListeners();
            micButton.onClick.AddListener(OnMicButtonTapped);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts an in-scene lesson: pans Main Camera to close-up position, rotates NPC to face camera.
    /// Called via dialogue choiceEvent "StartInSceneLesson:CameraName".
    /// </summary>
    public void StartInSceneLesson(string cameraName = "")
    {
        _isLessonActive = true;
        _currentNPC = DialogueManager.Instance != null ? DialogueManager.Instance.GetActiveNPC() : null;
        if (_currentNPC != null)
            _currentAnimator = _currentNPC.npcAnimator;

        // If explicitly requested "None", skip all camera panning
        if (cameraName.Equals("None", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[InSceneLessonController] Starting lesson with NO camera pan (cameraName = None).");
            return;
        }

        // Find the close-up camera object — used as a transform target only
        _currentCloseUpCam = FindCameraInScene(cameraName);
        if (_currentCloseUpCam == null) _currentCloseUpCam = defaultCloseUpCamera;

        if (_currentCloseUpCam == null)
        {
            Debug.LogWarning($"[InSceneLessonController] Could not find close-up camera '{cameraName}'.");
            return;
        }

        // Ensure it's active so its transform is readable; disable Camera component so it doesn't render
        _currentCloseUpCam.SetActive(true);
        Camera closeUpCamComp = _currentCloseUpCam.GetComponent<Camera>();
        if (closeUpCamComp != null) closeUpCamComp.enabled = false;

        // ── Disable Cinemachine so we can move MainCamera freely ──
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            _mainCamTransform = mainCam.transform;
            _savedMainCamPos = mainCam.transform.position;
            _savedMainCamRot = mainCam.transform.rotation;

            // Disable CinemachineBrain on MainCamera (stops Cinemachine from overriding the transform)
            _cinemachineBrain = GetBehaviourByName(mainCam.gameObject, "CinemachineBrain");
            if (_cinemachineBrain != null)
                _cinemachineBrain.enabled = false;
        }

        // Disable PlayerFollowCamera virtual camera (prevents it from taking over if brain is not disabled)
        GameObject playerFollowCamObj = GameObject.Find("PlayerFollowCamera");
        if (playerFollowCamObj != null)
        {
            _playerVirtualCam = GetBehaviourByName(playerFollowCamObj, "CinemachineVirtualCamera");
            if (_playerVirtualCam != null)
                _playerVirtualCam.enabled = false;
        }

        // ── Hide player character so they don't block the NPC close-up view ──
        SetPlayerVisible(false);

        // ── Pan MainCamera to close-up position ──
        if (_panCoroutine != null) StopCoroutine(_panCoroutine);
        _panCoroutine = StartCoroutine(PanCamera(
            _savedMainCamPos, _savedMainCamRot,
            _currentCloseUpCam.transform.position, _currentCloseUpCam.transform.rotation,
            onComplete: () =>
            {
                if (_currentNPC != null && _currentCloseUpCam != null)
                    StartCoroutine(RotateNPCToCamera(_currentNPC.transform, _currentCloseUpCam.transform));
            }
        ));

        Debug.Log($"[InSceneLessonController] Panning to '{_currentCloseUpCam.name}'. Cinemachine disabled for pan.");
    }

    /// <summary>
    /// Shows the in-scene floating mic button for a specific STT target phrase.
    /// </summary>
    public void ShowInSceneMic(string rawPhrase)
    {
        _targetPhrase = ResolveTemplatePhrase(rawPhrase);
        _isRecording = false;

        EnsureSpeechDependencies();

        if (PhraseEvaluator.Instance != null)
            PhraseEvaluator.Instance.SetRegion(RegionMode.Cebuano);

        // Ensure parent hierarchy (e.g. TeachingOverlayPanel) is active & visible
        GameObject micTarget = micButton != null ? micButton.gameObject : inSceneMicPanel;
        if (micTarget != null)
        {
            Transform p = micTarget.transform.parent;
            while (p != null && p.gameObject != gameObject)
            {
                if (!p.gameObject.activeSelf)
                {
                    p.gameObject.SetActive(true);
                    // Hide background panels — lesson runs in 3D scene
                    var bg = p.Find("BackgroundImage");
                    if (bg != null) bg.gameObject.SetActive(false);
                    var shadow = p.Find("Dimmer");
                    if (shadow != null) shadow.gameObject.SetActive(false);
                }
                CanvasGroup cg = p.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
                p = p.parent;
            }
            micTarget.SetActive(true);
        }

        if (inSceneMicPanel != null)
            inSceneMicPanel.SetActive(true);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            // Use FormatPromptPhrase so {name}/{place} templates show as hints, not resolved values
            string display = string.IsNullOrEmpty(_targetPhrase)
                ? "Tap mic and speak!"
                : $"Say: <b>\"{FormatPromptPhrase(rawPhrase)}\"</b>";
            promptText.text = display;
        }

        if (tapToStopText != null)
            tapToStopText.gameObject.SetActive(false);

        SetMicState(false);
        if (micButton != null)
        {
            micButton.gameObject.SetActive(true);
            micButton.interactable = true;
        }

        Debug.Log($"[InSceneLessonController] ShowInSceneMic target: '{_targetPhrase}'");
    }

    /// <summary>
    /// Called by DialogueManager when advancing to the next node.
    /// Skips clearing once after a successful STT so "Great job!" stays visible on the success node.
    /// </summary>
    public void ClearPromptAndFeedbackUI()
    {
        if (_suppressNextClear)
        {
            _suppressNextClear = false;
            return;
        }

        HideMicUI();
    }

    /// <summary>
    /// Public entry point for debug tools (e.g. STTDebugTool P-key) to trigger the full success flow:
    /// "Great job!" text, agree animation, _suppressNextClear flag, and dialogue advance.
    /// </summary>
    public void SimulateSuccess(string word = "")
    {
        HandleSuccess(string.IsNullOrEmpty(word) ? _targetPhrase : word);
    }

    /// <summary>
    /// Ends the lesson: pans Main Camera back to the player's original position.
    /// </summary>
    public void EndInSceneLesson()
    {
        _isLessonActive = false;
        _suppressNextClear = false;

        HideMicUI();

        // Pan main camera back to saved player position, then restore Cinemachine
        if (_mainCamTransform != null)
        {
            if (_panCoroutine != null) StopCoroutine(_panCoroutine);
            _panCoroutine = StartCoroutine(PanCamera(
                _mainCamTransform.position, _mainCamTransform.rotation,
                _savedMainCamPos, _savedMainCamRot,
                onComplete: () =>
                {
                    // Re-enable Cinemachine so it resumes controlling the player camera
                    if (_playerVirtualCam != null) { _playerVirtualCam.enabled = true; _playerVirtualCam = null; }
                    if (_cinemachineBrain != null) { _cinemachineBrain.enabled = true; _cinemachineBrain = null; }

                    // Restore player visibility now that we're back to the player camera
                    SetPlayerVisible(true);

                    if (_currentCloseUpCam != null)
                    {
                        _currentCloseUpCam.SetActive(false);
                        _currentCloseUpCam = null;
                    }
                }
            ));
        }
        else
        {
            // No pan needed — just restore Cinemachine and player immediately
            if (_playerVirtualCam != null) { _playerVirtualCam.enabled = true; _playerVirtualCam = null; }
            if (_cinemachineBrain != null) { _cinemachineBrain.enabled = true; _cinemachineBrain = null; }
            SetPlayerVisible(true);
            if (_currentCloseUpCam != null) { _currentCloseUpCam.SetActive(false); _currentCloseUpCam = null; }
        }

        Debug.Log("[InSceneLessonController] Lesson ended. Panning back & restoring Cinemachine.");
    }

    // ─────────────────────────────────────────────────────────────────
    // Dynamic Template Variable Resolution
    // ─────────────────────────────────────────────────────────────────

    public string ResolveTemplatePhrase(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string resolved = raw;
        if (resolved.Contains("{name}"))
        {
            string pn = PlayerPrefs.GetString("PlayerName", "Juan");
            resolved = resolved.Replace("{name}", string.IsNullOrEmpty(pn) ? "Juan" : pn);
        }
        if (resolved.Contains("{place}"))
        {
            string pp = PlayerPrefs.GetString("PlayerPlace", "Cebu");
            resolved = resolved.Replace("{place}", string.IsNullOrEmpty(pp) ? "Cebu" : pp);
        }
        return resolved;
    }

    /// <summary>
    /// Formats a phrase for display in the mic prompt.
    /// Keeps {name} and {place} as readable hints instead of resolved player values,
    /// so the prompt reads e.g. "ako si [your name]" rather than "ako si Juan".
    /// </summary>
    private string FormatPromptPhrase(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return raw
            .Replace("{name}", "<i>[your name]</i>")
            .Replace("{place}", "<i>[your place]</i>");
    }

    // ─────────────────────────────────────────────────────────────────
    // Camera Pan & NPC Rotation
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator PanCamera(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, System.Action onComplete = null)
    {
        if (_mainCamTransform == null)
        {
            Camera mc = Camera.main;
            if (mc != null) _mainCamTransform = mc.transform;
            else { onComplete?.Invoke(); yield break; }
        }

        float elapsed = 0f;
        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / panDuration));
            _mainCamTransform.position = Vector3.Lerp(fromPos, toPos, t);
            _mainCamTransform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }

        _mainCamTransform.position = toPos;
        _mainCamTransform.rotation = toRot;
        _panCoroutine = null;
        onComplete?.Invoke();
    }

    private GameObject FindCameraInScene(string cameraName)
    {
        if (string.IsNullOrEmpty(cameraName)) return null;

        // 1. Check inside CloseUpCameras container (handles inactive objects)
        GameObject container = GameObject.Find("CloseUpCameras");
        if (container != null)
        {
            Transform found = container.transform.Find(cameraName);
            if (found != null) return found.gameObject;
        }

        // 2. Fallback: search all scene objects including inactive
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.hideFlags == HideFlags.None && t.gameObject.scene.isLoaded &&
                t.name.Equals(cameraName, System.StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }

        return null;
    }

    private IEnumerator RotateNPCToCamera(Transform npcTransform, Transform cameraTransform)
    {
        if (npcTransform == null || cameraTransform == null) yield break;

        Vector3 dir = cameraTransform.position - npcTransform.position;
        dir.y = 0;
        if (dir == Vector3.zero) yield break;

        Quaternion target = Quaternion.LookRotation(dir);
        Quaternion start = npcTransform.rotation;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * npcTurnSpeed;
            npcTransform.rotation = Quaternion.Slerp(start, target, elapsed);
            yield return null;
        }
        npcTransform.rotation = target;
    }

    // ─────────────────────────────────────────────────────────────────
    // STT Recording & Speech Evaluation
    // ─────────────────────────────────────────────────────────────────

    private void OnMicButtonTapped()
    {
        if (!_isRecording) StartRecording();
        else StopRecording();
    }

    private void StartRecording()
    {
        _isRecording = true;
        SetMicState(true);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "Listening... Speak clearly!";
        }

        if (tapToStopText != null)
        {
            tapToStopText.text = "Tap to stop";
            tapToStopText.gameObject.SetActive(true);
        }

        if (SpeechRecorder.Instance != null)
            SpeechRecorder.Instance.StartRecording();

        Debug.Log($"[InSceneLessonController] Recording started. Target: '{_targetPhrase}'");
    }

    private void StopRecording()
    {
        _isRecording = false;
        SetMicState(false);

        if (micButton != null) micButton.interactable = false;

        if (tapToStopText != null)
            tapToStopText.gameObject.SetActive(false);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "Processing speech...";
        }

        string filePath = SpeechRecorder.Instance != null ? SpeechRecorder.Instance.StopRecording() : "";
        if (!string.IsNullOrEmpty(filePath))
        {
            string langCode = (PhraseEvaluator.Instance != null && PhraseEvaluator.Instance.CurrentRegion == RegionMode.Cebuano) ? "ceb" : "tl";
            GroqWhisperManager.Instance.Transcribe(filePath, OnTranscriptionSuccess, OnTranscriptionError, "", langCode);
        }
        else
        {
            OnTranscriptionError("Audio recording failed.");
        }
    }

    private void OnTranscriptionSuccess(string transcribedText)
    {
        Debug.Log($"[InSceneLessonController] Transcribed: \"{transcribedText}\"");

        if (tapToStopText != null) tapToStopText.gameObject.SetActive(false);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "Evaluating...";
        }

        string target = !string.IsNullOrEmpty(_targetPhrase) ? _targetPhrase :
            (DialogueManager.Instance?.PendingSTTChoice?.expectedSTTWord ?? "");
        target = ResolveTemplatePhrase(target);

        if (!string.IsNullOrEmpty(target) && PhraseEvaluator.Instance != null)
        {
            PhraseEvaluator.Instance.EvaluateSpeech(target, transcribedText, (t, score, result) =>
            {
                Debug.Log($"[InSceneLessonController] Score: {score:F0}%. Result: {result}");
                if (score >= 75f) HandleSuccess(transcribedText);
                else HandleFailure();
            });
        }
        else
        {
            HandleSuccess(transcribedText);
        }
    }

    private void HandleSuccess(string text)
    {
        Debug.Log("<color=green>[InSceneLessonController] STT SUCCESS!</color>");

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "<color=#55FF55><b>Great job! Correct!</b></color>";
        }

        if (tapToStopText != null) tapToStopText.gameObject.SetActive(false);
        if (micButton != null) micButton.gameObject.SetActive(false);

        // Play NPC success animation — reads from the STT node's 'Animation Trigger' field in the Inspector.
        // Leave that field empty for no animation. Fill it (e.g. 'clap', 'wave') to play one.
        string successTrigger = "";
        if (DialogueManager.Instance?.PendingSTTChoice != null)
        {
            var sttNode = FindNodeContainingChoice(DialogueManager.Instance.PendingSTTChoice);
            if (sttNode != null && !string.IsNullOrEmpty(sttNode.animationTrigger))
                successTrigger = sttNode.animationTrigger;
        }

        if (!string.IsNullOrEmpty(successTrigger))
        {
            if (_currentAnimator == null && _currentNPC != null)
                _currentAnimator = _currentNPC.npcAnimator;
            if (_currentAnimator != null)
                SafeSetTrigger(_currentAnimator, successTrigger);
        }

        // Suppress the next ClearPromptAndFeedbackUI so "Great job!" stays on the success node
        _suppressNextClear = true;

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.CompleteSTT(true);
    }

    private void HandleFailure()
    {
        Debug.Log("[InSceneLessonController] STT Failed. Retry.");

        if (tapToStopText != null) tapToStopText.gameObject.SetActive(false);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "<color=#FF7777><b>Not quite! Try again.</b></color>";
        }

        if (micButton != null) { micButton.gameObject.SetActive(true); micButton.interactable = true; }
        SetMicState(false);
    }

    private void OnTranscriptionError(string error)
    {
        Debug.LogError($"[InSceneLessonController] Error: {error}");

        if (tapToStopText != null) tapToStopText.gameObject.SetActive(false);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = $"<color=#FF7777>Error: {error}</color>";
        }

        if (micButton != null) { micButton.gameObject.SetActive(true); micButton.interactable = true; }
        SetMicState(false);
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private void HideMicUI()
    {
        if (promptText != null) { promptText.text = ""; promptText.gameObject.SetActive(false); }
        if (tapToStopText != null) tapToStopText.gameObject.SetActive(false);
        if (micButton != null) micButton.gameObject.SetActive(false);
        if (inSceneMicPanel != null) inSceneMicPanel.SetActive(false);
    }

    private void SetMicState(bool active)
    {
        if (micButtonImage != null)
            micButtonImage.sprite = active ? micActiveSprite : micInactiveSprite;
    }

    private void SafeSetTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger &&
                p.name.Equals(triggerName, System.StringComparison.OrdinalIgnoreCase))
            {
                animator.SetTrigger(p.name);
                return;
            }
        }
        Debug.LogWarning($"[InSceneLessonController] Trigger '{triggerName}' not found.");
    }

    private void EnsureSpeechDependencies()
    {
        if (SpeechRecorder.Instance == null && FindFirstObjectByType<SpeechRecorder>() == null)
            new GameObject("SpeechRecorder").AddComponent<SpeechRecorder>();
        if (GroqWhisperManager.Instance == null && FindFirstObjectByType<GroqWhisperManager>() == null)
            new GameObject("GroqWhisperManager").AddComponent<GroqWhisperManager>();
        if (PhraseEvaluator.Instance == null && FindFirstObjectByType<PhraseEvaluator>() == null)
            new GameObject("PhraseEvaluator").AddComponent<PhraseEvaluator>();
    }

    /// <summary>
    /// Hides or shows the player character by toggling all SkinnedMeshRenderers under PlayerArmature.
    /// Called when entering/exiting NPC close-up camera mode.
    /// </summary>
    private void SetPlayerVisible(bool visible)
    {
        // Use cached renderers if already found
        if (_playerRenderers == null || _playerRenderers.Length == 0)
        {
            GameObject playerObj = GameObject.Find("PlayerArmature");
            if (playerObj != null)
                _playerRenderers = playerObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        if (_playerRenderers != null)
        {
            foreach (var r in _playerRenderers)
                if (r != null) r.enabled = visible;
        }
    }

    /// <summary>
    /// Finds the DialogueNode in the current dialogue that contains the given choice.
    /// Used to read the node's animationTrigger for the success animation.
    /// </summary>
    private DialogueNode FindNodeContainingChoice(DialogueChoice target)
    {
        if (target == null || DialogueManager.Instance == null) return null;

        DialogueNode active = DialogueManager.Instance.GetActiveNode();
        if (active != null && active.choices != null)
        {
            foreach (var c in active.choices)
                if (c == target) return active;
        }
        return null;
    }

    /// <summary>
    /// Finds a MonoBehaviour/Behaviour by type name using reflection so we don't need
    /// a hard assembly reference to Cinemachine. Works with both old and new Cinemachine packages.
    /// </summary>
    private Behaviour GetBehaviourByName(GameObject go, string typeName)
    {
        if (go == null) return null;
        foreach (var comp in go.GetComponents<Behaviour>())
        {
            if (comp != null && comp.GetType().Name.Equals(typeName, System.StringComparison.OrdinalIgnoreCase))
                return comp;
        }
        return null;
    }
}
